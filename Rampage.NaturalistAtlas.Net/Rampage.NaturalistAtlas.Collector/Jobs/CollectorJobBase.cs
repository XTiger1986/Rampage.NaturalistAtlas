using Rampage.DependencyInjection;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.SchedulerJob;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.Collector.Jobs
{
    /// <summary>
    /// 采集作业基类：注入采集服务与设置服务，提供采集城市清单解析。
    /// 服务层已消化全部异常（sync_log 记状态），作业层无须再捕获
    /// Collector job base: injects the collection and setting services and parses the
    /// collection-city list. The service layer absorbs every exception (recorded in sync_log),
    /// so jobs need no try/catch
    /// </summary>
    public abstract class CollectorJobBase : SchedulerJobBase
    {
        [Auto]
        protected IAtlasCollectionService CollectionService = default!;

        [Auto]
        protected ISettingService SettingService = default!;

        /// <summary>
        /// 解析采集城市清单（GeneralSetting.CollectionCityIds，逗号分隔；空/非法回退默认城市）
        /// Parse the collection-city list (GeneralSetting.CollectionCityIds, comma separated; falls back to the default city)
        /// </summary>
        protected List<int> GetCollectionCityIds()
        {
            GeneralSetting setting = SettingService.GetSetting<GeneralSetting>();
            List<int> ids = new List<int>();
            string? raw = setting.CollectionCityIds;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                foreach (string part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (int.TryParse(part, out int id) && id > 0 && !ids.Contains(id))
                    {
                        ids.Add(id);
                    }
                }
            }
            if (ids.Count == 0)
            {
                ids.Add(setting.DefaultCityId);
            }
            return ids;
        }
    }
}
