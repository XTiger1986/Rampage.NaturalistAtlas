using Rampage.Enums;
using Rampage.NaturalistAtlas.Model.Dto.External;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Globalization;

namespace Rampage.NaturalistAtlas.Collection
{
    /// <summary>
    /// GBIF v1 响应 → 本库 Po 映射：纯函数静态类，iconic 按阶元推断、eventDate 双口径归一化、授权枚举换算收口于此
    /// GBIF v1 response → our Po mapping: a static pure-function class holding the rank-based iconic inference,
    /// the eventDate dual-basis normalization and the license translation
    /// </summary>
    public static class GbifMapper
    {
        /// <summary>iNaturalist 转发数据集 key（采集时跳过，避免与 iNat 源重复）/ The iNaturalist republish dataset key (skipped at ingest to avoid duplicating the iNat source)</summary>
        public const string InatRepublishDatasetKey = "50c9509d-22c7-4a22-a47d-8c48425ef4a7";

        /// <summary>IUCN 受威胁等级集合（保守口径）/ Threatened IUCN categories (conservative scope)</summary>
        private static readonly HashSet<string> ThreatenedIucnCategories = new HashSet<string> { "VU", "EN", "CR" };

        /// <summary>纲名 → 类群（判不了的按界回退）/ Class name → iconic (falls back by kingdom when unmatched)</summary>
        private static readonly Dictionary<string, IconicTaxon> ClassIconicMap = new Dictionary<string, IconicTaxon>
        {
            ["Aves"] = IconicTaxon.Birds,
            ["Mammalia"] = IconicTaxon.Mammals,
            ["Amphibia"] = IconicTaxon.Amphibians,
            ["Reptilia"] = IconicTaxon.Reptiles,
            ["Actinopterygii"] = IconicTaxon.RayFinnedFishes,
            ["Insecta"] = IconicTaxon.Insects,
            ["Arachnida"] = IconicTaxon.Arachnids,
            ["Mollusca"] = IconicTaxon.Mollusks
        };

        /// <summary>界名 → 类群 / Kingdom name → iconic</summary>
        private static readonly Dictionary<string, IconicTaxon> KingdomIconicMap = new Dictionary<string, IconicTaxon>
        {
            ["Plantae"] = IconicTaxon.Plants,
            ["Fungi"] = IconicTaxon.Fungi,
            ["Animalia"] = IconicTaxon.OtherAnimals,
            ["Protozoa"] = IconicTaxon.Protozoans,
            ["Chromista"] = IconicTaxon.Chromista,
            ["Bacteria"] = IconicTaxon.Unknown
        };

