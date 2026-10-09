using Microsoft.AspNetCore.Mvc;
using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.ExceptionExtensions;
using Rampage.Management;
using Rampage.Management.Permission;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.Web.Component;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rampage.NaturalistAtlas.ManagementSite.Controllers
{
    /// <summary>
    /// 观察数据管理：多条件检索 + 详情 + 隐藏/恢复 + 按作者批量隐藏（内容治理）
    /// Observation management: multi-condition search + detail + hide/restore + hide-by-author (content moderation)
    /// </summary>
    public class ObservationController : NaturalistAtlasManagementController
    {
        [Auto]
        protected IObservationService ObservationService = default!;

        [Auto]
        protected ICityService CityService = default!;

        /// <summary>
        /// 观察列表：城市/类群/鉴定级别/状态/来源/观察者/分类/日期范围筛选分页
        /// Observation list: city / iconic / grade / status / source / observer / taxon / date-range paging
        /// </summary>
        [PermissionAction(Title = "观察数据列表", Type = PermissionActionType.Page, Resource = "ObservationManagement")]
        public IActionResult Observation_List()
        {
            PagerInfo pagerInfo = BuildPager();
            ObservationAdminFilter filter = RenderModel<ObservationAdminFilter>();
            // 排序档位（白名单在服务层校验，非法值回落默认入库新→旧）
            string sort = Get<string>("Sort") ?? "create_desc";
            ViewBag.Sort = sort;

            // 城市下拉（全量含停用，「城市（国家）」消歧义；见 WebLogicUtility.BuildCityOptions）
            ViewBag.CityList = Internal.WebLogicUtility.BuildCityOptions(CityService);

            List<ObservationRowDto> list = ObservationService.GetObservationPageList(filter, pagerInfo, sort);

            ListViewData<ObservationRowDto, ObservationAdminFilter> viewModel = new ListViewData<ObservationRowDto, ObservationAdminFilter>
            {
                DataList = list,
                DataPager = new DataPager(pagerInfo),
                DataFilter = filter
            };
            return VW(viewModel);
        }

        /// <summary>
        /// 观察详情：主档 + 分类/城市 + 举报记录
        /// Observation detail: record + taxon / city + reports
        /// </summary>
        [PermissionAction(Title = "观察数据详情", Type = PermissionActionType.Query, Resource = "ObservationManagement")]
        public IActionResult Observation_Detail()
        {
            string observationId = Get<string>("ObservationId")!;
            ObservationDetailDto? item = ObservationService.GetObservationDetail(observationId);
            if (item == null)
            {
                throw new SuperHttpException("指定的观察不存在");
            }
            return VW(item);
        }

    }
}
