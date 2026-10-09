using Rampage.NaturalistAtlas.Model.Enums;
using System;

namespace Rampage.NaturalistAtlas.Collector.Jobs
{
    /// <summary>
    /// 删除对账作业（周度）：抽样比对源站，缺席记录下线 status=2
    /// Deletion reconciliation job (weekly): sample against the sources; absent rows go offline with status=2
    /// </summary>
    public class DeletionCheckJob : CollectorJobBase
    {
        /// <inheritdoc/>
        protected override void ExecuteJob()
        {
            foreach (int cityId in GetCollectionCityIds())
            {
                CollectionService.RunDeletionCheckAsync(AtlasSource.INaturalist, cityId).GetAwaiter().GetResult();
                CollectionService.RunDeletionCheckAsync(AtlasSource.Gbif, cityId).GetAwaiter().GetResult();
            }
        }
    }
}
