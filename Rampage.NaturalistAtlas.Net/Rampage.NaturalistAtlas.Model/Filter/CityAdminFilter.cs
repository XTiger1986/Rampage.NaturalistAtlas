using Rampage.Data.Filter;
using Rampage.Enums;

namespace Rampage.NaturalistAtlas.Model.Filter
{
    /// <summary>
    /// 城市列表筛选（管理后台） / City list filter (admin site)
    /// </summary>
    [Filter]
    public class CityAdminFilter : DataFilter
    {
        /// <summary>
        /// 国家Id / Country id
        /// </summary>
        public int? CountryId { get; set; }

        /// <summary>
        /// 省级行政区Id / Province id
        /// </summary>
        public int? ProvinceId { get; set; }

        /// <summary>
        /// 关键字：城市名称 / URL标识 模糊匹配 / Keywords: fuzzy match on city name / slug
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like, FieldName = "name", OrFields = "name_en,slug")]
        public string? Keywords { get; set; }

        /// <summary>
        /// 状态 / Status
        /// </summary>
        public CommonStatus? Status { get; set; }
    }
}
