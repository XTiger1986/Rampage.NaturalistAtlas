using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 观察数据 Dao：基础 CRUD 由 SuperDao 提供；聚合统计 SQL 统一收口在 DAL 层
    /// Observation Dao: CRUD comes from SuperDao; aggregate SQL is kept in the DAL layer
    /// （聚合统计口径：status=0 正常展示的记录 / Aggregate scope: status=0 visible rows）
    /// </summary>
    public partial interface IObservationDao : ISuperDao<Observation>
    {
        /// <summary>
        /// 总量统计：观察总量与研究级数量 / Base stats: total and research-grade counts
        /// </summary>
        /// <param name="cityId">口径城市（null=全站）/ Scope city (null = the whole site)</param>
        /// <returns>统计行（COUNT 查询恒有一行）/ The stat row (COUNT always yields a row)</returns>
        DashboardBaseStatDto GetBaseStat(int? cityId);

        /// <summary>
        /// 本月入库数（create_time &gt;= 月初）/ Rows ingested since the month start
        /// </summary>
        /// <param name="monthStart">月初零点（UTC）/ Month start at midnight (UTC)</param>
        /// <param name="cityId">口径城市（null=全站）/ Scope city (null = the whole site)</param>
        /// <returns>入库数 / The count</returns>
        long GetMonthInsertCount(DateTime monthStart, int? cityId);

        /// <summary>
        /// 最新源站发布时间（新鲜度度量）/ Newest source-created time (freshness metric)
        /// </summary>
        /// <param name="cityId">口径城市（null=全站）/ Scope city (null = the whole site)</param>
        /// <returns>最新时间，无数据为 null / The newest time, or null when empty</returns>
        DateTime? GetNewestSourceCreatedAt(int? cityId);

        /// <summary>
        /// 类群构成分布 / Iconic composition distribution
        /// </summary>
        /// <param name="cityId">口径城市（null=全站）/ Scope city (null = the whole site)</param>
        /// <returns>按数量降序的类群计数行 / Iconic count rows, descending</returns>
        List<DashboardIconicRowDto> GetIconicDistributionList(int? cityId);

        /// <summary>
        /// 指定日期起的入库趋势（按天分组，Label 为 YYYY-MM-DD）
        /// Ingestion trend since the given date, grouped by day (Label = YYYY-MM-DD)
        /// </summary>
        /// <param name="from">起始日零点（UTC）/ Start at midnight (UTC)</param>
        /// <param name="cityId">口径城市（null=全站）/ Scope city (null = the whole site)</param>
        /// <returns>按日升序的趋势行 / Trend rows, ascending</returns>
        List<DashboardTrendRowDto> GetIngestTrendList(DateTime from, int? cityId);

        /// <summary>
        /// 指定月份起的观察分布（按月分组，Label 为 YYYY-MM）
        /// Observation distribution since the given month, grouped by month (Label = YYYY-MM)
        /// </summary>
        /// <param name="from">起始月 / Start month</param>
        /// <param name="cityId">口径城市（null=全站）/ Scope city (null = the whole site)</param>
        /// <returns>按月升序的分布行 / Distribution rows, ascending</returns>
        List<DashboardTrendRowDto> GetMonthlyDistributionList(DateOnly from, int? cityId);

        /// <summary>
        /// 活跃城市排行（观察数 Top N，全站口径）/ Active-city ranking (observation top N, whole-site scope)
        /// </summary>
        /// <param name="limit">取前 N 城 / Top N</param>
        /// <returns>城市名 → 观察数切片 / City-name slices</returns>
        List<DashboardSliceDto> GetCityTopNList(int limit);

        /// <summary>
        /// 指定日期起的采集失败/熔断趋势（按天分组，status NOT IN (0 运行中, 1 成功)）
        /// Sync failure/skip trend since the given date, by day (status NOT IN (0 running, 1 success))
        /// </summary>
        /// <param name="from">起始日零点（UTC）/ Start at midnight (UTC)</param>
        /// <returns>按日升序的趋势行 / Trend rows, ascending</returns>
        List<DashboardTrendRowDto> GetSyncFailureTrendList(DateTime from);

        /// <summary>
        /// 指定日期起的举报工单量趋势（按天分组，全状态）/ Report intake trend since the given date, by day (all statuses)
        /// </summary>
        /// <param name="from">起始日零点（UTC）/ Start at midnight (UTC)</param>
        /// <returns>按日升序的趋势行 / Trend rows, ascending</returns>
        List<DashboardTrendRowDto> GetReportTrendList(DateTime from);

        /// <summary>
        /// 按作者批量隐藏：单条 UPDATE 将该观察者名下全部正常观察置为隐藏（带 update_time）
        /// Hide all of an observer's normal observations in one bulk UPDATE (with update_time)
        /// </summary>
        /// <param name="userLogin">观察者公开昵称 / Observer login</param>
        /// <param name="now">当前 UTC 时间 / The current UTC time</param>
        /// <returns>影响行数 / Affected rows</returns>
        int UpdateHiddenByAuthor(string userLogin, DateTime now);

        /// <summary>
        /// 采集端批量幂等入库：按 (source, source_observation_id) ON CONFLICT 更新业务列（不动 status/create_time），
        /// 内部按 200 行/语句分批；调用方可通过事务包裹
        /// Collector batch idempotent upsert keyed on (source, source_observation_id); business columns only
        /// (status/create_time untouched), chunked at 200 rows per statement; wrap in a transaction if needed
        /// </summary>
        /// <param name="rows">待入库行（调用方保证批内源站ID不重复）/ Rows to upsert (caller dedupes source ids in-batch)</param>
        /// <returns>新增/更新行数 / Inserted / updated counts</returns>
        UpsertCountDto SaveBatch(List<Observation> rows);

        /// <summary>
        /// 删除对账抽样：按城市随机取正常状态观察的源站ID / Deletion-check sample: random source ids of visible rows for the city
        /// </summary>
        /// <param name="source">数据源 / Data source</param>
        /// <param name="cityId">城市Id / City id</param>
        /// <param name="count">抽样数 / Sample count</param>
        /// <returns>源站观察ID列表 / The sampled source observation ids</returns>
        List<string> GetRandomSampleIdsList(AtlasSource source, int cityId, int count);

        /// <summary>
        /// 源站已删除下线（对账确认后批量置 status=2，仅影响正常行）
        /// Mark rows deleted on the source site (status=2 in batch after reconciliation; visible rows only)
        /// </summary>
        /// <param name="source">数据源 / Data source</param>
        /// <param name="sourceObservationIds">源站观察ID / Source observation ids</param>
        /// <returns>下线行数 / Affected row count</returns>
        int UpdateSourceDeleted(AtlasSource source, List<string> sourceObservationIds);
    }
}
