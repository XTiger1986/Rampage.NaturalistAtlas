using Microsoft.AspNetCore.Mvc;
using Rampage.DependencyInjection;
using Rampage.Management;
using Rampage.Management.Permission;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto;

namespace Rampage.NaturalistAtlas.ManagementSite.Controllers
{
    /// <summary>
    /// 系统设置（站点信息 / 业务参数 / 数据源占位）
    /// System settings (site info / business parameters / data-source placeholders)
    /// </summary>
    public class SettingController : NaturalistAtlasManagementController
    {

        [Auto]
        protected ISettingService SettingService = default!;

        [Auto]
        protected ICityService CityService = default!;


        /// <summary>
        /// 常规设置页：回显 setting 表中的 GeneralSetting JSON，保存走 api/setting/general/save；
        /// 附带全量城市（含停用）供默认城市下拉与采集清单多选
        /// General settings page: renders the GeneralSetting JSON; saving posts to api/setting/general/save.
        /// The full city list (disabled included) feeds the default-city dropdown and the collection multi-select
        /// </summary>
        [PermissionAction(Title = "常规设置", Type = PermissionActionType.Page, Resource = "SettingManagement")]
        public IActionResult GeneralConfig()
        {
            GeneralSetting setting = SettingService.GetSetting<GeneralSetting>();
            ViewBag.CityList = Internal.WebLogicUtility.BuildCityOptions(CityService);
            return VW(setting);
        }

    }
}
