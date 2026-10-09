using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Po;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 城市集邮缺口缓存 Dao（采集端每周全量重写；公开接口只读）
    /// City collection-gap cache Dao (fully rewritten weekly by the collector; read-only for the public API)
    /// </summary>
    public interface ICityWantedTaxonDao : ISuperDao<CityWantedTaxon>
    {
        /// <summary>
        /// 每城全量重写（事务内 DELETE + 批量 INSERT）
        /// Full rewrite for one city (DELETE + batched INSERT inside a transaction)
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="rows">快照行（调用方已赋雪花 id）/ Snapshot rows (snowflake ids assigned by the caller)</param>
        /// <returns>写入行数 / Rows written</returns>
        int SaveAll(int cityId, List<CityWantedTaxon> rows);

        /// <summary>
        /// 公开接口读取：按周边记录数降序 / Public read, descending by the nearby count
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="iconicList">类群筛选（空 = 全部）/ Iconic filter (empty = all)</param>
        /// <param name="limit">条数上限 / Limit</param>
        /// <returns>wanted 条目 / Wanted items</returns>
        List<WantedTaxonItemDto> GetPublicList(int cityId, short[] iconicList, int limit);
    }
}
