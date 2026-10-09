using Microsoft.AspNetCore.Mvc;
using Rampage.DependencyInjection;
using Rampage.Model;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.WebApi.Internal;
using System;

namespace Rampage.NaturalistAtlas.WebApi.InternalApi
{
    /// <summary>
    /// 统计接口：城市总览 / Stats API: city overview
    /// </summary>
    [Route("api/stats")]
    public class StatsApiController : NaturalistAtlasApiController
    {
        [Auto]
        protected IAtlasStatsService AtlasStatsService = default!;


        /// <summary>
        /// 城市总览（指标卡、类群构成、本月新星、活跃观察者）/ City overview (stat cards, iconic mix, rising stars, top observers)
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="researchOnly">仅研究级 / Research grade only</param>
        [HttpGet("overview")]
        public R<CityOverviewDto> QueryOverview([FromQuery] int cityId, [FromQuery] bool researchOnly = false)
        {
            try
            {
                CityOverviewDto? overview = AtlasStatsService.GetCityOverview(cityId, researchOnly);
                return overview == null
                    ? R<CityOverviewDto>.Failure("城市不存在")
                    : R<CityOverviewDto>.Success(overview);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<CityOverviewDto>.Failure(UnexpectedFailureMessage);
            }
        }
    }
}
