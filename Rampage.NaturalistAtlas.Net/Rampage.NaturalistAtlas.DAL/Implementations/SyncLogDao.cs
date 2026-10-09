using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 采集运行日志 Dao 实现 / Sync log Dao implementation
    /// </summary>
    public class SyncLogDao : SuperDao<SyncLog>, ISyncLogDao
    {
        /// <inheritdoc/>
        public int UpdateStaleRunningFailed(AtlasSource source, int cityId, string task, DateTime before)
        {
            return DatabaseClient.Execute(
                "UPDATE sync_log SET status = 2, end_time = @Time, message = '进程中断，未收尾 / interrupted, never finalized', update_time = @Time " +
                "WHERE source = @Source AND city_id = @CityId AND task = @Task AND status = 0 AND start_time < @Before",
                new { Time = DateTime.UtcNow, Source = (short)source, CityId = cityId, Task = task, Before = before });
        }

        /// <inheritdoc/>
        public int DeleteOldSuccessLogs(DateTime before)
        {
            return DatabaseClient.Execute(
                "DELETE FROM sync_log WHERE status = 1 AND start_time < @Before",
                new { Before = before });
        }
    }
}
