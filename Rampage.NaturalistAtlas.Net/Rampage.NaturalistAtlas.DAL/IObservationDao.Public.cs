using Rampage.NaturalistAtlas.Model.Dto.Public;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 观察数据 Dao：公开站查询部分（地图点位、卡片、物种榜、物候、总览）
    /// Observation Dao: public-site queries (map points, cards, species rank, phenology, overview)
    /// （口径：status=0 正常展示记录；鉴定级别缺省 research + needs_id / Scope: status=0 rows; quality defaults to research + needs_id）
    /// </summary>
    public partial interface IObservationDao
    {
        /// <summary>
        /// 地图点位（按观察日期新→旧，最多 limit 条）/ Map points, newest first, at most limit rows
        /// </summary>
        /// <param name="query">筛选 / Filter</param>
        /// <param name="limit">上限 / Limit</param>
        /// <returns>点位列表 / Points</returns>
        List<ObservationPointDto> GetPublicPointsList(AtlasObservationQuery query, int limit);

        /// <summary>
        /// 符合筛选的观察总数 / Count observations matching the filter
        /// </summary>
        /// <param name="query">筛选 / Filter</param>
        /// <param name="photoOnly">仅有图记录 / Only rows with a photo</param>
        /// <returns>总数 / Count</returns>
        long GetPublicCount(AtlasObservationQuery query, bool photoOnly = false);

        /// <summary>
        /// 观察卡片分页（按观察日期新→旧）/ Paged observation cards, newest first
        /// </summary>
        /// <param name="query">筛选 / Filter</param>
        /// <param name="offset">偏移 / Offset</param>
        /// <param name="size">条数 / Size</param>
        /// <param name="photoOnly">仅有图记录 / Only rows with a photo</param>
        /// <returns>卡片列表 / Cards</returns>
        List<ObservationCardDto> GetPublicCardPageList(AtlasObservationQuery query, int offset, int size, bool photoOnly = false);

        /// <summary>
        /// 观察详情（仅正常展示记录）/ Observation detail (visible rows only)
        /// </summary>
        /// <param name="id">观察 id / Observation id</param>
        /// <returns>详情，不存在或不可见返回 null / The detail, or null when missing/hidden</returns>
        ObservationDetailDto? GetPublicDetail(string id);

        /// <summary>
        /// 物种榜分页 / Paged species rank
        /// </summary>
        /// <param name="query">筛选 / Filter</param>
        /// <param name="keyword">物种名关键字（学名/中英文名模糊）/ Name keyword (fuzzy over scientific/Chinese/English names)</param>
        /// <param name="order">排序：record（记录数降序）/ observer（观察者降序）/ rare（记录数升序·稀缺）/ Order</param>
        /// <param name="offset">偏移 / Offset</param>
        /// <param name="size">条数 / Size</param>
        /// <returns>榜单条目 / Rank items</returns>
        List<SpeciesRankItemDto> GetSpeciesRankPageList(AtlasObservationQuery query, string? keyword, string order, int offset, int size);

        /// <summary>
        /// 物种榜总物种数 / Total species in the rank
        /// </summary>
        /// <param name="query">筛选 / Filter</param>
        /// <param name="keyword">物种名关键字 / Name keyword</param>
        /// <returns>物种数 / Species count</returns>
        long GetSpeciesRankCount(AtlasObservationQuery query, string? keyword);

        /// <summary>
        /// 物种在某城市的统计（研究级 + 待鉴定）/ Per-city species stats (research + needs_id)
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="taxonId">taxon.id</param>
        /// <returns>统计行（COUNT 恒有一行）/ Stat row (COUNT always yields a row)</returns>
        SpeciesStatRowDto GetSpeciesStat(int cityId, string taxonId);

        /// <summary>
        /// 由明细实时聚合逐旬物候（emergence_stat 未预聚合时的兜底）
        /// Ten-day emergence aggregated live from rows (fallback while emergence_stat is not populated)
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="taxonId">taxon.id</param>
        /// <param name="from">观察日期下限 / Observed-on lower bound</param>
        /// <returns>旬序聚合行（仅有记录的旬）/ Ten-day rows (only periods with records)</returns>
        List<EmergenceRowDto> GetTenDayEmergenceList(int cityId, string taxonId, DateOnly from);

        /// <summary>
        /// 由明细实时聚合逐月物候（emergence_history 无缓存时的兜底，全部本站数据）
        /// Monthly emergence aggregated live from rows (fallback while emergence_history is empty; all local data)
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="taxonId">taxon.id</param>
        /// <returns>月份聚合行 / Month rows</returns>
        List<EmergenceRowDto> GetMonthEmergenceList(int cityId, string taxonId);

        /// <summary>
        /// 物候矩阵单元（Top N 物种 × 12 月，行展开）/ Phenology matrix cells (top N species × 12 months, unpivoted)
        /// </summary>
        /// <param name="query">筛选（忽略月份）/ Filter (month ignored)</param>
        /// <param name="top">物种数 / Species count</param>
        /// <returns>单元行 / Cell rows</returns>
        List<PhenologyCellRowDto> GetPhenologyCellsList(AtlasObservationQuery query, int top);

        /// <summary>
        /// 相似物种：与指定分类同祖先链（同胞）且在本市有记录的物种，按记录数降序
        /// Similar species: siblings sharing the ancestry path with local records, by record count descending
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="taxonId">taxon.id</param>
        /// <param name="limit">条数上限 / Limit</param>
        /// <returns>物种条目 / Species items</returns>
        List<SpeciesRankItemDto> GetSimilarSpeciesList(int cityId, string taxonId, int limit);

        /// <summary>
        /// 城市总览基础统计 / City overview base stats
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="qualityList">鉴定级别 / Quality grades</param>
        /// <param name="monthStart">本月 1 日 / First day of this month</param>
        /// <returns>统计行 / Stat row</returns>
        OverviewBaseRowDto GetOverviewBase(int cityId, short[] qualityList, DateOnly monthStart);

        /// <summary>
        /// 城市类群构成 / City iconic distribution
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="qualityList">鉴定级别 / Quality grades</param>
        /// <returns>按数量降序 / Descending by count</returns>
        List<IconicCountDto> GetOverviewIconicList(int cityId, short[] qualityList);

        /// <summary>
        /// 城市活跃观察者 / City top observers
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="qualityList">鉴定级别 / Quality grades</param>
        /// <param name="limit">条数 / Limit</param>
        /// <returns>按数量降序 / Descending by count</returns>
        List<ObserverCountDto> GetTopObserversList(int cityId, short[] qualityList, int limit);

        /// <summary>
        /// 近 12 个月记录趋势（仅有记录的月份，BLL 补 0）/ 12-month record trend (months with rows only; the BLL fills zeros)
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="qualityList">鉴定级别 / Quality grades</param>
        /// <param name="from">起始月 1 日（含）/ First day of the starting month (inclusive)</param>
        /// <returns>月份计数（YYYY-MM 升序）/ Monthly counts ascending</returns>
        List<MonthlyCountDto> GetMonthly12List(int cityId, short[] qualityList, DateOnly from);

        /// <summary>
        /// 本月新星：本月之前研究级累计 &lt; threshold、截至目前 ≥ threshold 的物种（条目含首次达标日期 ReachedOn）
        /// Rising stars: species whose research-grade total was below threshold before this month and has reached it now (items carry the ReachedOn date)
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="monthStart">本月 1 日 / First day of this month</param>
        /// <param name="threshold">门槛 / Threshold</param>
        /// <param name="limit">条数 / Limit</param>
        /// <returns>物种条目 / Species items</returns>
        List<SpeciesRankItemDto> GetRisingStarsList(int cityId, DateOnly monthStart, int threshold, int limit);
    }
}
