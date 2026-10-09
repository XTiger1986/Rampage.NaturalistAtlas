using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System.Threading.Tasks;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 采集执行服务（Collector 作业唯一入口）：双源增量/回补切片/删除对账/分类刷新，
    /// 内部负责 sync_cursor 水位推进与 sync_log 全生命周期
    /// Collection execution service (the single entry for Collector jobs): dual-source increment /
    /// backfill slices / deletion check / taxon refresh, owning sync_cursor watermark advance and the
    /// sync_log lifecycle
    /// </summary>
    public interface IAtlasCollectionService : ISuperService<SyncLog, ISyncLogDao>
    {
        /// <summary>
        /// 增量同步（每小时：iNat 按 updated_since 水位，GBIF 滚动 eventDate 窗）
        /// Incremental sync (hourly: iNat by the updated_since watermark; GBIF by a rolling eventDate window)
        /// </summary>
        /// <param name="source">数据源 / Data source</param>
        /// <param name="cityId">城市Id / City id</param>
        Task RunIncrementAsync(AtlasSource source, int cityId);

        /// <summary>
        /// 回补一个时间窗切片（10 分钟节拍续跑，完成态 no-op）
        /// Backfill one time-window slice (continues on a 10-minute cadence; a no-op once done)
        /// </summary>
        /// <param name="source">数据源 / Data source</param>
        /// <param name="cityId">城市Id / City id</param>
        Task RunBackfillSliceAsync(AtlasSource source, int cityId);

        /// <summary>
        /// 删除对账（周度抽样比对，缺席标 status=2）
        /// Deletion reconciliation (weekly sampling; absent rows get status=2)
        /// </summary>
        /// <param name="source">数据源 / Data source</param>
        /// <param name="cityId">城市Id / City id</param>
        Task RunDeletionCheckAsync(AtlasSource source, int cityId);

        /// <summary>
        /// 分类档案刷新（每日，全局任务 city_id=0）
        /// Taxon profile refresh (daily; the global task with city_id = 0)
        /// </summary>
        /// <param name="source">数据源 / Data source</param>
        Task RunTaxonRefreshAsync(AtlasSource source);

        /// <summary>
        /// 集邮缺口刷新（每周；iNat v1 wanted 周边差集 → city_wanted_taxon 每城全量重写，
        /// 全局任务 city_id=0，config.CityIndex 轮转断点）
        /// Collection-gap refresh (weekly; the iNat v1 wanted diff fully rewrites
        /// city_wanted_taxon per city; the global task with city_id = 0 and a rotating CityIndex)
        /// </summary>
        Task RunWantedSyncAsync();

        /// <summary>
        /// 全历史物候直方图（每日渐进填充 emergence_history；按城市建游标）
        /// Full-history phenology histogram (daily, progressively filling emergence_history; per-city cursor)
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        Task RunHistogramAsync(int cityId);

        /// <summary>
        /// 解析采集城市Id清单（GeneralSetting.CollectionCityIds，空/非法回退默认城市）
        /// Parse the collection-city id list (GeneralSetting.CollectionCityIds; falls back to the default city)
        /// </summary>
        System.Collections.Generic.List<int> GetCollectionCityIdsList();
    }
}
