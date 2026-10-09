using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using System;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 采集水位 Dao / Sync cursor Dao
    /// </summary>
    public interface ISyncCursorDao : ISuperDao<SyncCursor>
    {
        /// <summary>
        /// 按 源+城市+任务 定位水位行 / Locate the cursor row by source + city + task
        /// </summary>
        /// <param name="source">数据源 / Data source</param>
        /// <param name="cityId">城市Id（全局任务约定 0）/ City id (0 by convention for global tasks)</param>
        /// <param name="task">任务类型（SyncTaskCodes）/ Task type (SyncTaskCodes)</param>
        /// <returns>水位行，不存在返回 null / The cursor row, or null when missing</returns>
        SyncCursor? SelectByTask(AtlasSource source, int cityId, string task);
    }
}
