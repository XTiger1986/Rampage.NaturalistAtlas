using Rampage.Enums;
using Rampage.NaturalistAtlas.Model.Dto.External;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Globalization;

namespace Rampage.NaturalistAtlas.Collection
{
    /// <summary>
    /// iNaturalist v2 响应 → 本库 Po 映射：纯函数静态类，全部枚举换算与字段归一化收口于此
    /// iNaturalist v2 response → our Po mapping: a static pure-function class holding every enum translation and normalization
    /// </summary>
    public static class InatMapper
    {
        /// <summary>
        /// 源站 iconic_taxon_id → 本库类群枚举（源站数字≠本库代码；映射表经 /v1/taxa/iconic 权威清单核对）
        /// Source iconic_taxon_id → our iconic enum (source numbers differ from our codes; verified against the authoritative /v1/taxa/iconic list)
        /// </summary>
        private static readonly Dictionary<int, IconicTaxon> IconicMap = new Dictionary<int, IconicTaxon>
        {
            { 3, IconicTaxon.Birds },
            { 40151, IconicTaxon.Mammals },
            { 20978, IconicTaxon.Amphibians },
            { 26036, IconicTaxon.Reptiles },
            { 47178, IconicTaxon.RayFinnedFishes },
            { 47158, IconicTaxon.Insects },
            { 47119, IconicTaxon.Arachnids },
            { 47115, IconicTaxon.Mollusks },
            { 47126, IconicTaxon.Plants },
            { 47170, IconicTaxon.Fungi },
            { 1, IconicTaxon.OtherAnimals },
            { 47686, IconicTaxon.Protozoans },
            { 48222, IconicTaxon.Chromista }
        };

        /// <summary>
        /// 13 个 iconic 类群的源站分类 id（集邮缺口 wanted 按祖先分类逐群拉取）
        /// Source taxon ids of the 13 iconic groups (collection-gap wanted fetches per ancestor)
        /// </summary>
        public static readonly int[] IconicSourceTaxonIds = IconicMap.Keys.ToArray();

        /// <summary>
        /// 源站 license_code → 本库授权枚举（源站无 PDM）/ Source license_code → our license enum (no PDM on this source)
        /// </summary>
        private static readonly Dictionary<string, AtlasLicense> LicenseMap = new Dictionary<string, AtlasLicense>
        {
            ["cc0"] = AtlasLicense.Cc0,
            ["cc-by"] = AtlasLicense.CcBy,
            ["cc-by-sa"] = AtlasLicense.CcBySa,
            ["cc-by-nc"] = AtlasLicense.CcByNc,
            ["cc-by-nc-sa"] = AtlasLicense.CcByNcSa,
            ["cc-by-nd"] = AtlasLicense.CcByNd,
            ["cc-by-nc-nd"] = AtlasLicense.CcByNcNd
        };

