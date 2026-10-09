using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Rampage.AppLog;
using Rampage.DependencyInjection;
using Rampage.NaturalistAtlas.BLL;
using Rampage.Web.Mvc;

namespace Rampage.NaturalistAtlas.WebApi.Internal
{
    [EnableCors("AllowSpecificOrigin")]
    [ServiceFilter(typeof(IdempotencyActionFilter))]
    public abstract class NaturalistAtlasApiController : SuperApiController
    {
        /// <summary>
        /// 未预期异常的统一对外文案：异常细节（可能含连接串/表名/堆栈）只进服务端日志，
        /// 不回传公网客户端；业务失败请走服务层 R.Failure(具体文案)，不要抛异常到控制器
        /// Unified outward message for unexpected exceptions: details (which may include connection
        /// strings, table names or stack frames) stay in the server log and never reach public
        /// clients; business failures belong in the service layer's R.Failure(text), not in throws
        /// </summary>
        protected const string UnexpectedFailureMessage = "服务繁忙，请稍后再试 / The service is busy, please try again later";

        [Auto]
        protected ISettingService SettingService = default!;

        protected void LogError(Exception ex)
        {
            RampageLogger.Error(ex);
        }

        protected void LogError(string? message, Exception ex)
        {
            RampageLogger.Error(message, ex);
        }

        protected void LogInfo(string message)
        {
            RampageLogger.Info(message);
        }




    }
}
