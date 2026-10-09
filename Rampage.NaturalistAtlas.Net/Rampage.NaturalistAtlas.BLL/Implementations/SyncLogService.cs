using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 同步日志管理服务实现（管理后台，只读） / Sync log admin service implementation (read-only)
    /// </summary>
    public class SyncLogService : SuperService<SyncLog, ISyncLogDao>, ISyncLogService
    {
        [Auto]
        protected ISyncLogDao SyncLogDao = default!;

        [Auto]
        protected ICityDao CityDao = default!;


        /// <inheritdoc/>
        public (List<SyncLog>, Dictionary<int, string>) GetSyncLogPageList(SyncLogAdminFilter filter, PagerInfo pager)
        {
            FilterWrapper<SyncLog> wrapper = new(filter);
            // 开始时间新→旧
            wrapper.OrderByDescending(m => m.StartTime);

            List<SyncLog> list = SyncLogDao.SelectPage(pager, wrapper);

            Dictionary<int, string> cityNameDict = new Dictionary<int, string>();
            List<int> distinctCityIdList = list.ConvertAll(l => l.CityId).Distinct().ToList();
            if (distinctCityIdList.Count > 0)
            {
                foreach (City city in CityDao.SelectByIds(distinctCityIdList))
                {
                    cityNameDict[city.Id] = city.Name ?? city.Id.ToString();
                }
            }
            return (list, cityNameDict);
        }

        /// <inheritdoc/>
        public SyncLog? GetSyncLog(string logId)
        {
            if (string.IsNullOrWhiteSpace(logId))
            {
                return null;
            }
            return SyncLogDao.SelectById(logId);
        }
    }
}
