using Rampage.Data.Filter;
using Rampage.Enums;

namespace Rampage.NaturalistAtlas.Model.Filter
{
    /// <summary>
    /// 国家列表筛选（管理后台） / Country list filter (admin site)
    /// </summary>
    [Filter]
    public class CountryAdminFilter : DataFilter
    {
        /// <summary>
        /// 关键字：代码 / 中文名 / 英文名 模糊匹配 / Keywords: fuzzy match on code / CN / EN names
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like, FieldName = "name", OrFields = "name_en,code")]
        public string? Keywords { get; set; }

        /// <summary>
        /// 状态 / Status
        /// </summary>
        public CommonStatus? Status { get; set; }
    }

    /// <summary>
    /// 省份列表筛选（管理后台） / Province list filter (admin site)
    /// </summary>
    [Filter]
    public class ProvinceAdminFilter : DataFilter
    {
        /// <summary>
        /// 国家Id / Country id
        /// </summary>
        public int? CountryId { get; set; }

        /// <summary>
        /// 关键字：区划码 / 中文名 / 英文名 模糊匹配 / Keywords: fuzzy match on code / CN / EN names
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like, FieldName = "name", OrFields = "name_en,code")]
        public string? Keywords { get; set; }

        /// <summary>
        /// 状态 / Status
        /// </summary>
        public CommonStatus? Status { get; set; }
    }
}