        /// <summary>
        /// 观察记录映射：不满足入库口径返回 null（计 fail_count）
        /// Map an observation: returns null when it fails the ingest baseline (counted as fail)
        /// </summary>
        /// <param name="dto">源站记录 / The source row</param>
        /// <param name="cityId">归属城市 / The owning city</param>
        /// <param name="now">本批入库时间（UTC）/ The batch ingest time (UTC)</param>
        /// <returns>本库观察行（TaxonId 由编排层回填）/ Our observation row (TaxonId filled by the orchestrator)</returns>
        public static Observation? MapObservation(InatObservationDto dto, int cityId, DateTime now)
        {
            // 口径：有图、有公开坐标、可解析分类、非私密、可解析观察日期与鉴定级别
            (double? lng, double? lat) = ResolveCoordinates(dto);
            if (dto.Id == null || dto.Taxon?.Id == null
                || dto.Photos == null || dto.Photos.Count == 0
                || lng == null || lat == null)
            {
                return null;
            }

            GeoPrivacy geoPrivacy = MergeGeoprivacy(dto.Geoprivacy, dto.TaxonGeoprivacy);
            if (geoPrivacy == GeoPrivacy.Private)
            {
                return null;
            }

            // 鉴定级别须为三个合法值之一（采集口径 research/needs_id 由查询参数控制）
            // The quality grade must be one of the three legal values (the research/needs_id scope is driven by query params)
            if (dto.QualityGrade != "research" && dto.QualityGrade != "needs_id" && dto.QualityGrade != "casual")
            {
                return null;
            }
            QualityGrade qualityGrade = MapQualityGrade(dto.QualityGrade);

            DateOnly observedOn = ParseLocalDate(dto.ObservedOn);
            if (observedOn == default)
            {
                return null;
            }

            string squareUrl = dto.Photos[0].Url ?? string.Empty;

            return new Observation
            {
                Id = string.Empty,
                Source = AtlasSource.INaturalist,
                SourceObservationId = dto.Id.Value.ToString(CultureInfo.InvariantCulture),
                CityId = cityId,
                TaxonId = null,
                IconicTaxon = MapIconic(dto.Taxon.IconicTaxonId),
                QualityGrade = qualityGrade,
                ObservedOn = observedOn,
                ObservedAt = ParseUtcDateTime(dto.TimeObservedAt) ?? ParseUtcDateTime(dto.TimeObservedAtUtc),
                Lng = Math.Round((decimal)lng.Value, 6),
                Lat = Math.Round((decimal)lat.Value, 6),
                PositionalAccuracy = dto.PositionalAccuracy ?? dto.PublicPositionalAccuracy,
                Geoprivacy = geoPrivacy,
                PhotoSquareUrl = squareUrl,
                PhotoMediumUrl = ReplaceSizeSegment(squareUrl, "medium"),
                PhotoLargeUrl = ReplaceSizeSegment(squareUrl, "large"),
                License = MapLicense(dto.Photos[0].LicenseCode),
                UserLogin = dto.User?.Login ?? string.Empty,
                RecordBasis = RecordBasis.HumanObservation,
                DatasetKey = null,
                DatasetTitle = null,
                IdentificationsCount = dto.IdentificationsCount ?? 0,
                SourceCreatedAt = ParseUtcDateTime(dto.CreatedAt),
                SourceUpdatedAt = ParseUtcDateTime(dto.UpdatedAt),
                Status = ObservationStatus.Normal,
                CreateTime = now,
                UpdateTime = now
            };
        }

