using Microsoft.AspNetCore.Mvc;
using Rampage.DependencyInjection;
using Rampage.Management.Mvc;
using Rampage.Management.Permission;
using Rampage.Model;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rampage.NaturalistAtlas.ManagementSite.InternalApi
{
    /// <summary>
    /// 常规设置接口：整体保存 GeneralSetting（保存前校验城市引用的有效性）
    /// General settings API: saves the GeneralSetting whole (city references validated first)
    /// </summary>
    [Route("api/setting")]
    public class SettingApiController : NaturalistAtlasManagementApiController
    {
        [Auto]
        protected ISettingService SettingService = default!;

        [Auto]
        protected ICityService CityService = default!;


        /// <summary>
        /// 保存常规设置：先校验默认城市与采集城市清单均指向真实存在的城市，
        /// 再落库（裸 Id 手填曾造成看板空转与采集空跑，入口处直接拦截）
        /// Save the general settings: default and collection cities must reference real
        /// cities before persisting (bare hand-typed ids once left an empty dashboard and an
        /// idle collector; block them at the entry)
        /// </summary>
        /// <param name="setting">常规设置 / The settings</param>
        /// <returns>保存结果 / The save result</returns>
        [PermissionAction(Title = "保存常规设置", Resource = "SettingOperation")]
        [RecordOperationFilter]
        [HttpPost("general/save")]
        public R QuerySaveGeneralSetting([FromForm][ValidAttribute] GeneralSetting setting)
        {
            try
            {
                if (CityService.GetCity(setting.DefaultCityId) == null)
                {
                    return R.Failure($"默认城市 {setting.DefaultCityId} 不存在，请从下拉中选择");
                }
                if (!string.IsNullOrWhiteSpace(setting.CollectionCityIds))
                {
                    List<string> invalidParts = new List<string>();
                    foreach (string part in setting.CollectionCityIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (!int.TryParse(part, out int id) || id <= 0 || CityService.GetCity(id) == null)
                        {
                            invalidParts.Add(part);
                        }
                    }
                    if (invalidParts.Count > 0)
                    {
                        return R.Failure($"采集城市清单包含无效项：{string.Join("、", invalidParts)}（须为存在的城市Id）");
                    }
                }
                return SettingService.SaveSetting<GeneralSetting>(setting, CurrentAdmin!.Id);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R.Failure(ex.Message);
            }
        }

    }
}
