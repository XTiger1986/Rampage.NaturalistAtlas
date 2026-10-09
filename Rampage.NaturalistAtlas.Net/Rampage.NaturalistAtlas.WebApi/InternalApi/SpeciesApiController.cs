using Microsoft.AspNetCore.Mvc;
using Rampage.Data.Models;
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
    /// 物种接口：物种榜、物种详情、物候曲线 / Species API: rank, detail and emergence curve
    /// </summary>
    [Route("api/species")]
    public class SpeciesApiController : NaturalistAtlasApiController
    {
        [Auto]
        protected IAtlasSpeciesService AtlasSpeciesService = default!;


        /// <summary>
        /// 物种榜分页 / Paged species rank
        /// </summary>
        /// <param name="query">组合筛选 / Filter</param>
        /// <param name="order">排序：record（默认）/ observer / rare（稀缺）/ Order: record (default) / observer / rare</param>
        /// <param name="keyword">物种名关键字 / Name keyword</param>
        /// <param name="pageIndex">页码 / Page index</param>
        /// <param name="pageSize">每页条数（最大 100）/ Page size (max 100)</param>
        [HttpGet("rank")]
        public R<PageData<SpeciesRankItemDto>> QueryRank([FromQuery] AtlasObservationQuery query, [FromQuery] string? order,
            [FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 30)
        {
            try
            {
                return R<PageData<SpeciesRankItemDto>>.Success(AtlasSpeciesService.GetRankPage(query, order, keyword, pageIndex, pageSize));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<PageData<SpeciesRankItemDto>>.Failure(UnexpectedFailureMessage);
            }
        }

        /// <summary>
        /// 物种详情（分类档案 + 本市统计 + 照片墙 + 分布点）/ Species detail (profile, city stats, photo wall, points)
        /// </summary>
        /// <param name="id">taxon.id</param>
        /// <param name="cityId">城市 id / City id</param>
        [HttpGet("{id}")]
        public R<SpeciesDetailDto> QueryDetail(string id, [FromQuery] int cityId)
        {
            try
            {
                SpeciesDetailDto? detail = AtlasSpeciesService.GetSpeciesDetail(id, cityId);
                return detail == null
                    ? R<SpeciesDetailDto>.Failure("该物种不存在")
                    : R<SpeciesDetailDto>.Success(detail);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<SpeciesDetailDto>.Failure(UnexpectedFailureMessage);
            }
        }

        /// <summary>
        /// 相似物种（同祖先链同胞，本市有记录）/ Similar species (ancestry siblings with local records)
        /// </summary>
        /// <param name="id">taxon.id</param>
        /// <param name="cityId">城市 id / City id</param>
        [HttpGet("{id}/similar")]
        public R<List<SpeciesRankItemDto>> QuerySimilar(string id, [FromQuery] int cityId)
        {
            try
            {
                return R<List<SpeciesRankItemDto>>.Success(AtlasSpeciesService.GetSimilarSpecies(id, cityId));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<List<SpeciesRankItemDto>>.Failure(UnexpectedFailureMessage);
            }
        }

        /// <summary>
        /// 物候曲线 / Emergence curve
        /// </summary>
        /// <param name="id">taxon.id</param>
        /// <param name="cityId">城市 id / City id</param>
        /// <param name="mode">detail（近两年逐旬，默认）/ history（全历史逐月）/ Mode</param>
        /// <param name="researchOnly">仅研究级 / Research grade only</param>
        [HttpGet("{id}/emergence")]
        public R<EmergenceResultDto> QueryEmergence(string id, [FromQuery] int cityId, [FromQuery] string? mode, [FromQuery] bool researchOnly = false)
        {
            try
            {
                return R<EmergenceResultDto>.Success(AtlasSpeciesService.GetEmergence(id, cityId, mode, researchOnly));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<EmergenceResultDto>.Failure(UnexpectedFailureMessage);
            }
        }
    }
}
