using Rampage.Enums;
using Rampage.Validation;
using System;

namespace Rampage.NaturalistAtlas.Model.Dto
{
    /// <summary>
    /// 城市保存输入（管理后台，Id&lt;=0 新增否则更新）
    /// City save input (admin site; Id&lt;=0 means create, otherwise update)
    /// </summary>
    public class CityInput
    {
        /// <summary>
        /// 城市Id（新增为 0） / City id (0 when creating)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 国家Id / Country id
        /// </summary>
        [IntegerRange(MinValue = 1)]
        public int CountryId { get; set; }

        /// <summary>
        /// 省级行政区Id（可空：城市国家或数据缺失） / Province id (nullable for city-states or missing data)
        /// </summary>
        public int? ProvinceId { get; set; }

        /// <summary>
        /// 城市名称（中文）/ City name (Chinese)
        /// </summary>
        [NotWhiteSpaceString]
        [StringLength(Min = 2, Max = 50)]
        public string Name { get; set; } = default!;

        /// <summary>
        /// 城市名称（英文，可空）/ City name (English; nullable)
        /// </summary>
        [StringLength(Min = 0, Max = 100)]
        public string? NameEn { get; set; }

        /// <summary>
        /// URL标识（小写字母/数字/连字符） / URL slug (lowercase letters / digits / hyphens)
        /// </summary>
        [NotWhiteSpaceString]
        [StringLength(Min = 2, Max = 50)]
        public string Slug { get; set; } = default!;

        /// <summary>
        /// bbox 西南角经度 / bbox southwest longitude
        /// </summary>
        public decimal SwLng { get; set; }

        /// <summary>
        /// bbox 西南角纬度 / bbox southwest latitude
        /// </summary>
        public decimal SwLat { get; set; }

        /// <summary>
        /// bbox 东北角经度 / bbox northeast longitude
        /// </summary>
        public decimal NeLng { get; set; }

        /// <summary>
        /// bbox 东北角纬度 / bbox northeast latitude
        /// </summary>
        public decimal NeLat { get; set; }

        /// <summary>
        /// 状态(1=启用,0=停用) / Status (1=enabled, 0=disabled)
        /// </summary>
        public CommonStatus Status { get; set; }

        /// <summary>
        /// iNaturalist 源站 place id（可空：无匹配 place 的城市回退 bbox 采集；来源档案管理入口）
        /// iNaturalist source place id (nullable; cities without one fall back to bbox collection)
        /// </summary>
        [IntegerRange(MinValue = 1)]
        public int? InatPlaceId { get; set; }
    }
}
