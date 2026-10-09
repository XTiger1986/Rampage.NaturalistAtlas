using Microsoft.AspNetCore.Mvc;
using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.ExceptionExtensions;
using Rampage.Management;
using Rampage.Management.Permission;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web.Component;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.ManagementSite.Controllers
{
    /// <summary>
    /// 城市管理：全球城市（国家-省份-城市三级档案；分页列表 + 新增/编辑/启停/删除）
    /// City management: worldwide cities within the country-province-city hierarchy
    /// </summary>
    public class CityController : NaturalistAtlasManagementController
    {
        [Auto]
        protected ICityService CityService = default!;

        [Auto]
        protected ICountryService CountryService = default!;

        [Auto]
        protected IProvinceService ProvinceService = default!;

        /// <summary>
        /// 城市列表：国家/省份/关键字/状态筛选分页
        /// City list: country / province / keywords / status filter paging
        /// </summary>
        [PermissionAction(Title = "城市列表", Type = PermissionActionType.Page, Resource = "CityManagement")]
        public IActionResult City_List()
        {
            PagerInfo pagerInfo = BuildPager();
            CityAdminFilter filter = RenderModel<CityAdminFilter>();

            ViewBag.CountryList = CountryService.GetCountryPageList(new CountryAdminFilter(), new PagerInfo { PageIndex = 1, PageSize = 300 });
            (List<City> list, Dictionary<int, string> countryNameDict, Dictionary<int, string> provinceNameDict) = CityService.GetCityPageList(filter, pagerInfo);
            ViewBag.CountryNameDict = countryNameDict;
            ViewBag.ProvinceNameDict = provinceNameDict;

            ListViewData<City, CityAdminFilter> viewModel = new ListViewData<City, CityAdminFilter>
            {
                DataList = list,
                DataPager = new DataPager(pagerInfo),
                DataFilter = filter
            };
            return VW(viewModel);
        }

        /// <summary>
        /// 添加城市：空模板进入共用编辑页
        /// Add city: enter the shared edit page with an empty template
        /// </summary>
        [PermissionAction(Title = "添加城市", Type = PermissionActionType.Page, Resource = "CityManagement")]
        public IActionResult City_Add()
        {
            ViewBag.CountryList = CountryService.GetCountryPageList(new CountryAdminFilter(), new PagerInfo { PageIndex = 1, PageSize = 300 });
            return VW("City_Edit", new City());
        }

        /// <summary>
        /// 编辑城市：按 CityId 回显；查无数据按不存在抛错
        /// Edit city: echo by CityId; throw not-found when missing
        /// </summary>
        [PermissionAction(Title = "编辑城市", Type = PermissionActionType.Page, Resource = "CityManagement")]
        public IActionResult City_Edit()
        {
            int cityId = Get<int>("CityId");
            City? item = CityService.GetCity(cityId);
            if (item == null)
            {
                throw new SuperHttpException("指定的城市不存在");
            }
            ViewBag.CountryList = CountryService.GetCountryPageList(new CountryAdminFilter(), new PagerInfo { PageIndex = 1, PageSize = 300 });
            // 已指向省份时回传其档案：省份若处于停用态不在级联列表里，须额外补一个回显项，
            // 否则下拉显示"不设置"、保存时静默丢掉省市关联
            // Echo the pointed-to province profile: a disabled province is absent from the cascade
            // list, and without an extra echo option the select shows "none" and saving silently
            // drops the province association
            if (item.ProvinceId.HasValue)
            {
                ViewBag.CurrentProvince = ProvinceService.GetProvince(item.ProvinceId.Value);
            }
            return VW(item);
        }

    }
}