        /// <summary>
        /// occurrence 记录映射：不满足入库口径返回 null（计 fail_count）
        /// Map an occurrence: returns null when it fails the ingest baseline (counted as fail)
        /// </summary>
        /// <param name="dto">源站记录 / The source row</param>
        /// <param name="cityId">归属城市 / The owning city</param>
        /// <param name="now">本批入库时间（UTC）/ The batch ingest time (UTC)</param>
        /// <returns>本库观察行（TaxonId 由编排层回填）/ Our observation row (TaxonId filled by the orchestrator)</returns>
        public static Observation? MapObservation(GbifOccurrenceDto dto, int cityId, DateTime now)
        {
            // 基线：PRESENT、有坐标、有静图、可解析分类与观察时间；iNat 转发跳过
            if (dto.Key == null || dto.TaxonKey == null
                || dto.DecimalLatitude == null || dto.DecimalLongitude == null
                || dto.OccurrenceStatus != "PRESENT")
            {
                return null;
            }
            if (string.Equals(dto.DatasetKey, InatRepublishDatasetKey, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            GbifMediaDto? photo = FindStillImage(dto.Media);
            if (photo == null)
            {
                return null;
            }

            (DateOnly observedOn, DateTime? observedAt) = ParseEventDate(dto.EventDate);
            if (observedOn == default)
            {
                return null;
            }

            return new Observation
            {
                Id = string.Empty,
                Source = AtlasSource.Gbif,
                SourceObservationId = dto.Key.Value.ToString(CultureInfo.InvariantCulture),
                CityId = cityId,
                TaxonId = null,
                IconicTaxon = InferIconic(dto),
                QualityGrade = QualityGrade.Research,
                ObservedOn = observedOn,
                ObservedAt = observedAt,
                Lng = Math.Round((decimal)dto.DecimalLongitude.Value, 6),
                Lat = Math.Round((decimal)dto.DecimalLatitude.Value, 6),
                PositionalAccuracy = dto.CoordinateUncertaintyInMeters.HasValue
                    ? (int)Math.Round(dto.CoordinateUncertaintyInMeters.Value, MidpointRounding.AwayFromZero)
                    : null,
                Geoprivacy = GeoPrivacy.Open,
                PhotoSquareUrl = photo.Identifier ?? string.Empty,
                PhotoMediumUrl = photo.Identifier ?? string.Empty,
                PhotoLargeUrl = photo.Identifier ?? string.Empty,
                License = MapLicense(photo.License),
                UserLogin = dto.RecordedBy ?? string.Empty,
                RecordBasis = MapRecordBasis(dto.BasisOfRecord),
                DatasetKey = dto.DatasetKey,
                DatasetTitle = dto.DatasetTitle,
                IdentificationsCount = 0,
                SourceCreatedAt = null,
                SourceUpdatedAt = null,
                Status = ObservationStatus.Normal,
                CreateTime = now,
                UpdateTime = now
            };
        }

        /// <summary>
        /// occurrence 行内嵌分类映射（find-or-create；Id 由编排层赋雪花）
        /// Map the taxon embedded in an occurrence row (find-or-create; the Id gets a snowflake from the orchestrator)
        /// </summary>
        /// <param name="dto">源站记录 / The source row</param>
        /// <param name="now">本批时间（UTC）/ The batch time (UTC)</param>
        /// <returns>本库分类行 / Our taxon row</returns>
        public static Taxon MapTaxon(GbifOccurrenceDto dto, DateTime now)
        {
            return new Taxon
            {
                Id = string.Empty,
                Source = AtlasSource.Gbif,
                SourceTaxonId = (dto.TaxonKey ?? 0).ToString(CultureInfo.InvariantCulture),
                Rank = MapRank(dto.TaxonRank),
                ScientificName = dto.ScientificName ?? string.Empty,
                // 中文名首采留空（口径与 iNat 采集一致）：源站 vernacularName 为英文俗名，
                // CN 名留给翻译管线/人工后台回填，避免英文名冒充中文名被刷新逻辑永久固化
                // Chinese name starts empty (same rule as the iNat collector): the source
                // vernacularName is an English common name; CN names await backfill so an English
                // stand-in never gets frozen in by the refresh preserve rule
                CommonNameCn = string.Empty,
                CommonNameEn = dto.VernacularName ?? string.Empty,
                IconicTaxon = InferIconic(dto),
                Threatened = false,
                Ancestry = BuildAncestry(dto),
                Status = CommonStatus.Enabled,
                CreateTime = now,
                UpdateTime = now
            };
        }

        /// <summary>
        /// 分类档案刷新映射（iucnRedListCategory 判受威胁）
        /// Map the species profile for the taxon refresh (iucnRedListCategory decides threatened)
        /// </summary>
        /// <param name="dto">源站档案 / The source profile</param>
        /// <param name="existing">已存在的本库行（保留 Id 与中文名）/ Our existing row (Id and the Chinese name preserved)</param>
        /// <param name="now">本批时间（UTC）/ The batch time (UTC)</param>
        /// <returns>刷新后的分类行 / The refreshed row</returns>
        public static Taxon MapTaxonRefresh(GbifSpeciesDto dto, Taxon existing, DateTime now)
        {
            existing.ScientificName = dto.ScientificName ?? existing.ScientificName;
            if (!string.IsNullOrEmpty(dto.VernacularName))
            {
                existing.CommonNameEn = dto.VernacularName;
            }
            if (dto.Rank != null)
            {
                existing.Rank = MapRank(dto.Rank);
            }
            if (!string.IsNullOrEmpty(dto.Kingdom))
            {
                // 仅当原本推断失准时重判（避免覆盖更精确的已有归类）
                if (KingdomIconicMap.TryGetValue(dto.Kingdom, out IconicTaxon kingdomIconic) && existing.IconicTaxon == IconicTaxon.Unknown)
                {
                    existing.IconicTaxon = kingdomIconic;
                }
            }
            existing.Threatened = dto.IucnRedListCategory != null && ThreatenedIucnCategories.Contains(dto.IucnRedListCategory);
            existing.Status = CommonStatus.Enabled;
            existing.UpdateTime = now;
            return existing;
        }

        /// <summary>iNat 对账用：该记录是否为 iNat 转发 / Deletion-reconcile helper: whether this row is an iNat republish</summary>
        public static bool IsInatRepublish(string? datasetKey)
        {
            return string.Equals(datasetKey, InatRepublishDatasetKey, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 授权换算：兼容枚举串（CC_BY_NC_4_0）与 URI（creativecommons.org/licenses/by-nc/4.0）两种形态，
        /// 归一化后按最长令牌优先匹配
        /// License translation: accepts both the enum form (CC_BY_NC_4_0) and the URI form
        /// (creativecommons.org/licenses/by-nc/4.0); normalized, longest token wins
        /// </summary>
        public static AtlasLicense MapLicense(string? license)
        {
            if (string.IsNullOrEmpty(license))
            {
                return AtlasLicense.AllRightsReserved;
            }
            string s = license.ToUpperInvariant().Replace('-', '_');
            if (s.Contains("ZERO")) { return AtlasLicense.Cc0; }
            if (s.Contains("CC0")) { return AtlasLicense.Cc0; }
            if (s.Contains("MARK")) { return AtlasLicense.Pdm; }
            if (s.Contains("PDM")) { return AtlasLicense.Pdm; }
            if (s.Contains("BY_NC_SA") || s.Contains("BY_NCSA")) { return AtlasLicense.CcByNcSa; }
            if (s.Contains("BY_NC_ND") || s.Contains("BY_NCND")) { return AtlasLicense.CcByNcNd; }
            if (s.Contains("BY_SA")) { return AtlasLicense.CcBySa; }
            if (s.Contains("BY_ND")) { return AtlasLicense.CcByNd; }
            if (s.Contains("BY_NC")) { return AtlasLicense.CcByNc; }
            if (s.Contains("BY")) { return AtlasLicense.CcBy; }
            return AtlasLicense.AllRightsReserved;
        }

        /// <summary>记录类型换算（可空：未知留空）/ Translate the basis of record (nullable: unknown stays null)</summary>
        public static RecordBasis? MapRecordBasis(string? basisOfRecord)
        {
            return basisOfRecord switch
            {
                "HUMAN_OBSERVATION" => RecordBasis.HumanObservation,
                "MACHINE_OBSERVATION" => RecordBasis.MachineObservation,
                "PRESERVED_SPECIMEN" => RecordBasis.Specimen,
                "FOSSIL_SPECIMEN" => RecordBasis.Specimen,
                "LIVING_SPECIMEN" => RecordBasis.Specimen,
                _ => null
            };
        }

        /// <summary>阶元字符串换算（大写形态）/ Translate the rank string (uppercase form)</summary>
        public static TaxonRank MapRank(string? rank)
        {
            return rank?.ToUpperInvariant() switch
            {
                "KINGDOM" => TaxonRank.Kingdom,
                "PHYLUM" => TaxonRank.Phylum,
                "CLASS" => TaxonRank.Class,
                "ORDER" => TaxonRank.Order,
                "FAMILY" => TaxonRank.Family,
                "GENUS" => TaxonRank.Genus,
                "SPECIES" => TaxonRank.Species,
                "SUBSPECIES" => TaxonRank.Subspecies,
                "VARIETY" => TaxonRank.Variety,
                "FORM" => TaxonRank.Form,
                "HYBRID" => TaxonRank.Hybrid,
                _ => TaxonRank.Unknown
            };
        }

        /// <summary>类群推断：先纲后界，均无匹配归未知 / Iconic inference: class first, then kingdom, else Unknown</summary>
        public static IconicTaxon InferIconic(GbifOccurrenceDto dto)
        {
            if (dto.ClassName != null && ClassIconicMap.TryGetValue(dto.ClassName, out IconicTaxon byClass))
            {
                return byClass;
            }
            if (dto.Kingdom != null && KingdomIconicMap.TryGetValue(dto.Kingdom, out IconicTaxon byKingdom))
            {
                return byKingdom;
            }
            return IconicTaxon.Unknown;
        }

        /// <summary>取首张带地址的静图 / Take the first still image with a URL</summary>
        public static GbifMediaDto? FindStillImage(List<GbifMediaDto>? media)
        {
            if (media == null)
            {
                return null;
            }
            foreach (GbifMediaDto item in media)
            {
                if (item.Type == "StillImage" && !string.IsNullOrEmpty(item.Identifier))
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// eventDate 归一化（年/月/日/区间多形态）：区间取前段；日期粒度填初值；带时刻才给 ObservedAt
        /// Normalize eventDate (year/month/day/range shapes): ranges use the leading segment; coarse granularity
        /// fills the initial day; ObservedAt is set only when a time part exists
        /// </summary>
        public static (DateOnly ObservedOn, DateTime? ObservedAt) ParseEventDate(string? eventDate)
        {
            if (string.IsNullOrEmpty(eventDate))
            {
                return (default, null);
            }
            string value = eventDate;
            int slash = value.IndexOf('/');
            if (slash > 0)
            {
                value = value.Substring(0, slash);
            }
            value = value.Trim();
            if (value.Length == 4 && int.TryParse(value, out int year))
            {
                return (new DateOnly(year, 1, 1), null);
            }
            if (value.Length == 7)
            {
                return (DateOnly.TryParse(value + "-01", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly month) ? month : default, null);
            }
            if (value.Length >= 19)
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime full))
                {
                    return (DateOnly.FromDateTime(full), DateTime.SpecifyKind(full, DateTimeKind.Utc));
                }
            }
            if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            {
                return (date, null);
            }
            return (default, null);
        }

        /// <summary>祖先链拼接（各阶元 key 以 - 相连，跳过空值）/ Build the ancestry chain (non-null rank keys joined by '-')</summary>
        public static string BuildAncestry(GbifOccurrenceDto dto)
        {
            List<string> parts = new List<string>();
            AppendKey(parts, dto.KingdomKey);
            AppendKey(parts, dto.PhylumKey);
            AppendKey(parts, dto.ClassKey);
            AppendKey(parts, dto.OrderKey);
            AppendKey(parts, dto.FamilyKey);
            AppendKey(parts, dto.GenusKey);
            AppendKey(parts, dto.SpeciesKey);
            return string.Join("-", parts);
        }

        private static void AppendKey(List<string> parts, int? key)
        {
            if (key != null && key > 0)
            {
                parts.Add(key.Value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
