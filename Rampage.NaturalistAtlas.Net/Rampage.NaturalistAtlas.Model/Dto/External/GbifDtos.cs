using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Rampage.NaturalistAtlas.Model.Dto.External
{
    /// <summary>
    /// GBIF v1 /occurrence/search 响应外层（count + offset 分页，endOfRecords 判终止）
    /// GBIF v1 /occurrence/search envelope (count + offset paging; endOfRecords stops the walk)
    /// </summary>
    public class GbifSearchResponse
    {
        /// <summary>命中总数 / Total hits</summary>
        [JsonPropertyName("count")]
        public long? Count { get; set; }

        /// <summary>是否已到末页 / Whether the last page was reached</summary>
        [JsonPropertyName("endOfRecords")]
        public bool? EndOfRecords { get; set; }

        /// <summary>结果集 / The result rows</summary>
        [JsonPropertyName("results")]
        public List<GbifOccurrenceDto>? Results { get; set; }
    }

    /// <summary>
    /// GBIF occurrence 记录（eventDate 为字符串：年/月/日/区间多种形态，映射层归一化为双口径）
    /// GBIF occurrence row (eventDate arrives as year/month/day/range strings; normalized to the dual basis in the mapper)
    /// </summary>
    public class GbifOccurrenceDto
    {
        /// <summary>occurrence key（数字）/ The occurrence key (numeric)</summary>
        [JsonPropertyName("key")]
        public long? Key { get; set; }

        /// <summary>数据集UUID（iNat 转发跳过锚点）/ Dataset UUID (anchor for skipping iNat republishes)</summary>
        [JsonPropertyName("datasetKey")]
        public string? DatasetKey { get; set; }

        /// <summary>数据集名称 / Dataset title</summary>
        [JsonPropertyName("datasetTitle")]
        public string? DatasetTitle { get; set; }

        /// <summary>记录类型（HUMAN_OBSERVATION/MACHINE_OBSERVATION/PRESERVED_SPECIMEN）/ Basis of record</summary>
        [JsonPropertyName("basisOfRecord")]
        public string? BasisOfRecord { get; set; }

        /// <summary>存在状态（过滤基线 PRESENT）/ Occurrence status (PRESENT is the filter baseline)</summary>
        [JsonPropertyName("occurrenceStatus")]
        public string? OccurrenceStatus { get; set; }

        /// <summary>观察时间（多形态字符串）/ Event date (multi-shape string)</summary>
        [JsonPropertyName("eventDate")]
        public string? EventDate { get; set; }

        /// <summary>记录人（可多人）/ Recorder (may list several)</summary>
        [JsonPropertyName("recordedBy")]
        public string? RecordedBy { get; set; }

        /// <summary>纬度 / Latitude</summary>
        [JsonPropertyName("decimalLatitude")]
        public double? DecimalLatitude { get; set; }

        /// <summary>经度 / Longitude</summary>
        [JsonPropertyName("decimalLongitude")]
        public double? DecimalLongitude { get; set; }

        /// <summary>坐标不确定性（米，源站可能带小数）/ Coordinate uncertainty (metres; the source may emit decimals)</summary>
        [JsonPropertyName("coordinateUncertaintyInMeters")]
        public double? CoordinateUncertaintyInMeters { get; set; }

        /// <summary>媒体集合 / The media list</summary>
        [JsonPropertyName("media")]
        public List<GbifMediaDto>? Media { get; set; }

        /// <summary>分类key / Accepted taxon key</summary>
        [JsonPropertyName("taxonKey")]
        public int? TaxonKey { get; set; }

        /// <summary>学名 / Scientific name</summary>
        [JsonPropertyName("scientificName")]
        public string? ScientificName { get; set; }

        /// <summary>常用名 / Vernacular name</summary>
        [JsonPropertyName("vernacularName")]
        public string? VernacularName { get; set; }

        /// <summary>阶元字符串（SPECIES/GENUS/...）/ Rank string</summary>
        [JsonPropertyName("taxonRank")]
        public string? TaxonRank { get; set; }

        /// <summary>界名 / Kingdom name</summary>
        [JsonPropertyName("kingdom")]
        public string? Kingdom { get; set; }

        /// <summary>界key / Kingdom key</summary>
        [JsonPropertyName("kingdomKey")]
        public int? KingdomKey { get; set; }

        /// <summary>门名 / Phylum name</summary>
        [JsonPropertyName("phylum")]
        public string? Phylum { get; set; }

        /// <summary>门key / Phylum key</summary>
        [JsonPropertyName("phylumKey")]
        public int? PhylumKey { get; set; }

        /// <summary>纲名 / Class name</summary>
        [JsonPropertyName("class")]
        public string? ClassName { get; set; }

        /// <summary>纲key / Class key</summary>
        [JsonPropertyName("classKey")]
        public int? ClassKey { get; set; }

        /// <summary>目名 / Order name</summary>
        [JsonPropertyName("order")]
        public string? Order { get; set; }

        /// <summary>目key / Order key</summary>
        [JsonPropertyName("orderKey")]
        public int? OrderKey { get; set; }

        /// <summary>科名 / Family name</summary>
        [JsonPropertyName("family")]
        public string? Family { get; set; }

        /// <summary>科key / Family key</summary>
        [JsonPropertyName("familyKey")]
        public int? FamilyKey { get; set; }

        /// <summary>属名 / Genus name</summary>
        [JsonPropertyName("genus")]
        public string? Genus { get; set; }

        /// <summary>属key / Genus key</summary>
        [JsonPropertyName("genusKey")]
        public int? GenusKey { get; set; }

        /// <summary>种名 / Species name</summary>
        [JsonPropertyName("species")]
        public string? Species { get; set; }

        /// <summary>种key / Species key</summary>
        [JsonPropertyName("speciesKey")]
        public int? SpeciesKey { get; set; }
    }

    /// <summary>
    /// GBIF 媒体项（type=StillImage 为入库口径，license 为 CC_BY_NC_4_0 风格枚举串）
    /// GBIF media item (type = StillImage is the ingest basis; license is a CC_BY_NC_4_0-style enum string)
    /// </summary>
    public class GbifMediaDto
    {
        /// <summary>媒体地址 / The media URL</summary>
        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }

        /// <summary>媒体类型（StillImage/MovingImage/Sound）/ Media type</summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>授权枚举串 / License enum string</summary>
        [JsonPropertyName("license")]
        public string? License { get; set; }
    }

    /// <summary>
    /// GBIF /species/{key} 记录（分类刷新：iucnRedListCategory 判受威胁）
    /// GBIF /species/{key} row (taxon refresh; iucnRedListCategory decides threatened)
    /// </summary>
    public class GbifSpeciesDto
    {
        /// <summary>分类key / Taxon key</summary>
        [JsonPropertyName("taxonKey")]
        public int? TaxonKey { get; set; }

        /// <summary>学名 / Scientific name</summary>
        [JsonPropertyName("scientificName")]
        public string? ScientificName { get; set; }

        /// <summary>常用名 / Vernacular name</summary>
        [JsonPropertyName("vernacularName")]
        public string? VernacularName { get; set; }

        /// <summary>阶元字符串 / Rank string</summary>
        [JsonPropertyName("rank")]
        public string? Rank { get; set; }

        /// <summary>界名 / Kingdom name</summary>
        [JsonPropertyName("kingdom")]
        public string? Kingdom { get; set; }

        /// <summary>IUCN 红色名录等级（LC/NT/VU/EN/CR/DD...）/ IUCN red-list category</summary>
        [JsonPropertyName("iucnRedListCategory")]
        public string? IucnRedListCategory { get; set; }
    }
}
