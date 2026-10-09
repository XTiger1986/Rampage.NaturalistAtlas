using Rampage.NaturalistAtlas.Model.Enums;
using System;

namespace Rampage.NaturalistAtlas.Collector.Jobs
{
    /// <summary>
    /// 分类档案刷新作业（每日凌晨 3 点）：双源各刷最久未更新的一批（全局任务 city_id=0）
    /// Taxon profile refresh job (daily at 03:00): each source refreshes its least-recently-updated batch (the global task, city_id=0)
    /// </summary>
    public class TaxonRefreshJob : CollectorJobBase
    {
        /// <inheritdoc/>
        protected override void ExecuteJob()
        {
            CollectionService.RunTaxonRefreshAsync(AtlasSource.INaturalist).GetAwaiter().GetResult();
            CollectionService.RunTaxonRefreshAsync(AtlasSource.Gbif).GetAwaiter().GetResult();
        }
    }
}
