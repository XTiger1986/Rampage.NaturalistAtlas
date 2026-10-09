using Rampage.Data.Filter;
using Rampage.Enums;
using Rampage.NaturalistAtlas.Model.Enums;

namespace Rampage.NaturalistAtlas.Model.Filter
{
    /// <summary>
    /// 分类单元列表筛选（管理后台） / Taxon list filter (admin site)
    /// </summary>
    [Filter]
    public class TaxonAdminFilter : DataFilter
    {
        /// <summary>
        /// 关键字：学名 / 中文名 / 英文名 模糊匹配 / Keywords: fuzzy match on scientific / Chinese / English names
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like, FieldName = "scientific_name", OrFields = "common_name_cn,common_name_en")]
        public string? Keywords { get; set; }

        /// <summary>
        /// 类群 / Iconic taxon
        /// </summary>
        public IconicTaxon? IconicTaxon { get; set; }

        /// <summary>
        /// 分类阶元 / Rank
        /// </summary>
        public TaxonRank? Rank { get; set; }

        /// <summary>
        /// 数据源 / Data source
        /// </summary>
        public AtlasSource? Source { get; set; }

        /// <summary>
        /// 状态 / Status
        /// </summary>
        public CommonStatus? Status { get; set; }

        /// <summary>
        /// 受威胁物种三态筛选（null=全部，true=仅受威胁，false=仅非受威胁）
        /// Threatened tri-state (null = all, true = threatened only, false = not threatened)
        /// </summary>
        public bool? Threatened { get; set; }
    }
}
