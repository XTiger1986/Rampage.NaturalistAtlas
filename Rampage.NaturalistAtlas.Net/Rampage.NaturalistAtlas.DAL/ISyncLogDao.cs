using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using System;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 采集运行日志 Dao / Sync log Dao
    /// </summary>
    public interface ISyncLogDao : ISuperDao<SyncLog>
    {
        /// <summary>
        /// 把超时未收尾的"运行中"日志标记为失败（进程中断遗留的僵尸行）
        /// Mark overdue still-running log rows failed (zombie rows left by an interrupted process)
        /// </summary>
        /// <param name="source">数据源 / Data source</param>
        /// <param name="cityId">城市Id / City id</param>
        /// <param name="task">任务类型 / Task type</param>
        /// <param name="before">仅处理开始时间早于该时刻的行 / Only rows started before this moment</param>
        /// <returns>收尾行数 / Affected row count</returns>
        int UpdateStaleRunningFailed(AtlasSource source, int cityId, string task, DateTime before);

        /// <summary>
        /// 清理过期的成功日志（失败/熔断行保留供排障，量也小）；防日志表被日常空转行无限灌满
        /// Purge aged success rows (failed/skipped rows stay for troubleshooting, and are rare);
        /// keeps the table from filling with routine no-op rows
        /// </summary>
        /// <param name="before">仅删除开始时间早于该时刻的成功行 / Only success rows started before this moment</param>
        /// <returns>删除行数 / Deleted row count</returns>
        int DeleteOldSuccessLogs(DateTime before);
    }
}
