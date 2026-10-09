using Microsoft.AspNetCore.Mvc;
using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.ExceptionExtensions;
using Rampage.Management;
using Rampage.Management.Permission;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web.Component;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rampage.NaturalistAtlas.ManagementSite.Controllers
{
    /// <summary>
    /// 采集同步管理：任务水位/状态 + config 参数编辑 + 运行日志（手动触发待采集端接入，仅预留按钮位）
    /// Sync management: task watermarks/status + config editing + run logs (manual trigger reserved until the collector lands)
    /// </summary>
    public class SyncController : NaturalistAtlasManagementController
    {
        [Auto]
        protected ISyncCursorService SyncCursorService = default!;

        [Auto]
        protected ISyncLogService SyncLogService = default!;

        [Auto]
        protected ICityService CityService = default!;

        /// <summary>
        /// 采集任务列表：数据源/城市/任务类型/上次状态筛选分页
        /// Task list: source / city / task-type / last-status filter paging
        /// </summary>
        [PermissionAction(Title = "采集任务列表", Type = PermissionActionType.Page, Resource = "SyncManagement")]
        public IActionResult Task_List()
        {
            PagerInfo pagerInfo = BuildPager();
            SyncCursorAdminFilter filter = RenderModel<SyncCursorAdminFilter>();

            // 城市下拉（全量，「城市（国家）」消歧义；见 WebLogicUtility.BuildCityOptions）
            ViewBag.CityList = Internal.WebLogicUtility.BuildCityOptions(CityService);
            (List<SyncCursor> list, Dictionary<int, string> cityNameDict) = SyncCursorService.GetSyncCursorPageList(filter, pagerInfo);
            ViewBag.CityNameDict = cityNameDict;

            ListViewData<SyncCursor, SyncCursorAdminFilter> viewModel = new ListViewData<SyncCursor, SyncCursorAdminFilter>
            {
                DataList = list,
                DataPager = new DataPager(pagerInfo),
                DataFilter = filter
            };
            return VW(viewModel);
        }

        /// <summary>
        /// 任务配置编辑：水位/上次运行回显 + 按 task 类型驱动的参数表单（保存走 api/sync/config/save）
        /// Task config edit: watermark/last-run echo plus a task-type-driven param form (saving posts to api/sync/config/save)
        /// </summary>
        [PermissionAction(Title = "编辑采集任务配置", Type = PermissionActionType.Page, Resource = "SyncManagement")]
        public IActionResult Task_Edit()
        {
            int id = Get<int>("Id");
            SyncTaskConfigEditDto? item = SyncCursorService.GetSyncCursorEdit(id);
            if (item == null)
            {
                throw new SuperHttpException("指定的采集任务不存在");
            }
            return VW(item);
        }

        /// <summary>
        /// 同步日志列表：数据源/城市/任务类型/状态/开始时间范围筛选分页
        /// Log list: source / city / task / status / start-time-range filter paging
        /// </summary>
        [PermissionAction(Title = "同步日志列表", Type = PermissionActionType.Page, Resource = "SyncManagement")]
        public IActionResult Log_List()
        {
            PagerInfo pagerInfo = BuildPager();
            SyncLogAdminFilter filter = RenderModel<SyncLogAdminFilter>();
            filter.StartTimeTo = IncludeEndOfDay(filter.StartTimeTo);

            // 城市下拉（全量，「城市（国家）」消歧义；见 WebLogicUtility.BuildCityOptions）
            ViewBag.CityList = Internal.WebLogicUtility.BuildCityOptions(CityService);
            (List<SyncLog> list, Dictionary<int, string> cityNameDict) = SyncLogService.GetSyncLogPageList(filter, pagerInfo);
            ViewBag.CityNameDict = cityNameDict;

            ListViewData<SyncLog, SyncLogAdminFilter> viewModel = new ListViewData<SyncLog, SyncLogAdminFilter>
            {
                DataList = list,
                DataPager = new DataPager(pagerInfo),
                DataFilter = filter
            };
            return VW(viewModel);
        }

        /// <summary>
        /// 同步日志详情：起止时间、请求/新增/更新/失败计数、运行摘要
        /// Log detail: times, request/insert/update/fail counts and the run message
        /// </summary>
        [PermissionAction(Title = "同步日志详情", Type = PermissionActionType.Query, Resource = "SyncManagement")]
        public IActionResult Log_Detail()
        {
            string logId = Get<string>("LogId")!;
            SyncLog? item = SyncLogService.GetSyncLog(logId);
            if (item == null)
            {
                throw new SuperHttpException("指定的同步日志不存在");
            }
            City? city = CityService.GetCity(item.CityId);
            ViewBag.CityName = city?.Name;
            return VW(item);
        }

    }
}
