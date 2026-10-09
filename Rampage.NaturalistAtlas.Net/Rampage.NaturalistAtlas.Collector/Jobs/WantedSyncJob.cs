using System;

namespace Rampage.NaturalistAtlas.Collector.Jobs
{
    /// <summary>
    /// 集邮缺口作业（每周四 04:30，错开整点增量）：iNat v1 wanted 周边差集 → city_wanted_taxon 每城全量重写
    /// Collection-gap job (weekly Thursday 04:30, offset from the hourly increment):
    /// the iNat v1 wanted diff fully rewrites city_wanted_taxon per city
    /// </summary>
    public class WantedSyncJob : CollectorJobBase
    {
        /// <inheritdoc/>
        protected override void ExecuteJob()
        {
            CollectionService.RunWantedSyncAsync().GetAwaiter().GetResult();
        }
    }
}
