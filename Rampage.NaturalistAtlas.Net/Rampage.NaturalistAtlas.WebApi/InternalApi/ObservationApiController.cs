using Microsoft.AspNetCore.Mvc;
using Rampage.Data.Models;
using Rampage.DependencyInjection;
using Rampage.Model;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.WebApi.Internal;
using System;

namespace Rampage.NaturalistAtlas.WebApi.InternalApi
{
    /// <summary>
    /// 观察接口：地图点位、卡片列表、观察详情（前端设计文档 §6）
    /// Observation API: map points, card list and detail (frontend design doc §6)
    /// </summary>
    [Route("api/observation")]
    public class ObservationApiController : NaturalistAtlasApiController
    {
        [Auto]
        protected IAtlasObservationService AtlasObservationService = default!;


        /// <summary>
        /// 地图点位（客户端 supercluster 聚合用；超过 limit 截断并标记 truncated）
        /// Map points for client-side supercluster clustering (truncated and flagged beyond limit)
        /// </summary>
        /// <param name="query">组合筛选 / Filter</param>
        /// <param name="limit">上限（默认 8000，最大 10000）/ Limit (default 8000, max 10000)</param>
        [HttpGet("points")]
        public R<ObservationPointResultDto> QueryPoints([FromQuery] AtlasObservationQuery query, [FromQuery] int limit = 8000)
        {
            try
            {
                return R<ObservationPointResultDto>.Success(AtlasObservationService.GetPoints(query, limit));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<ObservationPointResultDto>.Failure(UnexpectedFailureMessage);
            }
        }

        /// <summary>
        /// 观察卡片分页（按观察日期新→旧）/ Paged observation cards, newest first
        /// </summary>
        /// <param name="query">组合筛选 / Filter</param>
        /// <param name="pageIndex">页码 / Page index</param>
        /// <param name="pageSize">每页条数（最大 60）/ Page size (max 60)</param>
        /// <param name="photoOnly">仅有图记录（照片墙）/ Only rows with a photo (photo wall)</param>
        [HttpGet("list")]
        public R<PageData<ObservationCardDto>> QueryList([FromQuery] AtlasObservationQuery query, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 24, [FromQuery] bool photoOnly = false)
        {
            try
            {
                return R<PageData<ObservationCardDto>>.Success(AtlasObservationService.GetCardPage(query, pageIndex, pageSize, photoOnly));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<PageData<ObservationCardDto>>.Failure(UnexpectedFailureMessage);
            }
        }

        /// <summary>
        /// 观察详情（仅正常展示记录；附源站链接）/ Observation detail (visible rows only, with the source URL)
        /// </summary>
        /// <param name="id">观察 id / Observation id</param>
        [HttpGet("{id}")]
        public R<ObservationDetailDto> QueryDetail(string id)
        {
            try
            {
                ObservationDetailDto? detail = AtlasObservationService.GetDetail(id);
                return detail == null
                    ? R<ObservationDetailDto>.Failure("该观察不存在或已下线")
                    : R<ObservationDetailDto>.Success(detail);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<ObservationDetailDto>.Failure(UnexpectedFailureMessage);
            }
        }
    }
}
