using Microsoft.AspNetCore.Mvc;
using Rampage.DependencyInjection;
using Rampage.Management;
using Rampage.Management.Mvc;
using Rampage.Management.Permission;
using Rampage.Data;
using Rampage.Data.Query;
using Rampage.Enums;
using Rampage.Model;
using Rampage.NaturalistAtlas.ManagementSite.Internal;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.Validation;
using System;

namespace Rampage.NaturalistAtlas.ManagementSite.InternalApi
{
    /// <summary>
    /// 举报工单管理接口：处理（隐藏观察结单 / 驳回结单）
    /// Report admin API: handle (hide-and-close / reject-and-close)
    /// </summary>
    [Route("api/report")]
    public class ReportApiController : NaturalistAtlasManagementApiController
    {
        [Auto]
        protected IReportService ReportService = default!;


        /// <summary>
        /// 处理举报：Hide=true 隐藏对应观察并结单（同事务），否则驳回结单；仅待处理工单可操作
        /// Handle a report: Hide=true hides the observation and closes in one transaction; otherwise reject.
        /// Only pending tickets are operable.
        /// </summary>
        [PermissionAction(Title = "处理举报", Resource = "ReportOperation")]
        [RecordOperationFilter]
        [HttpPost("handle")]
        public R QueryHandleReport([FromForm][ValidAttribute] HandleReportInput input)
        {
            return Execute(() => ReportService.HandleReport(input, CurrentAdmin!.Id));
        }
        /// <summary>
        /// CSV 导出：按当前列表筛选导出工单（最多 5000 行），UTF-8 BOM 供 Excel 直开
        /// CSV export of tickets under the current list filter (5,000 rows max); UTF-8 BOM for Excel
        /// </summary>
        [PermissionAction(Title = "导出举报工单", Type = PermissionActionType.Query, Resource = "ReportManagement")]
        [HttpGet("export")]
        public IActionResult ExportList([FromQuery] ReportAdminFilter filter)
        {
            try
            {
                filter.CreateTimeTo = WebLogicUtility.IncludeEndOfDay(filter.CreateTimeTo);
                var rows = ReportService.GetReportPageList(
                    filter, new PagerInfo { PageIndex = 1, PageSize = 5000 });
                string csv = CsvTool.Build(
                    new[] { "举报Id", "观察Id", "原因", "补充说明", "联系方式", "工单状态", "提交时间(UTC)", "处理时间(UTC)", "处理备注" },
                    rows.ConvertAll(r => new object?[]
                    {
                        r.Id, r.ObservationId, EnumReader.GetText(r.Reason), r.Content, r.Contact,
                        EnumReader.GetText(r.Status), r.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        r.HandleTime?.ToString("yyyy-MM-dd HH:mm:ss"), r.HandleRemark
                    }));
                return File(CsvTool.ToDownloadBytes(csv), "text/csv; charset=utf-8",
                    $"reports-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return StatusCode(500);
            }
        }
    }
}
