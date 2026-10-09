using Rampage.Data;
using Rampage.Data.Query;
using Rampage.Model;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 举报工单管理服务（管理后台） / Report admin service (management site)
    /// </summary>
    public interface IReportService : ISuperService<Report, IReportDao>
    {
        /// <summary>
        /// 分页查询举报工单（批量补齐观察摘要）
        /// Query the paged report list with batch-resolved observation summaries
        /// </summary>
        /// <param name="filter">筛选条件 / Filter</param>
        /// <param name="pager">分页参数 / Pager</param>
        /// <returns>举报列表行 / Report rows</returns>
        List<ReportRowDto> GetReportPageList(ReportAdminFilter filter, PagerInfo pager);

        /// <summary>
        /// 按主键取举报（处理页回显） / Get a report by id (handle page echo)
        /// </summary>
        /// <param name="reportId">举报Id（雪花）/ Report id (snowflake)</param>
        /// <returns>举报实体，不存在返回 null / The report, or null when missing</returns>
        Model.Po.Report? GetReport(string reportId);

        /// <summary>
        /// 举报处理：Hide=true 时隐藏对应观察并结单（同事务），否则驳回结单；仅待处理工单可操作
        /// Handle a report: Hide=true hides the observation and closes the report in one transaction, otherwise reject;
        /// only pending reports can be handled
        /// </summary>
        /// <param name="input">处理输入 / Handle input</param>
        /// <param name="adminId">处理人admin.id / Handler admin id</param>
        /// <returns>操作结果 / Result</returns>
        R HandleReport(HandleReportInput input, int adminId);
    }
}
