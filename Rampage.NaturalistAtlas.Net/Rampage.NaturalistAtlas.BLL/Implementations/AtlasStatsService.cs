using Rampage.DependencyInjection;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 公开站统计服务实现 / Public stats service implementation
    /// </summary>
    public class AtlasStatsService : SuperService<Observation, IObservationDao>, IAtlasStatsService
    {
        /// <summary>总览缓存键模板（{0}=城市，{1}=仅研究级）/ Overview cache key template ({0}=city, {1}=research only)</summary>
        private const string CACHE_KEY_OVERVIEW_TPL = "NaturalistAtlas:Public:Overview:{0}:{1}";

        /// <summary>总览缓存秒数 / Overview cache lifetime</summary>
        private const double OVERVIEW_CACHE_SECONDS = 600;

        /// <summary>本月新星门槛（研究级累计条数）/ Rising-star threshold (cumulative research-grade records)</summary>
        private const int RISING_STAR_THRESHOLD = 5;

        /// <summary>本月新星条数 / Rising-star count</summary>
        private const int RISING_STAR_LIMIT = 12;

        /// <summary>活跃观察者条数 / Top observer count</summary>
        private const int TOP_OBSERVER_LIMIT = 10;

        [Auto]
        protected IObservationDao ObservationDao = default!;

        [Auto]
        protected ICityDao CityDao = default!;


        /// <inheritdoc/>
        public CityOverviewDto? GetCityOverview(int cityId, bool researchOnly)
        {
            if (cityId <= 0)
            {
                return null;
            }
            return HandleWebCache<CityOverviewDto?>(() => {
                City? city = CityDao.SelectById(cityId);
                if (city == null)
                {
                    return null;
                }
                short[] qualityList = researchOnly ? new short[] { 1 } : new short[] { 1, 2 };
                DateTime now = UtcNow;
                DateOnly monthStart = new DateOnly(now.Year, now.Month, 1);

                OverviewBaseRowDto baseRow = ObservationDao.GetOverviewBase(cityId, qualityList, monthStart);
                return new CityOverviewDto
                {
                    CityId = cityId,
                    CityName = city.Name,
                    TotalObservation = baseRow.Total,
                    ResearchCount = baseRow.Research,
                    SpeciesCount = baseRow.Species,
                    ObserverCount = baseRow.Observers,
                    MonthObservation = baseRow.MonthCount,
                    NewestSourceCreatedAt = baseRow.Newest,
                    // 近 12 个月趋势（含当月；BLL 补 0 月桶）
                    // 12-month trend (including this month; zero months filled by the BLL)
                    Monthly12 = FillMonthly12(ObservationDao.GetMonthly12List(cityId, qualityList, monthStart.AddMonths(-11)), monthStart),
                    IconicDistribution = ObservationDao.GetOverviewIconicList(cityId, qualityList),
                    TopObservers = ObservationDao.GetTopObserversList(cityId, qualityList, TOP_OBSERVER_LIMIT),
                    // 新星口径固定为研究级（与"仅研究级"开关无关），避免待鉴定记录误报
                    // Rising stars always count research grade (independent of the toggle) to avoid false positives from needs_id rows
                    RisingStars = ObservationDao.GetRisingStarsList(cityId, monthStart, RISING_STAR_THRESHOLD, RISING_STAR_LIMIT)
                };
            }, CACHE_KEY_OVERVIEW_TPL, OVERVIEW_CACHE_SECONDS, cityId, researchOnly ? 1 : 0);
        }

        /// <summary>
        /// 补齐 12 个月桶（无记录月份计 0）
        /// Fill the 12 month buckets (months without rows count as zero)
        /// </summary>
        private List<MonthlyCountDto> FillMonthly12(List<MonthlyCountDto> rows, DateOnly monthStart)
        {
            Dictionary<string, long> dict = rows.ToDictionary(r => r.Month, r => r.Count);
            List<MonthlyCountDto> filled = new List<MonthlyCountDto>(12);
            DateOnly month = monthStart.AddMonths(-11);
            while (month <= monthStart)
            {
                string label = month.ToString("yyyy-MM");
                filled.Add(new MonthlyCountDto { Month = label, Count = dict.TryGetValue(label, out long count) ? count : 0 });
                month = month.AddMonths(1);
            }
            return filled;
        }
    }
}
