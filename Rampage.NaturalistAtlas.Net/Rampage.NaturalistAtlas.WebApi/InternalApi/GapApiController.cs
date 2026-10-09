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
    /// 集邮缺口接口：周边已记录、本市未记录的物种清单（只读缓存表，设计文档 §5.9）
    /// Collection-gap API: taxa recorded nearby but not yet in the city (cache table, read-only)
    /// </summary>
    [Route("api/gaps")]
    public class GapApiController : NaturalistAtlasApiController
    {
        [Auto]
        protected IAtlasSpeciesService AtlasSpeciesService = default!;


        /// <summary>
        /// 缺口清单 / The gap list
        /// </summary>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="iconic">类群代码（逗号分隔，空 = 全部）/ Iconic codes (comma separated; empty = all)</param>
        [HttpGet]
        public R<List<WantedTaxonItemDto>> QueryGaps([FromQuery] int cityId, [FromQuery] string? iconic)
        {
            try
            {
                return R<List<WantedTaxonItemDto>>.Success(AtlasSpeciesService.GetGaps(cityId, iconic));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<List<WantedTaxonItemDto>>.Failure(UnexpectedFailureMessage);
            }
        }
    }
}
