using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 分类单元 Dao 实现 / Taxon Dao implementation
    /// </summary>
    public class TaxonDao : SuperDao<Taxon>, ITaxonDao
    {
        /// <summary>upsert RETURNING 行（xmax=0 即新增）/ Upsert RETURNING row (xmax = 0 means inserted)</summary>
        private class UpsertFlagRow
        {
            public bool IsInsert { get; set; }
        }

        /// <summary>
        /// upsert 列规格：(列名, 取值)。枚举显式转 short，避免驱动枚举映射歧义
        /// Upsert column spec: (column, value getter); enums cast to short explicitly to avoid driver enum-mapping ambiguity
        /// </summary>
        private static readonly (string Column, Func<Taxon, object?>)[] UPSERT_COLUMNS =
        {
            ("id", r => r.Id),
            ("source", r => (short)r.Source),
            ("source_taxon_id", r => r.SourceTaxonId),
            ("rank", r => (short)r.Rank),
            ("scientific_name", r => r.ScientificName),
            ("common_name_cn", r => r.CommonNameCn),
            ("common_name_en", r => r.CommonNameEn),
            ("iconic_taxon", r => (short)r.IconicTaxon),
            ("threatened", r => r.Threatened),
            ("ancestry", r => r.Ancestry),
            ("status", r => (short)r.Status),
            ("create_time", r => r.CreateTime),
            ("update_time", r => r.UpdateTime)
        };

        /// <summary>
        /// DO UPDATE 覆盖列（含 status：被观察引用即活跃）/ Columns overwritten by DO UPDATE (status included: referenced by an observation implies active)
        /// </summary>
        private static readonly string[] UPSERT_UPDATE_COLUMNS =
        {
            "rank", "scientific_name", "common_name_cn", "common_name_en", "iconic_taxon",
            "threatened", "ancestry", "status", "update_time"
        };

        private const int UPSERT_SQL_BATCH_SIZE = 200;

        private const string SELECT_COLUMN_SQL =
            "id AS Id, source AS Source, source_taxon_id AS SourceTaxonId, rank AS Rank, scientific_name AS ScientificName, " +
            "common_name_cn AS CommonNameCn, common_name_en AS CommonNameEn, iconic_taxon AS IconicTaxon, " +
            "threatened AS Threatened, ancestry AS Ancestry, status AS Status, create_time AS CreateTime, update_time AS UpdateTime";

        /// <inheritdoc/>
        public TaxonInfoDto? SelectTaxonInfo(string taxonId)
        {
            return DatabaseClient.SelectOne<TaxonInfoDto>(
                @"SELECT id AS TaxonId, source AS Source, source_taxon_id AS SourceTaxonId, rank AS Rank,
                    scientific_name AS ScientificName, NULLIF(common_name_cn, '') AS CommonNameCn, NULLIF(common_name_en, '') AS CommonNameEn,
                    iconic_taxon AS IconicTaxon, threatened AS Threatened, NULLIF(ancestry, '') AS Ancestry
                FROM taxon WHERE id = @Id", new { Id = taxonId });
        }

        /// <inheritdoc/>
        public UpsertCountDto BatchUpsert(List<Taxon> rows)
        {
            UpsertCountDto result = new UpsertCountDto();
            if (rows.Count == 0)
            {
                return result;
            }

            string columnList = string.Join(", ", UPSERT_COLUMNS.Select(c => c.Column));
            string conflictSet = string.Join(", ", UPSERT_UPDATE_COLUMNS.Select(c => $"{c} = EXCLUDED.{c}"));
            string sqlPrefix = $"INSERT INTO taxon ({columnList}) VALUES ";
            string sqlSuffix = $" ON CONFLICT (source, source_taxon_id) DO UPDATE SET {conflictSet} RETURNING (xmax = 0) AS IsInsert";

            for (int start = 0; start < rows.Count; start += UPSERT_SQL_BATCH_SIZE)
            {
                List<Taxon> batch = rows.Skip(start).Take(UPSERT_SQL_BATCH_SIZE).ToList();
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
        public List<Taxon> SelectBySourceIds(AtlasSource source, List<string> sourceTaxonIds)
        {
            if (sourceTaxonIds.Count == 0)
            {
                return new List<Taxon>();
            }
            return DatabaseClient.SelectList<Taxon>(
                $"SELECT {SELECT_COLUMN_SQL} FROM taxon WHERE source = @Source AND source_taxon_id = ANY(@Ids)",
                new { Source = (short)source, Ids = sourceTaxonIds.ToArray() });
        }

        /// <inheritdoc/>
        public List<Taxon> SelectRefreshBatch(AtlasSource source, int limit)
        {
            return DatabaseClient.SelectList<Taxon>(
                $"SELECT {SELECT_COLUMN_SQL} FROM taxon WHERE source = @Source ORDER BY update_time ASC LIMIT @Limit",
                new { Source = (short)source, Limit = limit });
        }
    }
}
