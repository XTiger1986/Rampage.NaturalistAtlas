using Rampage.DependencyInjection;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.SchedulerJob;
using System;

namespace Rampage.NaturalistAtlas.Collector.Jobs
{
    /// <summary>
    /// 采集日志清理作业（每日凌晨 4:50）：删除 N 天前的成功日志，失败/熔断行保留供排障。
    /// 增量任务每小时、回补每 10 分钟各写一条，不清理的话 sync_log 会无限增长
    /// Sync-log cleanup job (daily at 04:50): deletes success rows older than N days while
    /// failed/skipped rows stay for troubleshooting. The hourly increment and the 10-minute
    /// backfill each append a row — without cleanup sync_log grows unbounded
    /// </summary>
    public class SyncLogCleanJob : SchedulerJobBase
    {
        /// <summary>成功日志保留天数 / Days to retain success log rows</summary>
        private const int SUCCESS_RETENTION_DAYS = 30;

        [Auto]
        protected ISyncLogDao SyncLogDao = default!;

        /// <inheritdoc/>
        protected override void ExecuteJob()
        {
            DateTime threshold = DateTime.UtcNow.AddDays(-SUCCESS_RETENTION_DAYS);
            int deleted = SyncLogDao.DeleteOldSuccessLogs(threshold);
            if (deleted > 0)
            {
                LogInfo($"[日志清理] 删除{deleted}条{SUCCESS_RETENTION_DAYS}天前的成功日志");
            }
        }
    }
}