        /// <summary>
        /// 公开坐标解析：geojson（v2 主形态，[lng, lat]）→ latitude/longitude（v1 兼容）→ location 串（"lat,lng"）
        /// Public coordinate resolution: geojson (the v2 main shape, [lng, lat]) → latitude/longitude (v1-compatible) → location string ("lat,lng")
        /// </summary>
        public static (double? Lng, double? Lat) ResolveCoordinates(InatObservationDto dto)
        {
            if (dto.Geojson?.Coordinates != null && dto.Geojson.Coordinates.Count >= 2)
            {
                return (dto.Geojson.Coordinates[0], dto.Geojson.Coordinates[1]);
            }
            if (dto.Latitude != null && dto.Longitude != null)
            {
                return (dto.Longitude, dto.Latitude);
            }
            if (!string.IsNullOrEmpty(dto.Location))
            {
                string[] parts = dto.Location.Split(',');
                if (parts.Length == 2
                    && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat)
                    && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lng))
                {
                    return (lng, lat);
                }
            }
            return (null, null);
        }

        /// <summary>
        /// 分类映射（Id 由编排层赋雪花）/ Map a taxon (the Id gets a snowflake from the orchestrator)
        /// </summary>
        /// <param name="dto">源站分类 / The source taxon</param>
        /// <param name="now">本批时间（UTC）/ The batch time (UTC)</param>
        /// <returns>本库分类行 / Our taxon row</returns>
        public static Taxon MapTaxon(InatTaxonDto dto, DateTime now)
        {
            return new Taxon
            {
                Id = string.Empty,
                Source = AtlasSource.INaturalist,
                SourceTaxonId = (dto.Id ?? 0).ToString(CultureInfo.InvariantCulture),
                Rank = MapRank(dto.Rank),
                ScientificName = dto.Name ?? string.Empty,
                // 中文名首采留空：源站 preferred_common_name 是英文名，塞进 CN 字段会被刷新逻辑
                // 永久保留、名不副实；中文界面经 speciesName 回退链显示英文名，CN 名留给翻译管线回填
                // Chinese name starts empty: the source preferred_common_name is English, and storing it
                // in the CN column would be frozen forever by the refresh preserve rule; the Chinese UI
                // falls back to the English name via speciesName, and CN names await a translation backfill
                CommonNameCn = string.Empty,
                CommonNameEn = dto.PreferredCommonName ?? string.Empty,
                IconicTaxon = MapIconic(dto.IconicTaxonId),
                Threatened = dto.Threatened == true,
                Ancestry = dto.Ancestry?.Replace('/', '-') ?? string.Empty,
                Status = dto.IsActive == false ? CommonStatus.Disabled : CommonStatus.Enabled,
                CreateTime = now,
                UpdateTime = now
            };
        }

        /// <summary>源站类群代码换算，无匹配归未知 / Translate the source iconic code; unmatched falls back to Unknown</summary>
        public static IconicTaxon MapIconic(int? iconicTaxonId)
        {
            if (iconicTaxonId != null && IconicMap.TryGetValue(iconicTaxonId.Value, out IconicTaxon iconic))
            {
                return iconic;
            }
            return IconicTaxon.Unknown;
        }

        /// <summary>鉴定级别字符串换算，未知值按 casual 预留 / Translate the quality-grade string; unknown values fold into the reserved Casual</summary>
        public static QualityGrade MapQualityGrade(string? qualityGrade)
        {
            return qualityGrade switch
            {
                "research" => QualityGrade.Research,
                "needs_id" => QualityGrade.NeedsId,
                "casual" => QualityGrade.Casual,
                _ => QualityGrade.Casual
            };
        }

        /// <summary>授权代码换算，未知/空为保留所有权利 / Translate the license code; unknown or null means all rights reserved</summary>
        public static AtlasLicense MapLicense(string? licenseCode)
        {
            if (licenseCode != null && LicenseMap.TryGetValue(licenseCode, out AtlasLicense license))
            {
                return license;
            }
            return AtlasLicense.AllRightsReserved;
        }

        /// <summary>阶元字符串换算（属间杂交并入杂交）/ Translate the rank string (genus hybrids fold into Hybrid)</summary>
        public static TaxonRank MapRank(string? rank)
        {
            return rank switch
            {
                "kingdom" => TaxonRank.Kingdom,
                "phylum" => TaxonRank.Phylum,
                "class" => TaxonRank.Class,
                "order" => TaxonRank.Order,
                "family" => TaxonRank.Family,
                "genus" => TaxonRank.Genus,
                "species" => TaxonRank.Species,
                "subspecies" => TaxonRank.Subspecies,
                "variety" => TaxonRank.Variety,
                "form" => TaxonRank.Form,
                "hybrid" => TaxonRank.Hybrid,
                "genushybrid" => TaxonRank.Hybrid,
                _ => TaxonRank.Unknown
            };
        }

        /// <summary>坐标保护合并取严：任一 private 即 private，任一 obscured 即 obscured / Merge geoprivacy, strictest wins</summary>
        public static GeoPrivacy MergeGeoprivacy(string? geoprivacy, string? taxonGeoprivacy)
        {
            GeoPrivacy first = ParseGeoprivacy(geoprivacy);
            GeoPrivacy second = ParseGeoprivacy(taxonGeoprivacy);
            return first >= second ? first : second;
        }

        private static GeoPrivacy ParseGeoprivacy(string? value)
        {
            return value switch
            {
                "obscured" => GeoPrivacy.Obscured,
                "private" => GeoPrivacy.Private,
                _ => GeoPrivacy.Open
            };
        }

        /// <summary>photo URL 尺寸位替换（square → medium/large）/ Photo URL size-segment replacement (square → medium/large)</summary>
        public static string ReplaceSizeSegment(string squareUrl, string size)
        {
            int index = squareUrl.LastIndexOf("/square.", StringComparison.OrdinalIgnoreCase);
            return index < 0 ? squareUrl : squareUrl.Substring(0, index) + "/" + size + squareUrl.Substring(index + "/square.".Length - 1);
        }

        /// <summary>观察者本地日期解析（取串首日期段）/ Parse the observer-local date (the leading date segment of the string)</summary>
        public static DateOnly ParseLocalDate(string? value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 10)
            {
                return default;
            }
            return DateOnly.TryParse(value.Substring(0, 10), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date) ? date : default;
        }

        /// <summary>带偏移 ISO 串解析为 UTC（不可解析返回 null）/ Parse an offset ISO string to UTC (null when unparseable)</summary>
        public static DateTime? ParseUtcDateTime(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsed))
            {
                DateTime utc = parsed.UtcDateTime;
                return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            }
            return null;
        }
    }
}
