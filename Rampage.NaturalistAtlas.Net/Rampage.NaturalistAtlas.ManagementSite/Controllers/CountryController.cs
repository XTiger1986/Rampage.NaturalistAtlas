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
    /// 国家管理：全球国家档案（ISO 3166-1）完整增删改查
    /// Country management: full CRUD of worldwide country profiles (ISO 3166-1)
    /// </summary>
    public class CountryController : NaturalistAtlasManagementController
    {
        [Auto]
        protected ICountryService CountryService = default!;

        /// <summary>
        /// 国家列表：关键字（代码/中英文名）+ 状态筛选分页
        /// Country list: keywords (code / CN-EN names) + status filter paging
        /// </summary>
        [PermissionAction(Title = "国家列表", Type = PermissionActionType.Page, Resource = "CountryManagement")]
        public IActionResult Country_List()
        {
            PagerInfo pagerInfo = BuildPager();
            CountryAdminFilter filter = RenderModel<CountryAdminFilter>();

            List<Country> list = CountryService.GetCountryPageList(filter, pagerInfo);

            ListViewData<Country, CountryAdminFilter> viewModel = new ListViewData<Country, CountryAdminFilter>
            {
                DataList = list,
                DataPager = new DataPager(pagerInfo),
                DataFilter = filter
            };
            return VW(viewModel);
        }

        /// <summary>
        /// 添加国家：空模板进入共用编辑页
        /// Add country: enter the shared edit page with an empty template
        /// </summary>
        [PermissionAction(Title = "添加国家", Type = PermissionActionType.Page, Resource = "CountryManagement")]
        public IActionResult Country_Add()
        {
            return VW("Country_Edit", new Country());
        }

        /// <summary>
        /// 编辑国家：按 CountryId 回显；查无数据按不存在抛错
        /// Edit country: echo by CountryId; throw not-found when missing
        /// </summary>
        [PermissionAction(Title = "编辑国家", Type = PermissionActionType.Page, Resource = "CountryManagement")]
        public IActionResult Country_Edit()
        {
            int countryId = Get<int>("CountryId");
            Country? item = CountryService.GetCountry(countryId);
            if (item == null)
            {
                throw new SuperHttpException("指定的国家不存在");
            }
            return VW(item);
        }

    }
}
