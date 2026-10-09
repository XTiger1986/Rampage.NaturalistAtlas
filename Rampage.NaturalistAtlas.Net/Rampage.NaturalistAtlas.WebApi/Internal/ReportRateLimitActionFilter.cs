using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rampage.AppLog;
using Rampage.Model;
using Rampage.RedisLibrary;
using System;
using System.Threading.Tasks;

namespace Rampage.NaturalistAtlas.WebApi.Internal
{
    /// <summary>
    /// 举报接口 IP 限流：匿名写接口的防刷闸（每 IP 每分钟 3 条、每小时 20 条，Redis 固定窗口计数）。
    /// 幂等键只约束自觉携带请求头的客户端，直连刷接口必须靠本过滤器兜底；
    /// Redis 故障时放行（fail-open）：限流器不可用不应阻断真实举报，刷量风险由后端审核兜底
    /// Report-endpoint IP rate limiting: the anti-abuse gate for an anonymous write (3 per IP per
    /// minute, 20 per hour, fixed-window Redis counters). The idempotency key only constrains clients
    /// that volunteer the header, so direct floods need this filter; on Redis failure it fails open —
    /// an unavailable limiter must not block genuine reports, and floods fall back to manual review
    /// </summary>
    public class ReportRateLimitActionFilter : IAsyncActionFilter
    {
        private const int MINUTE_LIMIT = 3;
        private const int HOUR_LIMIT = 20;

        /// <inheritdoc/>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                string ip = Rampage.Web.Utilities.WebTool.GetClientIP(context.HttpContext.Request) ?? "unknown";
                RedisHelper redis = RedisFactory.GetClient()!;
                if (await ExceedsAsync(redis, $"na:rl:report:min:{ip}", MINUTE_LIMIT, TimeSpan.FromMinutes(1))
                    || await ExceedsAsync(redis, $"na:rl:report:hour:{ip}", HOUR_LIMIT, TimeSpan.FromHours(1)))
                {
                    context.Result = new JsonResult(R.Create(ResultCode.OperationTooFrequent, "举报提交过于频繁，请稍后再试"));
                    return;
                }
            }
            catch (Exception ex)
            {
                // fail-open：限流检查自身故障不阻断举报提交
                // Fail open: a limiter failure must not block report submission
                RampageLogger.Error("Report rate-limit check failed (fail-open).", ex);
            }
            await next();
        }

        /// <summary>
        /// 固定窗口计数：首次命中设置窗口过期；窗口内超出上限即限流
        /// Fixed-window counter: the first hit sets the window expiry; exceeding the limit within the window throttles
        /// </summary>
        private static async Task<bool> ExceedsAsync(RedisHelper redis, string key, int limit, TimeSpan window)
        {
            string fullKey = redis.GetFullKey(key);
            long count = await redis.Db.StringIncrementAsync(fullKey);
            if (count == 1)
            {
                await redis.Db.KeyExpireAsync(fullKey, window);
            }
            return count > limit;
        }
    }
}
