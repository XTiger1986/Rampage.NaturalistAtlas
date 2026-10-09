using Rampage.Data.Filter;
using Rampage.NaturalistAtlas.Model.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Filter
{
    /// <summary>
    /// 观察数据列表筛选（管理后台） / Observation list filter (admin site)
    /// </summary>
    [Filter]
    public class ObservationAdminFilter : DataFilter
    {
        /// <summary>
        /// 城市Id / City id
        /// </summary>
        public int? CityId { get; set; }

        /// <summary>
        /// 类群 / Iconic taxon
        /// </summary>
        public IconicTaxon? IconicTaxon { get; set; }

        /// <summary>
        /// 鉴定级别 / Quality grade
        /// </summary>
        public QualityGrade? QualityGrade { get; set; }

        /// <summary>
        /// 状态 / Status
        /// </summary>
        public ObservationStatus? Status { get; set; }

        /// <summary>
        /// 数据源 / Data source
        /// </summary>
        public AtlasSource? Source { get; set; }

        /// <summary>
        /// 观察者昵称模糊匹配 / Observer login fuzzy match
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like, FieldName = "user_login")]
        public string? UserLogin { get; set; }

        /// <summary>
        /// 分类单元Id精确匹配 / Taxon id exact match
        /// </summary>
        [FilterItem(FieldName = "taxon_id")]
        public string? TaxonId { get; set; }

        /// <summary>
        /// 源站观察Id精确匹配（原样字符串） / Source observation id exact match (verbatim string)
        /// </summary>
        [FilterItem(FieldName = "source_observation_id")]
        public string? SourceObservationId { get; set; }

        /// <summary>
        /// 观察日期起（含当天；DATE 列与零点时刻比较即含当天）
        /// Observed-on range start (inclusive; comparing a DATE column to midnight includes the day)
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.GreaterThanOrEqual, FieldName = "observed_on")]
        public DateTime? ObservedOnFrom { get; set; }

        /// <summary>
        /// 观察日期止（含当天）
        /// Observed-on range end (inclusive)
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.LessThanOrEqual, FieldName = "observed_on")]
        public DateTime? ObservedOnTo { get; set; }
    }
}
