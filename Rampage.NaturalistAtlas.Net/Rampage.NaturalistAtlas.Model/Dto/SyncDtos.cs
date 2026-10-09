using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Validation;
using System;

namespace Rampage.NaturalistAtlas.Model.Dto
{
    /// <summary>
    /// 采集任务配置编辑页模型：任务档案 + 表单化参数（config JSONB 的常用字段）
    /// Sync task config edit model: the task profile plus form-driven params (common fields of the config JSONB)
    /// </summary>
    public class SyncTaskConfigEditDto
    {
        /// <summary>任务Id（sync_cursor.id）/ Task id (sync_cursor.id)</summary>
        public int Id { get; set; }

        /// <summary>数据源 / Data source</summary>
        public AtlasSource Source { get; set; }

        /// <summary>城市Id / City id</summary>
        public int CityId { get; set; }

        /// <summary>城市名称 / City name</summary>
        public string? CityName { get; set; }

        /// <summary>任务类型 / Task type</summary>
        public string? Task { get; set; }

        /// <summary>增量水位（源站 updated_at，UTC）/ Incremental watermark (source updated_at, UTC)</summary>
        public DateTime? Watermark { get; set; }

        /// <summary>上次运行状态 / Last run status</summary>
        public CursorRunStatus LastRunStatus { get; set; }

        /// <summary>上次运行时间（UTC）/ Last run time (UTC)</summary>
        public DateTime LastRunTime { get; set; }

        /// <summary>上次运行摘要 / Last run message</summary>
        public string? LastRunMessage { get; set; }

        /// <summary>config 原文（JSON 预览用）/ Raw config JSON (for preview)</summary>
        public string? ConfigJson { get; set; }

        /// <summary>批量大小（拉取分页每页条数）/ Batch size (fetch page size)</summary>
        public int? BatchSize { get; set; }

        /// <summary>回补窗口起始日（backfill）/ Backfill window start (backfill)</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>回补窗口截止日（backfill）/ Backfill window end (backfill)</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>对账抽样数（deletion_check）/ Reconciliation sample size (deletion_check)</summary>
        public int? SampleSize { get; set; }
    }

    /// <summary>
    /// 采集任务配置保存输入（api/sync/config/save）：仅覆盖表单声明的键，config 其余键原样保留
    /// Sync task config save input: only form-declared keys are overwritten; other config keys are preserved
    /// </summary>
    public class SyncTaskConfigInput
    {
        /// <summary>
        /// 任务Id（sync_cursor.id）/ Task id (sync_cursor.id)
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// 批量大小（空则保留原值）/ Batch size (null keeps the original)
        /// </summary>
        [IntegerRange(MinValue = 1, MaxValue = 200)]
        public int? BatchSize { get; set; }

        /// <summary>
        /// 回补窗口起始日（空则保留原值）/ Backfill window start (null keeps the original)
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// 回补窗口截止日（空则保留原值）/ Backfill window end (null keeps the original)
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// 对账抽样数（空则保留原值）/ Reconciliation sample size (null keeps the original)
        /// </summary>
        [IntegerRange(MinValue = 1, MaxValue = 1000)]
        public int? SampleSize { get; set; }
    }

    /// <summary>
    /// 批量幂等入库计数（ON CONFLICT ... RETURNING xmax=0 判定的新增/更新行数）
    /// Batch idempotent-upsert counts (inserted/updated rows decided by ON CONFLICT ... RETURNING xmax = 0)
    /// </summary>
    public class UpsertCountDto
    {
        /// <summary>新增行数 / Inserted rows</summary>
        public int Insert { get; set; }

        /// <summary>更新行数 / Updated rows</summary>
        public int Update { get; set; }
    }
}
