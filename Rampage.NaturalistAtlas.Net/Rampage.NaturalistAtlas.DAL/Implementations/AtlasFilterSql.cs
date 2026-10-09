using Rampage.NaturalistAtlas.Model.Dto.Public;
using System.Collections.Generic;
using System.Text;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 公开站观察筛选 SQL 片段构建器：把 <see cref="AtlasObservationQuery"/> 翻译为 WHERE 条件与 Dapper 参数
    /// Public observation filter SQL builder: translates <see cref="AtlasObservationQuery"/> into a WHERE clause plus Dapper parameters
    /// </summary>
    /// <remarks>
    /// 约定表别名为 o（observation）。类群/鉴定级别为已校验的 short 代码，直接内联为 IN 列表（无注入面，且避免数组参数的驱动差异）；
    /// 其余条件一律参数化。恒含 o.status = 0（仅正常展示记录）。
    /// The observation alias is o. Iconic/quality codes are validated shorts and inlined as IN lists (no injection surface, and it sidesteps
    /// driver differences for array parameters); everything else is parameterized. o.status = 0 (visible rows only) is always included.
    /// </remarks>
    internal static class AtlasFilterSql
    {
        /// <summary>
        /// 构建 WHERE 条件（不含 WHERE 关键字）
        /// Build the WHERE condition (without the WHERE keyword)
        /// </summary>
        /// <param name="query">筛选 / Filter</param>
        /// <param name="param">Dapper 参数字典（就地追加）/ Dapper parameter dictionary (appended in place)</param>
        /// <param name="includeMonth">是否应用月份区间 / Whether to apply the month range</param>
        /// <returns>条件串 / The condition</returns>
        public static string BuildWhere(AtlasObservationQuery query, Dictionary<string, object?> param, bool includeMonth = true)
        {
            StringBuilder sb = new StringBuilder("o.status = 0");

            if (query.CityId.HasValue)
            {
                sb.Append(" AND o.city_id = @CityId");
                param["CityId"] = query.CityId.Value;
            }

            decimal[]? bbox = query.GetBBox();
            if (bbox != null)
            {
                sb.Append(" AND o.lng BETWEEN @West AND @East AND o.lat BETWEEN @South AND @North");
                param["West"] = bbox[0];
                param["South"] = bbox[1];
                param["East"] = bbox[2];
                param["North"] = bbox[3];
            }

            short[] iconicList = query.GetIconicList();
            if (iconicList.Length > 0)
            {
                sb.Append(" AND o.iconic_taxon IN (").Append(string.Join(",", iconicList)).Append(')');
            }

            sb.Append(" AND o.quality_grade IN (").Append(string.Join(",", query.GetQualityList())).Append(')');

            if (includeMonth)
            {
                (int From, int To)? month = query.GetMonthRange();
                if (month.HasValue)
                {
                    param["MonthFrom"] = month.Value.From;
                    param["MonthTo"] = month.Value.To;
                    // 起 ≤ 止为普通区间；起 > 止为跨年区间（如 11~2 = 11、12、1、2 月）
                    // from ≤ to is a plain range; from > to wraps the year end (e.g. 11~2 = Nov, Dec, Jan, Feb)
                    sb.Append(month.Value.From <= month.Value.To
                        ? " AND EXTRACT(MONTH FROM o.observed_on) BETWEEN @MonthFrom AND @MonthTo"
                        : " AND (EXTRACT(MONTH FROM o.observed_on) >= @MonthFrom OR EXTRACT(MONTH FROM o.observed_on) <= @MonthTo)");
                }
            }

            if (query.GetDateFrom() is { } dateFrom)
            {
                sb.Append(" AND o.observed_on >= @DateFrom");
                param["DateFrom"] = dateFrom;
            }
            if (query.GetDateTo() is { } dateTo)
            {
                sb.Append(" AND o.observed_on <= @DateTo");
                param["DateTo"] = dateTo;
            }

            if (!string.IsNullOrWhiteSpace(query.TaxonId))
            {
                sb.Append(" AND o.taxon_id = @TaxonId");
                param["TaxonId"] = query.TaxonId.Trim();
            }

            return sb.ToString();
        }

        /// <summary>
        /// 构建 LIKE 模糊匹配参数值（转义 % _ \）
        /// Build a LIKE pattern value (escaping % _ \)
        /// </summary>
        /// <param name="keyword">关键字 / Keyword</param>
        /// <returns>%keyword% 形式的模式串 / A %keyword% pattern</returns>
        public static string LikePattern(string keyword)
        {
            string escaped = keyword.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            return "%" + escaped + "%";
        }

        /// <summary>
        /// 构建转义后的精确匹配 LIKE 值（不包 % 前后缀；配合 ESCAPE '\' 做大小写不敏感的等值匹配，
        /// 转义用户输入里的 % _ \，"US_" 不会再命中 "USA"）
        /// Build an escaped exact-match LIKE value (no % wrapping; combined with ESCAPE '\'
        /// for case-insensitive equality, escaping % _ \ so "US_" no longer hits "USA")
        /// </summary>
        public static string EscapeLike(string keyword)
        {
            return keyword.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }

        /// <summary>ISO 8601 UTC 时间格式（to_char 模板）/ ISO 8601 UTC format (to_char template)</summary>
        public const string ISO_UTC_FORMAT = "YYYY-MM-DD\"T\"HH24:MI:SS\"Z\"";

        /// <summary>
        /// 点位投影列（需 LEFT JOIN taxon t）/ Point projection columns (requires LEFT JOIN taxon t)
        /// </summary>
        public const string POINT_COLUMNS = @"o.id AS Id, o.lng::float8 AS Lng, o.lat::float8 AS Lat,
            o.iconic_taxon AS IconicTaxon, o.quality_grade AS QualityGrade, o.taxon_id AS TaxonId,
            COALESCE(NULLIF(t.common_name_cn, ''), t.scientific_name) AS Name,
            NULLIF(o.photo_square_url, '') AS PhotoSquareUrl, o.license AS License,
            to_char(o.observed_on, 'YYYY-MM-DD') AS ObservedOn";

        /// <summary>
        /// 卡片投影列（需 LEFT JOIN taxon t）/ Card projection columns (requires LEFT JOIN taxon t)
        /// </summary>
        public const string CARD_COLUMNS = @"o.id AS Id, o.source AS Source, o.taxon_id AS TaxonId,
            t.scientific_name AS ScientificName, NULLIF(t.common_name_cn, '') AS CommonNameCn, NULLIF(t.common_name_en, '') AS CommonNameEn,
            o.iconic_taxon AS IconicTaxon, o.quality_grade AS QualityGrade,
            to_char(o.observed_on, 'YYYY-MM-DD') AS ObservedOn,
            to_char(o.observed_at, '" + ISO_UTC_FORMAT + @"') AS ObservedAt,
            o.lng::float8 AS Lng, o.lat::float8 AS Lat, o.geoprivacy AS Geoprivacy,
            NULLIF(o.photo_square_url, '') AS PhotoSquareUrl, NULLIF(o.photo_medium_url, '') AS PhotoMediumUrl,
            o.license AS License, NULLIF(o.user_login, '') AS UserLogin, o.dataset_title AS DatasetTitle,
            COALESCE(t.threatened, FALSE) AS Threatened";

        /// <summary>
        /// 物种榜投影：以 CTE pg（含 taxon_id/record_count/observer_count/last_on 与 taxon 字段）为源，LATERAL 取代表图
        /// Species-rank projection over the CTE pg (taxon_id/record_count/observer_count/last_on plus taxon fields), with a LATERAL representative photo
        /// </summary>
        /// <param name="photoCityCondition">代表图的城市条件（如 "AND x.city_id = @CityId"，可空串）/ City condition for the photo (may be empty)</param>
        /// <param name="orderBy">最终排序（基于 pg 列）/ Final ORDER BY over pg columns</param>
        /// <param name="extraColumns">追加投影列（可空串，如新星语境的 ReachedOn）/ Extra projection columns (may be empty, e.g. ReachedOn)</param>
        /// <param name="extraJoin">追加 JOIN（可空串）/ Extra JOIN clause (may be empty)</param>
        /// <returns>SELECT 语句尾部 / The trailing SELECT statement</returns>
        public static string RankProjection(string photoCityCondition, string orderBy, string extraColumns = "", string extraJoin = "")
        {
            // 代表图：优先研究级、再取最新；仅取有中图的记录。license/user 与代表图同行取回（版权署名）
            // Representative photo: research grade first, then newest; only rows with a medium photo.
            // license/user come from the same row (attribution)
            string extraCol = string.IsNullOrWhiteSpace(extraColumns) ? string.Empty : $",{extraColumns}";
            string extraJoinSql = string.IsNullOrWhiteSpace(extraJoin) ? string.Empty : extraJoin;
            return $@"SELECT pg.taxon_id AS TaxonId, pg.scientific_name AS ScientificName,
                NULLIF(pg.common_name_cn, '') AS CommonNameCn, NULLIF(pg.common_name_en, '') AS CommonNameEn,
                pg.iconic_taxon AS IconicTaxon, pg.threatened AS Threatened,
                pg.record_count AS RecordCount, pg.observer_count AS ObserverCount,
                to_char(pg.last_on, 'YYYY-MM-DD') AS LastObservedOn{extraCol},
                ph.sq AS PhotoSquareUrl, ph.md AS PhotoMediumUrl,
                ph.lic AS PhotoLicense, ph.usr AS PhotoUserLogin
            FROM pg {extraJoinSql}
            LEFT JOIN LATERAL (
                SELECT NULLIF(x.photo_square_url, '') AS sq, NULLIF(x.photo_medium_url, '') AS md,
                    x.license AS lic, NULLIF(x.user_login, '') AS usr
                FROM observation x
                WHERE x.taxon_id = pg.taxon_id AND x.status = 0 AND x.photo_medium_url <> '' {photoCityCondition}
                ORDER BY x.quality_grade, x.observed_on DESC
                LIMIT 1
            ) ph ON TRUE
            ORDER BY {orderBy}";
        }
    }
}
