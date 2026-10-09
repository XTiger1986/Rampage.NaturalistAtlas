using Microsoft.AspNetCore.Mvc;
using Rampage.Management;
using Rampage.DependencyInjection;
using Rampage.Management.Mvc;
using Rampage.Management.Permission;
using Rampage.Model;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.ManagementSite.Models;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.Validation;
using System;

namespace Rampage.NaturalistAtlas.ManagementSite.InternalApi
{
    /// <summary>
    /// 国家管理接口：保存/启停/删除
    /// Country admin API: save / status / delete
    /// </summary>
    [Route("api/country")]
    public class CountryApiController : NaturalistAtlasManagementApiController
    {
        [Auto]
        protected ICountryService CountryService = default!;


        /// <summary>
        /// 保存国家（Id&lt;=0 新增，否则更新；code 全站唯一）
        /// Save a country (create when Id&lt;=0; unique code)
        /// </summary>
        [PermissionAction(Title = "保存国家", Resource = "CountryOperation")]
        [RecordOperationFilter]
        [HttpPost("save")]
        public R QuerySaveCountry([FromForm][ValidAttribute] CountryInput input)
        {
            return Execute(() => CountryService.SaveCountry(input));
        }

        /// <summary>
        /// 国家启用/停用切换
        /// Toggle country enabled/disabled
        /// </summary>
        [PermissionAction(Title = "启停国家", Resource = "CountryOperation")]
        [RecordOperationFilter]
        [HttpPost("status")]
        public R QueryCountryStatus([FromForm][ValidAttribute] GeoStatusInput input)
        {
            return Execute(() => CountryService.ChangeCountryStatus(input.Id, input.Status));
        }

        /// <summary>
        /// 删除国家（被省份/城市引用时拒绝）
        /// Delete a country (rejected while referenced)
        /// </summary>
        [PermissionAction(Title = "删除国家", Resource = "CountryOperation")]
        [RecordOperationFilter]
        [HttpDelete("delete/{countryId}")]
        public R QueryDeleteCountry(int countryId)
        {
            return Execute(() => CountryService.DeleteCountry(countryId));
        }
    }

    /// <summary>
    /// 省份管理接口：保存/启停/删除 + 按国家级联查询
    /// Province admin API: save / status / delete plus country-cascade query
    /// </summary>
    [Route("api/province")]
    public class ProvinceApiController : NaturalistAtlasManagementApiController
    {
        [Auto]
        protected IProvinceService ProvinceService = default!;


        /// <summary>
        /// 保存省份（Id&lt;=0 新增，否则更新；(国家,区划码) 唯一）
        /// Save a province (create when Id&lt;=0; unique (country, code))
        /// </summary>
        [PermissionAction(Title = "保存省份", Resource = "ProvinceOperation")]
        [RecordOperationFilter]
        [HttpPost("save")]
        public R QuerySaveProvince([FromForm][ValidAttribute] ProvinceInput input)
        {
            return Execute(() => ProvinceService.SaveProvince(input));
        }

        /// <summary>
        /// 省份启用/停用切换
        /// Toggle province enabled/disabled
        /// </summary>
        [PermissionAction(Title = "启停省份", Resource = "ProvinceOperation")]
        [RecordOperationFilter]
        [HttpPost("status")]
        public R QueryProvinceStatus([FromForm][ValidAttribute] GeoStatusInput input)
        {
            return Execute(() => ProvinceService.ChangeProvinceStatus(input.Id, input.Status));
        }

        /// <summary>
        /// 删除省份（被城市引用时拒绝）
        /// Delete a province (rejected while referenced by cities)
        /// </summary>
        [PermissionAction(Title = "删除省份", Resource = "ProvinceOperation")]
        [RecordOperationFilter]
        [HttpDelete("delete/{provinceId}")]
        public R QueryDeleteProvince(int provinceId)
        {
            return Execute(() => ProvinceService.DeleteProvince(provinceId));
        }

        /// <summary>
        /// 按国家取启用省份列表（城市编辑页级联下拉数据源）
        /// Enabled provinces of a country (cascade dropdown source for the city edit page)
        /// </summary>
        [PermissionAction(Title = "查询省份列表", Type = PermissionActionType.Query, Resource = "ProvinceManagement")]
        [HttpGet("list")]
        public R<System.Collections.Generic.List<Model.Po.Province>> QueryProvinceList([FromQuery] int countryId)
        {
            try
            {
                return R<System.Collections.Generic.List<Model.Po.Province>>.Success(ProvinceService.GetProvinceListByCountry(countryId));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<System.Collections.Generic.List<Model.Po.Province>>.Failure(ex.Message);
            }
        }
    }
}
