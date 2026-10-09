using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Po
{
    /// <summary>
    /// 省级行政区（国家一级区划泛称：省/州/都道府县/大区等）/ Province (first-level subdivision: state/prefecture/region etc.)
    /// </summary>
    [TableMapping("province")]
    public class Province
    {

        /// <summary>
        /// 主键
        /// </summary>
        [FieldMapping(PrimaryKey = true, ColumnName = "id", AutoIncrement = true)]
        public int Id { get; set; }

        /// <summary>
        /// 国家id(country.id,无外键,业务层校验) / Country id (country.id; no FK, validated in the service layer)
        /// </summary>
        [FieldMapping(ColumnName = "country_id")]
        public int CountryId { get; set; }

        /// <summary>
        /// ISO 3166-2 一级区划代码(如CN-BJ/US-NY,可空) / ISO 3166-2 subdivision code (e.g. CN-BJ/US-NY; nullable)
        /// </summary>
        [FieldMapping(ColumnName = "code")]
        public string? Code { get; set; }

        /// <summary>
        /// 区划名称(中文) / Subdivision name (Chinese)
        /// </summary>
        [FieldMapping(ColumnName = "name")]
        public string Name { get; set; } = default!;

        /// <summary>
        /// 区划名称(英文) / Subdivision name (English)
        /// </summary>
        [FieldMapping(ColumnName = "name_en")]
        public string NameEn { get; set; } = default!;

        /// <summary>
        /// 地图定位中心点经度(前端地点搜索定位用) / Map-center longitude (frontend place-search positioning)
        /// </summary>
        [FieldMapping(ColumnName = "lng")]
        public decimal Lng { get; set; }

        /// <summary>
        /// 地图定位中心点纬度(前端地点搜索定位用) / Map-center latitude (frontend place-search positioning)
        /// </summary>
        [FieldMapping(ColumnName = "lat")]
        public decimal Lat { get; set; }

        /// <summary>
        /// 状态(1=启用,0=停用) / Status (1=enabled, 0=disabled)
        /// </summary>
        [FieldMapping(ColumnName = "status")]
        public CommonStatus Status { get; set; }

        /// <summary>
        /// 创建时间(UTC) / Create time (UTC)
        /// </summary>
        [FieldMapping(ColumnName = "create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间(UTC) / Update time (UTC)
        /// </summary>
        [FieldMapping(ColumnName = "update_time")]
        public DateTime UpdateTime { get; set; }

    }
}
