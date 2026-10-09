using Rampage.Data;
using System;

namespace Rampage.NaturalistAtlas.Model.Po
{
    /// <summary>
    /// 物候全历史缓存(源站直方图,R1.2全历史口径用)
    /// </summary>
    [TableMapping("emergence_history")]
    public class EmergenceHistory
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
        /// 月份(1-12)
        /// </summary>
        [FieldMapping(ColumnName = "month")]
        public short Month { get; set; }

        /// <summary>
        /// 全历史出现次数(源站直方图口径)
        /// </summary>
        [FieldMapping(ColumnName = "record_count")]
        public int RecordCount { get; set; }

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
