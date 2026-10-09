using Rampage.Data.Models;
using Rampage.Model;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 公开站地理服务：城市列表与地点搜索 / Public geo service: city list and place search
    /// </summary>
    public interface IAtlasGeoService : ISuperService<City, ICityDao>
    {
        /// <summary>
        /// 已启用城市列表（带国家/省份名与 bbox 中心点，缓存 10 分钟）
        /// Enabled cities with country/province names and bbox centers (cached for 10 minutes)
        /// </summary>
        /// <returns>城市列表 / Cities</returns>
        List<CityItemDto> GetCityList();

        /// <summary>
        /// 地点搜索（国家/省份/城市三级）/ Place search across countries, provinces and cities
        /// </summary>
        /// <param name="keyword">关键字 / Keyword</param>
        /// <param name="limit">条数上限（1~30）/ Limit (1~30)</param>
        /// <returns>搜索结果 / Results</returns>
        List<GeoSearchItemDto> Search(string? keyword, int limit);
    }

    /// <summary>
    /// 公开站观察服务：地图点位、卡片列表、详情与举报 / Public observation service: map points, card list, detail and reports
    /// </summary>
    public interface IAtlasObservationService : ISuperService<Observation, IObservationDao>
    {
        /// <summary>
        /// 地图点位（客户端聚合用，超过上限截断并标记）/ Map points for client clustering (truncated and flagged beyond the limit)
        /// </summary>
        /// <param name="query">筛选 / Filter</param>
        /// <param name="limit">上限（1~10000）/ Limit (1~10000)</param>
        /// <returns>点位结果 / Point result</returns>
        ObservationPointResultDto GetPoints(AtlasObservationQuery query, int limit);

        /// <summary>
        /// 观察卡片分页 / Paged observation cards
        /// </summary>
        /// <param name="query">筛选 / Filter</param>
        /// <param name="pageIndex">页码 / Page index</param>
        /// <param name="pageSize">每页条数（1~60）/ Page size (1~60)</param>
        /// <param name="photoOnly">仅有图记录（照片墙）/ Only rows with a photo (photo wall)</param>
        /// <returns>分页数据 / Paged data</returns>
        PageData<ObservationCardDto> GetCardPage(AtlasObservationQuery query, int pageIndex, int pageSize, bool photoOnly = false);

        /// <summary>
        /// 观察详情（附源站链接）/ Observation detail with the source page URL
        /// </summary>
        /// <param name="id">观察 id / Observation id</param>
        /// <returns>详情，不存在返回 null / The detail, or null when missing</returns>
        ObservationDetailDto? GetDetail(string id);

        /// <summary>
        /// 提交内容举报（匿名可用；由 Idempotency-Key 防重复提交）
        /// Submit a content report (anonymous allowed; duplicate submits are prevented by Idempotency-Key)
        /// </summary>
        /// <param name="input">举报输入 / Report input</param>
        /// <returns>成功时 Data 为举报 id / On success Data is the report id</returns>
        R<string> SubmitReport(ReportSubmitInput? input);
    }

    /// <summary>
    /// 公开站物种服务：物种榜、物种详情、物候曲线与物候矩阵 / Public species service: rank, detail, emergence curve and phenology matrix
    /// </summary>
    public interface IAtlasSpeciesService : ISuperService<Taxon, ITaxonDao>
    {
        /// <summary>
        /// 物种榜分页 / Paged species rank
        /// </summary>
        /// <param name="query">筛选 / Filter</param>
        /// <param name="order">排序：record（默认）/ observer / rare（稀缺：记录数升序，口径固定研究级）/ Order</param>
        /// <param name="keyword">物种名关键字 / Name keyword</param>
        /// <param name="pageIndex">页码 / Page index</param>
        /// <param name="pageSize">每页条数（1~100）/ Page size (1~100)</param>
        /// <returns>分页数据 / Paged data</returns>
        PageData<SpeciesRankItemDto> GetRankPage(AtlasObservationQuery query, string? order, string? keyword, int pageIndex, int pageSize);

        /// <summary>
        /// 分类祖先链（界›门›纲›目›科›属面包屑，ancestry 链上本站已同步的节点）/ Taxon ancestor chain (breadcrumb over synced nodes of the ancestry path)
        /// </summary>
        /// <param name="taxonId">taxon.id</param>
        /// <returns>按链序排列的祖先节点（无档案或链为空时返回空表）/ Ancestors in path order (empty when the taxon or path is missing)</returns>
        List<TaxonAncestorDto> GetTaxonAncestors(string taxonId);

        /// <summary>
        /// 物种详情（分类档案 + 本市统计 + 照片墙 + 分布点）/ Species detail (profile, city stats, photo wall, points)
        /// </summary>
        /// <param name="taxonId">taxon.id</param>
        /// <param name="cityId">城市 id / City id</param>
        /// <returns>详情，物种不存在返回 null / The detail, or null when the taxon is missing</returns>
        SpeciesDetailDto? GetSpeciesDetail(string taxonId, int cityId);

        /// <summary>
        /// 物候曲线：detail = 近两年逐旬（优先预聚合表），history = 全历史逐月（优先源站直方图缓存）
        /// Emergence curve: detail = last two years by ten-day (pre-aggregated first); history = all time by month (source histogram cache first)
        /// </summary>
        /// <param name="taxonId">taxon.id</param>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="mode">detail / history</param>
        /// <param name="researchOnly">仅研究级 / Research grade only</param>
        /// <returns>曲线（已补齐全部序号）/ Curve with every index filled</returns>
        EmergenceResultDto GetEmergence(string taxonId, int cityId, string? mode, bool researchOnly);

        /// <summary>
        /// 相似物种推荐：同祖先链（同胞）且本市有记录的物种
        /// Similar species: siblings sharing the ancestry path with local records
        /// </summary>
        /// <param name="taxonId">taxon.id</param>
        /// <param name="cityId">城市 id / City id</param>
        /// <returns>相似物种（记录数降序，无链或无同胞返回空表）/ Similar species (empty when no path or siblings)</returns>
        List<SpeciesRankItemDto> GetSimilarSpecies(string taxonId, int cityId);

        /// <summary>
        /// 物候矩阵（Top N 物种 × 12 月；忽略月份筛选）/ Phenology matrix (top N species × 12 months; month filter ignored)
        /// </summary>
        /// <param name="query">筛选 / Filter</param>
        /// <param name="top">物种数（5~100）/ Species count (5~100)</param>
        /// <returns>矩阵 / Matrix</returns>
        PhenologyMatrixDto GetPhenologyMatrix(AtlasObservationQuery query, int top);

        /// <summary>
        /// 集邮缺口（只读采集端周更的 city_wanted_taxon 缓存，请求链路不直连源站）
        /// Collection gaps (reads the weekly-refreshed city_wanted_taxon cache; never hits the source site)
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="iconic">类群代码（空 = 全部）/ Iconic codes (empty = all)</param>
        /// <returns>wanted 条目（周边记录数降序）/ Items descending by the nearby count</returns>
        List<WantedTaxonItemDto> GetGaps(int cityId, string? iconic);
    }

    /// <summary>
    /// 公开站统计服务：城市总览 / Public stats service: city overview
    /// </summary>
    public interface IAtlasStatsService : ISuperService<Observation, IObservationDao>
    {
        /// <summary>
        /// 城市总览（缓存 10 分钟）/ City overview (cached for 10 minutes)
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="researchOnly">仅研究级 / Research grade only</param>
        /// <returns>总览，城市不存在返回 null / The overview, or null when the city is missing</returns>
        CityOverviewDto? GetCityOverview(int cityId, bool researchOnly);
    }
}
