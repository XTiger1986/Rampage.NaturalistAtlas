using Rampage.Enums;
using Rampage.Validation;

namespace Rampage.NaturalistAtlas.Model.Dto
{
    /// <summary>
    /// 国家保存输入（管理后台，Id&lt;=0 新增否则更新）
    /// Country save input (admin site; Id&lt;=0 means create, otherwise update)
    /// </summary>
    public class CountryInput
    {
        /// <summary>
        /// 国家Id（新增为 0） / Country id (0 when creating)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ISO 3166-1 alpha-2 国家代码（2 位小写字母，全站唯一）
        /// ISO 3166-1 alpha-2 code (2 lowercase letters, globally unique)
        /// </summary>
        [NotWhiteSpaceString]
        [StringLength(Min = 2, Max = 2)]
        public string Code { get; set; } = default!;

        /// <summary>
        /// 国家名称（中文）/ Country name (Chinese)
        /// </summary>
        [NotWhiteSpaceString]
        [StringLength(Min = 2, Max = 100)]
        public string Name { get; set; } = default!;

        /// <summary>
        /// 国家名称（英文）/ Country name (English)
        /// </summary>
        [NotWhiteSpaceString]
        [StringLength(Min = 2, Max = 100)]
        public string NameEn { get; set; } = default!;

        /// <summary>
        /// 地图定位中心点经度 / Map-center longitude
        /// </summary>
        public decimal Lng { get; set; }

        /// <summary>
        /// 地图定位中心点纬度 / Map-center latitude
        /// </summary>
        public decimal Lat { get; set; }

        /// <summary>
        /// 状态(1=启用,0=停用) / Status (1=enabled, 0=disabled)
        /// </summary>
        public CommonStatus Status { get; set; }
    }

    /// <summary>
    /// 省份保存输入（管理后台，Id&lt;=0 新增否则更新）
    /// Province save input (admin site; Id&lt;=0 means create, otherwise update)
    /// </summary>
    public class ProvinceInput
    {
        /// <summary>
        /// 省份Id（新增为 0） / Province id (0 when creating)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 国家Id / Country id
        /// </summary>
        [IntegerRange(MinValue = 1)]
        public int CountryId { get; set; }

        /// <summary>
        /// ISO 3166-2 一级区划代码（可空；同国内唯一）
        /// ISO 3166-2 subdivision code (nullable; unique within the country)
        /// </summary>
        [StringLength(Min = 0, Max = 10)]
        public string? Code { get; set; }

        /// <summary>
        /// 区划名称（中文）/ Subdivision name (Chinese)
        /// </summary>
        [NotWhiteSpaceString]
        [StringLength(Min = 2, Max = 100)]
        public string Name { get; set; } = default!;

        /// <summary>
        /// 区划名称（英文）/ Subdivision name (English)
        /// </summary>
        [NotWhiteSpaceString]
        [StringLength(Min = 2, Max = 100)]
        public string NameEn { get; set; } = default!;

        /// <summary>
        /// 地图定位中心点经度 / Map-center longitude
        /// </summary>
        public decimal Lng { get; set; }

        /// <summary>
        /// 地图定位中心点纬度 / Map-center latitude
        /// </summary>
        public decimal Lat { get; set; }

        /// <summary>
        /// 状态(1=启用,0=停用) / Status (1=enabled, 0=disabled)
        /// </summary>
        public CommonStatus Status { get; set; }
    }
}
