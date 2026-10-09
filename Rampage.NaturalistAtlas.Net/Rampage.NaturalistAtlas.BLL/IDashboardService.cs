using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 运营看板聚合服务（管理后台首页） / Dashboard aggregate service (admin home)
    /// </summary>
    public interface IDashboardService : ISuperService<Observation, IObservationDao>
    {
        /// <summary>
        /// 汇总看板数据：指标卡 + 类群构成 + 趋势 + 同步健康度（观察口径 status=0）
        /// Aggregate the dashboard: stat cards + iconic composition + trends + sync health (observations status=0)
        /// </summary>
        /// <returns>看板聚合结果 / The dashboard aggregate</returns>
        DashboardDto GetDashboard();
    }
}
