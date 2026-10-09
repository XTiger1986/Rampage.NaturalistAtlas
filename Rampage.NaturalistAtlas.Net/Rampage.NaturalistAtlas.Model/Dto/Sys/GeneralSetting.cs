using Rampage.Validation;
using System;

namespace Rampage.NaturalistAtlas.Model.Dto
{
    /// <summary>
    /// 常规设置（setting 表 JSON 配置的强类型载体） / General settings (strongly-typed carrier of the setting-table JSON payload)
    /// </summary>
    public class GeneralSetting
    {

        /// <summary>
        /// 网站名称 / Site name
        /// </summary>
        [NotWhiteSpaceString]
        public string SiteName { get; set; } = "自然地图志";

        /// <summary>
        /// 头部 Meta 关键词 / Meta keywords in head
        /// </summary>
        [StringLength(Min = 2, Max = 200)]
        public string? SiteKeywords { get; set; }

        /// <summary>
        /// 头部 Meta 描述 / Meta description in head
        /// </summary>
        [StringLength(Min = 2, Max = 200)]
        public string? SiteDescription { get; set; }

        /// <summary>
        /// 版权所有HTML(中文) / Copyright HTML (Chinese)
        /// </summary>
        [StringLength(Min = 2, Max = 1000)]
        public string? Copyright { get; set; }

        /// <summary>
        /// 版权所有HTML(英文) / Copyright HTML (English)
        /// </summary>
        [StringLength(Min = 2, Max = 1000)]
        public string? CopyrightEnglish { get; set; }

        /// <summary>
        /// 备案信息HTML / ICP filing HTML
        /// </summary>
        [StringLength(Min = 2, Max = 2000)]
        public string? InternetInformation { get; set; }

        /// <summary>
        /// 默认城市ID（主库 city 表 SERIAL 主键；MVP 单城市北京种子 id=1）
        /// Default city id (SERIAL pk of the city table in the main DB; MVP single-city Beijing seed id=1)
        /// </summary>
        [IntegerRange(MinValue = 1, MaxValue = 9999)]
        public int DefaultCityId { get; set; } = 1;

        /// <summary>
        /// 隐藏观察内容的前端缓存 TTL（秒）。运营隐藏观察后，主站须在该时限内不可见（产品要求默认 300 秒）
        /// Front-end cache TTL in seconds for hidden observations. After an operator hides one, the main site must stop showing it within this window (product default: 300s)
        /// </summary>
        [IntegerRange(MinValue = 0, MaxValue = 3600)]
        public int HiddenObservationCacheTtlSeconds { get; set; } = 300;

        /// <summary>
        /// iNaturalist v2 API 基址（采集端数据源配置，Collector 读取）
        /// iNaturalist v2 API base url (data-source config consumed by the Collector)
        /// </summary>
        [UrlFormat]
        public string InatApiBaseUrl { get; set; } = "https://api.inaturalist.org/v2";

        /// <summary>
        /// iNaturalist v1 API 基址（仅集邮缺口的 wanted 端点在 v1，v2 不提供）
        /// iNaturalist v1 API base url (only the wanted endpoint used by collection gaps lives on v1; v2 does not provide it)
        /// </summary>
        [UrlFormat]
        public string InatV1ApiBaseUrl { get; set; } = "https://api.inaturalist.org/v1";

        /// <summary>
        /// 采集端对 iNaturalist 的节流上限（次/分钟，官方硬限 60，内置节流建议 30）
        /// Collector throttle cap against iNaturalist (requests/minute; official hard limit 60, built-in throttle suggests 30)
        /// </summary>
        [IntegerRange(MinValue = 1, MaxValue = 60)]
        public int InatRateLimitPerMinute { get; set; } = 30;

        /// <summary>
        /// GBIF Occurrence API 基址（双源并行采集的第二数据源，Collector 读取）
        /// GBIF Occurrence API base url (the second collection source, consumed by the Collector)
        /// </summary>
        [UrlFormat]
        public string GbifApiBaseUrl { get; set; } = "https://api.gbif.org/v1";

        /// <summary>
        /// 采集端对 GBIF 的节流上限（次/分钟，GBIF 无硬性限额但建议克制）
        /// Collector throttle cap against GBIF (requests/minute; GBIF has no hard limit but keep it moderate)
        /// </summary>
        [IntegerRange(MinValue = 1, MaxValue = 120)]
        public int GbifRateLimitPerMinute { get; set; } = 60;

        /// <summary>
        /// 采集城市ID清单（逗号分隔，MVP 默认北京；增量/回补/对账按此迭代）。
        /// 上限按全量城市估算：362 城 ≈ 1500 字符（每 id 约 4 字符 + 逗号），留足增长余量取 4000
        /// Collection city ids (comma separated; Beijing by default for the MVP; increment/backfill/deletion
        /// iterate these). The cap sizes for the full city set: 362 cities ≈ 1,500 chars (≈4 chars per id
        /// plus commas), so 4,000 leaves ample growth room
        /// </summary>
        [StringLength(Min = 1, Max = 4000)]
        public string CollectionCityIds { get; set; } = "1";

    }
}
