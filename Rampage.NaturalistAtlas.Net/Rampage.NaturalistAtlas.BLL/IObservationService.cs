using Rampage.Data;
using Rampage.Data.Query;
using Rampage.Model;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 观察数据管理服务（管理后台）：只读检索 + 内容隐藏/恢复
    /// Observation admin service: read-only search plus hide / restore moderation
    /// </summary>
    public interface IObservationService : ISuperService<Observation, IObservationDao>
    {
        /// <summary>
        /// 分页查询观察列表（批量补齐分类/城市展示名）
        /// Query the paged observation list with batch-resolved taxon / city names
        /// </summary>
        /// <param name="filter">筛选条件 / Filter</param>
        /// <param name="pager">分页参数 / Pager</param>
        /// <returns>观察列表行 / Observation rows</returns>
        /// <summary>
        /// 观察分页（管理后台）：排序可选 create_desc(默认)/create_asc/observed_desc/observed_asc
        /// Observation paging (admin): sort = create_desc (default) / create_asc / observed_desc / observed_asc
        /// </summary>
        List<ObservationRowDto> GetObservationPageList(ObservationAdminFilter filter, PagerInfo pager, string sort = "create_desc");

        /// <summary>
        /// 观察详情：主档 + 分类/城市 + 该观察的举报记录
        /// Observation detail: record + taxon / city + its reports
        /// </summary>
        /// <param name="observationId">观察Id（雪花）/ Observation id (snowflake)</param>
        /// <returns>详情，不存在返回 null / The detail, or null when missing</returns>
        ObservationDetailDto? GetObservationDetail(string observationId);

        /// <summary>
        /// 隐藏/恢复观察（仅在 0=正常 与 1=隐藏 之间切换；2=源站已删除只读）
        /// Hide or restore an observation (only between 0=normal and 1=hidden; 2=source-deleted is read-only)
        /// </summary>
        /// <param name="observationId">观察Id（雪花）/ Observation id (snowflake)</param>
        /// <param name="status">目标状态（0 或 1）/ Target status (0 or 1)</param>
        /// <returns>操作结果 / Result</returns>
        R UpdateObservationStatus(string observationId, ObservationStatus status);

        /// <summary>
        /// 按作者隐藏：将该观察者名下全部正常观察置为隐藏（产品文档 §十一 内容安全运营手段）
        /// Hide by author: hides every normal observation of the given observer (product doc §11 content-safety tool)
        /// </summary>
        /// <param name="userLogin">观察者公开昵称 / Observer public login</param>
        /// <returns>操作结果（Data 为受影响行数） / Result (Data carries the affected row count)</returns>
        R UpdateObservationsHiddenByAuthor(string userLogin);
    }
}
