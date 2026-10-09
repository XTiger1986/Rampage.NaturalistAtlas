using Rampage.RateLimit;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Rampage.NaturalistAtlas.Collection
{
    /// <summary>
    /// 源站不可用异常：单次请求重试额度耗尽仍失败，本采集周期内该源应熔断跳过
    /// Source-unavailable exception: the retry budget of a single call was exhausted;
    /// the source should be circuit-skipped for the rest of the collection cycle
    /// </summary>
    public class SourceUnavailableException : Exception
    {
        /// <summary>
        /// 以源名与原因构造 / Construct with the source name and the cause
        /// </summary>
        public SourceUnavailableException(string sourceName, string reason, Exception? inner = null)
            : base($"数据源 {sourceName} 本周期不可用：{reason} / source {sourceName} unavailable this cycle: {reason}", inner)
        {
        }
    }

    /// <summary>
    /// 采集源 HTTP 客户端基类：组合限流（滑动窗口）+ 退避重试（尊重 Retry-After）+ 熔断与请求计数。
    /// 非注入服务，由采集编排按运行时快照 new（基址/节流随 setting 表可变）
    /// Base HTTP client for collection sources: sliding-window throttling + backoff retries
    /// (honouring Retry-After) + circuit breaking and request counting. Not a DI service — the
    /// orchestrator news it per run (base url / throttle follow the setting table)
    /// </summary>
    public abstract class SourceApiClient
    {
        /// <summary>单次调用总尝试次数（1 次首发 + 3 次重试）/ Total attempts per call (1 + 3 retries)</summary>
        private const int MAX_ATTEMPTS = 4;

        /// <summary>基础退避（秒），按尝试次数线性放大 / Base backoff in seconds, scaled linearly by attempt</summary>
        private const int BACKOFF_BASE_SECONDS = 2;

        private static readonly ConcurrentDictionary<string, HttpClient> HTTP_CLIENTS = new ConcurrentDictionary<string, HttpClient>();

        private static readonly MemoryRateLimiter RATE_LIMITER = new MemoryRateLimiter();

        private readonly string _sourceName;
        private readonly string _baseUrl;
        private readonly string _userAgent;
        private readonly string _rateKey;
        private readonly int _ratePerMinute;
        private readonly int _timeoutSeconds;

        /// <summary>已发出的 HTTP 请求次数（含重试，供 sync_log.request_count）/ HTTP requests sent, retries included (feeds sync_log.request_count)</summary>
        public int RequestCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 构造采集客户端 / Construct the collection client
        /// </summary>
        /// <param name="sourceName">源名（异常与限流键用）/ Source name (for exceptions and the rate key)</param>
        /// <param name="baseUrl">API 基址 / API base url</param>
        /// <param name="userAgent">User-Agent（源站合规标识）/ User-Agent (source-side compliance identity)</param>
        /// <param name="ratePerMinute">节流上限（次/分钟）/ Throttle cap (requests/minute)</param>
        /// <param name="timeoutSeconds">单请求超时（秒，GBIF 复杂几何首查较慢）/ Per-request timeout in seconds (GBIF's first complex-geometry query is slow)</param>
        protected SourceApiClient(string sourceName, string baseUrl, string userAgent, int ratePerMinute, int timeoutSeconds = 30)
        {
            _sourceName = sourceName;
            _baseUrl = baseUrl.TrimEnd('/');
            _userAgent = userAgent;
            _rateKey = sourceName;
            _ratePerMinute = ratePerMinute;
            _timeoutSeconds = timeoutSeconds;
        }

        /// <summary>
        /// GET 请求并反序列化 JSON：限流等待 → 带重试请求（429/5xx/网络异常退避，尊重 Retry-After）
        /// GET and deserialize JSON: wait for throttle quota, then request with retries
        /// (backoff on 429/5xx/network errors, honouring Retry-After)
        /// </summary>
        /// <typeparam name="T">响应类型 / Response type</typeparam>
        /// <param name="path">路径（以 / 开头）/ Path (leading slash)</param>
        /// <param name="query">查询参数（null 值跳过，序列值重复键）/ Query params (nulls skipped; sequences repeat the key)</param>
        /// <param name="allowNotFound">404 视为源站已删除返回 null（删除对账用）/ Treat 404 as source-deleted and return null (deletion check)</param>
        /// <returns>反序列化结果 / The deserialized result</returns>
        /// <exception cref="SourceUnavailableException">重试额度耗尽仍失败 / The retry budget was exhausted</exception>
        protected async Task<T?> GetJsonAsync<T>(string path, IDictionary<string, object?>? query = null, bool allowNotFound = false) where T : class
        {
            string url = _baseUrl + path;
            if (query != null && query.Count > 0)
            {
                url += "?" + BuildQueryString(query);
            }

            Exception? lastError = null;
            for (int attempt = 1; attempt <= MAX_ATTEMPTS; attempt++)
            {
                await WaitForQuotaAsync();
                RequestCount++;

                HttpClient client = GetClient(url);
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");

                HttpResponseMessage? response = null;
                try
                {
                    response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    response?.Dispose();
                    if (attempt < MAX_ATTEMPTS)
                    {
                        await Task.Delay(GetBackoffSeconds(attempt) * 1000);
                        continue;
                    }
                    break;
                }

                using (response)
                {
                    if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                    {
                        lastError = new HttpRequestException($"HTTP {(int)response.StatusCode}");
                        if (attempt < MAX_ATTEMPTS)
                        {
                            await Task.Delay(GetRetryAfterMs(response) ?? GetBackoffSeconds(attempt) * 1000);
                            continue;
                        }
                        break;
                    }
                    if (!response.IsSuccessStatusCode)
                    {
                        if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
                        {
                            return null;
                        }
                        // 4xx（除 429/404豁免）为请求参数问题，重试无意义：直接失败
                        string body = await response.Content.ReadAsStringAsync();
                        throw new SourceUnavailableException(_sourceName, $"HTTP {(int)response.StatusCode} {Truncate(body, 300)}");
                    }
                    string json = await response.Content.ReadAsStringAsync();
                    try
                    {
                        return JsonSerializer.Deserialize<T>(json);
                    }
                    catch (JsonException ex)
                    {
                        // 响应体形态漂移也按源站不稳处理（本周期跳过该源）
                        lastError = ex;
                        if (attempt < MAX_ATTEMPTS)
                        {
                            await Task.Delay(GetBackoffSeconds(attempt) * 1000);
                            continue;
                        }
                        break;
                    }
                }
            }
            throw new SourceUnavailableException(_sourceName, "重试额度耗尽 / retry budget exhausted", lastError);
        }

        /// <summary>
        /// 退避秒数（线性 ×尝试序号，±20% 抖动）：抖动防多任务/多实例同刻集中重试
        /// Backoff seconds (linear in the attempt index, ±20% jitter): jitter keeps concurrent
        /// tasks / instances from retrying in lockstep
        /// </summary>
        private static int GetBackoffSeconds(int attempt)
        {
            double jitter = 0.8 + (JITTER_RANDOM.Value.NextDouble() * 0.4);
            return Math.Max(1, (int)Math.Round(BACKOFF_BASE_SECONDS * attempt * jitter));
        }

        /// <summary>抖动随机源（线程安全）/ Thread-safe jitter random source</summary>
        private static readonly System.Threading.ThreadLocal<System.Random> JITTER_RANDOM = new(() => new Random());

        /// <summary>读 Retry-After 响应头（秒数或 HTTP 日期），读不到返回 null / Read the Retry-After header (seconds or HTTP-date); null when absent</summary>
        private static int? GetRetryAfterMs(HttpResponseMessage response)
        {
            string? value = null;
            if (response.Headers.TryGetValues("Retry-After", out IEnumerable<string>? values))
            {
                foreach (string v in values)
                {
                    value = v;
                    break;
                }
            }
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            if (int.TryParse(value, out int seconds))
            {
                return Math.Min(Math.Max(seconds, 1), 300) * 1000;
            }
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset at))
            {
                int ms = (int)Math.Max((at - DateTimeOffset.UtcNow).TotalMilliseconds, 1000);
                return Math.Min(ms, 300000);
            }
            return null;
        }

        /// <summary>限流等待：窗口内超限则睡到最早释放时刻 / Throttle wait: sleep until a slot frees when the window is full</summary>
        private async Task WaitForQuotaAsync()
        {
            RateLimitResult decision = RATE_LIMITER.Limit(_rateKey, _ratePerMinute, TimeSpan.FromMinutes(1));
            while (!decision.Allowed)
            {
                long waitMs = decision.ResetAtUnixMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (waitMs > 0)
                {
                    await Task.Delay((int)Math.Min(waitMs + 50, 61000));
                }
                decision = RATE_LIMITER.Limit(_rateKey, _ratePerMinute, TimeSpan.FromMinutes(1));
            }
        }

        /// <summary>查询串拼装：null 跳过、序列重复键、不变文化数字格式 / Build the query string: nulls skipped, sequences repeat the key, invariant culture for numbers</summary>
        private static string BuildQueryString(IDictionary<string, object?> query)
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, object?> kv in query)
            {
                if (kv.Value == null)
                {
                    continue;
                }
                string key = Uri.EscapeDataString(kv.Key);
                if (kv.Value is System.Collections.IEnumerable sequence && kv.Value is not string)
                {
                    foreach (object? item in sequence)
                    {
                        if (item != null)
                        {
                            parts.Add($"{key}={Uri.EscapeDataString(Convert.ToString(item, CultureInfo.InvariantCulture) ?? string.Empty)}");
                        }
                    }
                }
                else
                {
                    parts.Add($"{key}={Uri.EscapeDataString(Convert.ToString(kv.Value, CultureInfo.InvariantCulture) ?? string.Empty)}");
                }
            }
            return string.Join("&", parts);
        }

        private static string Truncate(string text, int maxLength)
        {
            return text.Length <= maxLength ? text : text.Substring(0, maxLength);
        }

        /// <summary>按 scheme+host 缓存客户端（本客户端超时，连接池化、自动解压；与框架缓存隔离以免改动全局超时）/ Host-keyed client cache (per-client timeout, pooled connections, auto decompression; kept separate from the framework cache so the global timeout stays untouched)</summary>
        private HttpClient GetClient(string url)
        {
            Uri uri = new Uri(url);
            // 键含超时：同 host 不同超时配置的客户端不得共享首个实例
            // The key includes the timeout: same-host clients with different timeouts must not share
            string key = uri.Scheme + Uri.SchemeDelimiter + uri.Host + (uri.IsDefaultPort ? string.Empty : ":" + uri.Port) + "|t" + Math.Max(_timeoutSeconds, 5);
            return HTTP_CLIENTS.GetOrAdd(key, _ => new HttpClient(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                AutomaticDecompression = DecompressionMethods.All
            })
            {
                Timeout = TimeSpan.FromSeconds(Math.Max(_timeoutSeconds, 5))
            });
        }
    }
}
