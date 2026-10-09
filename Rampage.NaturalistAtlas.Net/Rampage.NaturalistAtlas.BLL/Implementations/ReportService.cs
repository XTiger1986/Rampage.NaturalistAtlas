using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Model;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 举报工单管理服务实现（管理后台）
    /// Report admin service implementation: hide-path closes the observation and the report in one transaction
    /// </summary>
    public class ReportService : SuperService<Report, IReportDao>, IReportService
    {
        [Auto]
        protected IReportDao ReportDao = default!;

        [Auto]
        protected IObservationDao ObservationDao = default!;


        /// <inheritdoc/>
        public List<ReportRowDto> GetReportPageList(ReportAdminFilter filter, PagerInfo pager)
        {
            FilterWrapper<Report> wrapper = new(filter);
            // 提交时间区间由筛选器 ValueScope 原生构建；待处理优先，其余按提交新→旧（两键排序：多次 OrderBy 叠加）
            wrapper.OrderBy(m => m.Status);
            wrapper.OrderByDescending(m => m.CreateTime);

            List<Report> list = ReportDao.SelectPage(pager, wrapper);
            if (list.Count == 0)
            {
                return new List<ReportRowDto>();
            }

            // 批量补齐观察摘要，避免逐行 N+1
            Dictionary<string, Observation> observationDict = ObservationDao
                .SelectByIds(list.Where(r => !string.IsNullOrEmpty(r.ObservationId))
                    .Select(r => r.ObservationId!).Distinct().ToList())
                .ToDictionary(o => o.Id);

            return list.Select(po => {
                Observation? observation = po.ObservationId != null && observationDict.TryGetValue(po.ObservationId!, out Observation? item) ? item : null;
                return new ReportRowDto
                {
                    Id = po.Id,
                    ObservationId = po.ObservationId,
                    Reason = po.Reason,
                    Content = po.Content,
                    Contact = po.Contact,
                    Status = po.Status,
                    CreateTime = po.CreateTime,
                    HandleAdminId = po.HandleAdminId,
                    HandleTime = po.HandleTime,
                    HandleRemark = po.HandleRemark,
                    ObservationPhotoSquareUrl = observation?.PhotoSquareUrl,
                    ObservationSourceId = observation?.SourceObservationId,
                    ObservationStatus = observation?.Status,
                    ObservationIconicTaxon = observation?.IconicTaxon,
                    ObservationUserLogin = observation?.UserLogin
                };
            }).ToList();
        }

        /// <inheritdoc/>
        public Model.Po.Report? GetReport(string reportId)
        {
            if (string.IsNullOrWhiteSpace(reportId))
            {
                return null;
            }
            return ReportDao.SelectById(reportId);
        }

        /// <inheritdoc/>
        public R HandleReport(HandleReportInput input, int adminId)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.ReportId))
            {
                return R.Failure("参数无效");
            }
            Report? report = ReportDao.SelectById(input.ReportId);
            if (report == null)
            {
                return R.Failure("指定的举报不存在");
            }
            if (report.Status != ReportStatus.Pending)
            {
                return R.Failure("该举报已处理，不能重复操作");
            }

            Observation? observation = string.IsNullOrEmpty(report.ObservationId)
                ? null
                : ObservationDao.SelectById(report.ObservationId!);

            ReportStatus targetStatus = input.Hide ? ReportStatus.Hidden : ReportStatus.Rejected;

            // 隐藏路径：观察置隐藏 + 举报结单，两表同事务（同库 Dao 共享一个事务）
            if (input.Hide)
            {
                if (observation == null)
                {
                    return R.Failure("对应的观察不存在（可能已被清理），请选择驳回结单");
                }
                if (observation.Status == ObservationStatus.SourceDeleted)
                {
                    return R.Failure("对应观察已被源站删除，无需隐藏，请选择驳回结单");
                }

                BeginTransaction(ObservationDao, System.Data.IsolationLevel.ReadCommitted);
                try
                {
                    UpdateFields<Observation> observationFields = new();
                    observationFields.Set(m => m.Status, ObservationStatus.Hidden);
                    observationFields.Set(m => m.UpdateTime, UtcNow);
                    if (!ObservationDao.UpdateById(observation.Id, observationFields))
                    {
                        RollbackTransaction();
                        return R.Failure("隐藏观察失败");
                    }

                    UpdateFields<Report> reportFields = new();
                    reportFields.Set(m => m.Status, targetStatus);
                    reportFields.Set(m => m.HandleAdminId, adminId);
                    reportFields.Set(m => m.HandleTime, UtcNow);
                    reportFields.Set(m => m.HandleRemark, input.Remark);
                    reportFields.Set(m => m.UpdateTime, UtcNow);
                    if (!ReportDao.UpdateById(report.Id, reportFields))
                    {
                        RollbackTransaction();
                        return R.Failure("举报结单失败");
                    }

                    CommitTransaction();
                    // 隐藏落地即失效公开站缓存前缀（验收"5 分钟内不可见"，缓存 TTL 本身 10 分钟）
                    RemoveWebCacheByPrefix(Rampage.NaturalistAtlas.Common.CacheKeys.PUBLIC_PREFIX);
                    return R.Success();
                }
                catch (Exception)
                {
                    RollbackTransaction();
                    throw;
                }
            }

            // 驳回路径：仅举报结单
            UpdateFields<Report> rejectFields = new();
            rejectFields.Set(m => m.Status, targetStatus);
            rejectFields.Set(m => m.HandleAdminId, adminId);
            rejectFields.Set(m => m.HandleTime, UtcNow);
            rejectFields.Set(m => m.HandleRemark, input.Remark);
            rejectFields.Set(m => m.UpdateTime, UtcNow);
            return ReportDao.UpdateById(report.Id, rejectFields) ? R.Success() : R.Failure();
        }
    }
}
