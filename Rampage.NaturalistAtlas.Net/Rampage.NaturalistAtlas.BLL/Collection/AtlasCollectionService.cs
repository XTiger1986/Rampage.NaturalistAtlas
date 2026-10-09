using Rampage.AppLog;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Dto.External;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Utilities;
using Rampage.Web;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Rampage.NaturalistAtlas.Collection
{
    /// <summary>
    /// 采集执行服务实现：编排"拉取 → 映射 → 入库 → 水位/日志"全程；SQL 全部收口在 DAL。
    /// 单源失败/熔断只影响该源（异常在 runner 内消化为 sync_log 状态）
    /// Collection execution: orchestrates fetch → map → upsert → watermark/log end to end; SQL stays in the DAL.
    /// A failing or circuit-broken source affects itself only (the runner folds exceptions into sync_log status)
    /// </summary>
    public class AtlasCollectionService : SuperService<SyncLog, ISyncLogDao>, IAtlasCollectionService
    {
        /// <summary>iNat 单周期页数上限（预算封顶 50×200 条/周期）/ iNat page cap per cycle (budget: 50×200 rows)</summary>
        private const int MaxPagesPerRun = 50;

        /// <summary>iNat 对账单批 id 数 / iNat deletion-check ids per batch call</summary>
        private const int InatIdBatchSize = 100;

        /// <summary>GBIF 单查询条数上限（超出必须切片）/ GBIF per-query record ceiling (slicing mandatory beyond it)</summary>
        private const long GbifQueryCeiling = 100000;

        /// <summary>分类刷新每批条数 / Taxon refresh batch size</summary>
        private const int TaxonRefreshBatchSize = 100;

        /// <summary>iNat 增量水位回退安全窗（分钟）：吸收排序乱序与边界抖动 / iNat watermark safety pullback in minutes (absorbs ordering jitter)</summary>
        private const int WatermarkSafetyMinutes = 5;

        /// <summary>iNat 增量首跑回看时长（小时）/ iNat increment look-back hours on first run</summary>
        private const int FirstRunLookbackHours = 2;

        /// <summary>集邮缺口单轮请求预算（每城至少 13 请求，翻页时更多；城市多时靠 CityIndex 轮转续跑）/ Collection-gap request budget per run (≥13 per city with paging; rotation continues larger sets)</summary>
        private const int WantedRequestBudget = 91;

        /// <summary>wanted 端点单页条数（v1 显式 per_page=100；源站若压得更低则按短页判停）/ wanted endpoint page size (explicit v1 per_page=100; a lower server-side cap stops paging via short-page detection)</summary>
        private const int WantedPageSize = 100;

        [Auto]
        protected ISyncLogDao SyncLogDao = default!;

        [Auto]
        protected ISyncCursorDao SyncCursorDao = default!;

        [Auto]
        protected IObservationDao ObservationDao = default!;

        [Auto]
        protected ITaxonDao TaxonDao = default!;

        [Auto]
        protected ICityDao CityDao = default!;

        [Auto]
        protected ICityWantedTaxonDao CityWantedTaxonDao = default!;

        [Auto]
        protected IEmergenceHistoryDao EmergenceHistoryDao = default!;

        [Auto]
        protected ISettingService SettingService = default!;

        /// <summary>
        /// 同源任务串行锁：增量与回补作业并发运行时，同源两个事务可能对同一分类"先查后插"撞唯一键，
        /// 按源串行化执行体消除竞态（不同源仍可并行）
        /// Per-source task locks: concurrent increment/backfill jobs can race on taxon
        /// find-or-insert (unique-key violation); serializing per source removes the race while
        /// keeping different sources parallel
        /// </summary>
        private static readonly Dictionary<AtlasSource, SemaphoreSlim> SourceLocks = new Dictionary<AtlasSource, SemaphoreSlim>
        {
            [AtlasSource.INaturalist] = new SemaphoreSlim(1, 1),
            [AtlasSource.Gbif] = new SemaphoreSlim(1, 1)
        };

        /// <summary>
        /// 双通道进度输出：控制台紧凑单行（运行可观测），文件日志完整上下文（排障）
        /// Dual-channel progress output: compact console line for live visibility, full context in the file log
        /// </summary>
        private void LogInfo(string message)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            RampageLogger.Info(message);
        }

        /// <summary>任务运行上下文（runner 组装）/ Task-run context (assembled by the runner)</summary>
        private class TaskRunContext
        {
            public GeneralSetting Setting = default!;
            public City? City;
            public SyncCursor Cursor = default!;
            public JsonObject Config = new JsonObject();
        }

        /// <summary>任务运行结果（成功路径回填日志与水位）/ Task-run result (feeds the log and watermark on success)</summary>
        private class TaskRunResult
        {
            public int RequestCount;
            public int InsertCount;
            public int UpdateCount;
            public int FailCount;
            public DateTime? NewWatermark;
            public string Message = string.Empty;
        }

        /// <summary>观察与所属分类的映射对（入库编排的基本单位）/ An observation paired with its taxon (the unit of ingest orchestration)</summary>
        private class IngestPair
        {
            public Observation Observation = default!;
            public Taxon Taxon = default!;
        }

        /// <summary>回补窗口状态 / Backfill window state</summary>
        private class BackfillWindow
        {
            public DateOnly From;
            public DateOnly To;
            public DateOnly SliceStart;
            public DateOnly SliceEnd;
            public int SliceDays;
            public bool Done;
            public long ResumeId;
            public int ResumeOffset;
        }

        #region 对外入口

        /// <inheritdoc/>
        public async Task RunIncrementAsync(AtlasSource source, int cityId)
        {
            await RunTaskAsync(source, cityId, SyncTaskCodes.Increment, ctx =>
                source == AtlasSource.INaturalist ? RunInatIncrementAsync(ctx) : RunGbifIncrementAsync(ctx));
        }

        /// <inheritdoc/>
        public async Task RunBackfillSliceAsync(AtlasSource source, int cityId)
        {
            await RunTaskAsync(source, cityId, SyncTaskCodes.Backfill, ctx =>
                source == AtlasSource.INaturalist ? RunInatBackfillSliceAsync(ctx) : RunGbifBackfillSliceAsync(ctx),
                silentWhen: IsBackfillDone);
        }

        /// <inheritdoc/>
        public async Task RunDeletionCheckAsync(AtlasSource source, int cityId)
        {
            await RunTaskAsync(source, cityId, SyncTaskCodes.DeletionCheck, ctx =>
                source == AtlasSource.INaturalist ? RunInatDeletionCheckAsync(ctx) : RunGbifDeletionCheckAsync(ctx));
        }

        /// <inheritdoc/>
        public async Task RunTaxonRefreshAsync(AtlasSource source)
        {
            await RunTaskAsync(source, 0, SyncTaskCodes.TaxonRefresh, ctx =>
                source == AtlasSource.INaturalist ? RunInatTaxonRefreshAsync(ctx) : RunGbifTaxonRefreshAsync(ctx));
        }

        /// <inheritdoc/>
        public async Task RunWantedSyncAsync()
        {
            await RunTaskAsync(AtlasSource.INaturalist, 0, SyncTaskCodes.WantedSync, RunWantedSyncBodyAsync);
        }

        /// <inheritdoc/>
        public async Task RunHistogramAsync(int cityId)
        {
            await RunTaskAsync(AtlasSource.INaturalist, cityId, SyncTaskCodes.Histogram, RunHistogramBodyAsync);
        }

        /// <inheritdoc/>
        public List<int> GetCollectionCityIds()
        {
            return ParseCollectionCities(SettingService.GetSettingFromCache<GeneralSetting>()).ConvertAll(c => c.Id);
        }

        #endregion

        #region 任务执行体

        /// <summary>
        /// iNat 增量：updated_since 时间窗 + id 升序翻页。
        /// 断点续扫：页预算耗尽时不推水位，把 IncLastId/IncSince 存进 config，下轮继续扫完同一窗口；
        /// 只有窗口扫尽（短页/空页）才推进水位——否则按 id 排序未取到的页里 updated_at 较小的记录会永久漏采
        /// iNat increment: updated_since window + ascending-id paging. On page-budget exhaustion the
        /// watermark does NOT advance; IncLastId/IncSince persist into config and the next run finishes the
        /// same window. The watermark advances only when the window is fully swept, or records with small
        /// updated_at on unfetched pages would be lost forever
        /// </summary>
        private async Task<TaskRunResult> RunInatIncrementAsync(TaskRunContext ctx)
        {
            TaskRunResult result = new TaskRunResult();
            InatApiClient api = new InatApiClient(ctx.Setting.InatApiBaseUrl, ctx.Setting.InatRateLimitPerMinute);
            long lastId = ReadConfigLong(ctx.Config, "IncLastId", 0);
            DateTime? resumeSince = ReadConfigDateTime(ctx.Config, "IncSince");
            DateTime since = resumeSince ?? ctx.Cursor.Watermark ?? UtcNow.AddHours(-FirstRunLookbackHours);
            bool resumed = resumeSince.HasValue;
            DateTime? maxUpdated = null;
            int batchSize = ReadConfigInt(ctx.Config, "BatchSize", 200);
            DateTime now = UtcNow;
            bool windowSwept = false;

            for (int page = 1; page <= MaxPagesPerRun; page++)
            {
                Dictionary<string, object?> query = BuildInatBaseQuery(ctx.City!, batchSize);
                query["updated_since"] = since.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
                if (lastId > 0)
                {
                    query["id_above"] = lastId;
                }

                InatObservationsResponse? response = await api.FetchObservationsAsync(query);
                result.RequestCount = api.RequestCount;
                List<InatObservationDto>? rows = response?.Results;
                if (rows == null || rows.Count == 0)
                {
                    windowSwept = true;
                    break;
                }

                IngestInatBatch(rows, ctx.Cursor.CityId, now, result);
                foreach (InatObservationDto row in rows)
                {
                    if (row.Id > lastId)
                    {
                        lastId = row.Id.Value;
                    }
                    DateTime? updated = InatMapper.ParseUtcDateTime(row.UpdatedAt);
                    if (updated.HasValue && (maxUpdated == null || updated > maxUpdated))
                    {
                        maxUpdated = updated;
                    }
                }
                LogInfo($"[iNat增量] 城市{ctx.Cursor.CityId} 第{page}页 取{rows.Count}条 累计新增{result.InsertCount} 更新{result.UpdateCount} 失败{result.FailCount} id>{lastId}");
                if (rows.Count < batchSize)
                {
                    windowSwept = true;
                    break;
                }
            }

            if (windowSwept)
            {
                // 窗口扫尽：清断点、推水位（带安全回退，吸收 updated_at 乱序，重复行由 upsert 幂等吸收）
                RemoveConfigKey(ctx.Config, "IncLastId");
                RemoveConfigKey(ctx.Config, "IncSince");
                if (maxUpdated.HasValue)
                {
                    DateTime candidate = maxUpdated.Value.AddMinutes(-WatermarkSafetyMinutes);
                    if (ctx.Cursor.Watermark == null || candidate > ctx.Cursor.Watermark)
                    {
                        result.NewWatermark = candidate;
                    }
                }
                result.Message = $"{(resumed ? "续扫完成 " : string.Empty)}新增{result.InsertCount} 更新{result.UpdateCount} 失败{result.FailCount} 请求{result.RequestCount}";
            }
            else
            {
                // 页预算耗尽：持久化断点（水位不动），下轮接着扫
                ctx.Config["IncLastId"] = lastId;
                ctx.Config["IncSince"] = since.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
                result.Message = $"窗口未扫尽已存断点(id>{lastId}) 新增{result.InsertCount} 更新{result.UpdateCount} 失败{result.FailCount} 请求{result.RequestCount}";
            }
            return result;
        }

        /// <summary>
        /// GBIF 增量：滚动 eventDate 窗（无 updated 过滤），upsert 幂等去重。
        /// 窗口端点持久化：断点扫描期间把 IncWindowEnd 锚定进 config——窗口若每轮随 now 滑动，
        /// 持久 offset 会指向错位的结果流（滑入头部的新记录被跳过）；只有窗口扫尽才重锚到当天
        /// 断点续扫：窗口大于单轮页预算时把 IncOffset 存进 config，下轮从该偏移继续翻页，
        /// 翻到 endOfRecords 才归零——否则每轮都从头拉前几页，后面的页永远翻不到
        /// GBIF increment: rolling eventDate window, deduped by idempotent upsert. The window end
        /// anchors into config (IncWindowEnd) while a resume offset exists — a window sliding with
        /// now every run would misalign the persisted offset (records slipping in at the head get
        /// skipped); it re-anchors to today only after the window is fully swept.
        /// Resume: on page-budget exhaustion IncOffset persists and the next run continues there,
        /// resetting only at endOfRecords, or pages beyond the budget would never be reached
        /// </summary>
        private async Task<TaskRunResult> RunGbifIncrementAsync(TaskRunContext ctx)
        {
            TaskRunResult result = new TaskRunResult();
            GbifApiClient api = new GbifApiClient(ctx.Setting.GbifApiBaseUrl, ctx.Setting.GbifRateLimitPerMinute);
            int windowDays = ReadConfigInt(ctx.Config, "WindowDays", 45);
            int maxPages = ReadConfigInt(ctx.Config, "MaxPagesPerRun", 20);
            int pageSize = Math.Min(ReadConfigInt(ctx.Config, "BatchSize", GbifApiClient.MaxPageSize), GbifApiClient.MaxPageSize);
            DateTime now = UtcNow;
            // 窗口终点锚定：无锚时取今天并写入 config；有锚（断点续扫中）沿用，保证 offset 语义一致
            // Window-end anchoring: today when unanchored (then persisted); reused while resuming so the offset stays meaningful
            bool anchored = ctx.Config.ContainsKey("IncWindowEnd");
            DateOnly windowEnd = anchored ? ReadConfigDate(ctx.Config, "IncWindowEnd", DateOnly.FromDateTime(now)) : DateOnly.FromDateTime(now);
            if (!anchored)
            {
                ctx.Config["IncWindowEnd"] = windowEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            string eventDate = $"{windowEnd.AddDays(-windowDays):yyyy-MM-dd},{windowEnd:yyyy-MM-dd}";

            int offset = ReadConfigInt(ctx.Config, "IncOffset", 0);
            bool resumed = offset > 0;
            bool windowSwept = false;
            for (int page = 1; page <= maxPages; page++)
            {
                GbifSearchResponse? response = await api.SearchOccurrencesAsync(BuildGbifQuery(ctx.City!, eventDate, pageSize, offset));
                result.RequestCount = api.RequestCount;
                List<GbifOccurrenceDto>? rows = response?.Results;
                if (rows == null || rows.Count == 0)
                {
                    windowSwept = true;
                    break;
                }

                IngestGbifBatch(rows, ctx.Cursor.CityId, now, result);
                offset += rows.Count;
                LogInfo($"[GBIF增量] 城市{ctx.Cursor.CityId} 第{page}页 取{rows.Count}条 累计新增{result.InsertCount} 更新{result.UpdateCount} 失败{result.FailCount} 偏移{offset}");
                if (response?.EndOfRecords == true || rows.Count < pageSize)
                {
                    windowSwept = true;
                    break;
                }
            }

            if (windowSwept)
            {
                RemoveConfigKey(ctx.Config, "IncOffset");
                // 窗口扫尽：解除锚定，下轮以当天重开新窗（滚动窗语义；重复行由 upsert 幂等吸收）
                // Window swept: lift the anchor so the next run opens a fresh window at today (rolling
                // window semantics; overlaps are absorbed by the idempotent upsert)
                RemoveConfigKey(ctx.Config, "IncWindowEnd");
            }
            else
            {
                ctx.Config["IncOffset"] = offset;
            }

            // GBIF 无增量水位语义：记上次成功时刻供诊断展示
            result.NewWatermark = now;
            result.Message = $"滚动窗{windowDays}天{(resumed ? "(续扫)" : string.Empty)}{(windowSwept ? string.Empty : $" 窗口未扫尽已存断点(偏移{offset})")} 新增{result.InsertCount} 更新{result.UpdateCount} 失败{result.FailCount} 请求{result.RequestCount}";
            return result;
        }

        /// <summary>iNat 回补切片：d1/d2 时间窗 + id_above 断点翻页，切片完推进 BfNextDate / iNat backfill slice: d1/d2 window + resumable id_above paging; BfNextDate advances per finished slice</summary>
        private async Task<TaskRunResult> RunInatBackfillSliceAsync(TaskRunContext ctx)
        {
            TaskRunResult result = new TaskRunResult();
            BackfillWindow window = ResolveBackfillWindow(ctx);
            if (window.Done)
            {
                result.Message = "回补已完成 / backfill done";
                return result;
            }

            InatApiClient api = new InatApiClient(ctx.Setting.InatApiBaseUrl, ctx.Setting.InatRateLimitPerMinute);
            int batchSize = ReadConfigInt(ctx.Config, "BatchSize", 200);
            DateTime now = UtcNow;
            long lastId = window.ResumeId;
            bool windowExhausted = false;

            for (int page = 1; page <= MaxPagesPerRun; page++)
            {
                Dictionary<string, object?> query = BuildInatBaseQuery(ctx.City!, batchSize);
                query["d1"] = window.SliceStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                query["d2"] = window.SliceEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (lastId > 0)
                {
                    query["id_above"] = lastId;
                }

                InatObservationsResponse? response = await api.FetchObservationsAsync(query);
                result.RequestCount = api.RequestCount;
                List<InatObservationDto>? rows = response?.Results;
                if (rows == null || rows.Count == 0)
                {
                    windowExhausted = true;
                    break;
                }

                IngestInatBatch(rows, ctx.Cursor.CityId, now, result);
                foreach (InatObservationDto row in rows)
                {
                    if (row.Id > lastId)
                    {
                        lastId = row.Id.Value;
                    }
                }
                LogInfo($"[iNat回补] 城市{ctx.Cursor.CityId} 切片{window.SliceStart:MM-dd}~{window.SliceEnd:MM-dd} 第{page}页 取{rows.Count}条 累计新增{result.InsertCount} 更新{result.UpdateCount} id>{lastId}");
                if (rows.Count < batchSize)
                {
                    windowExhausted = true;
                    break;
                }
            }

            AdvanceBackfillProgress(ctx, window, windowExhausted, lastId, 0);
            result.Message = $"切片{window.SliceStart:yyyy-MM-dd}~{window.SliceEnd:yyyy-MM-dd} {(windowExhausted ? "完成" : "续采中")} 新增{result.InsertCount} 更新{result.UpdateCount} 失败{result.FailCount}";
            return result;
        }

        /// <summary>GBIF 回补切片：eventDate 区间 + offset 断点翻页；单窗超限自动对半缩窗 / GBIF backfill slice: eventDate range + resumable offset paging; oversized windows auto-halve</summary>
        private async Task<TaskRunResult> RunGbifBackfillSliceAsync(TaskRunContext ctx)
        {
            TaskRunResult result = new TaskRunResult();
            BackfillWindow window = ResolveBackfillWindow(ctx);
            if (window.Done)
            {
                result.Message = "回补已完成 / backfill done";
                return result;
            }

            GbifApiClient api = new GbifApiClient(ctx.Setting.GbifApiBaseUrl, ctx.Setting.GbifRateLimitPerMinute);
            int pageSize = Math.Min(ReadConfigInt(ctx.Config, "BatchSize", GbifApiClient.MaxPageSize), GbifApiClient.MaxPageSize);
            DateTime now = UtcNow;
            string eventDate = $"{window.SliceStart:yyyy-MM-dd},{window.SliceEnd:yyyy-MM-dd}";
            int offset = window.ResumeOffset;
            long? totalCount = null;

            for (int page = 1; page <= MaxPagesPerRun; page++)
            {
                GbifSearchResponse? response = await api.SearchOccurrencesAsync(BuildGbifQuery(ctx.City!, eventDate, pageSize, offset));
                result.RequestCount = api.RequestCount;
                if (response?.Count > GbifQueryCeiling)
                {
                    // 单窗超限：对半缩窗留给下个节拍重试（同窗断点作废）
                    int halved = Math.Max(window.SliceDays / 2, 1);
                    ctx.Config["SliceDays"] = halved;
                    RemoveConfigKey(ctx.Config, "BfLastOffset");
                    result.Message = $"单窗命中{response?.Count}超上限，切片步长缩至{halved}天";
                    return result;
                }
                totalCount = response?.Count ?? totalCount;

                List<GbifOccurrenceDto>? rows = response?.Results;
                if (rows == null || rows.Count == 0)
                {
                    AdvanceBackfillProgress(ctx, window, true, 0, offset);
                    result.Message = $"切片{window.SliceStart:yyyy-MM-dd}~{window.SliceEnd:yyyy-MM-dd} 完成 新增{result.InsertCount} 更新{result.UpdateCount}";
                    return result;
                }

                IngestGbifBatch(rows, ctx.Cursor.CityId, now, result);
                offset += rows.Count;
                LogInfo($"[GBIF回补] 城市{ctx.Cursor.CityId} 切片{window.SliceStart:MM-dd}~{window.SliceEnd:MM-dd} 第{page}页 取{rows.Count}条 累计新增{result.InsertCount} 更新{result.UpdateCount} 偏移{offset}/{totalCount}");
                if (response?.EndOfRecords == true || rows.Count < pageSize || (totalCount.HasValue && offset >= totalCount.Value))
                {
                    AdvanceBackfillProgress(ctx, window, true, 0, offset);
                    result.Message = $"切片{window.SliceStart:yyyy-MM-dd}~{window.SliceEnd:yyyy-MM-dd} 完成 新增{result.InsertCount} 更新{result.UpdateCount} 失败{result.FailCount}";
                    return result;
                }
            }

            AdvanceBackfillProgress(ctx, window, false, 0, offset);
            result.Message = $"切片{window.SliceStart:yyyy-MM-dd}~{window.SliceEnd:yyyy-MM-dd} 续采中(已拉{offset}) 新增{result.InsertCount} 更新{result.UpdateCount}";
            return result;
        }

        /// <summary>iNat 删除对账：批量 id 查询，缺席即下线 / iNat deletion check: batch id lookups; absent ids go offline</summary>
        private async Task<TaskRunResult> RunInatDeletionCheckAsync(TaskRunContext ctx)
        {
            TaskRunResult result = new TaskRunResult();
            InatApiClient api = new InatApiClient(ctx.Setting.InatApiBaseUrl, ctx.Setting.InatRateLimitPerMinute);
            int sampleSize = ReadConfigInt(ctx.Config, "SampleSize", 50);
            List<string> sampleIds = ObservationDao.SelectRandomSampleIds(AtlasSource.INaturalist, ctx.Cursor.CityId, sampleSize);
            if (sampleIds.Count == 0)
            {
                result.Message = "无样本 / no sample";
                return result;
            }

            int deletedCount = 0;
            for (int start = 0; start < sampleIds.Count; start += InatIdBatchSize)
            {
                List<string> batch = sampleIds.Skip(start).Take(InatIdBatchSize).ToList();
                InatObservationsResponse? response = await api.FetchObservationsByIdsAsync(batch);
                result.RequestCount = api.RequestCount;
                int returnedCount = response?.Results?.Count ?? 0;
                if (response?.TotalResults is long total && total > returnedCount)
                {
                    // 响应被截断（per_page 之外仍有结果）：缺席判定不可信，跳过本批下线——宁可少删不可误删
                    // Truncated response (more hits beyond per_page): absence is unreliable, skip marking
                    // this batch — better to under-delete than to falsely mark live rows
                    LogInfo($"[iNat对账] 城市{ctx.Cursor.CityId} 批{start / InatIdBatchSize + 1} 响应截断(命中{total}/返回{returnedCount})，跳过缺席判定");
                    continue;
                }
                HashSet<string> present = new HashSet<string>(
                    response?.Results?.FindAll(r => r.Id != null).ConvertAll(r => r.Id!.Value.ToString(CultureInfo.InvariantCulture)) ?? new List<string>(),
                    StringComparer.Ordinal);
                List<string> missing = batch.FindAll(id => !present.Contains(id));
                if (missing.Count > 0)
                {
                    deletedCount += ObservationDao.MarkSourceDeleted(AtlasSource.INaturalist, missing);
                }
            }
            result.Message = $"抽样{sampleIds.Count} 确认源站删除{deletedCount}";
            return result;
        }

        /// <summary>GBIF 删除对账：逐条 occurrence 查询，404 即下线 / GBIF deletion check: per-key lookups; 404 goes offline</summary>
        private async Task<TaskRunResult> RunGbifDeletionCheckAsync(TaskRunContext ctx)
        {
            TaskRunResult result = new TaskRunResult();
            GbifApiClient api = new GbifApiClient(ctx.Setting.GbifApiBaseUrl, ctx.Setting.GbifRateLimitPerMinute);
            int sampleSize = ReadConfigInt(ctx.Config, "SampleSize", 50);
            List<string> sampleIds = ObservationDao.SelectRandomSampleIds(AtlasSource.Gbif, ctx.Cursor.CityId, sampleSize);
            if (sampleIds.Count == 0)
            {
                result.Message = "无样本 / no sample";
                return result;
            }

            List<string> missing = new List<string>();
            foreach (string id in sampleIds)
            {
                if (!long.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out long key))
                {
                    continue;
                }
                GbifOccurrenceDto? occurrence = await api.FetchOccurrenceAsync(key);
                result.RequestCount = api.RequestCount;
                if (occurrence == null || occurrence.Key == null)
                {
                    missing.Add(id);
                }
            }
            int deletedCount = missing.Count > 0 ? ObservationDao.MarkSourceDeleted(AtlasSource.Gbif, missing) : 0;
            result.Message = $"抽样{sampleIds.Count} 确认源站删除{deletedCount}";
            return result;
        }

        /// <summary>
        /// 集邮缺口：对配置城市中有 iNat place 的城市，按 13 个 iconic 祖先分类逐一调 v1 wanted 端点
        /// （周边已记录、本市未记录），全量重写 city_wanted_taxon。断点 config.CityIndex 轮转城市；
        /// 请求预算耗尽即止，下轮续跑。wanted 响应字段形态宽松解析，首跑以 dry-run 日志核对
        /// Collection gaps: for configured cities with an iNat place, call the v1 wanted endpoint per iconic
        /// ancestor (recorded nearby, absent locally) and fully rewrite city_wanted_taxon. config.CityIndex
        /// rotates cities; the run stops at the request budget and resumes next time. Wanted responses are
        /// parsed leniently — verify field shapes against the first-run logs
        /// </summary>
        private async Task<TaskRunResult> RunWantedSyncBodyAsync(TaskRunContext ctx)
        {
            TaskRunResult result = new TaskRunResult();
            // v1 客户端（wanted 端点仅 v1 提供）
            InatApiClient api = new InatApiClient(ctx.Setting.InatV1ApiBaseUrl, ctx.Setting.InatRateLimitPerMinute);
            int budget = WantedRequestBudget;

            List<City> cities = ParseCollectionCities(ctx.Setting);
            List<City> wantedCities = cities.FindAll(c => c.InatPlaceId.HasValue && c.Status == CommonStatus.Enabled);
            if (wantedCities.Count == 0)
            {
                result.Message = "无带 iNat place 的采集城市 / no collection city with an iNat place";
                return result;
            }

            int batchSize = ReadConfigInt(ctx.Config, "BatchSize", 500);
            int cityIndex = ReadConfigInt(ctx.Config, "CityIndex", 0) % wantedCities.Count;
            int processed = 0;
            int totalRows = 0;

            while (processed < wantedCities.Count && budget >= InatMapper.IconicSourceTaxonIds.Length)
            {
                City city = wantedCities[cityIndex];
                List<CityWantedTaxon> rows = new List<CityWantedTaxon>();
                bool cityComplete = true;
                for (int a = 0; a < InatMapper.IconicSourceTaxonIds.Length; a++)
                {
                    int ancestorId = InatMapper.IconicSourceTaxonIds[a];
                    // 翻页直到短页/命中总数/批上限；预算按请求计（每页 1 个）
                    // Page until a short page / the hit total / the batch cap; the budget counts requests
                    int page = 1;
                    int fetchedForAncestor = 0;
                    while (budget > 0)
                    {
                        InatWantedResponse? response = await api.FetchWantedTaxaAsync(
                            ancestorId.ToString(CultureInfo.InvariantCulture), city.InatPlaceId!.Value, page);
                        result.RequestCount = api.RequestCount;
                        budget--;
                        if (response?.Results == null)
                        {
                            break;
                        }
                        foreach (InatTaxonDto dto in response.Results)
                        {
                            CityWantedTaxon? row = MapWanted(dto, city.Id);
                            if (row != null && !rows.Exists(r => r.SourceTaxonId == row.SourceTaxonId))
                            {
                                rows.Add(row);
                            }
                            if (rows.Count >= batchSize)
                            {
                                break;
                            }
                        }
                        fetchedForAncestor += response.Results.Count;
                        if (rows.Count >= batchSize)
                        {
                            break;
                        }
                        if (response.TotalResults is long total)
                        {
                            if (fetchedForAncestor >= total)
                            {
                                break;
                            }
                        }
                        else if (response.Results.Count < WantedPageSize)
                        {
                            // 无命中总数时按短页判停（源站把 per_page 压到更低则提前停，宁少勿重）
                            // Without a hit total, stop on a short page (a lower server-side cap stops early)
                            break;
                        }
                        page++;
                    }
                    if (budget <= 0 && a < InatMapper.IconicSourceTaxonIds.Length - 1)
                    {
                        // 预算耗尽且仍有祖先未取：本城不完整
                        // Budget exhausted with ancestors remaining: the city list is incomplete
                        cityComplete = false;
                        break;
                    }
                }
                if (!cityComplete)
                {
                    // 整城不落库（ReplaceAll 是全量重写，半城数据会顶掉完整清单），下轮从该城续跑
                    // Skip the rewrite entirely (ReplaceAll is a full rewrite; a half list would displace
                    // the complete one) and resume from this city next run
                    LogInfo($"[集邮缺口] 预算在城市 {city.Name} 中途耗尽，本城跳过待下轮");
                    break;
                }
                CityWantedTaxonDao.ReplaceAll(city.Id, rows);
                totalRows += rows.Count;
                processed++;
                LogInfo($"[集邮缺口] {city.Name} 写入{rows.Count} 条（预算余{Math.Max(0, budget)}）");
                cityIndex = (cityIndex + 1) % wantedCities.Count;
                ctx.Config["CityIndex"] = cityIndex;
            }

            result.UpdateCount = totalRows;
            result.Message = $"刷新{processed}/{wantedCities.Count}城 共{totalRows}条 请求{result.RequestCount}";
            return result;
        }

        /// <summary>
        /// 全历史物候直方图（每日 03:30 渐进填充 emergence_history，兑现产品文档"全历史统计口径"）：
        /// 对该城市有观察的分类按缓存新旧逐个调 v1 histogram（一次请求得到该分类在该地点的全部年份逐月计数，
        /// 映射层跨年聚合成 12 个月桶）。单轮预算受限，从未拉过的最优先、其后最旧优先，多轮渐进覆盖全量；
        /// 无 iNat place 的城市跳过（histogram 仅支持 place 定位）
        /// Full-history phenology histogram (daily at 03:30, progressively filling emergence_history
        /// to honor the product doc's "all-time" caliber): walks the city's taxa ordered by cache
        /// freshness, one v1 histogram request per taxon (yielding every year's monthly counts at the
        /// place, folded into 12 month buckets by the mapper). Budget-capped per run — never-fetched
        /// first, then stalest first, covering the full set over multiple rounds; cities without an
        /// iNat place are skipped (the histogram only locates by place)
        /// </summary>
        private async Task<TaskRunResult> RunHistogramBodyAsync(TaskRunContext ctx)
        {
            TaskRunResult result = new TaskRunResult();
            if (ctx.City == null || !ctx.City.InatPlaceId.HasValue)
            {
                result.Message = "城市无 iNat place，跳过 / no iNat place, skipped";
                return result;
            }

            InatApiClient api = new InatApiClient(ctx.Setting.InatV1ApiBaseUrl, ctx.Setting.InatRateLimitPerMinute);
            int budget = ReadConfigInt(ctx.Config, "RequestBudget", 90);
            List<string> dueTaxaIds = EmergenceHistoryDao.SelectDueTaxaIds(ctx.Cursor.CityId, budget);
            if (dueTaxaIds.Count == 0)
            {
                result.Message = "无待刷新分类 / nothing due";
                return result;
            }

            // 本站 taxon id → 源站 id（histogram 用源站 id 定位）；缺失的分类跳过
            List<Taxon> taxa = TaxonDao.SelectByIds(dueTaxaIds);
            Dictionary<string, Taxon> taxonDict = taxa.ToDictionary(t => t.Id);

            int updated = 0;
            int skipped = 0;
            foreach (string taxonId in dueTaxaIds)
            {
                if (!taxonDict.TryGetValue(taxonId, out Taxon? taxon) || string.IsNullOrEmpty(taxon.SourceTaxonId))
                {
                    skipped++;
                    continue;
                }
                InatHistogramResponse? response = await api.FetchObservationHistogramAsync(taxon.SourceTaxonId, ctx.City.InatPlaceId.Value);
                result.RequestCount = api.RequestCount;
                if (response?.Results == null)
                {
                    skipped++;
                    continue;
                }
                List<EmergenceHistory> rows = MapHistogram(response.Results, ctx.Cursor.CityId, taxonId);
                if (rows.Count > 0)
                {
                    EmergenceHistoryDao.ReplaceMonths(ctx.Cursor.CityId, taxonId, rows);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
            result.UpdateCount = updated;
            result.Message = $"直方图刷新{updated}个分类（跳过{skipped}） 请求{result.RequestCount}";
            return result;
        }

        /// <summary>
        /// v1 histogram results（实测形态 {"month": {"YYYY-MM-DD": count}}，逐年分桶）→ 12 个月聚合行。
        /// 跨年同月求和得到全历史月谱；零行不落库（无记录的分类留给下次再试或明细兜底）
        /// Fold the v1 histogram results (live shape {"month": {"YYYY-MM-DD": count}}, bucketed per
        /// year) into 12 aggregated month rows: same-month buckets across years are summed into the
        /// all-time month profile; all-zero sets produce no rows (left for a later retry or the
        /// detail-caliber fallback)
        /// </summary>
        private static List<EmergenceHistory> MapHistogram(System.Text.Json.Nodes.JsonNode results, int cityId, string taxonId)
        {
            long[] monthCounts = new long[13];
            System.Text.Json.Nodes.JsonNode? buckets = results["month"];
            if (buckets is System.Text.Json.Nodes.JsonObject obj)
            {
                foreach (KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> kv in obj)
                {
                    // 键形如 "2001-02-01"：取月份段
                    string key = kv.Key ?? string.Empty;
                    string[] parts = key.Split('-');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int month) && month >= 1 && month <= 12)
                    {
                        monthCounts[month] += kv.Value?.GetValue<long>() ?? 0;
                    }
                }
            }
            DateTime now = DateTime.UtcNow;
            List<EmergenceHistory> rows = new List<EmergenceHistory>();
            for (int month = 1; month <= 12; month++)
            {
                if (monthCounts[month] > 0)
                {
                    rows.Add(new EmergenceHistory
                    {
                        Id = SnowflakeTool.NextId().ToString(CultureInfo.InvariantCulture),
                        CityId = cityId,
                        TaxonId = taxonId,
                        Month = (short)month,
                        RecordCount = (int)Math.Min(monthCounts[month], int.MaxValue),
                        CreateTime = now,
                        UpdateTime = now
                    });
                }
            }
            return rows;
        }

        /// <summary>wanted 条目 → 快照行（阶元/类群经 InatMapper 映射；中图由方图 URL 尺寸位替换；中文名留空，未入库分类无本站译名）</summary>
        private CityWantedTaxon? MapWanted(InatTaxonDto dto, int cityId)
        {
            if (dto.Id == null || string.IsNullOrEmpty(dto.Name))
            {
                return null;
            }
            Taxon mapped = InatMapper.MapTaxon(dto, DateTime.UtcNow);
            string square = dto.DefaultPhoto?.Url ?? string.Empty;
            // v1 default_photo.url 为 square 尺寸，中图靠 URL 尺寸位替换（/square.→/medium.）
            string medium = square.Contains("/square.", StringComparison.Ordinal)
                ? square.Replace("/square.", "/medium.", StringComparison.Ordinal)
                : string.Empty;
            return new CityWantedTaxon
            {
                Id = SnowflakeTool.NextId().ToString(CultureInfo.InvariantCulture),
                CityId = cityId,
                Source = (short)AtlasSource.INaturalist,
                SourceTaxonId = dto.Id.Value.ToString(CultureInfo.InvariantCulture),
                Rank = (short)mapped.Rank,
                ScientificName = dto.Name,
                CommonNameCn = string.Empty,
                CommonNameEn = dto.PreferredCommonName ?? string.Empty,
                IconicTaxon = (short)mapped.IconicTaxon,
                NearbyCount = (int)Math.Min(dto.ObservationsCount ?? 0, int.MaxValue),
                PhotoSquareUrl = square,
                PhotoMediumUrl = medium,
                // wanted 端点照片无作者名，仅存协议代码供前端署名
                // The wanted endpoint exposes no photo author; store the license code for attribution
                PhotoLicense = (short)InatMapper.MapLicense(dto.DefaultPhoto?.LicenseCode)
            };
        }

        /// <summary>解析采集城市（GeneralSetting.CollectionCityIds → City 行，跳过不存在的 id）</summary>
        private List<City> ParseCollectionCities(GeneralSetting setting)
        {
            List<City> cities = new List<City>();
            string raw = setting.CollectionCityIds ?? string.Empty;
            foreach (string part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(part, out int id) && id > 0)
                {
                    City? city = CityDao.SelectById(id);
                    if (city != null && !cities.Exists(c => c.Id == id))
                    {
                        cities.Add(city);
                    }
                }
            }
            if (cities.Count == 0)
            {
                City? fallback = CityDao.SelectById(setting.DefaultCityId);
                if (fallback != null)
                {
                    cities.Add(fallback);
                }
            }
            return cities;
        }

        /// <summary>iNat 分类刷新：最久未刷优先逐个刷新 / iNat taxon refresh: least-recently-refreshed first</summary>
        private async Task<TaskRunResult> RunInatTaxonRefreshAsync(TaskRunContext ctx)
        {
            TaskRunResult result = new TaskRunResult();
            InatApiClient api = new InatApiClient(ctx.Setting.InatApiBaseUrl, ctx.Setting.InatRateLimitPerMinute);
            List<Taxon> batch = TaxonDao.SelectRefreshBatch(AtlasSource.INaturalist, TaxonRefreshBatchSize);
            if (batch.Count == 0)
            {
                result.Message = "无待刷新分类 / nothing to refresh";
                return result;
            }

            DateTime now = UtcNow;
            List<Taxon> refreshed = new List<Taxon>();
            foreach (Taxon existing in batch)
            {
                InatTaxaResponse? response = await api.FetchTaxaAsync(existing.SourceTaxonId);
                result.RequestCount = api.RequestCount;
                InatTaxonDto? dto = response?.Results != null && response.Results.Count > 0 ? response.Results[0] : null;
                if (dto == null)
                {
                    continue;
                }
                Taxon row = InatMapper.MapTaxon(dto, now);
                row.Id = existing.Id;
                row.CommonNameCn = existing.CommonNameCn;
                row.CreateTime = existing.CreateTime;
                refreshed.Add(row);
                if (refreshed.Count % 25 == 0)
                {
                    LogInfo($"[iNat分类刷新] 已刷新{refreshed.Count}/{batch.Count} 请求{result.RequestCount}");
                }
            }
            if (refreshed.Count > 0)
            {
                TaxonDao.BatchUpsert(refreshed);
                result.UpdateCount = refreshed.Count;
            }
            result.Message = $"刷新{refreshed.Count}/{batch.Count}条分类 请求{result.RequestCount}";
            return result;
        }

        /// <summary>GBIF 分类刷新：species 档案 + IUCN 受威胁判定 / GBIF taxon refresh: species profiles with the IUCN threatened verdict</summary>
        private async Task<TaskRunResult> RunGbifTaxonRefreshAsync(TaskRunContext ctx)
        {
            TaskRunResult result = new TaskRunResult();
            GbifApiClient api = new GbifApiClient(ctx.Setting.GbifApiBaseUrl, ctx.Setting.GbifRateLimitPerMinute);
            List<Taxon> batch = TaxonDao.SelectRefreshBatch(AtlasSource.Gbif, TaxonRefreshBatchSize);
            if (batch.Count == 0)
            {
                result.Message = "无待刷新分类 / nothing to refresh";
                return result;
            }

            DateTime now = UtcNow;
            List<Taxon> refreshed = new List<Taxon>();
            foreach (Taxon existing in batch)
            {
                if (!int.TryParse(existing.SourceTaxonId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int taxonKey))
                {
                    continue;
                }
                GbifSpeciesDto? dto = await api.FetchSpeciesAsync(taxonKey);
                result.RequestCount = api.RequestCount;
                if (dto == null)
                {
                    continue;
                }
                refreshed.Add(GbifMapper.MapTaxonRefresh(dto, existing, now));
                if (refreshed.Count % 25 == 0)
                {
                    LogInfo($"[GBIF分类刷新] 已刷新{refreshed.Count}/{batch.Count} 请求{result.RequestCount}");
                }
            }
            if (refreshed.Count > 0)
            {
                TaxonDao.BatchUpsert(refreshed);
                result.UpdateCount = refreshed.Count;
            }
            result.Message = $"刷新{refreshed.Count}/{batch.Count}条分类 请求{result.RequestCount}";
            return result;
        }

        #endregion

        #region 入库编排

        /// <summary>iNat 批映射+入库（分页粒度，控制内存）/ Map and ingest an iNat page (page-grained to bound memory)</summary>
        private void IngestInatBatch(List<InatObservationDto> rows, int cityId, DateTime now, TaskRunResult result)
        {
            List<IngestPair> pairs = new List<IngestPair>();
            foreach (InatObservationDto row in rows)
            {
                Observation? observation = InatMapper.MapObservation(row, cityId, now);
                if (observation == null || row.Taxon == null)
                {
                    result.FailCount++;
                    continue;
                }
                pairs.Add(new IngestPair { Observation = observation, Taxon = InatMapper.MapTaxon(row.Taxon, now) });
            }
            UpsertCountDto count = IngestPairs(pairs);
            result.InsertCount += count.Insert;
            result.UpdateCount += count.Update;
            result.FailCount += pairs.Count - count.Insert - count.Update;
        }

        /// <summary>GBIF 批映射+入库（iNat 转发条目按口径静默跳过，不计失败）/ Map and ingest a GBIF page (iNat republishes skip silently by design)</summary>
        private void IngestGbifBatch(List<GbifOccurrenceDto> rows, int cityId, DateTime now, TaskRunResult result)
        {
            List<IngestPair> pairs = new List<IngestPair>();
            foreach (GbifOccurrenceDto row in rows)
            {
                if (GbifMapper.IsInatRepublish(row.DatasetKey))
                {
                    continue;
                }
                Observation? observation = GbifMapper.MapObservation(row, cityId, now);
                if (observation == null)
                {
                    result.FailCount++;
                    continue;
                }
                pairs.Add(new IngestPair { Observation = observation, Taxon = GbifMapper.MapTaxon(row, now) });
            }
            UpsertCountDto count = IngestPairs(pairs);
            result.InsertCount += count.Insert;
            result.UpdateCount += count.Update;
            result.FailCount += pairs.Count - count.Insert - count.Update;
        }

        /// <summary>
        /// 批量幂等入库：分类 find-or-create（缺失赋雪花）→ 回填观察 taxon_id/iconic → 双表事务 upsert。
        /// 批内按源站ID去重
        /// Batch idempotent ingest: taxon find-or-create (snowflake for the missing) → observation taxon_id/iconic
        /// backfill → two-table transactional upsert. Deduped by source id in-batch
        /// </summary>
        private UpsertCountDto IngestPairs(List<IngestPair> pairs)
        {
            UpsertCountDto total = new UpsertCountDto();
            if (pairs.Count == 0)
            {
                return total;
            }
            AtlasSource source = pairs[0].Observation.Source;

            Dictionary<string, Observation> observationDict = new Dictionary<string, Observation>(StringComparer.Ordinal);
            Dictionary<string, Taxon> taxonDict = new Dictionary<string, Taxon>(StringComparer.Ordinal);
            Dictionary<string, string> observationTaxonKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (IngestPair pair in pairs)
            {
                string observationKey = pair.Observation.SourceObservationId;
                if (!observationDict.ContainsKey(observationKey))
                {
                    observationDict.Add(observationKey, pair.Observation);
                    if (!string.IsNullOrEmpty(pair.Taxon.SourceTaxonId))
                    {
                        observationTaxonKeys[observationKey] = pair.Taxon.SourceTaxonId;
                        if (!taxonDict.ContainsKey(pair.Taxon.SourceTaxonId))
                        {
                            taxonDict.Add(pair.Taxon.SourceTaxonId, pair.Taxon);
                        }
                    }
                }
            }

            List<Taxon> newTaxa = new List<Taxon>();
            if (taxonDict.Count > 0)
            {
                List<Taxon> existing = TaxonDao.SelectBySourceIds(source, taxonDict.Keys.ToList());
                foreach (Taxon row in existing)
                {
                    taxonDict[row.SourceTaxonId] = row;
                }
                foreach (Taxon taxon in taxonDict.Values)
                {
                    if (string.IsNullOrEmpty(taxon.Id))
                    {
                        taxon.Id = SnowflakeTool.NextId().ToString(CultureInfo.InvariantCulture);
                        newTaxa.Add(taxon);
                    }
                }
            }

            List<Observation> ready = new List<Observation>();
            foreach (KeyValuePair<string, Observation> kv in observationDict)
            {
                if (observationTaxonKeys.TryGetValue(kv.Key, out string? taxonKey) && taxonDict.TryGetValue(taxonKey, out Taxon? taxon) && !string.IsNullOrEmpty(taxon.Id))
                {
                    kv.Value.TaxonId = taxon.Id;
                    kv.Value.IconicTaxon = taxon.IconicTaxon;
                    kv.Value.Id = SnowflakeTool.NextId().ToString(CultureInfo.InvariantCulture);
                    ready.Add(kv.Value);
                }
            }

            BeginTransaction(TaxonDao);
            try
            {
                if (newTaxa.Count > 0)
                {
                    TaxonDao.BatchUpsert(newTaxa);
                }
                if (ready.Count > 0)
                {
                    UpsertCountDto count = ObservationDao.BatchUpsert(ready);
                    total.Insert += count.Insert;
                    total.Update += count.Update;
                }
                CommitTransaction();
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
            return total;
        }

        #endregion

        #region runner 与工具

        /// <summary>
        /// 任务执行骨架：水位行 get-or-create → sync_log 生命周期 → 熔断/失败路径收尾。
        /// 所有异常在内部消化（单源故障不影响另一源），作业层无须再捕获。
        /// silentWhen 为真时跳过运行日志（完成态任务空转防灌表），僵尸日志收尾仍执行
        /// Task skeleton: cursor get-or-create → sync_log lifecycle → circuit/failure finishing.
        /// Every exception is absorbed here (one failing source never blocks the other); jobs need no
        /// try/catch. A truthy silentWhen skips the run log (idle done-state tasks would otherwise
        /// flood the table); zombie finalization still runs
        /// </summary>
        private async Task RunTaskAsync(AtlasSource source, int cityId, string task, Func<TaskRunContext, Task<TaskRunResult>> body, Func<JsonObject, bool>? silentWhen = null)
        {
            DateTime now = UtcNow;
            SyncCursor? cursor = null;
            SyncLog log = new SyncLog
            {
                Id = SnowflakeTool.NextId().ToString(CultureInfo.InvariantCulture),
                Source = source,
                CityId = cityId,
                Task = task,
                Status = SyncLogStatus.Running,
                StartTime = now,
                RequestCount = 0,
                InsertCount = 0,
                UpdateCount = 0,
                FailCount = 0,
                Message = string.Empty,
                CreateTime = now,
                UpdateTime = now
            };

            try
            {
                GeneralSetting setting = SettingService.GetSettingFromCache<GeneralSetting>();
                cursor = SyncCursorDao.SelectByTask(source, cityId, task);
                if (cursor == null)
                {
                    cursor = new SyncCursor
                    {
                        Source = source,
                        CityId = cityId,
                        Task = task,
                        Watermark = null,
                        Config = "{}",
                        LastRunStatus = CursorRunStatus.NotRun,
                        LastRunTime = default,
                        LastRunMessage = string.Empty,
                        CreateTime = now,
                        UpdateTime = now
                    };
                    SyncCursorDao.Insert(cursor);
                }

                TaskRunContext ctx = new TaskRunContext
                {
                    Setting = setting,
                    City = cityId > 0 ? CityDao.SelectById(cityId) : null,
                    Cursor = cursor,
                    Config = ParseConfig(cursor.Config)
                };
                if (cityId > 0 && ctx.City == null)
                {
                    throw new InvalidOperationException($"城市 {cityId} 不存在 / city {cityId} missing");
                }

                // 收尾上次进程中断遗留的"运行中"僵尸日志（仅同任务范围、开始于 1 小时前之前）；静默路径也执行
                // Finalize zombie still-running rows left by an interrupted process (same task scope,
                // started over an hour ago); runs on the silent path too
                SyncLogDao.MarkStaleRunningFailed(source, cityId, task, UtcNow.AddHours(-1));

                if (silentWhen != null && silentWhen(ctx.Config))
                {
                    // 静默路径：任务处于完成态（如回补已到窗尾），不写运行日志、不执行体——否则
                    // 调度器每 10 分钟一轮的空转也会各插一条日志，把 sync_log 灌成噪音海
                    // Silent path: the task is in a done state (e.g. backfill window finished) — no run
                    // log and no body, or the 10-minute scheduler would insert a noise row per cycle
                    LogInfo($"[空转跳过] {source} {task} 城市{cityId}：任务已完成，无日志写入");
                    return;
                }

                SyncLogDao.Insert(log);

                LogInfo($"[开始] {source} {task} 城市{cityId}" + (ctx.Cursor.Watermark.HasValue ? $" 水位{ctx.Cursor.Watermark.Value:MM-dd HH:mm}" : string.Empty));

                // 同源串行化：执行体含"先查后插"入库编排，防止增量/回补并发时撞唯一键
                SemaphoreSlim sourceLock = SourceLocks[source];
                TaskRunResult result;
                await sourceLock.WaitAsync();
                try
                {
                    result = await body(ctx);
                }
                finally
                {
                    sourceLock.Release();
                }

                double seconds = (UtcNow - now).TotalSeconds;
                LogInfo($"[完成] {source} {task} 城市{cityId}：{result.Message}，耗时{seconds:F0}s");

                UpdateFields<SyncLog> logFields = new UpdateFields<SyncLog>();
                logFields.Set(m => m.Status, SyncLogStatus.Success);
                logFields.Set(m => m.EndTime, UtcNow);
                logFields.Set(m => m.RequestCount, result.RequestCount);
                logFields.Set(m => m.InsertCount, result.InsertCount);
                logFields.Set(m => m.UpdateCount, result.UpdateCount);
                logFields.Set(m => m.FailCount, result.FailCount);
                logFields.Set(m => m.Message, Truncate(result.Message, 1000));
                logFields.Set(m => m.UpdateTime, UtcNow);
                SyncLogDao.UpdateById(log.Id, logFields);

                UpdateFields<SyncCursor> cursorFields = new UpdateFields<SyncCursor>();
                if (result.NewWatermark.HasValue)
                {
                    cursorFields.Set(m => m.Watermark, result.NewWatermark.Value);
                }
                cursorFields.Set(m => m.LastRunStatus, CursorRunStatus.Success);
                cursorFields.Set(m => m.LastRunTime, UtcNow);
                cursorFields.Set(m => m.LastRunMessage, Truncate(result.Message, 500));
                cursorFields.Set(m => m.Config, ctx.Config.ToJsonString());
                cursorFields.Set(m => m.UpdateTime, UtcNow);
                SyncCursorDao.UpdateById(cursor.Id, cursorFields);
            }
            catch (SourceUnavailableException ex)
            {
                // 熔断也记完整异常（内层网络/超时详情进日志，便于排查）
                // Circuit breaks log the full exception too (inner network/timeout details land in the log)
                RampageLogger.Error(ex);
                LogInfo($"[熔断跳过] {source} {task} 城市{cityId}：{ex.Message}");
                FinishFailure(log, cursor, SyncLogStatus.CircuitSkipped, CursorRunStatus.CircuitSkipped, ex.Message);
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                LogInfo($"[失败] {source} {task} 城市{cityId}：{ex.Message}");
                FinishFailure(log, cursor, SyncLogStatus.Failed, CursorRunStatus.Failed, ex.Message);
            }
        }

        /// <summary>失败/熔断收尾：日志与水位行置对应状态（收尾自身异常不再上抛）/ Failure/skip finishing: mark the log and cursor (finishing errors are swallowed)</summary>
        private void FinishFailure(SyncLog log, SyncCursor? cursor, SyncLogStatus logStatus, CursorRunStatus cursorStatus, string message)
        {
            try
            {
                UpdateFields<SyncLog> logFields = new UpdateFields<SyncLog>();
                logFields.Set(m => m.Status, logStatus);
                logFields.Set(m => m.EndTime, UtcNow);
                logFields.Set(m => m.Message, Truncate(message, 1000));
                logFields.Set(m => m.UpdateTime, UtcNow);
                SyncLogDao.UpdateById(log.Id, logFields);

                if (cursor != null)
                {
                    UpdateFields<SyncCursor> cursorFields = new UpdateFields<SyncCursor>();
                    cursorFields.Set(m => m.LastRunStatus, cursorStatus);
                    cursorFields.Set(m => m.LastRunTime, UtcNow);
                    cursorFields.Set(m => m.LastRunMessage, Truncate(message, 500));
                    cursorFields.Set(m => m.UpdateTime, UtcNow);
                    SyncCursorDao.UpdateById(cursor.Id, cursorFields);
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }
        }

        /// <summary>iNat 查询公共参数（地理按行政区划优先，回退 bbox；采集口径 research+needs_id+带图+有坐标）/ iNat shared query params (administrative place first, bbox fallback; research+needs_id+photo+geo scope)</summary>
        private Dictionary<string, object?> BuildInatBaseQuery(City city, int batchSize)
        {
            Dictionary<string, object?> query = new Dictionary<string, object?>
            {
                ["photos"] = "true",
                ["has"] = new List<object?> { "geo" },
                ["quality_grade"] = "research,needs_id",
                ["per_page"] = Math.Min(batchSize, 200),
                ["order_by"] = "id",
                ["order"] = "asc"
            };
            if (city.InatPlaceId.HasValue)
            {
                query["place_id"] = city.InatPlaceId.Value;
            }
            else
            {
                query["swlng"] = city.SwLng.ToString(CultureInfo.InvariantCulture);
                query["swlat"] = city.SwLat.ToString(CultureInfo.InvariantCulture);
                query["nelng"] = city.NeLng.ToString(CultureInfo.InvariantCulture);
                query["nelat"] = city.NeLat.ToString(CultureInfo.InvariantCulture);
            }
            return query;
        }

        /// <summary>GBIF 查询公共参数（过滤基线 PRESENT+坐标+静图；geometry 按边界 WKT 优先，回退 bbox 矩形）/ GBIF shared query params (PRESENT+coordinate+StillImage baseline; boundary WKT first, bbox rectangle fallback)</summary>
        private Dictionary<string, object?> BuildGbifQuery(City city, string eventDate, int limit, int offset)
        {
            string geometry = !string.IsNullOrEmpty(city.BoundaryWkt) ? city.BoundaryWkt : BuildBboxWkt(city);
            return new Dictionary<string, object?>
            {
                ["geometry"] = geometry,
                ["occurrenceStatus"] = "PRESENT",
                ["hasCoordinate"] = "true",
                ["mediaType"] = "StillImage",
                ["eventDate"] = eventDate,
                ["limit"] = limit,
                ["offset"] = offset
            };
        }

        /// <summary>由城市 bbox 生成矩形 WKT / Build a rectangle WKT from the city bbox</summary>
        private string BuildBboxWkt(City city)
        {
            return FormattableString.Invariant(
                $"POLYGON(({city.SwLng} {city.SwLat},{city.NeLng} {city.SwLat},{city.NeLng} {city.NeLat},{city.SwLng} {city.NeLat},{city.SwLng} {city.SwLat}))");
        }

        /// <summary>回补窗口是否已完成（BfDone 置位，或进度越过错过的窗尾）/ Whether the backfill window is finished (BfDone set, or progress past the window end)</summary>
        private bool IsBackfillDone(JsonObject config)
        {
            if (ReadConfigBool(config, "BfDone", false))
            {
                return true;
            }
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly to = ReadConfigDate(config, "ToDate", today);
            DateOnly from = ReadConfigDate(config, "FromDate", to.AddMonths(-24));
            return ReadConfigDate(config, "BfNextDate", from) > to;
        }

        /// <summary>解析回补窗口（含断点）：公共键 FromDate/ToDate 管理页可改，私有键 BfNextDate/BfDone/BfLastId/BfLastOffset/SliceDays 为采集端进度 / Resolve the backfill window with resume points</summary>
        private BackfillWindow ResolveBackfillWindow(TaskRunContext ctx)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly to = ReadConfigDate(ctx.Config, "ToDate", today);
            DateOnly from = ReadConfigDate(ctx.Config, "FromDate", to.AddMonths(-24));
            int sliceDays = Math.Max(ReadConfigInt(ctx.Config, "SliceDays", 30), 1);
            DateOnly next = ReadConfigDate(ctx.Config, "BfNextDate", from);
            return new BackfillWindow
            {
                From = from,
                To = to,
                SliceStart = next,
                SliceEnd = next.AddDays(sliceDays),
                SliceDays = sliceDays,
                Done = IsBackfillDone(ctx.Config),
                ResumeId = ReadConfigLong(ctx.Config, "BfLastId", 0),
                ResumeOffset = ReadConfigInt(ctx.Config, "BfLastOffset", 0)
            };
        }

        /// <summary>推进回补进度：切片完成推进 BfNextDate 并清断点；未完成持久化断点；到窗尾置 BfDone / Advance backfill progress</summary>
        private void AdvanceBackfillProgress(TaskRunContext ctx, BackfillWindow window, bool windowExhausted, long lastId, int lastOffset)
        {
            if (windowExhausted)
            {
                RemoveConfigKey(ctx.Config, "BfLastId");
                RemoveConfigKey(ctx.Config, "BfLastOffset");
                ctx.Config["BfNextDate"] = window.SliceEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (window.SliceEnd > window.To)
                {
                    ctx.Config["BfDone"] = true;
                }
            }
            else
            {
                if (lastId > 0)
                {
                    ctx.Config["BfLastId"] = lastId;
                }
                if (lastOffset > 0)
                {
                    ctx.Config["BfLastOffset"] = lastOffset;
                }
            }
        }

        /// <summary>解析 config JSONB 为可写节点；空/非法给空对象 / Parse the config JSONB into a mutable node; empty or invalid yields an empty object</summary>
        private JsonObject ParseConfig(string? config)
        {
            if (!string.IsNullOrWhiteSpace(config))
            {
                try
                {
                    if (JsonNode.Parse(config) is JsonObject node)
                    {
                        return node;
                    }
                }
                catch (JsonException)
                {
                    // 非法 JSON 视为空配置（下一次运行会整体重建）
                }
            }
            return new JsonObject();
        }

        private int ReadConfigInt(JsonObject node, string key, int fallback)
        {
            return node.TryGetPropertyValue(key, out JsonNode? value) && (int?)value is int parsed ? parsed : fallback;
        }

        private long ReadConfigLong(JsonObject node, string key, long fallback)
        {
            return node.TryGetPropertyValue(key, out JsonNode? value) && (long?)value is long parsed ? parsed : fallback;
        }

        private bool ReadConfigBool(JsonObject node, string key, bool fallback)
        {
            return node.TryGetPropertyValue(key, out JsonNode? value) && (bool?)value is bool parsed ? parsed : fallback;
        }

        private DateTime? ReadConfigDateTime(JsonObject node, string key)
        {
            if (node.TryGetPropertyValue(key, out JsonNode? value) && value != null
                && DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsed))
            {
                return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }
            return null;
        }

        private DateOnly ReadConfigDate(JsonObject node, string key, DateOnly fallback)
        {
            if (node.TryGetPropertyValue(key, out JsonNode? value) && value != null
                && DateOnly.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed))
            {
                return parsed;
            }
            return fallback;
        }

        private void RemoveConfigKey(JsonObject node, string key)
        {
            if (node.ContainsKey(key))
            {
                node.Remove(key);
            }
        }

        private string Truncate(string text, int maxLength)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : (text.Length <= maxLength ? text : text.Substring(0, maxLength));
        }

        #endregion
    }
}
