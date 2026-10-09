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
    /// 国家管理服务（管理后台） / Country admin service (management site)
    /// </summary>
    public interface ICountryService : ISuperService<Country, ICountryDao>
    {
        /// <summary>
        /// 分页查询国家列表 / Query the paged country list
        /// </summary>
        List<Country> GetCountryPageList(CountryAdminFilter filter, PagerInfo pager);

        /// <summary>
        /// 按主键取国家（编辑页回显） / Get a country by id (edit page echo)
        /// </summary>
        Country? GetCountry(int countryId);

        /// <summary>
        /// 保存国家（Id&lt;=0 新增否则更新），含 code 唯一校验（2 位小写字母）
        /// Save a country (create when Id&lt;=0) with the unique-code check (2 lowercase letters)
        /// </summary>
        R SaveCountry(CountryInput input);

        /// <summary>
        /// 启用/停用国家 / Enable or disable a country
        /// </summary>
        R UpdateCountryStatus(int countryId, CommonStatus status);

        /// <summary>
        /// 删除国家（被省份/城市引用时拒绝） / Delete a country (rejected while referenced by provinces or cities)
        /// </summary>
        R DeleteCountry(int countryId);
    }

    /// <summary>
    /// 省份管理服务（管理后台） / Province admin service (management site)
    /// </summary>
    public interface IProvinceService : ISuperService<Province, IProvinceDao>
    {
        /// <summary>
        /// 分页查询省份列表（附国家名字典）/ Query the paged province list with a country-name dictionary
        /// </summary>
        (List<Province> List, Dictionary<int, string> CountryNameDict) GetProvincePageList(ProvinceAdminFilter filter, PagerInfo pager);

        /// <summary>
        /// 按主键取省份（编辑页回显） / Get a province by id (edit page echo)
        /// </summary>
        Province? GetProvince(int provinceId);

        /// <summary>
        /// 保存省份（Id&lt;=0 新增否则更新）：国家存在性 + (国家,区划码) 唯一 + 同国名称查重
        /// Save a province (create when Id&lt;=0): country existence + (country, code) unique + same-country name check
        /// </summary>
        R SaveProvince(ProvinceInput input);

        /// <summary>
        /// 启用/停用省份 / Enable or disable a province
        /// </summary>
        R UpdateProvinceStatus(int provinceId, CommonStatus status);

        /// <summary>
        /// 删除省份（被城市引用时拒绝） / Delete a province (rejected while referenced by cities)
        /// </summary>
        R DeleteProvince(int provinceId);

        /// <summary>
        /// 取指定国家下的省份列表（级联下拉数据源，名称 A→Z）
        /// List provinces of a country (cascade dropdown source, name A→Z)
        /// </summary>
        List<Province> GetProvinceListByCountry(int countryId);
    }
}
