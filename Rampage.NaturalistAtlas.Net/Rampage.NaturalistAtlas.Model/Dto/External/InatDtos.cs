using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Rampage.NaturalistAtlas.Model.Dto.External
{
    /// <summary>
    /// iNaturalist v2 /observations 响应外层（results 数组分页；字段以 fields 参数请求为准）
    /// iNaturalist v2 /observations response envelope (paged results array; fields follow the fields parameter)
    /// </summary>
    public class InatObservationsResponse
    {
        /// <summary>结果集 / The result rows</summary>
        [JsonPropertyName("results")]
        public List<InatObservationDto>? Results { get; set; }

        /// <summary>命中总数（存在则可判响应是否被截断；缺失/未返回时为 null）/ Total hits, when present, detects a truncated response; null when not returned</summary>
        [JsonPropertyName("total_results")]
        public long? TotalResults { get; set; }
    }

    /// <summary>
    /// iNaturalist v2 观察记录（时间字段为字符串，含时区偏移，映射层手工解析为 UTC）
    /// iNaturalist v2 observation row (timestamps arrive as strings with offsets; parsed to UTC in the mapper)
    /// </summary>
    public class InatObservationDto
    {
        /// <summary>观察ID（数字）/ Observation id (numeric)</summary>
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        /// <summary>观察UUID / Observation UUID</summary>
        [JsonPropertyName("uuid")]
        public string? Uuid { get; set; }

        /// <summary>观察日期（观察者本地）/ Observed-on (observer-local date string)</summary>
        [JsonPropertyName("observed_on")]
        public string? ObservedOn { get; set; }

        /// <summary>观察时刻（本地时区偏移 ISO 串；v2 的 _utc 字段恒为 null，此为实际承载字段）
        /// Observed-at (local-offset ISO string; the v2 _utc field is always null — this carries the value)</summary>
        [JsonPropertyName("time_observed_at")]
        public string? TimeObservedAt { get; set; }

        /// <summary>观察时刻 UTC 备用字段（v2 实测恒 null）/ Observed-at UTC fallback (always null in v2)</summary>
        [JsonPropertyName("time_observed_at_utc")]
        public string? TimeObservedAtUtc { get; set; }

        /// <summary>源站发布时间（ISO 串）/ Source created-at (ISO string)</summary>
        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        /// <summary>源站最后更新（ISO 串，增量水位）/ Source updated-at (ISO string; the incremental watermark)</summary>
        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        /// <summary>鉴定级别（research/needs_id/casual）/ Quality grade string</summary>
        [JsonPropertyName("quality_grade")]
        public string? QualityGrade { get; set; }

        /// <summary>公开坐标（v2 以 geojson 承载，[lng, lat]）/ Public coordinates (v2 carries them as geojson, [lng, lat])</summary>
        [JsonPropertyName("geojson")]
        public InatGeojsonDto? Geojson { get; set; }

        /// <summary>公开纬度（v1 兼容字段）/ Public latitude (v1-compatible field)</summary>
        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        /// <summary>公开经度（v1 兼容字段）/ Public longitude (v1-compatible field)</summary>
        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        /// <summary>公开坐标备选形态（"lat,lng" 字符串）/ Public coordinates fallback ("lat,lng" string)</summary>
        [JsonPropertyName("location")]
        public string? Location { get; set; }

        /// <summary>位置精度（米）/ Positional accuracy (metres)</summary>
        [JsonPropertyName("positional_accuracy")]
        public int? PositionalAccuracy { get; set; }

        /// <summary>位置精度备选字段名（v2 命名待实测校正）/ Positional accuracy alternate field name (v2 naming to verify)</summary>
        [JsonPropertyName("public_positional_accuracy")]
        public int? PublicPositionalAccuracy { get; set; }

        /// <summary>坐标保护（open/obscured/private，可空=open）/ Geoprivacy (open/obscured/private; null = open)</summary>
        [JsonPropertyName("geoprivacy")]
        public string? Geoprivacy { get; set; }

        /// <summary>分类坐标保护（需与 geoprivacy 合并取严）/ Taxon geoprivacy (merged with geoprivacy, stricter wins)</summary>
        [JsonPropertyName("taxon_geoprivacy")]
        public string? TaxonGeoprivacy { get; set; }

        /// <summary>鉴定次数 / Identification count</summary>
        [JsonPropertyName("identifications_count")]
        public int? IdentificationsCount { get; set; }

        /// <summary>观察者 / The observer</summary>
        [JsonPropertyName("user")]
        public InatUserDto? User { get; set; }

        /// <summary>分类 / The taxon</summary>
        [JsonPropertyName("taxon")]
        public InatTaxonDto? Taxon { get; set; }

        /// <summary>照片（首图为入库口径）/ Photos (the first one is the ingest basis)</summary>
        [JsonPropertyName("photos")]
        public List<InatPhotoDto>? Photos { get; set; }
    }

    /// <summary>
    /// iNaturalist 用户公开信息 / iNaturalist public user info
    /// </summary>
    public class InatUserDto
    {
        /// <summary>公开昵称 / Public login</summary>
        [JsonPropertyName("login")]
        public string? Login { get; set; }
    }

    /// <summary>
    /// iNaturalist GeoJSON 点（coordinates 顺序 [lng, lat]）/ iNaturalist GeoJSON point (coordinates ordered [lng, lat])
    /// </summary>
    public class InatGeojsonDto
    {
        /// <summary>坐标（[lng, lat]）/ The coordinates ([lng, lat])</summary>
        [JsonPropertyName("coordinates")]
        public List<double>? Coordinates { get; set; }
    }

    /// <summary>
    /// iNaturalist 分类单元（iconic_taxon_id 为源站数字代码，映射层换算本库 1-14 枚举）
    /// iNaturalist taxon (iconic_taxon_id is the source numeric code, translated to our 1-14 enum in the mapper)
    /// </summary>
    public class InatTaxonDto
    {
        /// <summary>分类ID（数字）/ Taxon id (numeric)</summary>
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        /// <summary>学名 / Scientific name</summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>阶元字符串（species/genus/...）/ Rank string</summary>
        [JsonPropertyName("rank")]
        public string? Rank { get; set; }

        /// <summary>源站类群代码（数字≠本库枚举，需映射）/ Source iconic code (differs from our enum; needs mapping)</summary>
        [JsonPropertyName("iconic_taxon_id")]
        public int? IconicTaxonId { get; set; }

        /// <summary>祖先链（分隔符形式实测校正）/ Ancestry chain (separator verified against live responses)</summary>
        [JsonPropertyName("ancestry")]
        public string? Ancestry { get; set; }

        /// <summary>常用名（英文优先）/ Preferred common name</summary>
        [JsonPropertyName("preferred_common_name")]
        public string? PreferredCommonName { get; set; }

        /// <summary>默认照片（v1 形态；v2 响应无此字段，保持可空）/ Default photo (v1 shape; absent in v2 responses, kept nullable)</summary>
        [JsonPropertyName("default_photo")]
        public InatTaxonPhotoDto? DefaultPhoto { get; set; }

        /// <summary>观察计数（v1 wanted 列表语境 = 周边口径；v2 响应无此字段）/ Observation count (nearby scope in the v1 wanted list; absent in v2)</summary>
        [JsonPropertyName("observations_count")]
        public long? ObservationsCount { get; set; }

        /// <summary>是否活跃（false=源站已弃用）/ Whether active (false = deprecated on source)</summary>
        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        /// <summary>受威胁（v2 字段形态待实测）/ Threatened (v2 field shape to verify)</summary>
        [JsonPropertyName("threatened")]
        public bool? Threatened { get; set; }
    }

    /// <summary>
    /// iNaturalist v1 分类默认照片（wanted 列表用；url 为 square 尺寸，中大图靠 URL 尺寸位替换）
    /// iNaturalist v1 taxon default photo (for the wanted list; url is square size, larger sizes derive from the URL size segment)
    /// </summary>
    public class InatTaxonPhotoDto
    {
        /// <summary>方图 URL / Square-size url</summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>授权代码（cc0/cc-by/…，可空=保留所有权利；部分响应缺省）/ License code (null = all rights reserved; absent in some responses)</summary>
        [JsonPropertyName("license_code")]
        public string? LicenseCode { get; set; }

        /// <summary>源站预制署名串（可空；备用字段）/ Pre-rolled attribution string from the source (nullable; reserved)</summary>
        [JsonPropertyName("attribution")]
        public string? Attribution { get; set; }
    }

    /// <summary>
    /// iNaturalist v1 /taxa/{id}/wanted 响应外层（results 为扁平分类对象，实测形态）
    /// iNaturalist v1 /taxa/{id}/wanted response envelope (results are flat taxon objects, shape verified live)
    /// </summary>
    public class InatWantedResponse
    {
        /// <summary>结果集（与 /taxa 同形）/ The result rows (same shape as /taxa)</summary>
        [JsonPropertyName("results")]
        public List<InatTaxonDto>? Results { get; set; }

        /// <summary>命中总数（翻页终止判据；缺失/未返回时为 null，回退按短页判停）/ Total hits — the paging stop condition; null falls back to short-page detection</summary>
        [JsonPropertyName("total_results")]
        public long? TotalResults { get; set; }
    }

    /// <summary>
    /// iNaturalist v1 /observations/histogram 响应（interval=month）。
    /// results 形态宽松（实测为数组，内含 { "YYYY-MM": count } 映射），保留 JsonNode 原样由映射层解析
    /// iNaturalist v1 /observations/histogram response (interval=month). The results shape is
    /// lenient (an array of { "YYYY-MM": count } maps live); kept as raw JsonNode for the mapper
    /// </summary>
    public class InatHistogramResponse
    {
        /// <summary>逐月计数（原始形态，映射层解析）/ Month counts (raw shape, parsed by the mapper)</summary>
        [JsonPropertyName("results")]
        public System.Text.Json.Nodes.JsonNode? Results { get; set; }
    }

    /// <summary>
    /// iNaturalist 照片（url 返回 square 尺寸，中大图靠 URL 尺寸位替换）
    /// iNaturalist photo (url comes in square size; medium/large derived by URL size-segment replacement)
    /// </summary>
    public class InatPhotoDto
    {
        /// <summary>照片UUID / Photo UUID</summary>
        [JsonPropertyName("uuid")]
        public string? Uuid { get; set; }

        /// <summary>square 尺寸 URL / The square-size URL</summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>授权代码（cc0/cc-by/...，可空=保留所有权利）/ License code (null = all rights reserved)</summary>
        [JsonPropertyName("license_code")]
        public string? LicenseCode { get; set; }
    }

    /// <summary>
    /// iNaturalist v2 /taxa 响应外层 / iNaturalist v2 /taxa response envelope
    /// </summary>
    public class InatTaxaResponse
    {
        /// <summary>结果集 / The result rows</summary>
        [JsonPropertyName("results")]
        public List<InatTaxonDto>? Results { get; set; }
    }
}
