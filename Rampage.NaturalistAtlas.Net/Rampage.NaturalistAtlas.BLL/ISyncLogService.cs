using Rampage.Data;
using Rampage.Data.Query;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 同步日志管理服务（管理后台，只读） / Sync log admin service (read-only)
    /// </summary>
    public interface ISyncLogService : ISuperService<SyncLog, ISyncLogDao>
    {
        /// <summary>
        /// 分页查询同步日志（批量补齐城市名）/ Query the paged log list with city names
        /// </summary>
        /// <param name="filter">筛选条件 / Filter</param>
        /// <param name="pager">分页参数 / Pager</param>
        /// <returns>日志列表（附城市名字典）/ Logs plus a city-name dictionary</returns>
        (List<SyncLog> List, Dictionary<int, string> CityNameDict) GetSyncLogPageList(SyncLogAdminFilter filter, PagerInfo pager);

        /// <summary>
        /// 按主键取日志（详情页）/ Get a log by id (detail page)
        /// </summary>
        /// <param name="logId">日志Id（雪花）/ Log id (snowflake)</param>
        /// <returns>日志实体，不存在返回 null / The log, or null when missing</returns>
        SyncLog? GetSyncLog(string logId);
    }
}
