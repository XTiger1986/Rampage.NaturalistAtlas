using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 物候全历史缓存 Dao 实现 / Full-history emergence cache Dao implementation
    /// </summary>
    public class EmergenceHistoryDao : SuperDao<EmergenceHistory>, IEmergenceHistoryDao
    {
        /// <summary>批量插入的每块行数 / Rows per bulk-insert chunk</summary>
        private const int INSERT_CHUNK_SIZE = 100;

        /// <summary>单值字符串行（SQL 投影）/ Single string-value row (SQL projection)</summary>
        private class StringRow
        {
            public string? Value { get; set; }
        }

        /// <inheritdoc/>
        public List<EmergenceRowDto> SelectMonthSeries(int cityId, string taxonId)
        {
            return DatabaseClient.SelectList<EmergenceRowDto>(
                @"SELECT month::int AS Idx, record_count::bigint AS RecordCount, 0::bigint AS ResearchCount
                FROM emergence_history
                WHERE city_id = @CityId AND taxon_id = @TaxonId",
                new { CityId = cityId, TaxonId = taxonId });
        }

        /// <inheritdoc/>
        public void ReplaceMonths(int cityId, string taxonId, List<EmergenceHistory> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }
            DatabaseClient.BeginTransaction();
            try
            {
                DatabaseClient.Execute(
                    "DELETE FROM emergence_history WHERE city_id = @CityId AND taxon_id = @TaxonId",
                    new { CityId = cityId, TaxonId = taxonId });
                for (int start = 0; start < rows.Count; start += INSERT_CHUNK_SIZE)
                {
                    List<EmergenceHistory> chunk = rows.Skip(start).Take(INSERT_CHUNK_SIZE).ToList();
                    List<string> values = new List<string>();
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        values.Add($"(@Id{i}, @CityId, @TaxonId, @Month{i}, @Count{i}, @Time, @Time)");
                    }
                    Dictionary<string, object?> param = new Dictionary<string, object?>
                    {
                        ["CityId"] = cityId,
                        ["TaxonId"] = taxonId,
                        ["Time"] = DateTime.UtcNow
                    };
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        param[$"Id{i}"] = chunk[i].Id;
                        param[$"Month{i}"] = chunk[i].Month;
                        param[$"Count{i}"] = chunk[i].RecordCount;
                    }
                    DatabaseClient.Execute(
                        $"INSERT INTO emergence_history (id, city_id, taxon_id, month, record_count, create_time, update_time) VALUES {string.Join(", ", values)}",
                        param);
                }
                DatabaseClient.CommitTransaction();
            }
            catch
            {
                DatabaseClient.RollbackTransaction();
                throw;
            }
        }

        /// <inheritdoc/>
        public List<string> SelectDueTaxaIds(int cityId, int limit)
        {
            // MAX(eh.update_time) 为 NULL = 该城市此分类从未拉过直方图（最优先）；
            // 否则按缓存最旧优先，每日低频节拍渐进覆盖全量分类
            // NULL max = never fetched for this city (highest priority); otherwise
            // least-recently-refreshed first, so the daily cadence walks the whole set over time
            return DatabaseClient.SelectList<StringRow>(
                @"SELECT o.taxon_id AS Value
                FROM observation o
                LEFT JOIN emergence_history eh ON eh.city_id = o.city_id AND eh.taxon_id = o.taxon_id
                WHERE o.city_id = @CityId AND o.status = 0 AND o.taxon_id IS NOT NULL AND o.taxon_id <> ''
                GROUP BY o.taxon_id
                ORDER BY MAX(eh.update_time) NULLS FIRST, o.taxon_id
                LIMIT @Limit",
                new { CityId = cityId, Limit = limit })
                .ConvertAll(r => r.Value!)
                .FindAll(v => !string.IsNullOrEmpty(v));
        }
    }
}
