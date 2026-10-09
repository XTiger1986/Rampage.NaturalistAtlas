using Rampage.NaturalistAtlas.BLL;
using System;

namespace Rampage.NaturalistAtlas.Collector.Jobs
{
    /// <summary>
    /// 全历史物候直方图作业（每日凌晨 03:30）：对采集城市渐进拉取 iNat v1 histogram
    /// 填充 emergence_history（兑现产品文档"全历史统计口径"），单城单轮预算受限、
    /// 从未拉过的分类最优先、其后最旧优先，多轮覆盖全量
    /// Full-history phenology histogram job (daily at 03:30): progressively pulls the iNat v1
    /// histogram per collection city to fill emergence_history (honoring the product doc's
    /// "all-time" caliber); budget-capped per city per run, never-fetched taxa first then
    /// stalest first, covering the full set over rounds
    /// </summary>
    public class HistogramJob : CollectorJobBase
    {
        /// <inheritdoc/>
        protected override void ExecuteJob()
        {
            foreach (int cityId in GetCollectionCityIds())
            {
                CollectionService.RunHistogramAsync(cityId).GetAwaiter().GetResult();
            }
        }
    }
}
