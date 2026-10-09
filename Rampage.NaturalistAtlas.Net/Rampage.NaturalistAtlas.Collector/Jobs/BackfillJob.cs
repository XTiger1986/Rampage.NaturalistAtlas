using Rampage.NaturalistAtlas.Model.Enums;
using System;

namespace Rampage.NaturalistAtlas.Collector.Jobs
{
    /// <summary>
    /// 历史回补作业（10 分钟节拍）：每次跑一个时间窗切片（config 存进度断点续采），完成态 no-op
    /// Historical backfill job (10-minute cadence): one time-window slice per run
    /// (progress persisted in config for resumption); a no-op once done
    /// </summary>
    public class BackfillJob : CollectorJobBase
    {
        /// <inheritdoc/>
        protected override void ExecuteJob()
        {
            foreach (int cityId in GetCollectionCityIds())
            {
                CollectionService.RunBackfillSliceAsync(AtlasSource.INaturalist, cityId).GetAwaiter().GetResult();
                CollectionService.RunBackfillSliceAsync(AtlasSource.Gbif, cityId).GetAwaiter().GetResult();
            }
        }
    }
}
