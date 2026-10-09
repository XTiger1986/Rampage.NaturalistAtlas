using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.Model.Dto.Public
{
    // 公开站接口 DTO：与前端 src/api/types.ts 一一对应（前端 http 层把 PascalCase 统一转为 camelCase）。
    // 枚举字段一律以数字（short）输出；日期在 SQL 中以 to_char 投影为字符串，避免时区换算歧义。
    // Public API DTOs mirroring the frontend src/api/types.ts (the frontend http layer camelizes the PascalCase keys).
    // Enum fields are emitted as numbers (short); dates are projected to strings via to_char in SQL to avoid time zone ambiguity.

    #region 地理 / Geo

    /// <summary>
    /// 城市（GET api/geo/cities）/ City item
    /// </summary>
    public class CityItemDto
    {
        /// <summary>城市 id / City id</summary>
        public int Id { get; set; }

        /// <summary>中文名 / Chinese name</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>英文名 / English name</summary>
        public string? NameEn { get; set; }

        /// <summary>URL 标识 / URL slug</summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>国家 id / Country id</summary>
        public int CountryId { get; set; }

        /// <summary>国家名 / Country name</summary>
        public string? CountryName { get; set; }

        /// <summary>省级行政区名 / Province name</summary>
        public string? ProvinceName { get; set; }

        /// <summary>bbox [西南经度, 西南纬度, 东北经度, 东北纬度] / bbox [swLng, swLat, neLng, neLat]</summary>
        public double[] Bbox { get; set; } = Array.Empty<double>();

        /// <summary>定位中心点 [lng, lat]（由 bbox 推算）/ Center [lng, lat] derived from the bbox</summary>
        public double[] Center { get; set; } = Array.Empty<double>();
    }

    /// <summary>
    /// 地点搜索结果（GET api/geo/search）/ Geo search item
    /// </summary>
    public class GeoSearchItemDto
    {
        /// <summary>层级：country / province / city / Level</summary>
        public string Level { get; set; } = string.Empty;

        /// <summary>对应表主键 / Primary key of the matching table</summary>
        public int Id { get; set; }

        /// <summary>中文名 / Chinese name</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>英文名 / English name</summary>
        public string? NameEn { get; set; }

        /// <summary>上级名称 / Parent name</summary>
        public string? ParentName { get; set; }

        /// <summary>定位中心点 [lng, lat] / Center [lng, lat]</summary>
        public double[] Center { get; set; } = Array.Empty<double>();

        /// <summary>城市级 bbox，其余为 null / City bbox; null for other levels</summary>
        public double[]? Bbox { get; set; }

        /// <summary>城市级 slug，其余为 null / City slug; null for other levels</summary>
        public string? Slug { get; set; }
    }

    /// <summary>
    /// 地点搜索 SQL 行（DAL 内部投影）/ Geo search SQL row (DAL projection)
    /// </summary>
    public class GeoSearchRowDto
    {
        /// <summary>层级 / Level</summary>
        public string Level { get; set; } = string.Empty;
        /// <summary>主键 / Id</summary>
        public int Id { get; set; }
        /// <summary>中文名 / Name</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>英文名 / English name</summary>
        public string? NameEn { get; set; }
        /// <summary>上级名称 / Parent name</summary>
        public string? ParentName { get; set; }
        /// <summary>中心经度 / Center longitude</summary>
        public double Lng { get; set; }
        /// <summary>中心纬度 / Center latitude</summary>
        public double Lat { get; set; }
        /// <summary>bbox 西南经度（城市级）/ bbox south-west longitude (city only)</summary>
        public double? SwLng { get; set; }
        /// <summary>bbox 西南纬度 / bbox south-west latitude</summary>
        public double? SwLat { get; set; }
        /// <summary>bbox 东北经度 / bbox north-east longitude</summary>
        public double? NeLng { get; set; }
        /// <summary>bbox 东北纬度 / bbox north-east latitude</summary>
        public double? NeLat { get; set; }
        /// <summary>城市 slug / City slug</summary>
        public string? Slug { get; set; }
    }

    #endregion

    #region 观察 / Observation

    /// <summary>
    /// 轻量点位（地图客户端聚合用）/ Lightweight point for client-side clustering
    /// </summary>
    public class ObservationPointDto
    {
        /// <summary>观察 id / Observation id</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>经度 / Longitude</summary>
        public double Lng { get; set; }
        /// <summary>纬度 / Latitude</summary>
        public double Lat { get; set; }
        /// <summary>类群代码 / Iconic taxon code</summary>
        public short IconicTaxon { get; set; }
        /// <summary>鉴定级别 / Quality grade</summary>
        public short QualityGrade { get; set; }
        /// <summary>taxon.id</summary>
        public string? TaxonId { get; set; }
        /// <summary>展示名（中文常用名 &gt; 学名）/ Display name (Chinese common name, else scientific name)</summary>
        public string? Name { get; set; }
        /// <summary>方图 URL / Square photo URL</summary>
        public string? PhotoSquareUrl { get; set; }
        /// <summary>授权协议（地图照片卡微标署名用；作者名与大图完整署名在详情/大图链路）/ Photo license (micro-badge on map cards; full attribution lives in the detail/lightbox path)</summary>
        public short License { get; set; }
        /// <summary>观察日期 YYYY-MM-DD / Observed-on date</summary>
        public string ObservedOn { get; set; } = string.Empty;
    }

    /// <summary>
    /// 点位响应 / Point result
    /// </summary>
    public class ObservationPointResultDto
    {
        /// <summary>点位列表（按观察日期新→旧，截断于上限）/ Points, newest first, truncated at the limit</summary>
        public List<ObservationPointDto> Points { get; set; } = new List<ObservationPointDto>();
        /// <summary>符合条件总数 / Total matching rows</summary>
        public long Total { get; set; }
        /// <summary>是否被上限截断 / Whether the list was truncated</summary>
        public bool Truncated { get; set; }
    }

    /// <summary>
    /// 观察卡片（列表项）/ Observation card (list item)
    /// </summary>
    public class ObservationCardDto
    {
        /// <summary>观察 id / Observation id</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>数据源（1=iNat，2=GBIF）/ Source</summary>
        public short Source { get; set; }
        /// <summary>taxon.id</summary>
        public string? TaxonId { get; set; }
        /// <summary>学名 / Scientific name</summary>
        public string? ScientificName { get; set; }
        /// <summary>中文常用名 / Chinese common name</summary>
        public string? CommonNameCn { get; set; }
        /// <summary>英文常用名 / English common name</summary>
        public string? CommonNameEn { get; set; }
        /// <summary>类群代码 / Iconic taxon code</summary>
        public short IconicTaxon { get; set; }
        /// <summary>鉴定级别 / Quality grade</summary>
        public short QualityGrade { get; set; }
        /// <summary>观察者本地日期 YYYY-MM-DD / Observer-local date</summary>
        public string ObservedOn { get; set; } = string.Empty;
        /// <summary>观察时刻 ISO 8601（UTC，带 Z），可空 / Observed-at ISO 8601 (UTC with Z), nullable</summary>
        public string? ObservedAt { get; set; }
        /// <summary>经度 / Longitude</summary>
        public double Lng { get; set; }
        /// <summary>纬度 / Latitude</summary>
        public double Lat { get; set; }
        /// <summary>坐标保护（0=开放，1=模糊，2=私密）/ Geoprivacy</summary>
        public short Geoprivacy { get; set; }
        /// <summary>方图 URL / Square photo URL</summary>
        public string? PhotoSquareUrl { get; set; }
        /// <summary>中图 URL / Medium photo URL</summary>
        public string? PhotoMediumUrl { get; set; }
        /// <summary>授权代码 / License code</summary>
        public short License { get; set; }
        /// <summary>观察者昵称 / Observer login</summary>
        public string? UserLogin { get; set; }
        /// <summary>GBIF 数据集名称 / GBIF dataset title</summary>
        public string? DatasetTitle { get; set; }
        /// <summary>受威胁物种 / Threatened taxon</summary>
        public bool Threatened { get; set; }
    }

    /// <summary>
    /// 观察详情 / Observation detail
    /// </summary>
    public class ObservationDetailDto : ObservationCardDto
    {
        /// <summary>源站观察 id / Source observation id</summary>
        public string SourceObservationId { get; set; } = string.Empty;
        /// <summary>大图 URL / Large photo URL</summary>
        public string? PhotoLargeUrl { get; set; }
        /// <summary>位置精度（米）/ Positional accuracy in meters</summary>
        public int? PositionalAccuracy { get; set; }
        /// <summary>鉴定次数 / Identification count</summary>
        public int IdentificationsCount { get; set; }
        /// <summary>记录类型 / Record basis</summary>
        public short? RecordBasis { get; set; }
        /// <summary>城市名 / City name</summary>
        public string? CityName { get; set; }
        /// <summary>源站原始页面链接（BLL 按数据源拼接）/ Source page URL (built in the BLL per source)</summary>
        public string? SourceUrl { get; set; }
    }

    /// <summary>
    /// 举报提交（POST api/report，JSON 正文）/ Report submission (JSON body)
    /// </summary>
    public class ReportSubmitInput
    {
        /// <summary>被举报观察 id / Reported observation id</summary>
        public string? ObservationId { get; set; }
        /// <summary>原因（0=其他，1=不当内容，2=错误鉴定，3=隐私问题，4=垃圾信息）/ Reason</summary>
        public short Reason { get; set; }
        /// <summary>补充说明（≤500 字）/ Details (≤500 chars)</summary>
        public string? Content { get; set; }
        /// <summary>联系方式（可选，≤100 字）/ Contact (optional, ≤100 chars)</summary>
        public string? Contact { get; set; }
    }

    #endregion

    #region 集邮缺口 / Collection gaps

    /// <summary>
    /// 集邮缺口条目（GET api/gaps）：周边已记录、本市未记录的分类快照
    /// Collection-gap item: taxa recorded nearby but not yet in the city
    /// </summary>
    public class WantedTaxonItemDto
    {
        /// <summary>源站分类 id（本市无记录，本地 taxon 表大概率缺失，外链与图片用快照字段）/ Source taxon id</summary>
        public string SourceTaxonId { get; set; } = string.Empty;
        /// <summary>分类阶元代码（AtlasEnums.TaxonRank）/ Rank code</summary>
        public short Rank { get; set; }
        /// <summary>学名 / Scientific name</summary>
        public string? ScientificName { get; set; }
        /// <summary>中文常用名 / Chinese common name</summary>
        public string? CommonNameCn { get; set; }
        /// <summary>英文常用名 / English common name</summary>
        public string? CommonNameEn { get; set; }
        /// <summary>类群代码（1~14）/ Iconic taxon code</summary>
        public short IconicTaxon { get; set; }
        /// <summary>周边记录次数（源站口径）/ Nearby observation count</summary>
        public int NearbyCount { get; set; }
        /// <summary>代表图方图 / Representative square photo</summary>
        public string? PhotoSquareUrl { get; set; }
        /// <summary>代表图中图 / Representative medium photo</summary>
        public string? PhotoMediumUrl { get; set; }
        /// <summary>代表图授权协议（版权署名用；源站 wanted 照片无作者名，仅协议）/ Representative photo license (for attribution; the wanted endpoint exposes no author, license only)</summary>
        public short PhotoLicense { get; set; }
    }

    #endregion

    #region 物种 / Species

    /// <summary>
    /// 物种榜条目 / Species rank item
    /// </summary>
    public class SpeciesRankItemDto
    {
        /// <summary>taxon.id</summary>
        public string TaxonId { get; set; } = string.Empty;
        /// <summary>学名 / Scientific name</summary>
        public string? ScientificName { get; set; }
        /// <summary>中文常用名 / Chinese common name</summary>
        public string? CommonNameCn { get; set; }
        /// <summary>英文常用名 / English common name</summary>
        public string? CommonNameEn { get; set; }
        /// <summary>类群代码 / Iconic taxon code</summary>
        public short IconicTaxon { get; set; }
        /// <summary>受威胁物种 / Threatened taxon</summary>
        public bool Threatened { get; set; }
        /// <summary>记录数 / Record count</summary>
        public long RecordCount { get; set; }
        /// <summary>观察者数 / Observer count</summary>
        public long ObserverCount { get; set; }
        /// <summary>代表图方图 / Representative square photo</summary>
        public string? PhotoSquareUrl { get; set; }
        /// <summary>代表图中图 / Representative medium photo</summary>
        public string? PhotoMediumUrl { get; set; }
        /// <summary>代表图授权协议（与代表图同一行的 license，版权署名用）/ Representative photo license (from the same row as the photo, for attribution)</summary>
        public short? PhotoLicense { get; set; }
        /// <summary>代表图作者（与代表图同一行的 user_login）/ Representative photo author (the same row's user_login)</summary>
        public string? PhotoUserLogin { get; set; }
        /// <summary>最近观察日期 YYYY-MM-DD / Last observed-on date</summary>
        public string? LastObservedOn { get; set; }
        /// <summary>
        /// 首次达标日期 YYYY-MM-DD（仅新星语境填充：研究级累计达到门槛那天的 observed_on；榜单主链路为 null）
        /// Date the threshold was first reached (rising stars only; null on the rank path)
        /// </summary>
        public string? ReachedOn { get; set; }
    }

    /// <summary>
    /// 分类档案 / Taxon profile
    /// </summary>
    public class TaxonInfoDto
    {
        /// <summary>taxon.id</summary>
        public string TaxonId { get; set; } = string.Empty;
        /// <summary>数据源 / Source</summary>
        public short Source { get; set; }
        /// <summary>源站分类 id / Source taxon id</summary>
        public string SourceTaxonId { get; set; } = string.Empty;
        /// <summary>分类阶元 / Rank</summary>
        public short Rank { get; set; }
        /// <summary>学名 / Scientific name</summary>
        public string? ScientificName { get; set; }
        /// <summary>中文常用名 / Chinese common name</summary>
        public string? CommonNameCn { get; set; }
        /// <summary>英文常用名 / English common name</summary>
        public string? CommonNameEn { get; set; }
        /// <summary>类群代码 / Iconic taxon code</summary>
        public short IconicTaxon { get; set; }
        /// <summary>受威胁物种 / Threatened taxon</summary>
        public bool Threatened { get; set; }
        /// <summary>祖先链 / Ancestry</summary>
        public string? Ancestry { get; set; }
    }

    /// <summary>
    /// 分类祖先链节点（GET api/taxon/{id}/ancestors）/ Taxon ancestor node
    /// </summary>
    public class TaxonAncestorDto
    {
        /// <summary>taxon.id</summary>
        public string TaxonId { get; set; } = string.Empty;
        /// <summary>分类阶元代码（AtlasEnums.TaxonRank）/ Rank code</summary>
        public short Rank { get; set; }
        /// <summary>学名 / Scientific name</summary>
        public string? ScientificName { get; set; }
        /// <summary>中文常用名 / Chinese common name</summary>
        public string? CommonNameCn { get; set; }
        /// <summary>英文常用名 / English common name</summary>
        public string? CommonNameEn { get; set; }
    }

    /// <summary>
    /// 物种本市统计 SQL 行 / Per-city species stat row
    /// </summary>
    public class SpeciesStatRowDto
    {
        /// <summary>记录数 / Record count</summary>
        public long RecordCount { get; set; }
        /// <summary>观察者数 / Observer count</summary>
        public long ObserverCount { get; set; }
        /// <summary>首次观察日期 / First observed-on</summary>
        public string? FirstObservedOn { get; set; }
        /// <summary>最近观察日期 / Last observed-on</summary>
        public string? LastObservedOn { get; set; }
    }

    /// <summary>
    /// 物种详情 / Species detail
    /// </summary>
    public class SpeciesDetailDto
    {
        /// <summary>分类档案 / Taxon profile</summary>
        public TaxonInfoDto Taxon { get; set; } = new TaxonInfoDto();
        /// <summary>本市记录数 / Record count in the city</summary>
        public long RecordCount { get; set; }
        /// <summary>本市观察者数 / Observer count in the city</summary>
        public long ObserverCount { get; set; }
        /// <summary>首次观察日期 / First observed-on</summary>
        public string? FirstObservedOn { get; set; }
        /// <summary>最近观察日期 / Last observed-on</summary>
        public string? LastObservedOn { get; set; }
        /// <summary>照片墙（最新有图记录）/ Photo wall (latest records with photos)</summary>
        public List<ObservationCardDto> Photos { get; set; } = new List<ObservationCardDto>();
        /// <summary>本市分布点 / Distribution points in the city</summary>
        public List<ObservationPointDto> Points { get; set; } = new List<ObservationPointDto>();
    }

    /// <summary>
    /// 物候聚合 SQL 行（旬序或月份）/ Emergence aggregate row (ten-day index or month)
    /// </summary>
    public class EmergenceRowDto
    {
        /// <summary>序号（旬 1~36 / 月 1~12）/ Index</summary>
        public int Idx { get; set; }
        /// <summary>记录数 / Record count</summary>
        public long RecordCount { get; set; }
        /// <summary>研究级记录数 / Research-grade count</summary>
        public long ResearchCount { get; set; }
    }

    /// <summary>
    /// 物候曲线单点 / Emergence point
    /// </summary>
    public class EmergencePointDto
    {
        /// <summary>序号（旬 1~36 / 月 1~12）/ Index</summary>
        public int Index { get; set; }
        /// <summary>记录数 / Record count</summary>
        public long RecordCount { get; set; }
        /// <summary>研究级记录数 / Research-grade count</summary>
        public long ResearchCount { get; set; }
    }

    /// <summary>
    /// 物候曲线 / Emergence curve
    /// </summary>
    public class EmergenceResultDto
    {
        /// <summary>口径：detail（本站明细近两年）/ history（全历史）/ Mode</summary>
        public string Mode { get; set; } = "detail";
        /// <summary>粒度：tenDay / month / Unit</summary>
        public string Unit { get; set; } = "tenDay";
        /// <summary>样本量（总记录数）/ Sample size</summary>
        public long SampleSize { get; set; }
        /// <summary>曲线（已补齐全部序号）/ Series with every index filled</summary>
        public List<EmergencePointDto> Series { get; set; } = new List<EmergencePointDto>();
    }

    #endregion

    #region 物候矩阵 / Phenology matrix

    /// <summary>
    /// 物候矩阵 SQL 行（物种 × 月份展开）/ Phenology matrix SQL row (species × month, unpivoted)
    /// </summary>
    public class PhenologyCellRowDto
    {
        /// <summary>taxon.id</summary>
        public string TaxonId { get; set; } = string.Empty;
        /// <summary>学名 / Scientific name</summary>
        public string? ScientificName { get; set; }
        /// <summary>中文常用名 / Chinese common name</summary>
        public string? CommonNameCn { get; set; }
        /// <summary>类群代码 / Iconic taxon code</summary>
        public short IconicTaxon { get; set; }
        /// <summary>物种总记录数 / Species total</summary>
        public long Total { get; set; }
        /// <summary>月份（无记录时 null）/ Month (null when no record)</summary>
        public int? Month { get; set; }
        /// <summary>该月记录数 / Count in the month</summary>
        public long Count { get; set; }
    }

    /// <summary>
    /// 物候矩阵行 / Phenology matrix row
    /// </summary>
    public class PhenologyRowDto
    {
        /// <summary>taxon.id</summary>
        public string TaxonId { get; set; } = string.Empty;
        /// <summary>学名 / Scientific name</summary>
        public string? ScientificName { get; set; }
        /// <summary>中文常用名 / Chinese common name</summary>
        public string? CommonNameCn { get; set; }
        /// <summary>类群代码 / Iconic taxon code</summary>
        public short IconicTaxon { get; set; }
        /// <summary>总记录数 / Total</summary>
        public long Total { get; set; }
        /// <summary>1~12 月记录数（长度固定 12）/ Counts for months 1~12 (always 12 long)</summary>
        public long[] Counts { get; set; } = new long[12];
    }

    /// <summary>
    /// 物候矩阵 / Phenology matrix
    /// </summary>
    public class PhenologyMatrixDto
    {
        /// <summary>行（按总记录数降序）/ Rows, by total descending</summary>
        public List<PhenologyRowDto> Rows { get; set; } = new List<PhenologyRowDto>();
    }

    #endregion

    #region 总览 / Overview

    /// <summary>
    /// 城市总览基础统计 SQL 行 / City overview base stat row
    /// </summary>
    public class OverviewBaseRowDto
    {
        /// <summary>观察总数 / Total</summary>
        public long Total { get; set; }
        /// <summary>研究级数 / Research-grade count</summary>
        public long Research { get; set; }
        /// <summary>物种数 / Species count</summary>
        public long Species { get; set; }
        /// <summary>观察者数 / Observer count</summary>
        public long Observers { get; set; }
        /// <summary>本月观察数 / Observations this month</summary>
        public long MonthCount { get; set; }
        /// <summary>最新源站发布时间 ISO 8601 / Newest source-created time</summary>
        public string? Newest { get; set; }
    }

    /// <summary>
    /// 类群计数 / Iconic count
    /// </summary>
    public class IconicCountDto
    {
        /// <summary>类群代码 / Iconic taxon code</summary>
        public short IconicTaxon { get; set; }
        /// <summary>数量 / Count</summary>
        public long Count { get; set; }
    }

    /// <summary>
    /// 观察者计数 / Observer count
    /// </summary>
    public class ObserverCountDto
    {
        /// <summary>观察者昵称 / Observer login</summary>
        public string UserLogin { get; set; } = string.Empty;
        /// <summary>数量 / Count</summary>
        public long Count { get; set; }
    }

    /// <summary>
    /// 月份计数（近 12 个月趋势）/ Monthly count (12-month trend)
    /// </summary>
    public class MonthlyCountDto
    {
        /// <summary>月份 YYYY-MM / Month</summary>
        public string Month { get; set; } = string.Empty;
        /// <summary>数量 / Count</summary>
        public long Count { get; set; }
    }

    /// <summary>
    /// 城市总览（GET api/stats/overview）/ City overview
    /// </summary>
    public class CityOverviewDto
    {
        /// <summary>城市 id / City id</summary>
        public int CityId { get; set; }
        /// <summary>城市名 / City name</summary>
        public string? CityName { get; set; }
        /// <summary>观察总数 / Total observations</summary>
        public long TotalObservation { get; set; }
        /// <summary>研究级数 / Research-grade count</summary>
        public long ResearchCount { get; set; }
        /// <summary>物种数 / Species count</summary>
        public long SpeciesCount { get; set; }
        /// <summary>观察者数 / Observer count</summary>
        public long ObserverCount { get; set; }
        /// <summary>本月观察数 / Observations this month</summary>
        public long MonthObservation { get; set; }
        /// <summary>近 12 个月记录趋势（含 0 月桶）/ 12-month record trend (zero months included)</summary>
        public List<MonthlyCountDto> Monthly12 { get; set; } = new List<MonthlyCountDto>();
        /// <summary>类群构成 / Iconic distribution</summary>
        public List<IconicCountDto> IconicDistribution { get; set; } = new List<IconicCountDto>();
        /// <summary>本月新星 / Rising stars of the month</summary>
        public List<SpeciesRankItemDto> RisingStars { get; set; } = new List<SpeciesRankItemDto>();
        /// <summary>活跃观察者 / Top observers</summary>
        public List<ObserverCountDto> TopObservers { get; set; } = new List<ObserverCountDto>();
        /// <summary>最新源站发布时间 ISO 8601 / Newest source-created time</summary>
        public string? NewestSourceCreatedAt { get; set; }
    }

    #endregion
}
