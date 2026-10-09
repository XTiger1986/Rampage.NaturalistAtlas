using Rampage.NaturalistAtlas.Model.Enums;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.Model.Dto
{
    /// <summary>
    /// 运营看板聚合结果（管理后台首页） / Dashboard aggregate (admin home)
    /// 统计口径：观察均取 status=0（正常展示）的记录 / Observation metrics count status=0 (visible) rows only
    /// </summary>
    public class DashboardDto
    {
        /// <summary>看板口径城市（常规设置默认城市）/ The city this dashboard scopes to (default city in general settings)</summary>
        public string? CityName { get; set; }

        /// <summary>观察总量（正常态）/ Total visible observations</summary>
        public long TotalObservation { get; set; }

        /// <summary>研究级数量 / Research-grade count</summary>
        public long ResearchCount { get; set; }

        /// <summary>研究级占比（0~1，无数据为 null）/ Research ratio (0~1, null when empty)</summary>
        public double? ResearchRatio { get; set; }

        /// <summary>待处理举报数 / Pending report count</summary>
        public long PendingReportCount { get; set; }

        /// <summary>本月入库数 / Rows ingested this month</summary>
        public long MonthInsertCount { get; set; }

        /// <summary>数据新鲜度：最新观察源站发布距今天数（null=无数据）/ Freshness: days since the newest source-created observation (null when empty)</summary>
        public double? FreshnessDays { get; set; }

        /// <summary>类群构成（饼图，top8+其他）/ Iconic composition (pie, top 8 + others)</summary>
        public List<DashboardSliceDto> IconicDistribution { get; set; } = new List<DashboardSliceDto>();

        /// <summary>近 30 天入库趋势（折线）/ Ingestion trend of the last 30 days (line)</summary>
        public List<DashboardPointDto> Recent30Days { get; set; } = new List<DashboardPointDto>();

        /// <summary>近 12 个月观察分布（柱状）/ Observation distribution of the last 12 months (bar)</summary>
        public List<DashboardPointDto> Monthly12 { get; set; } = new List<DashboardPointDto>();

        /// <summary>活跃城市排行（观察数 Top10，全站口径）/ Active-city ranking (observation top 10, whole-site)</summary>
        public List<DashboardSliceDto> CityTop10 { get; set; } = new List<DashboardSliceDto>();

        /// <summary>近 30 天采集失败/熔断趋势（折线，全站口径）/ Sync failure/skip trend of the last 30 days (line, whole-site)</summary>
        public List<DashboardPointDto> SyncFailure30Days { get; set; } = new List<DashboardPointDto>();

        /// <summary>近 30 天举报工单量趋势（折线，全站口径）/ Report intake trend of the last 30 days (line, whole-site)</summary>
        public List<DashboardPointDto> Report30Days { get; set; } = new List<DashboardPointDto>();

        /// <summary>同步健康度（各任务最近一次状态 + 今日失败数）/ Sync health (per-task last run + today's failures)</summary>
        public List<SyncTaskHealthDto> SyncHealthList { get; set; } = new List<SyncTaskHealthDto>();
    }

    /// <summary>
    /// 看板饼图分片 / Dashboard pie slice
    /// </summary>
    public class DashboardSliceDto
    {
        /// <summary>展示名（类群中文名）/ Display label (iconic taxon name)</summary>
        public string Label { get; set; } = default!;

        /// <summary>数量 / Count</summary>
        public long Value { get; set; }
    }

    /// <summary>
    /// 看板折线/柱状数据点 / Dashboard line/bar point
    /// </summary>
    public class DashboardPointDto
    {
        /// <summary>横轴标签（日期 YYYY-MM-DD 或月份 YYYY-MM）/ X label (date or month)</summary>
        public string Label { get; set; } = default!;

        /// <summary>数量 / Count</summary>
        public long Value { get; set; }
    }

    /// <summary>
    /// 采集任务健康度行 / Sync task health row
    /// </summary>
    public class SyncTaskHealthDto
    {
        /// <summary>任务类型代码（varchar 常量）/ Task type code (varchar constant)</summary>
        public string TaskCode { get; set; } = default!;

        /// <summary>任务类型展示名（中文）/ Task type display name (Chinese)</summary>
        public string Task { get; set; } = default!;

        /// <summary>上次运行状态文案 / Last run status text</summary>
        public string LastRunStatusText { get; set; } = default!;

        /// <summary>上次运行是否成功 / Whether the last run succeeded</summary>
        public bool IsHealthy { get; set; }

        /// <summary>上次运行时间（UTC）/ Last run time (UTC)</summary>
        public DateTime? LastRunTime { get; set; }

        /// <summary>上次运行摘要 / Last run message</summary>
        public string? LastRunMessage { get; set; }
    }

    /// <summary>
    /// 观察总量统计行（DAL 聚合 SQL 投影）/ Observation base-stat row (DAL aggregate SQL projection)
    /// </summary>
    public class DashboardBaseStatDto
    {
        /// <summary>观察总量（status=0）/ Total visible observations</summary>
        public long Total { get; set; }

        /// <summary>研究级数量 / Research-grade count</summary>
        public long Research { get; set; }
    }

    /// <summary>
    /// 类群计数行（DAL 聚合 SQL 投影，iconic_taxon 为 smallint 代码）
    /// Iconic count row (DAL aggregate SQL projection; iconic_taxon is a smallint code)
    /// </summary>
    public class DashboardIconicRowDto
    {
        /// <summary>类群代码 / Iconic taxon code</summary>
        public IconicTaxon IconicTaxon { get; set; }

        /// <summary>数量 / Count</summary>
        public long Count { get; set; }
    }

    /// <summary>
    /// 趋势点行（DAL 聚合 SQL 投影，Label 为日期/月份字符串避免 DateOnly 映射歧义）
    /// Trend point row (DAL aggregate SQL projection; Label is a date/month string to avoid DateOnly mapping ambiguity)
    /// </summary>
    public class DashboardTrendRowDto
    {
        /// <summary>横轴标签（YYYY-MM-DD 或 YYYY-MM）/ X label</summary>
        public string Label { get; set; } = default!;

        /// <summary>数量 / Count</summary>
        public long Value { get; set; }
    }
}
