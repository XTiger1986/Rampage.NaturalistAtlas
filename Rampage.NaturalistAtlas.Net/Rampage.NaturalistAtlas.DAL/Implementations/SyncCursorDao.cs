using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 采集水位 Dao 实现 / Sync cursor Dao implementation
    /// </summary>
    public class SyncCursorDao : SuperDao<SyncCursor>, ISyncCursorDao
    {
        /// <inheritdoc/>
        public SyncCursor? SelectByTask(AtlasSource source, int cityId, string task)
        {
            List<SyncCursor> rows = DatabaseClient.SelectList<SyncCursor>(
                @"SELECT id AS Id, source AS Source, city_id AS CityId, task AS Task, watermark AS Watermark,
                    config AS Config, last_run_status AS LastRunStatus, last_run_time AS LastRunTime,
                    last_run_message AS LastRunMessage, create_time AS CreateTime, update_time AS UpdateTime
                FROM sync_cursor WHERE source = @Source AND city_id = @CityId AND task = @Task",
                new { Source = (short)source, CityId = cityId, Task = task });
            return rows.Count > 0 ? rows[0] : null;
        }
    }
}
