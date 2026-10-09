using Microsoft.AspNetCore.Mvc;
using Rampage.DependencyInjection;
using Rampage.Model;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.WebApi.Internal;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.WebApi.InternalApi
{
    /// <summary>
    /// 地理接口：城市列表与地点搜索 / Geo API: city list and place search
    /// </summary>
    [Route("api/geo")]
    public class GeoApiController : NaturalistAtlasApiController
    {
        [Auto]
        protected IAtlasGeoService AtlasGeoService = default!;


        /// <summary>
        /// 已启用城市列表（城市切换器、默认视野）/ Enabled cities (city switcher and initial viewport)
        /// </summary>
        [HttpGet("cities")]
        public R<List<CityItemDto>> QueryCities()
        {
            try
            {
                return R<List<CityItemDto>>.Success(AtlasGeoService.GetCityList());
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<List<CityItemDto>>.Failure(UNEXPECTED_FAILURE_MESSAGE);
            }
        }

        /// <summary>
        /// 地点搜索（国家 / 省份 / 城市）/ Place search (country / province / city)
        /// </summary>
        /// <param name="keyword">关键字 / Keyword</param>
        /// <param name="limit">条数上限（默认 12，最大 30）/ Limit (default 12, max 30)</param>
        [HttpGet("search")]
        public R<List<GeoSearchItemDto>> Search([FromQuery] string? keyword, [FromQuery] int limit = 12)
        {
            try
            {
                return R<List<GeoSearchItemDto>>.Success(AtlasGeoService.GetSearchList(keyword, limit));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<List<GeoSearchItemDto>>.Failure(UNEXPECTED_FAILURE_MESSAGE);
            }
        }
    }
}
