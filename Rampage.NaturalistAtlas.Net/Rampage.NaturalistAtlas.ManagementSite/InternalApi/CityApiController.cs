using Microsoft.AspNetCore.Mvc;
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
    /// 城市管理接口：保存/启停/删除
    /// City admin API: save / status / delete
    /// </summary>
    [Route("api/city")]
    public class CityApiController : NaturalistAtlasManagementApiController
    {
        [Auto]
        protected ICityService CityService = default!;


        /// <summary>
        /// 保存城市（Id&lt;=0 新增，否则更新）
        /// Save a city (create when Id&lt;=0, update otherwise)
        /// </summary>
        [PermissionAction(Title = "保存城市", Resource = "CityOperation")]
        [RecordOperationFilter]
        [HttpPost("save")]
        public R QuerySaveCity([FromForm][ValidAttribute] CityInput input)
        {
            return Execute(() => CityService.SaveCity(input));
        }

        /// <summary>
        /// 城市启用/停用切换
        /// Toggle city enabled/disabled
        /// </summary>
        [PermissionAction(Title = "启停城市", Resource = "CityOperation")]
        [RecordOperationFilter]
        [HttpPost("status")]
        public R QueryCityStatus([FromForm][ValidAttribute] CityStatusInput input)
        {
            return Execute(() => CityService.UpdateCityStatus(input.Id, input.Status));
        }

        /// <summary>
        /// 删除城市（被观察/同步任务/物候统计引用时拒绝）
        /// Delete a city (rejected while referenced by observations / sync tasks / emergence stats)
        /// </summary>
        [PermissionAction(Title = "删除城市", Resource = "CityOperation")]
        [RecordOperationFilter]
        [HttpDelete("delete/{cityId}")]
        public R QueryDeleteCity(int cityId)
        {
            return Execute(() => CityService.DeleteCity(cityId));
        }
    }
}
