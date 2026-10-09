using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 预聚合物候统计 Dao 实现 / Pre-aggregated emergence stat Dao implementation
    /// </summary>
    public class EmergenceStatDao : SuperDao<EmergenceStat>, IEmergenceStatDao
    {
        /// <inheritdoc/>
        public List<EmergenceRowDto> GetTenDaySeriesList(int cityId, string taxonId, int fromYear)
        {
            return DatabaseClient.SelectList<EmergenceRowDto>(
                @"SELECT ten_day::int AS Idx, SUM(record_count) AS RecordCount, SUM(research_count) AS ResearchCount
                FROM emergence_stat
                WHERE city_id = @CityId AND taxon_id = @TaxonId AND stat_year >= @FromYear
                GROUP BY ten_day",
                new { CityId = cityId, TaxonId = taxonId, FromYear = fromYear });
        }
    }
}
