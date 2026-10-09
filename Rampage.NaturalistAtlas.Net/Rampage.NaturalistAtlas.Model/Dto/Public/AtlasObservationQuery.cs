using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rampage.NaturalistAtlas.Model.Dto.Public
{
    /// <summary>
    /// 公开站观察组合筛选（observation/list、observation/points、species/rank、phenology/matrix 共用，[FromQuery] 绑定）
    /// Public observation filter shared by observation/list, observation/points, species/rank and phenology/matrix ([FromQuery] bound)
    /// </summary>
    /// <remarks>
    /// 与前端 src/api/types.ts 的 ObservationQuery 一一对应；数组参数以逗号分隔传输（如 iconic=1,6,9）。
    /// 解析方法对非法值一律静默忽略（公开接口不因脏参数报错，按"无该筛选"处理）。
    /// Mirrors ObservationQuery in the frontend src/api/types.ts; arrays travel comma-separated (e.g. iconic=1,6,9).
    /// The parse helpers silently ignore invalid values (a public API treats a dirty parameter as "no such filter" rather than failing).
    /// </remarks>
    public class AtlasObservationQuery
    {
        /// <summary>城市 id（city.id）/ City id</summary>
        public int? CityId { get; set; }

        /// <summary>视口矩形 "w,s,e,n"（经纬度）/ Viewport rectangle "w,s,e,n" (degrees)</summary>
        public string? Bbox { get; set; }

        /// <summary>类群代码，逗号分隔（IconicTaxon 1~14）/ Iconic taxon codes, comma separated</summary>
        public string? Iconic { get; set; }

        /// <summary>起始月（1~12，可大于 MonthTo 表示跨年，如 11~2）/ Start month (1~12; greater than MonthTo means wrapping the year end)</summary>
        public int? MonthFrom { get; set; }

        /// <summary>结束月（1~12）/ End month (1~12)</summary>
        public int? MonthTo { get; set; }

        /// <summary>鉴定级别，逗号分隔（缺省 research + needs_id）/ Quality grades, comma separated (defaults to research + needs_id)</summary>
        public string? Quality { get; set; }

        /// <summary>观察日期下限 YYYY-MM-DD（含）/ Observed-on lower bound YYYY-MM-DD (inclusive)</summary>
        public string? D1 { get; set; }

        /// <summary>观察日期上限 YYYY-MM-DD（含）/ Observed-on upper bound YYYY-MM-DD (inclusive)</summary>
        public string? D2 { get; set; }

        /// <summary>限定物种 taxon.id / Restrict to one taxon (taxon.id)</summary>
        public string? TaxonId { get; set; }

        /// <summary>
        /// 解析类群代码列表（仅保留 1~14）/ Parse the iconic codes (1~14 only)
        /// </summary>
        /// <returns>去重后的代码数组，空数组表示不筛选 / Distinct codes; empty means no filter</returns>
        public short[] GetIconicList()
        {
            return ParseCodes(Iconic, 1, 14);
        }

        /// <summary>
        /// 解析鉴定级别列表（仅保留 1~3；缺省返回 research + needs_id）
        /// Parse the quality grades (1~3 only; defaults to research + needs_id)
        /// </summary>
        /// <returns>去重后的级别数组 / Distinct grades</returns>
        public short[] GetQualityList()
        {
            short[] list = ParseCodes(Quality, 1, 3);
            return list.Length > 0 ? list : new short[] { 1, 2 };
        }

        /// <summary>
        /// 解析视口矩形；格式或范围非法返回 null
        /// Parse the viewport rectangle; returns null when the format or range is invalid
        /// </summary>
        /// <returns>[w, s, e, n] 或 null / [w, s, e, n] or null</returns>
        public decimal[]? GetBBox()
        {
            if (string.IsNullOrWhiteSpace(Bbox))
            {
                return null;
            }
            string[] parts = Bbox.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 4)
            {
                return null;
            }
            decimal[] values = new decimal[4];
            for (int i = 0; i < 4; i++)
            {
                if (!decimal.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                {
                    return null;
                }
            }
            // 西 < 东、南 < 北，且在经纬度合法范围内（跨日界线的视口不在城市粒度业务范围内，不支持）
            // West < east and south < north within valid ranges (antimeridian-crossing viewports are outside the city-scale scope)
            bool valid = values[0] >= -180 && values[2] <= 180 && values[1] >= -90 && values[3] <= 90
                && values[0] < values[2] && values[1] < values[3];
            return valid ? values : null;
        }

        /// <summary>
        /// 规范化后的月份区间；任一端缺失或非法、或为全年（1~12）时返回 null
        /// Normalized month range; null when either end is missing/invalid or the range is the full year (1~12)
        /// </summary>
        /// <returns>(起, 止) 或 null / (from, to) or null</returns>
        public (int From, int To)? GetMonthRange()
        {
            if (MonthFrom is not (>= 1 and <= 12) || MonthTo is not (>= 1 and <= 12))
            {
                return null;
            }
            if (MonthFrom == 1 && MonthTo == 12)
            {
                return null;
            }
            return (MonthFrom.Value, MonthTo.Value);
        }

        /// <summary>解析日期下限 / Parse the lower date bound</summary>
        public DateOnly? GetDateFrom()
        {
            return ParseDate(D1);
        }

        /// <summary>解析日期上限 / Parse the upper date bound</summary>
        public DateOnly? GetDateTo()
        {
            return ParseDate(D2);
        }

        /// <summary>
        /// 缓存键片段：规范化后的全部筛选条件拼接（同义参数得到同一键）
        /// Cache key fragment: all normalized filters joined (equivalent parameters yield the same key)
        /// </summary>
        /// <returns>缓存键片段 / The key fragment</returns>
        public string ToCacheKey()
        {
            (int From, int To)? month = GetMonthRange();
            decimal[]? bbox = GetBBox();
            return string.Join("|",
                CityId?.ToString() ?? "-",
                bbox == null ? "-" : string.Join(",", bbox.Select(v => v.ToString("0.####", CultureInfo.InvariantCulture))),
                string.Join(",", GetIconicList().OrderBy(v => v)),
                month == null ? "-" : $"{month.Value.From}-{month.Value.To}",
                string.Join(",", GetQualityList().OrderBy(v => v)),
                GetDateFrom()?.ToString("yyyyMMdd") ?? "-",
                GetDateTo()?.ToString("yyyyMMdd") ?? "-",
                string.IsNullOrWhiteSpace(TaxonId) ? "-" : TaxonId.Trim());
        }

        /// <summary>
        /// 浅拷贝（BLL 需要派生口径时克隆后覆写字段，如稀缺榜固定研究级）
        /// Shallow copy (the BLL clones then overrides fields for derived scopes, e.g. the rarity board's research-grade scope)
        /// </summary>
        /// <returns>新查询对象 / A new query</returns>
        public AtlasObservationQuery Clone()
        {
            return (AtlasObservationQuery)MemberwiseClone();
        }

        /// <summary>
        /// 复制一份去掉月份筛选的查询（物候矩阵需要完整 12 个月）
        /// Clone without the month filter (the phenology matrix needs all 12 months)
        /// </summary>
        /// <returns>新查询对象 / A new query</returns>
        public AtlasObservationQuery WithoutMonth()
        {
            AtlasObservationQuery copy = Clone();
            copy.MonthFrom = null;
            copy.MonthTo = null;
            return copy;
        }

        /// <summary>
        /// 解析逗号分隔的数字代码，保留 [min, max] 范围内的去重值
        /// Parse comma-separated numeric codes, keeping distinct values within [min, max]
        /// </summary>
        private static short[] ParseCodes(string? text, short min, short max)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<short>();
            }
            List<short> list = new List<short>();
            foreach (string part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (short.TryParse(part, out short value) && value >= min && value <= max && !list.Contains(value))
                {
                    list.Add(value);
                }
            }
            return list.ToArray();
        }

        /// <summary>解析 YYYY-MM-DD 日期 / Parse a YYYY-MM-DD date</summary>
        private static DateOnly? ParseDate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            return DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
                ? date
                : null;
        }
    }
}
