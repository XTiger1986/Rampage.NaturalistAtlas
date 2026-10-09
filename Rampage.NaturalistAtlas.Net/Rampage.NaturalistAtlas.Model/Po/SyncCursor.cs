using Rampage.Data;
using Rampage.NaturalistAtlas.Model.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Po
{
    /// <summary>
    /// 采集水位
    /// </summary>
    [TableMapping("sync_cursor")]
    public class SyncCursor
    {

        /// <summary>
        /// 主键
        /// </summary>
        [FieldMapping(PrimaryKey = true, ColumnName = "id", AutoIncrement = true)]
        public int Id { get; set; }

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
        /// 任务类型(increment=增量,backfill=回补,taxon_refresh=分类刷新,deletion_check=删除对账,histogram=直方图)
        /// </summary>
        [FieldMapping(ColumnName = "task")]
        public string? Task { get; set; }
        
        /// <summary>
        /// 增量水位(源站updated_at,UTC,可空:尚未开始采集)
        /// </summary>
        [FieldMapping(ColumnName = "watermark")]
        public DateTime? Watermark { get; set; }
        
        /// <summary>
        /// 任务参数与进度(回补时间窗切片等,程序内定义DTO) / Task parameters and progress (DTOs defined in code)
        /// </summary>
        [FieldMapping(ColumnName = "config", Jsonb = true)]
        public string? Config { get; set; }
        
        /// <summary>
        /// 上次运行状态(0=未运行,1=成功,2=失败,3=熔断跳过) / Last run status (0=not run, 1=success, 2=failed, 3=circuit skipped)
        /// </summary>
        [FieldMapping(ColumnName = "last_run_status")]
        public CursorRunStatus LastRunStatus { get; set; }
        
        /// <summary>
        /// 上次运行时间(UTC)
        /// </summary>
        [FieldMapping(ColumnName = "last_run_time")]
        public DateTime LastRunTime { get; set; }
        
        /// <summary>
        /// 上次运行摘要
        /// </summary>
        [FieldMapping(ColumnName = "last_run_message")]
        public string? LastRunMessage { get; set; }
        
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
