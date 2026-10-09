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
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.Validation;
using System;

namespace Rampage.NaturalistAtlas.ManagementSite.InternalApi
{
    /// <summary>
    /// 采集同步管理接口：任务 config 保存（手动触发待采集端接入后开放）
    /// Sync admin API: task config save (manual trigger opens after the collector lands)
    /// </summary>
    [Route("api/sync")]
    public class SyncApiController : NaturalistAtlasManagementApiController
    {
        [Auto]
        protected ISyncCursorService SyncCursorService = default!;

        [Auto]
        protected ISyncLogService SyncLogService = default!;


        /// <summary>
        /// 保存采集任务配置：仅覆盖表单声明的字段，其余 config 键原样保留
        /// Save the sync task config: only form-declared fields are overwritten; other config keys are preserved
        /// </summary>
        [PermissionAction(Title = "保存采集任务配置", Resource = "SyncOperation")]
        [RecordOperationFilter]
        [HttpPost("config/save")]
        public R QuerySaveSyncConfig([FromForm][ValidAttribute] SyncTaskConfigInput input)
        {
            return Execute(() => SyncCursorService.SaveSyncCursorConfig(input));
        }
        /// <summary>
        /// CSV 导出：按当前列表筛选导出运行日志（最多 5000 行），UTF-8 BOM 供 Excel 直开
        /// CSV export of run logs under the current list filter (5,000 rows max); UTF-8 BOM for Excel
        /// </summary>
        [PermissionAction(Title = "导出同步日志", Type = PermissionActionType.Query, Resource = "SyncManagement")]
        [HttpGet("log/export")]
        public IActionResult ExportLogList([FromQuery] SyncLogAdminFilter filter)
        {
            try
            {
                filter.StartTimeTo = WebLogicUtility.IncludeEndOfDay(filter.StartTimeTo);
                var (rows, _) = SyncLogService.GetSyncLogPageList(
                    filter, new PagerInfo { PageIndex = 1, PageSize = 5000 });
                string csv = CsvTool.Build(
                    new[] { "日志Id", "数据源", "城市Id", "任务", "任务类型", "状态", "开始时间(UTC)", "结束时间(UTC)", "请求", "新增", "更新", "失败", "摘要" },
                    rows.ConvertAll(r => new object?[]
                    {
                        r.Id, EnumReader.GetText(r.Source), r.CityId, r.Task, SyncTaskCodes.LabelOf(r.Task),
                        EnumReader.GetText(r.Status), r.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        r.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"), r.RequestCount, r.InsertCount,
                        r.UpdateCount, r.FailCount, r.Message
                    }));
                return File(CsvTool.ToDownloadBytes(csv), "text/csv; charset=utf-8",
                    $"sync-logs-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return StatusCode(500);
            }
        }
    }
}
