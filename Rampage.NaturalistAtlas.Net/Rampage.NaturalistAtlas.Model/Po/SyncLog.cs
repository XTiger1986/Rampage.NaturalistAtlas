using Rampage.Data;
using Rampage.NaturalistAtlas.Model.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Po
{
    /// <summary>
    /// 采集运行日志
    /// </summary>
    [TableMapping("sync_log")]
    public class SyncLog
    {

        /// <summary>
        /// 主键(雪花Id)
        /// </summary>
        [FieldMapping(PrimaryKey = true, ColumnName = "id")]
        public string Id { get; set; } = default!;
        
        /// <summary>
        /// 数据源(1=iNaturalist,2=GBIF) / Data source (1=iNaturalist, 2=GBIF)
        /// </summary>
        [FieldMapping(ColumnName = "source")]
        public AtlasSource Source { get; set; }
        
        /// <summary>
        /// city.id
        /// </summary>
        [FieldMapping(ColumnName = "city_id")]
        public int CityId { get; set; }
        
        /// <summary>
        /// 任务类型(取值同sync_cursor.task)
        /// </summary>
        [FieldMapping(ColumnName = "task")]
        public string? Task { get; set; }
        
        /// <summary>
        /// 状态(0=运行中,1=成功,2=失败,3=熔断跳过) / Status (0=running, 1=success, 2=failed, 3=circuit skipped)
        /// </summary>
        [FieldMapping(ColumnName = "status")]
        public SyncLogStatus Status { get; set; }
        
        /// <summary>
        /// 开始时间(UTC)
        /// </summary>
        [FieldMapping(ColumnName = "start_time")]
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// 结束时间(UTC,可空:运行中)
        /// </summary>
        [FieldMapping(ColumnName = "end_time")]
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// 请求次数
        /// </summary>
        [FieldMapping(ColumnName = "request_count")]
        public int RequestCount { get; set; }
        
        /// <summary>
        /// 新增条数
        /// </summary>
        [FieldMapping(ColumnName = "insert_count")]
        public int InsertCount { get; set; }
        
        /// <summary>
        /// 更新条数
        /// </summary>
        [FieldMapping(ColumnName = "update_count")]
        public int UpdateCount { get; set; }
        
        /// <summary>
        /// 失败条数
        /// </summary>
        [FieldMapping(ColumnName = "fail_count")]
        public int FailCount { get; set; }
        
        /// <summary>
        /// 运行摘要(失败原因等)
        /// </summary>
        [FieldMapping(ColumnName = "message")]
        public string? Message { get; set; }
        
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
