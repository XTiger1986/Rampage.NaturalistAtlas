using Rampage.NaturalistAtlas.Model.Dto.External;
using System;

namespace Rampage.NaturalistAtlas.Collection
{
    /// <summary>
    /// GBIF v1 API 客户端：只拼端点与参数，不做字段映射
    /// GBIF v1 API client: endpoint/parameter assembly only, no field mapping
    /// </summary>
    public class GbifApiClient : SourceApiClient
    {
        /// <summary>合规 UA / Compliance UA</summary>
        public const string ClientUserAgent = "NaturalistAtlas-Collector/1.0 (+https://1986z.com)";

        /// <summary>单查询 offset+limit 上限（超出须时间窗切片）/ offset+limit ceiling per query (slicing required beyond it)</summary>
        public const long MaxQueryRecords = 100000;

        /// <summary>单页上限（GBIF 300）/ Page ceiling (GBIF allows 300)</summary>
        public const int MaxPageSize = 300;

        /// <summary>
        /// 构造 GBIF 客户端 / Construct the GBIF client
        /// </summary>
        /// <param name="baseUrl">v1 基址 / v1 base url</param>
        /// <param name="ratePerMinute">节流上限（次/分钟）/ Throttle cap (requests/minute)</param>
        public GbifApiClient(string baseUrl, int ratePerMinute)
            : base("GBIF", baseUrl, ClientUserAgent, ratePerMinute, 60)
        {
        }

        /// <summary>
        /// occurrence 搜索（过滤基线 occurrenceStatus=PRESENT + hasCoordinate + mediaType=StillImage 由编排层注入）
        /// Occurrence search (the orchestrator injects the PRESENT + hasCoordinate + StillImage baseline)
        /// </summary>
        /// <param name="query">查询参数 / Query params</param>
        /// <returns>响应 / The response</returns>
        public Task<GbifSearchResponse?> SearchOccurrencesAsync(Dictionary<string, object?> query)
        {
            return GetJsonAsync<GbifSearchResponse>("/occurrence/search", query);
        }

        /// <summary>
        /// 取单条 occurrence（删除对账，404 视为源站已删返回 null）/ Fetch one occurrence (deletion check; 404 maps to null = deleted on the source)
        /// </summary>
        /// <param name="occurrenceKey">occurrence key / The occurrence key</param>
        /// <returns>响应，源站已删为 null / The response, or null when deleted on the source</returns>
        public Task<GbifOccurrenceDto?> FetchOccurrenceAsync(long occurrenceKey)
        {
            return GetJsonAsync<GbifOccurrenceDto>($"/occurrence/{occurrenceKey}", null, true);
        }

        /// <summary>
        /// 取分类档案（分类刷新：iucnRedListCategory 判受威胁）/ Fetch the species profile (taxon refresh; iucnRedListCategory decides threatened)
        /// </summary>
        /// <param name="taxonKey">分类key / The taxon key</param>
        /// <returns>响应 / The response</returns>
        public Task<GbifSpeciesDto?> FetchSpeciesAsync(int taxonKey)
        {
            return GetJsonAsync<GbifSpeciesDto>($"/species/{taxonKey}");
        }
    }
}
