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
    /// 物候接口：物种 × 月份矩阵 / Phenology API: species × month matrix
    /// </summary>
    [Route("api/phenology")]
    public class PhenologyApiController : NaturalistAtlasApiController
    {
        [Auto]
        protected IAtlasSpeciesService AtlasSpeciesService = default!;


        /// <summary>
        /// 物候矩阵（Top N 物种 × 12 月；月份筛选被忽略）/ Phenology matrix (top N species × 12 months; the month filter is ignored)
        /// </summary>
        /// <param name="query">组合筛选 / Filter</param>
        /// <param name="top">物种数（默认 30，5~100）/ Species count (default 30, 5~100)</param>
        [HttpGet("matrix")]
        public R<PhenologyMatrixDto> QueryMatrix([FromQuery] AtlasObservationQuery query, [FromQuery] int top = 30)
        {
            try
            {
                return R<PhenologyMatrixDto>.Success(AtlasSpeciesService.GetPhenologyMatrix(query, top));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<PhenologyMatrixDto>.Failure(UNEXPECTED_FAILURE_MESSAGE);
            }
        }
    }
}
