using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 观察数据 Dao 实现：聚合统计 SQL 收口于此（GroupBy/Filter 聚合 ORM 通道不覆盖）
    /// Observation Dao implementation: aggregate SQL lives here (beyond the ORM group-by path)
    /// </summary>
    public partial class ObservationDao : SuperDao<Observation>, IObservationDao
    {
        /// <summary>标量包装行（DatabaseClient 泛型约束 class）/ Scalar wrapper row (the client generic requires a class)</summary>
        private class ScalarRow<T> where T : struct
        {
            public T? Value { get; set; }
        }

        /// <summary>单值字符串行 / Single string-value row</summary>
        private class StringRow
        {
            public string? Value { get; set; }
        }

        /// <summary>upsert RETURNING 行（xmax=0 即新增）/ Upsert RETURNING row (xmax = 0 means inserted)</summary>
        private class UpsertFlagRow
        {
            public bool IsInsert { get; set; }
        }

        /// <summary>
        /// upsert 列规格：(列名, 取值)。枚举显式转 short，避免驱动枚举映射歧义
        /// Upsert column spec: (column, value getter); enums cast to short explicitly to avoid driver enum-mapping ambiguity
        /// </summary>
        private static readonly (string Column, Func<Observation, object?>)[] UPSERT_COLUMNS =
        {
            ("id", r => r.Id),
            ("source", r => (short)r.Source),
            ("source_observation_id", r => r.SourceObservationId),
            ("city_id", r => r.CityId),
            ("taxon_id", r => r.TaxonId),
            ("iconic_taxon", r => (short)r.IconicTaxon),
            ("quality_grade", r => (short)r.QualityGrade),
            ("observed_on", r => r.ObservedOn),
            ("observed_at", r => r.ObservedAt),
            ("lng", r => r.Lng),
            ("lat", r => r.Lat),
            ("positional_accuracy", r => r.PositionalAccuracy),
            ("geoprivacy", r => (short)r.Geoprivacy),
            ("photo_square_url", r => r.PhotoSquareUrl),
            ("photo_medium_url", r => r.PhotoMediumUrl),
            ("photo_large_url", r => r.PhotoLargeUrl),
            ("license", r => (short)r.License),
            ("user_login", r => r.UserLogin),
            ("record_basis", r => r.RecordBasis.HasValue ? (short)r.RecordBasis.Value : null),
            ("dataset_key", r => r.DatasetKey),
            ("dataset_title", r => r.DatasetTitle),
            ("identifications_count", r => r.IdentificationsCount),
            ("source_created_at", r => r.SourceCreatedAt),
            ("source_updated_at", r => r.SourceUpdatedAt),
            ("status", r => (short)r.Status),
            ("create_time", r => r.CreateTime),
            ("update_time", r => r.UpdateTime)
        };

        /// <summary>
        /// DO UPDATE 覆盖的业务列：status/create_time 与身份列不覆盖（不冲掉运营隐藏与源站已删除态）
        /// Business columns overwritten by DO UPDATE: status/create_time and identity columns excluded (never clobber operator-hidden or source-deleted state)
        /// </summary>
        private static readonly string[] UPSERT_UPDATE_COLUMNS =
        {
            "taxon_id", "iconic_taxon", "quality_grade", "observed_on", "observed_at", "lng", "lat",
            "positional_accuracy", "geoprivacy", "photo_square_url", "photo_medium_url", "photo_large_url",
            "license", "user_login", "record_basis", "dataset_key", "dataset_title", "identifications_count",
            "source_created_at", "source_updated_at", "update_time"
        };

        private const int UPSERT_SQL_BATCH_SIZE = 200;


        /// <summary>
        /// 口径城市过滤片段：cityId 为已校验 int，直接内联（与类群/鉴定级别小整数的处理一致，
        /// 避免 Npgsql 对 NULL 参数的类型推断问题）；null = 全站不过滤
        /// Scope-city filter fragment: cityId is a validated int and is inlined directly (same as the
        /// iconic/quality short codes, sidestepping Npgsql NULL-type inference); null = whole site
        /// </summary>
        private static string CityScope(int? cityId)
        {
            return cityId.HasValue ? $" AND city_id = {cityId.Value}" : string.Empty;
        }

        /// <inheritdoc/>
        public DashboardBaseStatDto SelectBaseStat(int? cityId)
        {
            return DatabaseClient.SelectOne<DashboardBaseStatDto>(
                $"SELECT COUNT(*) AS Total, COUNT(*) FILTER (WHERE quality_grade = 1) AS Research FROM observation WHERE status = 0{CityScope(cityId)}")
                ?? new DashboardBaseStatDto();
        }

        /// <inheritdoc/>
        public long SelectMonthInsertCount(DateTime monthStart, int? cityId)
        {
            return DatabaseClient.SelectOne<ScalarRow<long>>(
                $"SELECT COUNT(*) AS Value FROM observation WHERE status = 0 AND create_time >= @MonthStart{CityScope(cityId)}",
                new { MonthStart = monthStart })?.Value ?? 0;
        }

        /// <inheritdoc/>
        public DateTime? SelectNewestSourceCreatedAt(int? cityId)
        {
            // MAX 于空结果集返回 NULL，Dapper 映射为 default(DateTime)，按无数据处理
            DateTime value = DatabaseClient.SelectOne<ScalarRow<DateTime>>(
                $"SELECT MAX(source_created_at) AS Value FROM observation WHERE status = 0{CityScope(cityId)}")?.Value ?? default;
            return value == default ? null : value;
        }

        /// <inheritdoc/>
        public List<DashboardIconicRowDto> SelectIconicDistribution(int? cityId)
        {
            return DatabaseClient.SelectList<DashboardIconicRowDto>(
                $"SELECT iconic_taxon AS IconicTaxon, COUNT(*) AS Count FROM observation WHERE status = 0{CityScope(cityId)} GROUP BY iconic_taxon ORDER BY 2 DESC");
        }

        /// <inheritdoc/>
        public List<DashboardTrendRowDto> SelectIngestTrend(DateTime from, int? cityId)
        {
            return DatabaseClient.SelectList<DashboardTrendRowDto>(
                $"SELECT to_char(create_time, 'YYYY-MM-DD') AS Label, COUNT(*) AS Value FROM observation WHERE status = 0 AND create_time >= @From{CityScope(cityId)} GROUP BY 1 ORDER BY 1",
                new { From = from });
        }

        /// <inheritdoc/>
        public List<DashboardTrendRowDto> SelectMonthlyDistribution(DateOnly from, int? cityId)
        {
            return DatabaseClient.SelectList<DashboardTrendRowDto>(
                $"SELECT to_char(date_trunc('month', observed_on), 'YYYY-MM') AS Label, COUNT(*) AS Value FROM observation WHERE status = 0 AND observed_on >= @From{CityScope(cityId)} GROUP BY 1 ORDER BY 1",
                new { From = from });
        }

        /// <inheritdoc/>
        public List<DashboardSliceDto> SelectCityTopN(int limit)
        {
            return DatabaseClient.SelectList<DashboardSliceDto>(
                @"SELECT COALESCE(NULLIF(c.name, ''), c.id::text) AS Label, COUNT(o.id) AS Value
                FROM observation o JOIN city c ON c.id = o.city_id
                WHERE o.status = 0
                GROUP BY c.id, c.name
                ORDER BY 2 DESC
                LIMIT @Limit",
                new { Limit = limit });
        }

        /// <inheritdoc/>
        public List<DashboardTrendRowDto> SelectSyncFailureTrend(DateTime from)
        {
            return DatabaseClient.SelectList<DashboardTrendRowDto>(
                "SELECT to_char(start_time, 'YYYY-MM-DD') AS Label, COUNT(*) AS Value FROM sync_log WHERE status NOT IN (0, 1) AND start_time >= @From GROUP BY 1 ORDER BY 1",
                new { From = from });
        }

        /// <inheritdoc/>
        public List<DashboardTrendRowDto> SelectReportTrend(DateTime from)
        {
            return DatabaseClient.SelectList<DashboardTrendRowDto>(
                "SELECT to_char(create_time, 'YYYY-MM-DD') AS Label, COUNT(*) AS Value FROM report WHERE create_time >= @From GROUP BY 1 ORDER BY 1",
                new { From = from });
        }

        /// <inheritdoc/>
        public UpsertCountDto BatchUpsert(List<Observation> rows)
        {
            UpsertCountDto result = new UpsertCountDto();
            if (rows.Count == 0)
            {
                return result;
            }

            string columnList = string.Join(", ", UPSERT_COLUMNS.Select(c => c.Column));
            string conflictSet = string.Join(", ", UPSERT_UPDATE_COLUMNS.Select(c => $"{c} = EXCLUDED.{c}"));
            string sqlPrefix = $"INSERT INTO observation ({columnList}) VALUES ";
            string sqlSuffix = $" ON CONFLICT (source, source_observation_id) DO UPDATE SET {conflictSet} RETURNING (xmax = 0) AS IsInsert";

            for (int start = 0; start < rows.Count; start += UPSERT_SQL_BATCH_SIZE)
            {
                List<Observation> batch = rows.Skip(start).Take(UPSERT_SQL_BATCH_SIZE).ToList();
                Dictionary<string, object?> param = new Dictionary<string, object?>();
                StringBuilder values = new StringBuilder();
                for (int i = 0; i < batch.Count; i++)
                {
                    if (i > 0)
                    {
                        values.Append(", ");
                    }
                    values.Append('(').Append(string.Join(", ", UPSERT_COLUMNS.Select((c, j) => $"@r{i}_c{j}"))).Append(')');
                    for (int j = 0; j < UPSERT_COLUMNS.Length; j++)
                    {
                        param[$"r{i}_c{j}"] = UPSERT_COLUMNS[j].Item2(batch[i]);
                    }
                }

                List<UpsertFlagRow> flags = DatabaseClient.SelectList<UpsertFlagRow>(sqlPrefix + values.ToString() + sqlSuffix, param);
                int inserted = flags.Count(f => f.IsInsert);
                result.Insert += inserted;
                result.Update += flags.Count - inserted;
            }
            return result;
        }

        /// <inheritdoc/>
        public List<string> SelectRandomSampleIds(AtlasSource source, int cityId, int count)
        {
            return DatabaseClient.SelectList<StringRow>(
                "SELECT source_observation_id AS Value FROM observation WHERE source = @Source AND city_id = @CityId AND status = 0 ORDER BY random() LIMIT @Count",
                new { Source = (short)source, CityId = cityId, Count = count })
                .FindAll(r => !string.IsNullOrEmpty(r.Value))
                .ConvertAll(r => r.Value!);
        }

        /// <inheritdoc/>
        public int HideByAuthor(string userLogin, DateTime now)
        {
            return DatabaseClient.Execute(
                "UPDATE observation SET status = 1, update_time = @Now WHERE user_login = @UserLogin AND status = 0",
                new { UserLogin = userLogin, Now = now });
        }

        /// <inheritdoc/>
        public int MarkSourceDeleted(AtlasSource source, List<string> sourceObservationIds)
        {
            if (sourceObservationIds.Count == 0)
            {
                return 0;
            }
            return DatabaseClient.Execute(
                "UPDATE observation SET status = 2, update_time = @UpdateTime WHERE source = @Source AND source_observation_id = ANY(@Ids) AND status = 0",
                new { UpdateTime = DateTime.UtcNow, Source = (short)source, Ids = sourceObservationIds.ToArray() });
        }
    }
}
