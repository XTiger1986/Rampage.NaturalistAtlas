using Rampage.Data;
using Rampage.Data.Query;
using Rampage.Enums;
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
    /// 城市管理服务（管理后台，全球城市：国家-省份-城市三级档案）
    /// City admin service (worldwide cities within the country-province-city hierarchy)
    /// </summary>
    public interface ICityService : ISuperService<City, ICityDao>
    {
        /// <summary>
        /// 分页查询城市列表（附国家/省名字典） / Query the paged city list with country / province dictionaries
        /// </summary>
        /// <param name="filter">筛选条件 / Filter</param>
        /// <param name="pager">分页参数 / Pager</param>
        /// <returns>城市列表与名称字典 / The list plus name dictionaries</returns>
        (List<City> List, Dictionary<int, string> CountryNameDict, Dictionary<int, string> ProvinceNameDict) GetCityPageList(CityAdminFilter filter, PagerInfo pager);

        /// <summary>
        /// 按主键取城市（编辑页回显） / Get a city by id (edit page echo)
        /// </summary>
        /// <param name="cityId">城市Id / City id</param>
        /// <returns>城市实体，不存在返回 null / The city, or null when missing</returns>
        City? GetCity(int cityId);

        /// <summary>
        /// 保存城市（Id&lt;=0 新增否则更新），含国家/省份归属校验、slug 唯一与 bbox 合法性校验
        /// Save a city (create when Id&lt;=0) with country/province ownership, slug-unique and bbox checks
        /// </summary>
        /// <param name="input">保存输入 / Save input</param>
        /// <returns>操作结果（Data 为城市Id） / Result (Data carries the city id)</returns>
        R SaveCity(CityInput input);

        /// <summary>
        /// 启用/停用城市 / Enable or disable a city
        /// </summary>
        /// <param name="cityId">城市Id / City id</param>
        /// <param name="status">目标状态 / Target status</param>
        /// <returns>操作结果 / Result</returns>
        R ChangeCityStatus(int cityId, CommonStatus status);

        /// <summary>
        /// 删除城市（被观察/同步游标/物候统计引用时拒绝）
        /// Delete a city (rejected while referenced by observations, sync cursors or emergence stats)
        /// </summary>
        /// <param name="cityId">城市Id / City id</param>
        /// <returns>操作结果 / Result</returns>
        R DeleteCity(int cityId);
    }
}
