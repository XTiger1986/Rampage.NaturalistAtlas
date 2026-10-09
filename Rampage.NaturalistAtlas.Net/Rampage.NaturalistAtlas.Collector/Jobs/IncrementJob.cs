using Rampage.NaturalistAtlas.Model.Enums;
using System;

namespace Rampage.NaturalistAtlas.Collector.Jobs
{
    /// <summary>
    /// 增量同步作业（每小时）：双源 × 采集城市；服务层单源熔断不影响另一源
    /// Incremental sync job (hourly): both sources × collection cities; a circuit-broken
    /// source never affects the other
    /// </summary>
    public class IncrementJob : CollectorJobBase
    {
        /// <inheritdoc/>
        protected override void ExecuteJob()
        {
            foreach (int cityId in GetCollectionCityIds())
            {
                CollectionService.RunIncrementAsync(AtlasSource.INaturalist, cityId).GetAwaiter().GetResult();
                CollectionService.RunIncrementAsync(AtlasSource.Gbif, cityId).GetAwaiter().GetResult();
            }
        }
    }
}
