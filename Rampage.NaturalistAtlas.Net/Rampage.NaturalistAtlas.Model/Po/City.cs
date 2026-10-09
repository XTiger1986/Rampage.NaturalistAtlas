using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Po
{
    /// <summary>
    /// 城市
    /// </summary>
    [TableMapping("city")]
    public class City
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
        /// 省级行政区id(province.id,可空:城市国家或数据缺失,无外键,业务层校验)
        /// Province id (province.id; nullable for city-states or missing data; no FK, validated in the service layer)
        /// </summary>
        [FieldMapping(ColumnName = "province_id")]
        public int? ProvinceId { get; set; }

        /// <summary>
        /// 城市名称(中文)
        /// </summary>
        [FieldMapping(ColumnName = "name")]
        public string? Name { get; set; }

        /// <summary>
        /// 城市名称(英文,可空) / City name (English; nullable)
        /// </summary>
        [FieldMapping(ColumnName = "name_en")]
        public string? NameEn { get; set; }

        /// <summary>
        /// URL标识
        /// </summary>
        [FieldMapping(ColumnName = "slug")]
        public string? Slug { get; set; }

        /// <summary>
        /// bbox西南角经度
        /// </summary>
        [FieldMapping(ColumnName = "sw_lng")]
        public decimal SwLng { get; set; }

        /// <summary>
        /// bbox西南角纬度
        /// </summary>
        [FieldMapping(ColumnName = "sw_lat")]
        public decimal SwLat { get; set; }

        /// <summary>
        /// bbox东北角经度
        /// </summary>
        [FieldMapping(ColumnName = "ne_lng")]
        public decimal NeLng { get; set; }

        /// <summary>
        /// bbox东北角纬度
        /// </summary>
        [FieldMapping(ColumnName = "ne_lat")]
        public decimal NeLat { get; set; }

        /// <summary>
        /// iNaturalist place_id（源站行政区划过滤用，可空：回退 bbox）
        /// iNaturalist place_id (source-side administrative filter; nullable, falls back to bbox)
        /// </summary>
        [FieldMapping(ColumnName = "inat_place_id")]
        public int? InatPlaceId { get; set; }

        /// <summary>
        /// GBIF geometry 参数 WKT 边界（简化多边形，可空：回退 bbox 矩形）
        /// WKT boundary for the GBIF geometry parameter (simplified polygon; nullable, falls back to the bbox rectangle)
        /// </summary>
        [FieldMapping(ColumnName = "boundary_wkt")]
        public string? BoundaryWkt { get; set; }

        /// <summary>
        /// 状态(1=启用,0=停用)
        /// </summary>
        [FieldMapping(ColumnName = "status")]
        public CommonStatus Status { get; set; }

        /// <summary>
        /// 创建时间(UTC)
        /// </summary>
        [FieldMapping(ColumnName = "create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间(UTC)
        /// </summary>
        [FieldMapping(ColumnName = "update_time")]
        public DateTime UpdateTime { get; set; }

    }
}
