using Rampage.Data;
using System;

namespace Rampage.NaturalistAtlas.Model.Po
{
    /// <summary>
    /// 预聚合物候统计(预留,数据量上来后启用)
    /// </summary>
    [TableMapping("emergence_stat")]
    public class EmergenceStat
    {

        /// <summary>
        /// 主键(雪花Id)
        /// </summary>
        [FieldMapping(PrimaryKey = true, ColumnName = "id")]
        public string Id { get; set; } = default!;

        /// <summary>
        /// city.id
        /// </summary>
        [FieldMapping(ColumnName = "city_id")]
        public int CityId { get; set; }

        /// <summary>
        /// taxon.id(雪花Id)
        /// </summary>
        [FieldMapping(ColumnName = "taxon_id")]
        public string? TaxonId { get; set; }

        /// <summary>
        /// 统计年份
        /// </summary>
        [FieldMapping(ColumnName = "stat_year")]
        public short StatYear { get; set; }

        /// <summary>
        /// 旬序(1-36,每月1-10日/11-20日/21日-月末)
        /// </summary>
        [FieldMapping(ColumnName = "ten_day")]
        public short TenDay { get; set; }

        /// <summary>
        /// 出现次数(research+needs_id)
        /// </summary>
        [FieldMapping(ColumnName = "record_count")]
        public int RecordCount { get; set; }

        /// <summary>
        /// 出现次数(仅research)
        /// </summary>
        [FieldMapping(ColumnName = "research_count")]
        public int ResearchCount { get; set; }

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
