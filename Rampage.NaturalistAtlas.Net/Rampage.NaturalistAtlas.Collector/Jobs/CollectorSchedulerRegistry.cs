using FluentScheduler;
using System;

namespace Rampage.NaturalistAtlas.Collector.Jobs
{
    /// <summary>
    /// 采集作业表：增量（启动即跑+每小时）、回补（启动即跑+每10分钟切片续采）、
    /// 分类刷新（每日 03:00）、物候直方图（每日 03:30）、删除对账（每周四 04:00）、
    /// 集邮缺口（每周四 04:30）、日志清理（每日 04:50）
    /// Collector job schedule: increment (now + hourly), backfill (now + one slice per 10 minutes),
    /// taxon refresh (daily 03:00), phenology histogram (daily 03:30), deletion check (weekly
    /// Thursday 04:00), collection gaps (weekly Thursday 04:30), log cleanup (daily 04:50)
    /// </summary>
    public class CollectorSchedulerRegistry : Registry
    {

        public CollectorSchedulerRegistry()
        {
            Schedule<IncrementJob>().NonReentrant().ToRunNow().AndEvery(1).Hours();
            Schedule<BackfillJob>().NonReentrant().ToRunNow().AndEvery(10).Minutes();
            Schedule<TaxonRefreshJob>().NonReentrant().ToRunEvery(1).Days().At(3, 0);
            Schedule<HistogramJob>().NonReentrant().ToRunEvery(1).Days().At(3, 30);
            Schedule<DeletionCheckJob>().NonReentrant().ToRunEvery(1).Weeks().On(DayOfWeek.Thursday).At(4, 0);
            Schedule<WantedSyncJob>().NonReentrant().ToRunEvery(1).Weeks().On(DayOfWeek.Thursday).At(4, 30);
            Schedule<SyncLogCleanJob>().NonReentrant().ToRunEvery(1).Days().At(4, 50);
        }

    }
}
