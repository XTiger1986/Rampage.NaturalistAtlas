using Rampage.Data.Filter;
using Rampage.NaturalistAtlas.Model.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Filter
{
    /// <summary>
    /// 举报工单列表筛选（管理后台） / Report list filter (admin site)
    /// </summary>
    [Filter]
    public class ReportAdminFilter : DataFilter
    {
        /// <summary>
        /// 举报状态 / Report status
        /// </summary>
        public ReportStatus? Status { get; set; }

        /// <summary>
        /// 举报原因 / Report reason
        /// </summary>
        public ReportReason? Reason { get; set; }

        /// <summary>
        /// 观察Id精确匹配（观察详情跳转举报记录用） / Observation id exact match (used from the observation detail page)
        /// </summary>
        [FilterItem(FieldName = "observation_id")]
        public string? ObservationId { get; set; }

        /// <summary>
        /// 提交时间起（含当天；视图提交前把止值补到当天 23:59:59 以含整天）
        /// Submitted-time range start (the view bumps the end value to 23:59:59 to include the whole day)
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.GreaterThanOrEqual, FieldName = "create_time")]
        public DateTime? CreateTimeFrom { get; set; }

        /// <summary>
        /// 提交时间止（含当天）
        /// Submitted-time range end (inclusive)
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.LessThanOrEqual, FieldName = "create_time")]
        public DateTime? CreateTimeTo { get; set; }
    }
}
