using Rampage.Data;
using Rampage.Data.Models;
using Rampage.DependencyInjection;
using Rampage.Model;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Utilities;
using Rampage.Web;
using System;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 公开站观察服务实现 / Public observation service implementation
    /// </summary>
    public class AtlasObservationService : SuperService<Observation, IObservationDao>, IAtlasObservationService
    {
        /// <summary>点位缓存键模板（{0}=规范化筛选，{1}=上限）/ Point cache key template ({0}=normalized filter, {1}=limit)</summary>
        private const string CACHE_KEY_POINTS_TPL = "NaturalistAtlas:Public:Points:{0}:{1}";

        /// <summary>点位缓存秒数（采集为小时级增量，5 分钟足够新鲜）/ Point cache lifetime (ingestion is hourly, 5 minutes is fresh enough)</summary>
        private const double POINTS_CACHE_SECONDS = 300;

        /// <summary>举报补充说明长度上限 / Max report content length</summary>
        private const int REPORT_CONTENT_MAX = 500;

        /// <summary>举报联系方式长度上限 / Max report contact length</summary>
        private const int REPORT_CONTACT_MAX = 100;

        [Auto]
        protected IObservationDao ObservationDao = default!;

        [Auto]
        protected IReportDao ReportDao = default!;


        /// <inheritdoc/>
        public ObservationPointResultDto GetPoints(AtlasObservationQuery query, int limit)
        {
            limit = Math.Clamp(limit, 1, 10000);
            return HandleWebCache(() => {
                List<ObservationPointDto> points = ObservationDao.GetPublicPointsList(query, limit);
                // 未触顶时总数即点位数，省去一次 COUNT
                // Below the limit the total equals the point count, saving a COUNT query
                bool truncated = points.Count >= limit;
                return new ObservationPointResultDto
                {
                    Points = points,
                    Total = truncated ? ObservationDao.GetPublicCount(query) : points.Count,
                    Truncated = truncated
                };
            }, CACHE_KEY_POINTS_TPL, POINTS_CACHE_SECONDS, query.ToCacheKey(), limit) ?? new ObservationPointResultDto();
        }

        /// <inheritdoc/>
        public PageData<ObservationCardDto> GetCardPage(AtlasObservationQuery query, int pageIndex, int pageSize, bool photoOnly = false)
        {
            PagerInfo pager = new PagerInfo { PageIndex = pageIndex, PageSize = Math.Clamp(pageSize, 1, 60) };
            List<ObservationCardDto> list = ObservationDao.GetPublicCardPageList(query, (pager.PageIndex - 1) * pager.PageSize, pager.PageSize, photoOnly);
            pager.RecordCount = ObservationDao.GetPublicCount(query, photoOnly);
            return new PageData<ObservationCardDto> { Pager = pager, DataList = list };
        }

        /// <inheritdoc/>
        public ObservationDetailDto? GetDetail(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }
            ObservationDetailDto? detail = ObservationDao.GetPublicDetail(id.Trim());
            if (detail != null)
            {
                detail.SourceUrl = BuildSourceUrl(detail.Source, detail.SourceObservationId);
            }
            return detail;
        }

        /// <inheritdoc/>
        public R<string> SubmitReport(ReportSubmitInput? input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.ObservationId))
            {
                return R<string>.Failure("参数无效");
            }
            if (!Enum.IsDefined(typeof(ReportReason), input.Reason))
            {
                return R<string>.Failure("举报原因无效");
            }
            string content = (input.Content ?? string.Empty).Trim();
            string contact = (input.Contact ?? string.Empty).Trim();
            if (content.Length > REPORT_CONTENT_MAX)
            {
                return R<string>.Failure($"补充说明不能超过 {REPORT_CONTENT_MAX} 字");
            }
            if (contact.Length > REPORT_CONTACT_MAX)
            {
                return R<string>.Failure($"联系方式不能超过 {REPORT_CONTACT_MAX} 字");
            }
            // 选择"其他"时必须说明原因，便于运营判断
            // "Other" requires a description so operators can judge the report
            if (input.Reason == (short)ReportReason.Other && content.Length == 0)
            {
                return R<string>.Failure("选择“其他”时请填写补充说明");
            }

            Observation? observation = ObservationDao.SelectById(input.ObservationId.Trim());
            if (observation == null || observation.Status != ObservationStatus.Normal)
            {
                return R<string>.Failure("该观察不存在或已下线");
            }

            DateTime now = UtcNow;
            Report report = new Report
            {
                Id = SnowflakeTool.NextIdString(),
                ObservationId = observation.Id,
                Reason = (ReportReason)input.Reason,
                Content = content,
                Contact = contact,
                Status = ReportStatus.Pending,
                HandleRemark = string.Empty,
                CreateTime = now,
                UpdateTime = now
            };
            return ReportDao.Insert(report, insertId: true)
                ? R<string>.Success(report.Id)
                : R<string>.Failure("举报提交失败，请稍后再试");
        }

        /// <summary>
        /// 按数据源拼接源站原始页面链接 / Build the source page URL per data source
        /// </summary>
        /// <param name="source">数据源 / Source</param>
        /// <param name="sourceObservationId">源站观察 id / Source observation id</param>
        /// <returns>链接，未知数据源返回 null / The URL, or null for an unknown source</returns>
        private string? BuildSourceUrl(short source, string sourceObservationId)
        {
            if (string.IsNullOrWhiteSpace(sourceObservationId))
            {
                return null;
            }
            string escaped = Uri.EscapeDataString(sourceObservationId);
            return (AtlasSource)source switch
            {
                AtlasSource.INaturalist => $"https://www.inaturalist.org/observations/{escaped}",
                AtlasSource.Gbif => $"https://www.gbif.org/occurrence/{escaped}",
                _ => null
            };
        }
    }
}
