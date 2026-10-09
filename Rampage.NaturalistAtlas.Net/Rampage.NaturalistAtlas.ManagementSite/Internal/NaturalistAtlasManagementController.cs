using Microsoft.AspNetCore.Mvc.Filters;
using Rampage.AppLog;
using Rampage.Data;
using Rampage.Data.Query;
using Rampage.Management.Mvc;
using System;

namespace Rampage.NaturalistAtlas.ManagementSite.Controllers
{
    /// <summary>
    /// 管理后台控制器基类：登录/权限过滤与通用工具
    /// Management-site controller base: auth/permission filtering and shared helpers
    /// </summary>
    public abstract class NaturalistAtlasManagementController : SuperManagementController
    {

        protected override bool BeforeActionExecuting(ActionExecutingContext context)
        {
            base.BeforeActionExecuting(context);

            return true;
        }

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

        /// <summary>
        /// 时间戳列的日期止值含当天整日：date 输入只给日期（零点），零点止值会漏掉当天余下时间。
        /// 必须在控制器层补齐——曾用前端把 'T23:59:59' 拼进 date 输入的值，但按 HTML 值净化规则
        /// 非法格式会被浏览器清空，实际提交的止值为空（筛选静默失效）。DATE 列无此问题
        /// （date 与零点比较天然含当天），仅限 create_time/start_time 等时间戳列使用
        /// Make a date-only end value cover the whole day for timestamp columns: a date input yields
        /// midnight, and a midnight upper bound drops the rest of the day. Normalize here in the
        /// controller — the old client-side rewrite appended 'T23:59:59' into the date input, but the
        /// HTML value-sanitization rule makes the browser wipe the invalid format (the end filter was
        /// silently lost). DATE columns are unaffected (a date compares to midnight inclusively);
        /// use only for timestamp columns such as create_time/start_time
        /// </summary>
        /// <param name="end">止值（含当天）/ The end value (to include its day)</param>
        /// <returns>补齐后的止值 / The normalized end value</returns>
        protected static DateTime? IncludeEndOfDay(DateTime? end)
        {
            return Internal.WebLogicUtility.IncludeEndOfDay(end);
        }

        /// <summary>
        /// 构建分页参数：页码 page + 每页条数 ps（默认 20，上限 100）
        /// Build pager params: page index + page size (default 20, capped at 100)
        /// </summary>
        /// <returns>分页参数 / The pager info</returns>
        protected PagerInfo BuildPager()
        {
            PagerInfo pagerInfo = new PagerInfo
            {
                PageIndex = Get<int>("page"),
                PageSize = Get<int>("ps", 20)
            };
            if (pagerInfo.PageSize > 100)
            {
                pagerInfo.PageSize = 100;
            }
            return pagerInfo;
        }

    }
}
