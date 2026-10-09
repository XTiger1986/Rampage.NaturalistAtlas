using Rampage.Data;
using Rampage.Data.Models;
using Rampage.DependencyInjection;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 公开站物种服务实现 / Public species service implementation
    /// </summary>
    public class AtlasSpeciesService : SuperService<Taxon, ITaxonDao>, IAtlasSpeciesService
    {
        /// <summary>物种榜缓存键模板 / Species rank cache key template</summary>
        private const string CACHE_KEY_RANK_TPL = "NaturalistAtlas:Public:Rank:{0}:{1}:{2}:{3}:{4}";

        /// <summary>物种详情缓存键模板 / Species detail cache key template</summary>
        private const string CACHE_KEY_DETAIL_TPL = "NaturalistAtlas:Public:Species:{0}:{1}";

        /// <summary>物候曲线缓存键模板 / Emergence cache key template</summary>
        private const string CACHE_KEY_EMERGENCE_TPL = "NaturalistAtlas:Public:Emergence:{0}:{1}:{2}:{3}";

        /// <summary>物候矩阵缓存键模板 / Phenology matrix cache key template</summary>
        private const string CACHE_KEY_MATRIX_TPL = "NaturalistAtlas:Public:Matrix:{0}:{1}";

        /// <summary>祖先链缓存键模板 / Ancestors cache key template</summary>
        private const string CACHE_KEY_ANCESTORS_TPL = "NaturalistAtlas:Public:Ancestors:{0}";

        /// <summary>集邮缺口缓存键模板 / Collection-gap cache key template</summary>
        private const string CACHE_KEY_GAPS_TPL = "NaturalistAtlas:Public:Gaps:{0}:{1}";

        /// <summary>相似物种缓存键模板 / Similar-species cache key template</summary>
        private const string CACHE_KEY_SIMILAR_TPL = "NaturalistAtlas:Public:Similar:{0}:{1}";

        /// <summary>榜单/详情缓存秒数 / Rank and detail cache lifetime</summary>
        private const double SHORT_CACHE_SECONDS = 300;

        /// <summary>物候类聚合缓存秒数（变化慢）/ Phenology aggregate cache lifetime (changes slowly)</summary>
        private const double LONG_CACHE_SECONDS = 600;

        /// <summary>照片墙张数 / Photo wall size</summary>
        private const int PHOTO_WALL_SIZE = 60;

        /// <summary>物种分布点上限 / Species distribution point limit</summary>
        private const int SPECIES_POINT_LIMIT = 3000;

        /// <summary>集邮缺口返回条数上限 / Collection-gap item limit</summary>
        private const int GAPS_LIMIT = 500;

        /// <summary>相似物种返回条数上限 / Similar-species item limit</summary>
        private const int SIMILAR_LIMIT = 12;

        /// <summary>明细口径回溯年数（含当年）/ Years covered by the detail mode (including this year)</summary>
        private const int DETAIL_YEARS = 2;

        [Auto]
        protected ITaxonDao TaxonDao = default!;

        [Auto]
        protected IObservationDao ObservationDao = default!;

        [Auto]
        protected IEmergenceStatDao EmergenceStatDao = default!;

        [Auto]
        protected IEmergenceHistoryDao EmergenceHistoryDao = default!;

        [Auto]
        protected ICityWantedTaxonDao CityWantedTaxonDao = default!;


        /// <inheritdoc/>
        public PageData<SpeciesRankItemDto> GetRankPage(AtlasObservationQuery query, string? order, string? keyword, int pageIndex, int pageSize)
        {
            string orderKey = NormalizeRankOrder(order);
            string? kw = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
            if (kw != null && kw.Length > 50)
            {
                kw = kw.Substring(0, 50);
            }
            PagerInfo pager = new PagerInfo { PageIndex = pageIndex, PageSize = Math.Clamp(pageSize, 1, 100) };

            // 稀缺榜口径固定研究级（排除误鉴定的待鉴定记录）；克隆须同时用于分页与计数，防 totalPage 口径漂移
            // The rarity board counts research grade only (excludes misidentified needs_id rows);
            // the clone feeds both paging and counting so the pager stays consistent
            AtlasObservationQuery effective = query;
            if (orderKey == "rare")
            {
                effective = query.Clone();
                effective.Quality = "1";
            }

            return HandleWebCache(() =>
            {
                List<SpeciesRankItemDto> list = ObservationDao.SelectSpeciesRankPage(effective, kw, orderKey, (pager.PageIndex - 1) * pager.PageSize, pager.PageSize);
                pager.RecordCount = ObservationDao.CountSpeciesRank(effective, kw);
                return new PageData<SpeciesRankItemDto> { Pager = pager, DataList = list };
            }, CACHE_KEY_RANK_TPL, SHORT_CACHE_SECONDS,
                effective.ToCacheKey(), orderKey, kw ?? "-", pager.PageIndex, pager.PageSize)
                ?? new PageData<SpeciesRankItemDto> { Pager = pager, DataList = new List<SpeciesRankItemDto>() };
        }

        /// <inheritdoc/>
        public SpeciesDetailDto? GetSpeciesDetail(string taxonId, int cityId)
        {
            if (string.IsNullOrWhiteSpace(taxonId))
            {
                return null;
            }
            string id = taxonId.Trim();
            return HandleWebCache<SpeciesDetailDto?>(() =>
            {
                TaxonInfoDto? taxon = TaxonDao.SelectTaxonInfo(id);
                if (taxon == null)
                {
                    return null;
                }
                SpeciesDetailDto detail = new SpeciesDetailDto { Taxon = taxon };
                if (cityId <= 0)
                {
                    return detail;
                }

                SpeciesStatRowDto stat = ObservationDao.SelectSpeciesStat(cityId, id);
                detail.RecordCount = stat.RecordCount;
                detail.ObserverCount = stat.ObserverCount;
                detail.FirstObservedOn = stat.FirstObservedOn;
                detail.LastObservedOn = stat.LastObservedOn;

                AtlasObservationQuery query = new AtlasObservationQuery { CityId = cityId, TaxonId = id };
                detail.Photos = ObservationDao.SelectPublicCardPage(query, 0, PHOTO_WALL_SIZE, photoOnly: true);
                detail.Points = ObservationDao.SelectPublicPoints(query, SPECIES_POINT_LIMIT);
                return detail;
            }, CACHE_KEY_DETAIL_TPL, SHORT_CACHE_SECONDS, id, cityId);
        }

        /// <inheritdoc/>
        public EmergenceResultDto GetEmergence(string taxonId, int cityId, string? mode, bool researchOnly)
        {
            bool history = string.Equals(mode, "history", StringComparison.OrdinalIgnoreCase);
            string id = (taxonId ?? string.Empty).Trim();
            if (id.Length == 0 || cityId <= 0)
            {
                return BuildEmergence(history, new List<EmergenceRowDto>(), researchOnly);
            }

            return HandleWebCache(() =>
            {
                List<EmergenceRowDto> rows;
                if (history)
                {
                    // 全历史：优先源站直方图缓存（无研究级拆分，仅研究级时改用本站明细）
                    // All time: prefer the source histogram cache (no research split; research-only falls back to local rows)
                    rows = researchOnly ? new List<EmergenceRowDto>() : EmergenceHistoryDao.SelectMonthSeries(cityId, id);
                    if (rows.Count == 0)
                    {
                        rows = ObservationDao.SelectMonthEmergence(cityId, id);
                    }
                }
                else
                {
                    // 明细：近两年逐旬，优先预聚合表，未启用时由明细实时聚合
                    // Detail: last two years by ten-day, pre-aggregated first, else aggregated live from rows
                    int fromYear = UtcNow.Year - DETAIL_YEARS + 1;
                    rows = EmergenceStatDao.SelectTenDaySeries(cityId, id, fromYear);
                    if (rows.Count == 0)
                    {
                        rows = ObservationDao.SelectTenDayEmergence(cityId, id, new DateOnly(fromYear, 1, 1));
                    }
                }
                return BuildEmergence(history, rows, researchOnly);
            }, CACHE_KEY_EMERGENCE_TPL, LONG_CACHE_SECONDS, id, cityId, history ? "h" : "d", researchOnly ? 1 : 0)
                ?? BuildEmergence(history, new List<EmergenceRowDto>(), researchOnly);
        }

        /// <inheritdoc/>
        public PhenologyMatrixDto GetPhenologyMatrix(AtlasObservationQuery query, int top)
        {
            top = Math.Clamp(top, 5, 100);
            AtlasObservationQuery noMonth = query.WithoutMonth();
            return HandleWebCache(() =>
            {
                // 行展开 → 物种行：SQL 已按总数降序，按首次出现顺序归组即保持排序
                // Unpivoted cells → species rows: SQL is ordered by total, so grouping in first-seen order keeps the ranking
                List<PhenologyRowDto> rows = new List<PhenologyRowDto>();
                Dictionary<string, PhenologyRowDto> dict = new Dictionary<string, PhenologyRowDto>();
                foreach (PhenologyCellRowDto cell in ObservationDao.SelectPhenologyCells(noMonth, top))
                {
                    if (!dict.TryGetValue(cell.TaxonId, out PhenologyRowDto? row))
                    {
                        row = new PhenologyRowDto
                        {
                            TaxonId = cell.TaxonId,
                            ScientificName = cell.ScientificName,
                            CommonNameCn = cell.CommonNameCn,
                            IconicTaxon = cell.IconicTaxon,
                            Total = cell.Total
                        };
                        dict[cell.TaxonId] = row;
                        rows.Add(row);
                    }
                    if (cell.Month is >= 1 and <= 12)
                    {
                        row.Counts[cell.Month.Value - 1] = cell.Count;
                    }
                }
                return new PhenologyMatrixDto { Rows = rows };
            }, CACHE_KEY_MATRIX_TPL, LONG_CACHE_SECONDS, noMonth.ToCacheKey(), top) ?? new PhenologyMatrixDto();
        }

        /// <inheritdoc/>
        public List<TaxonAncestorDto> GetTaxonAncestors(string taxonId)
        {
            string id = (taxonId ?? string.Empty).Trim();
            if (id.Length == 0)
            {
                return new List<TaxonAncestorDto>();
            }
            return HandleWebCache(() => ResolveAncestors(id), CACHE_KEY_ANCESTORS_TPL, LONG_CACHE_SECONDS, id)
                ?? new List<TaxonAncestorDto>();
        }

        /// <summary>
        /// 解析祖先链：ancestry 为源站 id 串（"48460-1-2-3"），一次批量查出本站已同步的成员后按链序重排
        /// Resolve the ancestor chain: ancestry is a source-id path ("48460-1-2-3"); one batch query then reordered along the path
        /// </summary>
        private List<TaxonAncestorDto> ResolveAncestors(string taxonId)
        {
            TaxonInfoDto? taxon = TaxonDao.SelectTaxonInfo(taxonId);
            if (taxon == null || string.IsNullOrWhiteSpace(taxon.Ancestry))
            {
                return new List<TaxonAncestorDto>();
            }
            string[] segments = taxon.Ancestry.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
            {
                return new List<TaxonAncestorDto>();
            }
            // 一次批量查询整条链（比逐段查询少 N-1 次往返；链最长约 70 段，IN 完全可行）
            // One batch query for the whole chain (saves N-1 round trips; the path is at most ~70 segments, fine for IN)
            List<Taxon> found = TaxonDao.SelectBySourceIds((AtlasSource)taxon.Source, segments.ToList());
            Dictionary<string, Taxon> bySourceId = found.ToDictionary(t => t.SourceTaxonId, t => t);
            List<TaxonAncestorDto> chain = new List<TaxonAncestorDto>(segments.Length);
            foreach (string segment in segments)
            {
                if (bySourceId.TryGetValue(segment, out Taxon? node))
                {
                    chain.Add(new TaxonAncestorDto
                    {
                        TaxonId = node.Id,
                        Rank = (short)node.Rank,
                        ScientificName = node.ScientificName,
                        CommonNameCn = node.CommonNameCn,
                        CommonNameEn = node.CommonNameEn
                    });
                }
                // 未同步到本站的链段直接跳过（面包屑仍保持链序）
                // Segments not synced locally are skipped (the breadcrumb keeps the path order)
            }
            return chain;
        }

        /// <inheritdoc/>
        public List<WantedTaxonItemDto> GetGaps(int cityId, string? iconic)
        {
            if (cityId <= 0)
            {
                return new List<WantedTaxonItemDto>();
            }
            short[] iconicList = ParseIconicCodes(iconic);
            return HandleWebCache(() => CityWantedTaxonDao.SelectPublic(cityId, iconicList, GAPS_LIMIT),
                CACHE_KEY_GAPS_TPL, LONG_CACHE_SECONDS, cityId, string.Join(",", iconicList))
                ?? new List<WantedTaxonItemDto>();
        }

        /// <summary>
        /// 解析逗号分隔的类群代码（1~14，非法值静默忽略）
        /// Parse comma-separated iconic codes (1~14; invalid values silently ignored)
        /// </summary>
        private static short[] ParseIconicCodes(string? iconic)
        {
            if (string.IsNullOrWhiteSpace(iconic))
            {
                return Array.Empty<short>();
            }
            List<short> list = new List<short>();
            foreach (string part in iconic.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (short.TryParse(part, out short code) && code >= 1 && code <= 14 && !list.Contains(code))
                {
                    list.Add(code);
                }
            }
            return list.ToArray();
        }

        /// <inheritdoc/>
        public List<SpeciesRankItemDto> GetSimilarSpecies(string taxonId, int cityId)
        {
            string id = (taxonId ?? string.Empty).Trim();
            if (id.Length == 0 || cityId <= 0)
            {
                return new List<SpeciesRankItemDto>();
            }
            return HandleWebCache(() => ObservationDao.SelectSimilarSpecies(cityId, id, SIMILAR_LIMIT),
                CACHE_KEY_SIMILAR_TPL, LONG_CACHE_SECONDS, id, cityId)
                ?? new List<SpeciesRankItemDto>();
        }

        /// <summary>
        /// 归一化榜单排序键：record（默认）/ observer / rare，非法值回落 record
        /// Normalize the rank order key: record (default) / observer / rare; invalid values fall back to record
        /// </summary>
        private static string NormalizeRankOrder(string? order)
        {
            return (order ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "observer" => "observer",
                "rare" => "rare",
                _ => "record"
            };
        }

        /// <summary>
        /// 组装物候曲线：补齐全部序号（旬 1~36 / 月 1~12），仅研究级时记录数取研究级数
        /// Assemble the curve: fill every index (ten-day 1~36 / month 1~12); research-only uses the research count as the record count
        /// </summary>
        private EmergenceResultDto BuildEmergence(bool history, List<EmergenceRowDto> rows, bool researchOnly)
        {
            int size = history ? 12 : 36;
            Dictionary<int, EmergenceRowDto> dict = rows.Where(r => r.Idx >= 1 && r.Idx <= size).ToDictionary(r => r.Idx);
            List<EmergencePointDto> series = Enumerable.Range(1, size).Select(i =>
            {
                dict.TryGetValue(i, out EmergenceRowDto? row);
                long research = row?.ResearchCount ?? 0;
                return new EmergencePointDto
                {
                    Index = i,
                    RecordCount = researchOnly ? research : row?.RecordCount ?? 0,
                    ResearchCount = research
                };
            }).ToList();

            return new EmergenceResultDto
            {
                Mode = history ? "history" : "detail",
                Unit = history ? "month" : "tenDay",
                SampleSize = series.Sum(p => p.RecordCount),
                Series = series
            };
        }
    }
}
