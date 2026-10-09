using Rampage.Data;
using Rampage.Data.Query;
using Rampage.Model;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 采集任务管理服务（管理后台）：水位展示 + config 参数编辑（手动触发待采集端接入）
    /// Sync cursor admin service: watermark display + config editing (manual trigger awaits the collector)
    /// </summary>
    public interface ISyncCursorService : ISuperService<SyncCursor, ISyncCursorDao>
    {
        /// <summary>
        /// 分页查询采集任务（批量补齐城市名）/ Query the paged task list with city names
        /// </summary>
        /// <param name="filter">筛选条件 / Filter</param>
        /// <param name="pager">分页参数 / Pager</param>
        /// <returns>任务列表（附城市名字典）/ Tasks plus a city-name dictionary</returns>
        (List<SyncCursor> List, Dictionary<int, string> CityNameDict) GetSyncCursorPageList(SyncCursorAdminFilter filter, PagerInfo pager);

        /// <summary>
        /// 任务配置编辑页模型（解析 config 常用字段）/ Config edit model (common config fields parsed)
        /// </summary>
        /// <param name="id">sync_cursor.id / The cursor id</param>
        /// <returns>编辑模型，不存在返回 null / The edit model, or null when missing</returns>
        SyncTaskConfigEditDto? GetSyncCursorEdit(int id);

        /// <summary>
        /// 保存任务配置：仅覆盖表单字段，其余 config 键原样保留
        /// Save the task config: only form fields are overwritten; other config keys are preserved
        /// </summary>
        /// <param name="input">保存输入 / Save input</param>
        /// <returns>操作结果 / Result</returns>
        R SaveSyncCursorConfig(SyncTaskConfigInput input);
    }
}
