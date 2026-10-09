using Rampage.Data.Filter;
using Rampage.NaturalistAtlas.Model.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Filter
{
    /// <summary>
    /// 采集任务筛选（sync_cursor，管理后台） / Sync cursor filter (admin site)
    /// </summary>
    [Filter]
    public class SyncCursorAdminFilter : DataFilter
    {
        /// <summary>
        /// 数据源 / Data source
        /// </summary>
        public AtlasSource? Source { get; set; }

        /// <summary>
        /// 城市Id / City id
        /// </summary>
        public int? CityId { get; set; }

        /// <summary>
        /// 任务类型 / Task type
        /// </summary>
        [FilterItem(FieldName = "task")]
        public string? Task { get; set; }

        /// <summary>
        /// 上次运行状态 / Last run status
        /// </summary>
        public CursorRunStatus? LastRunStatus { get; set; }
    }

    /// <summary>
    /// 同步日志筛选（sync_log，管理后台） / Sync log filter (admin site)
    /// </summary>
    [Filter]
    public class SyncLogAdminFilter : DataFilter
    {
        /// <summary>
        /// 数据源 / Data source
        /// </summary>
        public AtlasSource? Source { get; set; }

        /// <summary>
        /// 城市Id / City id
        /// </summary>
        public int? CityId { get; set; }

        /// <summary>
        /// 任务类型 / Task type
        /// </summary>
        [FilterItem(FieldName = "task")]
        public string? Task { get; set; }

        /// <summary>
        /// 运行状态 / Run status
        /// </summary>
        public SyncLogStatus? Status { get; set; }

        /// <summary>
        /// 开始时间起（含当天；视图提交前把止值补到当天 23:59:59）
        /// Start-time range start (the view bumps the end value to 23:59:59 to include the whole day)
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.GreaterThanOrEqual, FieldName = "start_time")]
        public DateTime? StartTimeFrom { get; set; }

        /// <summary>
        /// 开始时间止（含当天） / Start-time range end (inclusive)
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.LessThanOrEqual, FieldName = "start_time")]
        public DateTime? StartTimeTo { get; set; }
    }
}
