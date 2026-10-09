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
using Rampage.Web.Component;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.ManagementSite.Controllers
{
    /// <summary>
    /// 举报工单管理：待处理队列 → 处理（隐藏观察/驳回），含已处理历史
    /// Report management: pending queue → handle (hide the observation / reject), with handled history
    /// </summary>
    public class ReportController : NaturalistAtlasManagementController
    {
        [Auto]
        protected IReportService ReportService = default!;

        [Auto]
        protected IObservationService ObservationService = default!;

        /// <summary>
        /// 举报列表：状态/原因/提交时间筛选分页（默认待处理优先）
        /// Report list: status / reason / submitted-time paging (pending first by default ordering)
        /// </summary>
        [PermissionAction(Title = "举报工单列表", Type = PermissionActionType.Page, Resource = "ReportManagement")]
        public IActionResult Report_List()
        {
            PagerInfo pagerInfo = BuildPager();
            ReportAdminFilter filter = RenderModel<ReportAdminFilter>();
            filter.CreateTimeTo = IncludeEndOfDay(filter.CreateTimeTo);

            List<ReportRowDto> list = ReportService.GetReportPageList(filter, pagerInfo);

            ListViewData<ReportRowDto, ReportAdminFilter> viewModel = new ListViewData<ReportRowDto, ReportAdminFilter>
            {
                DataList = list,
                DataPager = new DataPager(pagerInfo),
                DataFilter = filter
            };
            return VW(viewModel);
        }

        /// <summary>
        /// 举报处理页：回显工单与对应观察摘要，提交走 api/report/handle
        /// Report handle page: echoes the ticket and the observation summary; saving posts to api/report/handle
        /// </summary>
        [PermissionAction(Title = "处理举报工单", Type = PermissionActionType.Page, Resource = "ReportManagement")]
        public IActionResult Report_Handle()
        {
            string reportId = Get<string>("ReportId")!;
            Model.Po.Report? report = ReportService.GetReport(reportId);
            if (report == null)
            {
                throw new SuperHttpException("指定的举报不存在");
            }

            // 观察摘要：复用观察详情（含分类/城市），工单页直接展示
            ObservationDetailDto? observationDetail = string.IsNullOrEmpty(report.ObservationId)
                ? null
                : ObservationService.GetObservationDetail(report.ObservationId!);

            ViewBag.Report = report;
            return VW(observationDetail);
        }

    }
}
