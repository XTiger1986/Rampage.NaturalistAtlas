using Rampage.NaturalistAtlas.Model.Dto.Public;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 观察数据 Dao 实现：公开站查询部分（筛选条件统一由 <see cref="AtlasFilterSql"/> 构建）
    /// Observation Dao implementation: public-site queries (filters built uniformly by <see cref="AtlasFilterSql"/>)
    /// </summary>
    public partial class ObservationDao
    {
        /// <inheritdoc/>
        public List<ObservationPointDto> SelectPublicPoints(AtlasObservationQuery query, int limit)
        {
            Dictionary<string, object?> param = new Dictionary<string, object?> { ["Limit"] = limit };
            string where = AtlasFilterSql.BuildWhere(query, param);
            return DatabaseClient.SelectList<ObservationPointDto>(
                $@"SELECT {AtlasFilterSql.POINT_COLUMNS}
                FROM observation o LEFT JOIN taxon t ON t.id = o.taxon_id
                WHERE {where}
                ORDER BY o.observed_on DESC, o.id DESC
                LIMIT @Limit", param);
        }

        /// <inheritdoc/>
        public long CountPublic(AtlasObservationQuery query, bool photoOnly = false)
        {
            Dictionary<string, object?> param = new Dictionary<string, object?>();
            string where = AtlasFilterSql.BuildWhere(query, param);
            if (photoOnly)
            {
                where += " AND o.photo_medium_url <> ''";
            }
            return DatabaseClient.SelectOne<ScalarRow<long>>(
                $"SELECT COUNT(*) AS Value FROM observation o WHERE {where}", param)?.Value ?? 0;
        }

        /// <inheritdoc/>
        public List<ObservationCardDto> SelectPublicCardPage(AtlasObservationQuery query, int offset, int size, bool photoOnly = false)
        {
            Dictionary<string, object?> param = new Dictionary<string, object?> { ["Offset"] = offset, ["Size"] = size };
            string where = AtlasFilterSql.BuildWhere(query, param);
            if (photoOnly)
            {
                where += " AND o.photo_medium_url <> ''";
            }
            return DatabaseClient.SelectList<ObservationCardDto>(
                $@"SELECT {AtlasFilterSql.CARD_COLUMNS}
                FROM observation o LEFT JOIN taxon t ON t.id = o.taxon_id
                WHERE {where}
                ORDER BY o.observed_on DESC, o.id DESC
                LIMIT @Size OFFSET @Offset", param);
        }

        /// <inheritdoc/>
        public ObservationDetailDto? SelectPublicDetail(string id)
        {
            return DatabaseClient.SelectOne<ObservationDetailDto>(
                $@"SELECT {AtlasFilterSql.CARD_COLUMNS},
                    o.source_observation_id AS SourceObservationId, NULLIF(o.photo_large_url, '') AS PhotoLargeUrl,
                    o.positional_accuracy AS PositionalAccuracy, o.identifications_count AS IdentificationsCount,
                    o.record_basis AS RecordBasis, c.name AS CityName
                FROM observation o
                LEFT JOIN taxon t ON t.id = o.taxon_id
                LEFT JOIN city c ON c.id = o.city_id
                WHERE o.id = @Id AND o.status = 0", new { Id = id });
        }

        /// <summary>
        /// 物种榜公共 CTE：agg（按物种聚合）+ 关键字过滤后的 taxon 连接
        /// Species-rank shared CTE: agg (per-species aggregate) joined to taxon with the keyword filter
        /// </summary>
        private static string BuildRankSource(AtlasObservationQuery query, string? keyword, Dictionary<string, object?> param, out string keywordWhere)
        {
            string where = AtlasFilterSql.BuildWhere(query, param);
            keywordWhere = string.Empty;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                param["Keyword"] = AtlasFilterSql.LikePattern(keyword);
                keywordWhere = "WHERE (t.scientific_name ILIKE @Keyword OR t.common_name_cn ILIKE @Keyword OR t.common_name_en ILIKE @Keyword)";
            }
            return $@"agg AS (
                SELECT o.taxon_id, COUNT(*) AS record_count,
                    COUNT(DISTINCT NULLIF(o.user_login, '')) AS observer_count, MAX(o.observed_on) AS last_on
                FROM observation o
                WHERE {where}
                GROUP BY o.taxon_id
            )";
        }

        /// <inheritdoc/>
        public List<SpeciesRankItemDto> SelectSpeciesRankPage(AtlasObservationQuery query, string? keyword, string order, int offset, int size)
        {
            Dictionary<string, object?> param = new Dictionary<string, object?> { ["Offset"] = offset, ["Size"] = size };
            string agg = BuildRankSource(query, keyword, param, out string keywordWhere);
            // rare = 稀缺榜：记录数升序、观察者数升序（BLL 层已把口径限定为研究级）
            // rare = rarity board: records ascending, observers ascending (the BLL restricts it to research grade)
            string orderSql = order switch
            {
                "observer" => "observer_count DESC, record_count DESC, taxon_id",
                "rare" => "record_count ASC, observer_count ASC, taxon_id",
                _ => "record_count DESC, observer_count DESC, taxon_id"
            };
            string photoCity = query.CityId.HasValue ? "AND x.city_id = @CityId" : string.Empty;

            // 先在 pg 中完成排序分页，再对当页物种做 LATERAL 取图，避免对全部物种执行子查询
            // Sort and page inside pg first, then LATERAL-fetch photos for the current page only
            return DatabaseClient.SelectList<SpeciesRankItemDto>(
                $@"WITH {agg},
                pg AS (
                    SELECT a.taxon_id, a.record_count, a.observer_count, a.last_on,
                        t.scientific_name, t.common_name_cn, t.common_name_en, t.iconic_taxon, t.threatened
                    FROM agg a JOIN taxon t ON t.id = a.taxon_id
                    {keywordWhere}
                    ORDER BY {orderSql}
                    LIMIT @Size OFFSET @Offset
                )
                {AtlasFilterSql.RankProjection(photoCity, "pg." + orderSql.Replace(", ", ", pg."))}", param);
        }

        /// <inheritdoc/>
        public long CountSpeciesRank(AtlasObservationQuery query, string? keyword)
        {
            Dictionary<string, object?> param = new Dictionary<string, object?>();
            string agg = BuildRankSource(query, keyword, param, out string keywordWhere);
            return DatabaseClient.SelectOne<ScalarRow<long>>(
                $@"WITH {agg}
                SELECT COUNT(*) AS Value FROM agg a JOIN taxon t ON t.id = a.taxon_id {keywordWhere}", param)?.Value ?? 0;
        }

        /// <inheritdoc/>
        public SpeciesStatRowDto SelectSpeciesStat(int cityId, string taxonId)
        {
            return DatabaseClient.SelectOne<SpeciesStatRowDto>(
                @"SELECT COUNT(*) AS RecordCount, COUNT(DISTINCT NULLIF(user_login, '')) AS ObserverCount,
                    to_char(MIN(observed_on), 'YYYY-MM-DD') AS FirstObservedOn, to_char(MAX(observed_on), 'YYYY-MM-DD') AS LastObservedOn
                FROM observation
                WHERE status = 0 AND city_id = @CityId AND taxon_id = @TaxonId AND quality_grade IN (1, 2)",
                new { CityId = cityId, TaxonId = taxonId }) ?? new SpeciesStatRowDto();
        }

        /// <inheritdoc/>
        public List<EmergenceRowDto> SelectTenDayEmergence(int cityId, string taxonId, DateOnly from)
        {
            // 旬序 = (月 - 1) × 3 + 旬（1~10 日 → 1，11~20 日 → 2，21 日~月末 → 3）
            // Ten-day index = (month - 1) × 3 + period (days 1-10 → 1, 11-20 → 2, 21-end → 3)
            return DatabaseClient.SelectList<EmergenceRowDto>(
                @"SELECT (EXTRACT(MONTH FROM observed_on)::int - 1) * 3 + LEAST(2, (EXTRACT(DAY FROM observed_on)::int - 1) / 10) + 1 AS Idx,
                    COUNT(*) AS RecordCount, COUNT(*) FILTER (WHERE quality_grade = 1) AS ResearchCount
                FROM observation
                WHERE status = 0 AND city_id = @CityId AND taxon_id = @TaxonId AND quality_grade IN (1, 2) AND observed_on >= @From
                GROUP BY 1",
                new { CityId = cityId, TaxonId = taxonId, From = from });
        }

        /// <inheritdoc/>
        public List<EmergenceRowDto> SelectMonthEmergence(int cityId, string taxonId)
        {
            return DatabaseClient.SelectList<EmergenceRowDto>(
                @"SELECT EXTRACT(MONTH FROM observed_on)::int AS Idx,
                    COUNT(*) AS RecordCount, COUNT(*) FILTER (WHERE quality_grade = 1) AS ResearchCount
                FROM observation
                WHERE status = 0 AND city_id = @CityId AND taxon_id = @TaxonId AND quality_grade IN (1, 2)
                GROUP BY 1",
                new { CityId = cityId, TaxonId = taxonId });
        }

        /// <inheritdoc/>
        public List<SpeciesRankItemDto> SelectSimilarSpecies(int cityId, string taxonId, int limit)
        {
            // 同胞口径：ancestry 相同（即同父，天然限定同属内种/种下）；本站未同步祖先链的返回空。
            // 注意 taxon.status 是 CommonStatus（1=启用/0=停用），与 observation.status（0=正常）语义相反
            // Sibling scope: identical ancestry (same parent, naturally within one genus); empty when the
            // path is missing. taxon.status is CommonStatus (1=enabled/0=disabled) — the opposite
            // convention of observation.status (0=normal)
            return DatabaseClient.SelectList<SpeciesRankItemDto>(
                $@"WITH me AS (
                    SELECT id, source, ancestry FROM taxon WHERE id = @TaxonId AND ancestry <> ''
                ), pg AS (
                    SELECT t.id AS taxon_id, t.scientific_name, t.common_name_cn, t.common_name_en,
                        t.iconic_taxon, t.threatened,
                        agg.record_count, agg.observer_count, agg.last_on
                    FROM taxon t
                    JOIN me ON t.source = me.source AND t.ancestry = me.ancestry AND t.id <> me.id AND t.status = 1
                    JOIN LATERAL (
                        SELECT COUNT(*) AS record_count,
                            COUNT(DISTINCT NULLIF(o.user_login, '')) AS observer_count,
                            MAX(o.observed_on) AS last_on
                        FROM observation o
                        WHERE o.status = 0 AND o.city_id = @CityId AND o.taxon_id = t.id AND o.quality_grade IN (1, 2)
                    ) agg ON agg.record_count > 0
                    ORDER BY agg.record_count DESC, t.id
                    LIMIT @Limit
                )
                {AtlasFilterSql.RankProjection("AND x.city_id = @CityId", "pg.record_count DESC, pg.taxon_id")}",
                new { CityId = cityId, TaxonId = taxonId, Limit = limit });
        }

        /// <inheritdoc/>
        public List<PhenologyCellRowDto> SelectPhenologyCells(AtlasObservationQuery query, int top)
        {
            Dictionary<string, object?> param = new Dictionary<string, object?> { ["Top"] = top };
            string where = AtlasFilterSql.BuildWhere(query, param, includeMonth: false);
            return DatabaseClient.SelectList<PhenologyCellRowDto>(
                $@"WITH top AS (
                    SELECT o.taxon_id, COUNT(*) AS total
                    FROM observation o
                    WHERE {where}
                    GROUP BY o.taxon_id
                    ORDER BY total DESC, o.taxon_id
                    LIMIT @Top
                ), m AS (
                    SELECT o.taxon_id, EXTRACT(MONTH FROM o.observed_on)::int AS month, COUNT(*) AS cnt
                    FROM observation o JOIN top ON top.taxon_id = o.taxon_id
                    WHERE {where}
                    GROUP BY 1, 2
                )
                SELECT top.taxon_id AS TaxonId, t.scientific_name AS ScientificName, NULLIF(t.common_name_cn, '') AS CommonNameCn,
                    t.iconic_taxon AS IconicTaxon, top.total AS Total, m.month AS Month, COALESCE(m.cnt, 0) AS Count
                FROM top
                JOIN taxon t ON t.id = top.taxon_id
                LEFT JOIN m ON m.taxon_id = top.taxon_id
                ORDER BY top.total DESC, top.taxon_id, m.month", param);
        }

        /// <inheritdoc/>
        public OverviewBaseRowDto SelectOverviewBase(int cityId, short[] qualityList, DateOnly monthStart)
        {
            return DatabaseClient.SelectOne<OverviewBaseRowDto>(
                $@"SELECT COUNT(*) AS Total, COUNT(*) FILTER (WHERE quality_grade = 1) AS Research,
                    COUNT(DISTINCT taxon_id) AS Species, COUNT(DISTINCT NULLIF(user_login, '')) AS Observers,
                    COUNT(*) FILTER (WHERE observed_on >= @MonthStart) AS MonthCount,
                    to_char(MAX(source_created_at), '{AtlasFilterSql.ISO_UTC_FORMAT}') AS Newest
                FROM observation
                WHERE status = 0 AND city_id = @CityId AND quality_grade IN ({string.Join(",", qualityList)})",
                new { CityId = cityId, MonthStart = monthStart }) ?? new OverviewBaseRowDto();
        }

        /// <inheritdoc/>
        public List<IconicCountDto> SelectOverviewIconic(int cityId, short[] qualityList)
        {
            return DatabaseClient.SelectList<IconicCountDto>(
                $@"SELECT iconic_taxon AS IconicTaxon, COUNT(*) AS Count
                FROM observation
                WHERE status = 0 AND city_id = @CityId AND quality_grade IN ({string.Join(",", qualityList)})
                GROUP BY iconic_taxon
                ORDER BY 2 DESC",
                new { CityId = cityId });
        }

        /// <inheritdoc/>
        public List<ObserverCountDto> SelectTopObservers(int cityId, short[] qualityList, int limit)
        {
            return DatabaseClient.SelectList<ObserverCountDto>(
                $@"SELECT user_login AS UserLogin, COUNT(*) AS Count
                FROM observation
                WHERE status = 0 AND city_id = @CityId AND user_login <> '' AND quality_grade IN ({string.Join(",", qualityList)})
                GROUP BY user_login
                ORDER BY 2 DESC, 1
                LIMIT @Limit",
                new { CityId = cityId, Limit = limit });
        }

        /// <inheritdoc/>
        public List<MonthlyCountDto> SelectMonthly12(int cityId, short[] qualityList, DateOnly from)
        {
            return DatabaseClient.SelectList<MonthlyCountDto>(
                $@"SELECT to_char(date_trunc('month', observed_on), 'YYYY-MM') AS Month, COUNT(*) AS Count
                FROM observation
                WHERE status = 0 AND city_id = @CityId AND quality_grade IN ({string.Join(",", qualityList)}) AND observed_on >= @From
                GROUP BY 1
                ORDER BY 1",
                new { CityId = cityId, From = from });
        }

        /// <inheritdoc/>
        public List<SpeciesRankItemDto> SelectRisingStars(int cityId, DateOnly monthStart, int threshold, int limit)
        {
            // ReachedOn：研究级累计第 threshold 条的 observed_on（LATERAL 只对 LIMIT 后的当页物种执行）
            // ReachedOn: the observed_on of the threshold-th research-grade record (LATERAL over the paged species only)
            return DatabaseClient.SelectList<SpeciesRankItemDto>(
                $@"WITH s AS (
                    SELECT o.taxon_id, COUNT(*) AS record_count,
                        COUNT(DISTINCT NULLIF(o.user_login, '')) AS observer_count, MAX(o.observed_on) AS last_on
                    FROM observation o
                    WHERE o.status = 0 AND o.city_id = @CityId AND o.quality_grade = 1
                    GROUP BY o.taxon_id
                    HAVING COUNT(*) FILTER (WHERE o.observed_on < @MonthStart) < @Threshold AND COUNT(*) >= @Threshold
                ), pg AS (
                    SELECT s.taxon_id, s.record_count, s.observer_count, s.last_on,
                        t.scientific_name, t.common_name_cn, t.common_name_en, t.iconic_taxon, t.threatened
                    FROM s JOIN taxon t ON t.id = s.taxon_id
                    ORDER BY s.record_count DESC, s.taxon_id
                    LIMIT @Limit
                )
                {AtlasFilterSql.RankProjection("AND x.city_id = @CityId", "pg.record_count DESC, pg.taxon_id",
                    "to_char(r.observed_on, 'YYYY-MM-DD') AS ReachedOn",
                    @"JOIN LATERAL (
                        SELECT x.observed_on FROM observation x
                        WHERE x.city_id = @CityId AND x.taxon_id = pg.taxon_id AND x.quality_grade = 1 AND x.status = 0
                        ORDER BY x.observed_on ASC, x.id ASC
                        OFFSET @Threshold - 1 LIMIT 1
                    ) r ON TRUE")}",
                new { CityId = cityId, MonthStart = monthStart, Threshold = threshold, Limit = limit });
        }
    }
}
