using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 分类单元 Dao / Taxon Dao
    /// </summary>
    public interface ITaxonDao : ISuperDao<Taxon>
    {
        /// <summary>
        /// 公开站分类档案（枚举以数字投影）/ Public taxon profile (enums projected as numbers)
        /// </summary>
        /// <param name="taxonId">taxon.id</param>
        /// <returns>档案，不存在返回 null / The profile, or null when missing</returns>
        TaxonInfoDto? GetTaxonInfo(string taxonId);

        /// <summary>
        /// 采集端批量幂等入库：按 (source, source_taxon_id) ON CONFLICT 刷新档案字段（含 status，入库即活跃），
        /// 内部按 200 行/语句分批 / Collector batch idempotent upsert keyed on (source, source_taxon_id);
        /// profile fields refreshed (status included: being ingested implies active), chunked at 200 rows per statement
        /// </summary>
        /// <param name="rows">待入库行（调用方保证批内源站ID不重复）/ Rows to upsert (caller dedupes source ids in-batch)</param>
        /// <returns>新增/更新行数 / Inserted / updated counts</returns>
        UpsertCountDto SaveBatch(List<Taxon> rows);

        /// <summary>
        /// 按源站ID批量查已有分类（find-or-create 的查找步骤）
        /// Load existing taxa by source ids (the lookup step of find-or-create)
        /// </summary>
        /// <param name="source">数据源 / Data source</param>
        /// <param name="sourceTaxonIds">源站分类ID（原样字符串）/ Source taxon ids (verbatim strings)</param>
        /// <returns>已存在的分类行 / The existing rows</returns>
        List<Taxon> GetBySourceIdsList(AtlasSource source, List<string> sourceTaxonIds);

        /// <summary>
        /// 取最久未刷新的一批分类（分类刷新任务的轮转批次）
        /// Take the least-recently refreshed batch (rotation batch of the taxon-refresh task)
        /// </summary>
        /// <param name="source">数据源 / Data source</param>
        /// <param name="limit">批量上限 / Batch limit</param>
        /// <returns>按 update_time 升序的分类行 / Rows ordered by update_time ascending</returns>
        List<Taxon> GetRefreshBatchList(AtlasSource source, int limit);
    }
}
