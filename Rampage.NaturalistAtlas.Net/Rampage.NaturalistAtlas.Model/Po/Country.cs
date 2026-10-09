using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Po
{
    /// <summary>
    /// 国家 / Country
    /// </summary>
    [TableMapping("country")]
    public class Country
    {

        /// <summary>
        /// 主键
        /// </summary>
        [FieldMapping(PrimaryKey = true, ColumnName = "id", AutoIncrement = true)]
        public int Id { get; set; }

        /// <summary>
        /// ISO 3166-1 alpha-2 国家代码(小写,如cn/gb) / ISO 3166-1 alpha-2 code (lowercase, e.g. cn/gb)
        /// </summary>
        [FieldMapping(ColumnName = "code")]
        public string Code { get; set; } = default!;

        /// <summary>
        /// 国家名称(中文) / Country name (Chinese)
        /// </summary>
        [FieldMapping(ColumnName = "name")]
        public string Name { get; set; } = default!;

        /// <summary>
        /// 国家名称(英文) / Country name (English)
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
