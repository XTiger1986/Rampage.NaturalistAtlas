using Rampage.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Enums
{
    /// <summary>
    /// 数据源枚举（iNaturalist 与 GBIF 双源并行采集） / Data source enum (iNaturalist + GBIF dual-source collection)
    /// </summary>
    public enum AtlasSource : short
    {
        /// <summary>
        /// iNaturalist / iNaturalist
        /// </summary>
        [EnumFieldAttribute("INaturalist")]
        INaturalist = 1,

        /// <summary>
        /// GBIF / GBIF
        /// </summary>
        [EnumFieldAttribute("Gbif")]
        Gbif = 2
    }

    /// <summary>
    /// 类群枚举：smallint 代码与表设计注释对齐，与 iNaturalist 13 个 iconic 类群一一映射（Animalia 归入其他动物）
    /// Iconic taxon enum: smallint codes aligned with the table-design comments; one-to-one with iNat's 13 iconic taxa (Animalia folded into Other Animals)
    /// </summary>
    public enum IconicTaxon : short
    {
        /// <summary>鸟 / Birds</summary>
        [EnumFieldAttribute("Birds")]
        Birds = 1,

        /// <summary>哺乳 / Mammals</summary>
        [EnumFieldAttribute("Mammals")]
        Mammals = 2,

        /// <summary>两栖 / Amphibians</summary>
        [EnumFieldAttribute("Amphibians")]
        Amphibians = 3,

        /// <summary>爬行动物 / Reptiles</summary>
        [EnumFieldAttribute("Reptiles")]
        Reptiles = 4,

        /// <summary>辐鳍鱼 / Ray-finned Fishes</summary>
        [EnumFieldAttribute("RayFinnedFishes")]
        RayFinnedFishes = 5,

        /// <summary>昆虫 / Insects</summary>
        [EnumFieldAttribute("Insects")]
        Insects = 6,

        /// <summary>蛛形 / Arachnids</summary>
        [EnumFieldAttribute("Arachnids")]
        Arachnids = 7,

        /// <summary>软体 / Mollusks</summary>
        [EnumFieldAttribute("Mollusks")]
        Mollusks = 8,

        /// <summary>植物 / Plants</summary>
        [EnumFieldAttribute("Plants")]
        Plants = 9,

        /// <summary>真菌 / Fungi</summary>
        [EnumFieldAttribute("Fungi")]
        Fungi = 10,

        /// <summary>其他动物（含 Animalia 泛类） / Other Animals (Animalia folded in)</summary>
        [EnumFieldAttribute("OtherAnimals")]
        OtherAnimals = 11,

        /// <summary>原生动物 / Protozoans</summary>
        [EnumFieldAttribute("Protozoans")]
        Protozoans = 12,

        /// <summary>色藻 / Chromista</summary>
        [EnumFieldAttribute("Chromista")]
        Chromista = 13,

        /// <summary>未知 / Unknown</summary>
        [EnumFieldAttribute("Unknown")]
        Unknown = 14
    }

    /// <summary>
    /// 记录类型枚举（GBIF basisOfRecord 归并）：smallint 代码与表设计注释对齐
    /// Record basis enum (GBIF basisOfRecord folded): smallint codes aligned with the table-design comments
    /// </summary>
    public enum RecordBasis : short
    {
        /// <summary>人类观察（含 iNat 全部记录）/ Human observation (covers all iNat records)</summary>
        [EnumFieldAttribute("HumanObservation")]
        HumanObservation = 0,

        /// <summary>机器观察（相机陷阱/声呐等）/ Machine observation (camera traps etc.)</summary>
        [EnumFieldAttribute("MachineObservation")]
        MachineObservation = 1,

        /// <summary>标本（博物馆标本照片）/ Preserved specimen (museum specimen photos)</summary>
        [EnumFieldAttribute("Specimen")]
        Specimen = 2
    }

    /// <summary>
    /// 分类阶元枚举：smallint 代码与表设计注释对齐 / Taxon rank enum: smallint codes aligned with the table-design comments
    /// </summary>
    public enum TaxonRank : short
    {
        /// <summary>未知 / Unknown</summary>
        [EnumFieldAttribute("Unknown")]
        Unknown = 0,

        /// <summary>界 / Kingdom</summary>
        [EnumFieldAttribute("Kingdom")]
        Kingdom = 1,

        /// <summary>门 / Phylum</summary>
        [EnumFieldAttribute("Phylum")]
        Phylum = 2,

        /// <summary>纲 / Class</summary>
        [EnumFieldAttribute("Class")]
        Class = 3,

        /// <summary>目 / Order</summary>
        [EnumFieldAttribute("Order")]
        Order = 4,

        /// <summary>科 / Family</summary>
        [EnumFieldAttribute("Family")]
        Family = 5,

        /// <summary>属 / Genus</summary>
        [EnumFieldAttribute("Genus")]
        Genus = 6,

        /// <summary>种 / Species</summary>
        [EnumFieldAttribute("Species")]
        Species = 7,

        /// <summary>亚种 / Subspecies</summary>
        [EnumFieldAttribute("Subspecies")]
        Subspecies = 8,

        /// <summary>变种 / Variety</summary>
        [EnumFieldAttribute("Variety")]
        Variety = 9,

        /// <summary>变型 / Form</summary>
        [EnumFieldAttribute("Form")]
        Form = 10,

        /// <summary>杂交 / Hybrid</summary>
        [EnumFieldAttribute("Hybrid")]
        Hybrid = 11
    }

    /// <summary>
    /// 鉴定级别枚举：smallint 代码与表设计注释对齐 / Quality grade enum: smallint codes aligned with the table-design comments
    /// </summary>
    public enum QualityGrade : short
    {
        /// <summary>研究级 / Research grade</summary>
        [EnumFieldAttribute("Research")]
        Research = 1,

        /// <summary>待鉴定 / Needs ID</summary>
        [EnumFieldAttribute("NeedsId")]
        NeedsId = 2,

        /// <summary>随手记录（预留） / Casual (reserved)</summary>
        [EnumFieldAttribute("Casual")]
        Casual = 3
    }

    /// <summary>
    /// 坐标保护枚举：smallint 代码与表设计注释对齐 / Geoprivacy enum: smallint codes aligned with the table-design comments
    /// </summary>
    public enum GeoPrivacy : short
    {
        /// <summary>开放 / Open</summary>
        [EnumFieldAttribute("Open")]
        Open = 0,

        /// <summary>模糊 / Obscured</summary>
        [EnumFieldAttribute("Obscured")]
        Obscured = 1,

        /// <summary>私密 / Private</summary>
        [EnumFieldAttribute("Private")]
        Private = 2
    }

    /// <summary>
    /// 首图授权枚举：smallint 代码与表设计注释对齐 / Photo license enum: smallint codes aligned with the table-design comments
    /// </summary>
    public enum AtlasLicense : short
    {
        /// <summary>保留所有权利 / All rights reserved</summary>
        [EnumFieldAttribute("AllRightsReserved")]
        AllRightsReserved = 0,

        /// <summary>CC0 / CC0</summary>
        [EnumFieldAttribute("Cc0")]
        Cc0 = 1,

        /// <summary>CC-BY / CC-BY</summary>
        [EnumFieldAttribute("CcBy")]
        CcBy = 2,

        /// <summary>CC-BY-SA / CC-BY-SA</summary>
        [EnumFieldAttribute("CcBySa")]
        CcBySa = 3,

        /// <summary>CC-BY-NC / CC-BY-NC</summary>
        [EnumFieldAttribute("CcByNc")]
        CcByNc = 4,

        /// <summary>CC-BY-NC-SA / CC-BY-NC-SA</summary>
        [EnumFieldAttribute("CcByNcSa")]
        CcByNcSa = 5,

        /// <summary>CC-BY-ND / CC-BY-ND</summary>
        [EnumFieldAttribute("CcByNd")]
        CcByNd = 6,

        /// <summary>CC-BY-NC-ND / CC-BY-NC-ND</summary>
        [EnumFieldAttribute("CcByNcNd")]
        CcByNcNd = 7,

        /// <summary>PDM 公有领域标记（GBIF 常见） / Public Domain Mark (common on GBIF)</summary>
        [EnumFieldAttribute("Pdm")]
        Pdm = 8
    }

    /// <summary>
    /// 观察状态枚举：smallint 代码与表设计注释对齐 / Observation status enum: smallint codes aligned with the table-design comments
    /// </summary>
    public enum ObservationStatus : short
    {
        /// <summary>正常 / Normal</summary>
        [EnumFieldAttribute("Normal")]
        Normal = 0,

        /// <summary>隐藏（运营处置） / Hidden (operator action)</summary>
        [EnumFieldAttribute("Hidden")]
        Hidden = 1,

        /// <summary>源站已删除（对账下线，只读） / Deleted on the source site (reconciled offline, read-only)</summary>
        [EnumFieldAttribute("SourceDeleted")]
        SourceDeleted = 2
    }

    /// <summary>
    /// 举报原因枚举：smallint 代码与表设计注释对齐 / Report reason enum: smallint codes aligned with the table-design comments
    /// </summary>
    public enum ReportReason : short
    {
        /// <summary>其他 / Other</summary>
        [EnumFieldAttribute("Other")]
        Other = 0,

        /// <summary>不当内容 / Inappropriate content</summary>
        [EnumFieldAttribute("InappropriateContent")]
        InappropriateContent = 1,

        /// <summary>错误鉴定 / Misidentification</summary>
        [EnumFieldAttribute("Misidentification")]
        Misidentification = 2,

        /// <summary>隐私问题 / Privacy issue</summary>
        [EnumFieldAttribute("PrivacyIssue")]
        PrivacyIssue = 3,

        /// <summary>垃圾信息 / Spam</summary>
        [EnumFieldAttribute("Spam")]
        Spam = 4
    }

    /// <summary>
    /// 举报状态枚举：smallint 代码与表设计注释对齐 / Report status enum: smallint codes aligned with the table-design comments
    /// </summary>
    public enum ReportStatus : short
    {
        /// <summary>待处理 / Pending</summary>
        [EnumFieldAttribute("Pending")]
        Pending = 0,

        /// <summary>已隐藏（处理为隐藏观察） / Resolved by hiding the observation</summary>
        [EnumFieldAttribute("Hidden")]
        Hidden = 1,

        /// <summary>驳回 / Rejected</summary>
        [EnumFieldAttribute("Rejected")]
        Rejected = 2
    }

    /// <summary>
    /// 采集任务上次运行状态（sync_cursor.last_run_status）：smallint 代码与表设计注释对齐
    /// Cursor last-run status (sync_cursor.last_run_status): smallint codes aligned with the table-design comments
    /// </summary>
    public enum CursorRunStatus : short
    {
        /// <summary>未运行 / Not run</summary>
        [EnumFieldAttribute("NotRun")]
        NotRun = 0,

        /// <summary>成功 / Success</summary>
        [EnumFieldAttribute("Success")]
        Success = 1,

        /// <summary>失败 / Failed</summary>
        [EnumFieldAttribute("Failed")]
        Failed = 2,

        /// <summary>熔断跳过 / Circuit-breaker skipped</summary>
        [EnumFieldAttribute("CircuitSkipped")]
        CircuitSkipped = 3
    }

    /// <summary>
    /// 同步日志运行状态（sync_log.status）：smallint 代码与表设计注释对齐
    /// Sync-log run status (sync_log.status): smallint codes aligned with the table-design comments
    /// </summary>
    public enum SyncLogStatus : short
    {
        /// <summary>运行中 / Running</summary>
        [EnumFieldAttribute("Running")]
        Running = 0,

        /// <summary>成功 / Success</summary>
        [EnumFieldAttribute("Success")]
        Success = 1,

        /// <summary>失败 / Failed</summary>
        [EnumFieldAttribute("Failed")]
        Failed = 2,

        /// <summary>熔断跳过 / Circuit-breaker skipped</summary>
        [EnumFieldAttribute("CircuitSkipped")]
        CircuitSkipped = 3
    }

    /// <summary>
    /// 采集任务类型（sync_cursor.task / sync_log.task 的取值集合，列为 varchar 非 smallint）
    /// Sync task types (value set of sync_cursor.task / sync_log.task; the column is varchar, not smallint)
    /// </summary>
    public static class SyncTaskCodes
    {
        /// <summary>增量同步（按源站 updated_at 水位）/ Incremental sync (by source updated_at watermark)</summary>
        public const string Increment = "increment";

        /// <summary>历史回补（时间窗切片）/ Historical backfill (time-window slices)</summary>
        public const string Backfill = "backfill";

        /// <summary>分类档案刷新 / Taxon profile refresh</summary>
        public const string TaxonRefresh = "taxon_refresh";

        /// <summary>删除对账（抽样比对下线）/ Deletion reconciliation (sampled comparison takedown)</summary>
        public const string DeletionCheck = "deletion_check";

        /// <summary>直方图拉取（物候全史口径，预留）/ Histogram fetch (full-history phenology, reserved)</summary>
        public const string Histogram = "histogram";

        /// <summary>集邮缺口刷新（iNat wanted 周边差集 → city_wanted_taxon 全量重写）/ Collection-gap refresh (iNat wanted diff → full rewrite of city_wanted_taxon)</summary>
        public const string WantedSync = "wanted_sync";

        /// <summary>
        /// 全部合法任务类型（下拉与校验用）/ All legal task types (for dropdowns and validation)
        /// </summary>
        public static readonly string[] All = { Increment, Backfill, TaxonRefresh, DeletionCheck, Histogram, WantedSync };

        /// <summary>
        /// 任务类型 → 中文展示名（任务代码是 varchar 常量而非 smallint 枚举，不走 EnumReader；
        /// 管理端列表/日志/看板健康度统一取此映射，未知代码原样显示）
        /// Task code → Chinese display name (task codes are varchar constants, not smallint enums, so
        /// EnumReader does not apply; admin lists/logs/dashboard use this map, unknown codes pass through)
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
        {
            [Increment] = "增量同步",
            [Backfill] = "历史回补",
            [TaxonRefresh] = "分类刷新",
            [DeletionCheck] = "删除对账",
            [Histogram] = "直方图拉取",
            [WantedSync] = "集邮缺口"
        };

        /// <summary>
        /// 任务类型的中文展示名（未知代码原样返回）/ Chinese display name of a task code (unknown codes pass through)
        /// </summary>
        public static string LabelOf(string? task)
        {
            return task != null && Labels.TryGetValue(task, out string? label) ? label : (task ?? "-");
        }
    }
}
