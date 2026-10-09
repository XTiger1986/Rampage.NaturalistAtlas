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
    /// 分类单元管理：数据由同步任务维护，后台只读（检索 + 详情）
    /// Taxon management: data is maintained by sync jobs; the admin side is read-only (search + detail)
    /// </summary>
    public class TaxonController : NaturalistAtlasManagementController
    {
        [Auto]
        protected ITaxonService TaxonService = default!;

        /// <summary>
        /// 分类单元列表：关键字（学名/中文名/英文名）+ 类群/阶元/数据源/状态筛选分页
        /// Taxon list: keywords (scientific/CN/EN names) + iconic/rank/source/status filter paging
        /// </summary>
        [PermissionAction(Title = "分类单元列表", Type = PermissionActionType.Page, Resource = "TaxonManagement")]
        public IActionResult Taxon_List()
        {
            PagerInfo pagerInfo = BuildPager();
            TaxonAdminFilter filter = RenderModel<TaxonAdminFilter>();

            List<Taxon> list = TaxonService.GetTaxonPageList(filter, pagerInfo);

            ListViewData<Taxon, TaxonAdminFilter> viewModel = new ListViewData<Taxon, TaxonAdminFilter>
            {
                DataList = list,
                DataPager = new DataPager(pagerInfo),
                DataFilter = filter
            };
            return VW(viewModel);
        }

        /// <summary>
        /// 分类单元详情：档案 + 关联观察数 + 祖先链
        /// Taxon detail: profile + observation count + ancestry chain
        /// </summary>
        [PermissionAction(Title = "分类单元详情", Type = PermissionActionType.Query, Resource = "TaxonManagement")]
        public IActionResult Taxon_Detail()
        {
            string taxonId = Get<string>("TaxonId")!;
            Model.Dto.TaxonDetailDto? item = TaxonService.GetTaxonDetail(taxonId);
            if (item == null)
            {
                throw new SuperHttpException("指定的分类单元不存在");
            }
            return VW(item);
        }

    }
}
