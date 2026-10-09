using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 运营看板聚合服务实现 / Dashboard aggregate service implementation
    /// 编排层：聚合 SQL 全部收口在 DAL（ObservationDao），此处仅做组装、补洞与文案化
    /// Orchestration only: aggregate SQL lives in the DAL (ObservationDao); this layer assembles, fills gaps and localizes
    /// </summary>
    public class DashboardService : SuperService<Observation, IObservationDao>, IDashboardService
    {
        private const int ICONIC_TOP_N = 8;

        [Auto]
        protected IObservationDao ObservationDao = default!;

        [Auto]
        protected ISyncCursorDao SyncCursorDao = default!;

        [Auto]
        protected IReportDao ReportDao = default!;

        [Auto]
        protected ICityDao CityDao = default!;

        [Auto]
        protected ISettingService SettingService = default!;


        /// <inheritdoc/>
        public DashboardDto GetDashboard()
        {
            DateTime now = UtcNow;
            DashboardDto result = new DashboardDto();

            // 口径城市：观察类指标按默认城市过滤（页面文案"口径城市"与实现终于一致）；城市缺失时退全站
            // Scope city: observation metrics filter by the default city (the on-page copy finally
            // matches the implementation); falls back to whole-site when the city is missing
            GeneralSetting setting = SettingService.GetSetting<GeneralSetting>();
            City? city = CityDao.SelectById(setting.DefaultCityId);
            int? cityId = city?.Id;
            result.CityName = city?.Name;

            DashboardBaseStatDto baseStat = ObservationDao.GetBaseStat(cityId);
            result.TotalObservation = baseStat.Total;
            result.ResearchCount = baseStat.Research;
            result.ResearchRatio = baseStat.Total > 0 ? (double)baseStat.Research / baseStat.Total : null;

            result.MonthInsertCount = ObservationDao.GetMonthInsertCount(new DateTime(now.Year, now.Month, 1), cityId);

            DateTime? newest = ObservationDao.GetNewestSourceCreatedAt(cityId);
            if (newest.HasValue)
            {
                result.FreshnessDays = Math.Round((now - newest.Value).TotalDays, 1);
            }

            result.IconicDistribution = BuildIconicSlices(ObservationDao.GetIconicDistributionList(cityId));

            result.Recent30Days = FillDateGaps(
                ObservationDao.GetIngestTrendList(now.Date.AddDays(-29), cityId),
                now.Date.AddDays(-29), now.Date);

            DateOnly monthFrom = DateOnly.FromDateTime(now.AddMonths(-11));
            result.Monthly12 = FillMonthGaps(
                ObservationDao.GetMonthlyDistributionList(monthFrom, cityId),
                monthFrom, DateOnly.FromDateTime(now));

            // 运营视角的附加图表（全站口径：城市排行天然跨城；失败/举报趋势关心的是整个采集与工单面）
            // Ops-view extra charts (whole-site: the ranking is cross-city by nature; failure/report
            // trends watch the whole collection and ticket surface)
            result.CityTop10 = ObservationDao.GetCityTopNList(10);
            result.SyncFailure30Days = FillDateGaps(
                ObservationDao.GetSyncFailureTrendList(now.Date.AddDays(-29)),
                now.Date.AddDays(-29), now.Date);
            result.Report30Days = FillDateGaps(
                ObservationDao.GetReportTrendList(now.Date.AddDays(-29)),
                now.Date.AddDays(-29), now.Date);

            result.PendingReportCount = ReportDao.Count(r => r.Status == ReportStatus.Pending);
            result.SyncHealthList = BuildSyncHealth();
            return result;
        }

        /// <summary>
        /// 类群构成切片：top N + 其余合并为「其他」/ Iconic slices: top N plus an "others" merge
        /// </summary>
        private List<DashboardSliceDto> BuildIconicSlices(List<DashboardIconicRowDto> rowList)
        {
            List<DashboardSliceDto> sliceList = rowList.Take(ICONIC_TOP_N).Select(row => new DashboardSliceDto
            {
                Label = EnumReader.GetText(row.IconicTaxon)!,
                Value = row.Count
            }).ToList();
            long rest = rowList.Skip(ICONIC_TOP_N).Sum(row => row.Count);
            if (rest > 0)
            {
                sliceList.Add(new DashboardSliceDto { Label = "其他", Value = rest });
            }
            return sliceList;
        }

        /// <summary>
        /// 采集健康度：每任务最近一条 cursor 状态
        /// Sync health: latest cursor status per task
        /// </summary>
        private List<SyncTaskHealthDto> BuildSyncHealth()
        {
            List<SyncTaskHealthDto> healthList = new List<SyncTaskHealthDto>();
            List<SyncCursor> cursorList = SyncCursorDao.SelectList(c => true);
            // PG 堆内更新行会漂移：内存按任务序稳定排序兜底
            foreach (SyncCursor cursor in cursorList.OrderBy(c => c.Task).ThenBy(c => c.CityId))
            {
                healthList.Add(new SyncTaskHealthDto
                {
                    TaskCode = cursor.Task ?? "-",
                    Task = SyncTaskCodes.LabelOf(cursor.Task),
                    LastRunStatusText = EnumReader.GetText(cursor.LastRunStatus)!,
                    IsHealthy = cursor.LastRunStatus == CursorRunStatus.Success,
                    LastRunTime = cursor.LastRunTime == DateTime.MinValue ? null : cursor.LastRunTime,
                    LastRunMessage = cursor.LastRunMessage
                });
            }
            return healthList;
        }

        /// <summary>
        /// 补齐日期空洞（无入库的日期补 0，折线连续）/ Fill date gaps with zero for a continuous line
        /// </summary>
        private List<DashboardPointDto> FillDateGaps(List<DashboardTrendRowDto> rowList, DateTime from, DateTime to)
        {
            Dictionary<string, long> dict = rowList.ToDictionary(r => r.Label, r => r.Value);
            List<DashboardPointDto> pointList = new List<DashboardPointDto>();
            for (DateTime day = from; day <= to; day = day.AddDays(1))
            {
                string label = day.ToString("yyyy-MM-dd");
                pointList.Add(new DashboardPointDto { Label = label, Value = dict.TryGetValue(label, out long value) ? value : 0 });
            }
            return pointList;
        }

        /// <summary>
        /// 补齐月份空洞（近 12 个月）/ Fill month gaps (last 12 months)
        /// </summary>
        private List<DashboardPointDto> FillMonthGaps(List<DashboardTrendRowDto> rowList, DateOnly from, DateOnly to)
        {
            Dictionary<string, long> dict = rowList.ToDictionary(r => r.Label, r => r.Value);
            List<DashboardPointDto> pointList = new List<DashboardPointDto>();
            DateOnly month = new DateOnly(from.Year, from.Month, 1);
            while (month <= to)
            {
                string label = month.ToString("yyyy-MM");
                pointList.Add(new DashboardPointDto { Label = label, Value = dict.TryGetValue(label, out long value) ? value : 0 });
                month = month.AddMonths(1);
            }
            return pointList;
        }
    }
}
