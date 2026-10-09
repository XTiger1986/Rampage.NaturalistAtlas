using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 物候全历史缓存 Dao / Full-history emergence cache Dao
    /// </summary>
    public interface IEmergenceHistoryDao : ISuperDao<EmergenceHistory>
    {
        /// <summary>
        /// 逐月全历史物候（源站直方图口径，无研究级拆分）/ Monthly full-history emergence (source histogram; no research split)
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="taxonId">taxon.id</param>
        /// <returns>月份行，无缓存时为空 / Month rows; empty when not cached</returns>
        List<EmergenceRowDto> SelectMonthSeries(int cityId, string taxonId);

        /// <summary>
        /// 全量重写一个 (城市, 分类) 的 12 个月行（删除旧行 + 分块批量插入，事务内）
        /// Fully rewrite the 12 month rows of a (city, taxon) — delete then chunked bulk insert in a transaction
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="taxonId">taxon.id</param>
        /// <param name="rows">12 个月行（month 1~12）/ The 12 month rows</param>
        void ReplaceMonths(int cityId, string taxonId, List<EmergenceHistory> rows);

        /// <summary>
        /// 选取待刷新直方图的分类：该城市有可见观察的分类，按直方图缓存新旧排序
        /// （从未拉过的最优先，其次最久未更新——每日低频渐进覆盖全量）
        /// Pick taxa whose histogram is due: taxa with visible observations in the city, ordered
        /// by cache freshness (never-fetched first, then least-recently updated — a daily
        /// low-frequency walk that gradually covers everything)
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="limit">取前 N 个 / Top N</param>
        /// <returns>分类 id 列表（本站雪花 id）/ Taxon ids (our snowflake ids)</returns>
        List<string> SelectDueTaxaIds(int cityId, int limit);
    }
}
