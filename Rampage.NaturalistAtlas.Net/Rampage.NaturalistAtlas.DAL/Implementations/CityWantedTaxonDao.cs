using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Po;
using System.Collections.Generic;
using System.Text;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 城市集邮缺口缓存 Dao 实现
    /// City collection-gap cache Dao implementation
    /// </summary>
    public class CityWantedTaxonDao : SuperDao<CityWantedTaxon>, ICityWantedTaxonDao
    {
        /// <summary>单语句插入行数（参数个数 = 行数 × 列数，需低于 PG 65535 上限）/ Rows per INSERT statement</summary>
        private const int INSERT_BATCH_SIZE = 100;

        /// <inheritdoc/>
        public int ReplaceAll(int cityId, List<CityWantedTaxon> rows)
        {
            DatabaseClient.BeginTransaction();
            try
            {
                DatabaseClient.Execute("DELETE FROM city_wanted_taxon WHERE city_id = @CityId", new { CityId = cityId });
                int written = 0;
                for (int start = 0; start < rows.Count; start += INSERT_BATCH_SIZE)
                {
                    List<CityWantedTaxon> batch = rows.GetRange(start, System.Math.Min(INSERT_BATCH_SIZE, rows.Count - start));
                    Dictionary<string, object?> param = new Dictionary<string, object?> { ["CityId"] = cityId };
                    StringBuilder values = new StringBuilder();
                    for (int i = 0; i < batch.Count; i++)
                    {
                        if (i > 0)
                        {
                            values.Append(", ");
                        }
                        values.Append($"(@i{i}, @CityId, @s{i}, @t{i}, @r{i}, @n{i}, @cn{i}, @ce{i}, @ic{i}, @ct{i}, @ps{i}, @pm{i}, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)");
                        CityWantedTaxon row = batch[i];
                        param[$"i{i}"] = row.Id;
                        param[$"s{i}"] = row.Source;
                        param[$"t{i}"] = row.SourceTaxonId;
                        param[$"r{i}"] = row.Rank;
                        param[$"n{i}"] = row.ScientificName ?? string.Empty;
                        param[$"cn{i}"] = row.CommonNameCn ?? string.Empty;
                        param[$"ce{i}"] = row.CommonNameEn ?? string.Empty;
                        param[$"ic{i}"] = row.IconicTaxon;
                        param[$"ct{i}"] = row.NearbyCount;
                        param[$"ps{i}"] = row.PhotoSquareUrl ?? string.Empty;
                        param[$"pm{i}"] = row.PhotoMediumUrl ?? string.Empty;
                    }
                    string sql =
                        "INSERT INTO city_wanted_taxon (id, city_id, source, source_taxon_id, rank, scientific_name, common_name_cn, common_name_en, iconic_taxon, nearby_count, photo_square_url, photo_medium_url, create_time, update_time) VALUES " +
                        values.ToString();
                    written += DatabaseClient.Execute(sql, param);
                }
                DatabaseClient.CommitTransaction();
                return written;
            }
            catch
            {
                DatabaseClient.RollbackTransaction();
                throw;
            }
        }

        /// <inheritdoc/>
        public List<WantedTaxonItemDto> SelectPublic(int cityId, short[] iconicList, int limit)
        {
            string iconicWhere = iconicList.Length > 0 ? " AND iconic_taxon IN (" + string.Join(",", iconicList) + ")" : string.Empty;
            return DatabaseClient.SelectList<WantedTaxonItemDto>(
                $"SELECT source_taxon_id AS SourceTaxonId, rank AS Rank, scientific_name AS ScientificName," +
                $" NULLIF(common_name_cn, '') AS CommonNameCn, NULLIF(common_name_en, '') AS CommonNameEn," +
                $" iconic_taxon AS IconicTaxon, nearby_count AS NearbyCount," +
                $" NULLIF(photo_square_url, '') AS PhotoSquareUrl, NULLIF(photo_medium_url, '') AS PhotoMediumUrl," +
                $" photo_license AS PhotoLicense" +
                $" FROM city_wanted_taxon WHERE city_id = @CityId{iconicWhere}" +
                $" ORDER BY nearby_count DESC, source_taxon_id LIMIT @Limit",
                new { CityId = cityId, Limit = limit });
        }
    }
}
