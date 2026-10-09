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
    /// 省份管理：国家一级区划（省/州/都道府县/大区等）完整增删改查
    /// Province management: full CRUD of first-level subdivisions
    /// </summary>
    public class ProvinceController : NaturalistAtlasManagementController
    {
        [Auto]
        protected IProvinceService ProvinceService = default!;

        [Auto]
        protected ICountryService CountryService = default!;

        /// <summary>
        /// 省份列表：国家 + 关键字（区划码/中英文名）+ 状态筛选分页
        /// Province list: country + keywords + status filter paging
        /// </summary>
        [PermissionAction(Title = "省份列表", Type = PermissionActionType.Page, Resource = "ProvinceManagement")]
        public IActionResult Province_List()
        {
            PagerInfo pagerInfo = BuildPager();
            ProvinceAdminFilter filter = RenderModel<ProvinceAdminFilter>();

            ViewBag.CountryList = CountryService.GetCountryPageList(new CountryAdminFilter(), new PagerInfo { PageIndex = 1, PageSize = 300 });
            (List<Province> list, Dictionary<int, string> countryNameDict) = ProvinceService.GetProvincePageList(filter, pagerInfo);
            ViewBag.CountryNameDict = countryNameDict;

            ListViewData<Province, ProvinceAdminFilter> viewModel = new ListViewData<Province, ProvinceAdminFilter>
            {
                DataList = list,
                DataPager = new DataPager(pagerInfo),
                DataFilter = filter
            };
            return VW(viewModel);
        }

        /// <summary>
        /// 添加省份：空模板进入共用编辑页
        /// Add province: enter the shared edit page with an empty template
        /// </summary>
        [PermissionAction(Title = "添加省份", Type = PermissionActionType.Page, Resource = "ProvinceManagement")]
        public IActionResult Province_Add()
        {
            ViewBag.CountryList = CountryService.GetCountryPageList(new CountryAdminFilter(), new PagerInfo { PageIndex = 1, PageSize = 300 });
            return VW("Province_Edit", new Province());
        }

        /// <summary>
        /// 编辑省份：按 ProvinceId 回显；查无数据按不存在抛错
        /// Edit province: echo by ProvinceId; throw not-found when missing
        /// </summary>
        [PermissionAction(Title = "编辑省份", Type = PermissionActionType.Page, Resource = "ProvinceManagement")]
        public IActionResult Province_Edit()
        {
            int provinceId = Get<int>("ProvinceId");
            Province? item = ProvinceService.GetProvince(provinceId);
            if (item == null)
            {
                throw new SuperHttpException("指定的省份不存在");
            }
            ViewBag.CountryList = CountryService.GetCountryPageList(new CountryAdminFilter(), new PagerInfo { PageIndex = 1, PageSize = 300 });
            return VW(item);
        }

    }
}
