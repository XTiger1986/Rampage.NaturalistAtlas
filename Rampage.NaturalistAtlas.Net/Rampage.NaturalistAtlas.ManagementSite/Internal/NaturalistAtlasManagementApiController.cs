using Rampage.AppLog;
using Rampage.Model;
using System;
using Rampage.Management.Web.Api;

namespace Rampage.NaturalistAtlas.ManagementSite.InternalApi
{
    /// <summary>
    /// 管理后台 InternalApi 控制器基类：提供统一异常包裹，收敛各 action 的 try/catch 样板
    /// Admin InternalApi controller base: a uniform exception wrapper that collapses per-action try/catch boilerplate
    /// </summary>
    public class NaturalistAtlasManagementApiController : SuperManagementApiController
    {


        protected override void Initialize()
        {
            base.Initialize();

        }

        protected void LogError(Exception ex)
        {
            RampageLogger.Error(ex);
        }

        /// <summary>
        /// 统一执行包裹：异常记日志并回失败 R（管理端可回传异常文案，供管理员排障）
        /// Uniform wrapper: logs the exception and returns a failure R (the admin site may surface
        /// the message to the operator for troubleshooting)
        /// </summary>
        protected R Execute(Func<R> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R.Failure(ex.Message);
            }
        }

        /// <summary>泛型版统一执行包裹 / Generic uniform wrapper</summary>
        protected R<T> Execute<T>(Func<R<T>> action) where T : class
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<T>.Failure(ex.Message);
            }
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
