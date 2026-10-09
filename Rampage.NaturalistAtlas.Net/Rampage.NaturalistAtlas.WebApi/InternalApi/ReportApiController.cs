using Microsoft.AspNetCore.Mvc;
using Rampage.DependencyInjection;
using Rampage.Model;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.WebApi.Internal;
using System;

namespace Rampage.NaturalistAtlas.WebApi.InternalApi
{
    /// <summary>
    /// 举报接口：匿名提交内容举报（产品文档 §3.2 内容安全）
    /// Report API: anonymous content reports (product doc §3.2 content safety)
    /// </summary>
    /// <remarks>
    /// 前端每次打开举报框生成 Idempotency-Key，基类挂载的 IdempotencyActionFilter 对重复提交直接回放首次结果。
    /// The frontend generates an Idempotency-Key per dialog; the base-class IdempotencyActionFilter replays the first result for duplicates.
    /// </remarks>
    [Route("api/report")]
    public class ReportApiController : NaturalistAtlasApiController
    {
        [Auto]
        protected IAtlasObservationService AtlasObservationService = default!;


        /// <summary>
        /// 提交举报（JSON 正文），成功返回举报 id；前置 IP 限流（每分钟 3 条/每小时 20 条）防匿名刷量
        /// Submit a report (JSON body); returns the report id on success, IP-rate-limited
        /// (3/min, 20/hour) against anonymous flooding
        /// </summary>
        /// <param name="input">举报输入 / Report input</param>
        [HttpPost]
        [ServiceFilter(typeof(ReportRateLimitActionFilter))]
        public R<string> Submit([FromBody] ReportSubmitInput? input)
        {
            try
            {
                return AtlasObservationService.SubmitReport(input);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<string>.Failure(UNEXPECTED_FAILURE_MESSAGE);
            }
        }
    }
}
