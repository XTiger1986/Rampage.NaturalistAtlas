using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 预聚合物候统计 Dao / Pre-aggregated emergence stat Dao
    /// </summary>
    public interface IEmergenceStatDao : ISuperDao<EmergenceStat>
    {
        /// <summary>
        /// 逐旬物候（跨年份按旬序合计）/ Ten-day emergence summed across years by index
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="taxonId">taxon.id</param>
        /// <param name="fromYear">起始统计年份（含）/ First stat year (inclusive)</param>
        /// <returns>旬序聚合行，无预聚合数据时为空 / Ten-day rows; empty when not pre-aggregated</returns>
        List<EmergenceRowDto> SelectTenDaySeries(int cityId, string taxonId, int fromYear);
    }
}
