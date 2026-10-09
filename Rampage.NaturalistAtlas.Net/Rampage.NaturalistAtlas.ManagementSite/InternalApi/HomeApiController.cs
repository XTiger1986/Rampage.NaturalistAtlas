using Microsoft.AspNetCore.Mvc;
using Rampage.Management;
using Rampage.DependencyInjection;
using Rampage.Management.Mvc;
using Rampage.Management.Permission;
using Rampage.Model;
using Rampage.NaturalistAtlas.BLL;
using System;

namespace Rampage.NaturalistAtlas.ManagementSite.InternalApi
{
    /// <summary>
    /// 首页看板接口：指标卡与图表数据聚合（观察口径 status=0）
    /// Dashboard API: stat-card and chart aggregates (observations status=0)
    /// </summary>
    [Route("api/home")]
    public class HomeApiController : NaturalistAtlasManagementApiController
    {
        [Auto]
        protected IDashboardService DashboardService = default!;


        /// <summary>
        /// 看板聚合数据（首页图表加载用）
        /// Dashboard aggregate (consumed by the home-page charts)
        /// </summary>
        [PermissionAction(Title = "查询运营看板", Type = PermissionActionType.Query, Resource = "DashboardManagement")]
        [HttpGet("dashboard")]
        public R<Rampage.NaturalistAtlas.Model.Dto.DashboardDto> QueryDashboard()
        {
            try
            {
                return R<Rampage.NaturalistAtlas.Model.Dto.DashboardDto>.Success(DashboardService.GetDashboard());
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<Rampage.NaturalistAtlas.Model.Dto.DashboardDto>.Failure(ex.Message);
            }
        }
    }
}
