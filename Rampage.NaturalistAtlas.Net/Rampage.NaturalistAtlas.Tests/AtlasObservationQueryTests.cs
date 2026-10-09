using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Enums;

namespace Rampage.NaturalistAtlas.Tests
{
    /// <summary>
    /// 公开站组合筛选参数单测（契约 §6.2）：非法值静默忽略、跨年区间、bbox 解析
    /// Public filter parameter tests (contract §6.2): illegal values ignored silently,
    /// wrap-around month ranges, bbox parsing
    /// </summary>
    public class AtlasObservationQueryTests
    {
        [Fact]
        public void IconicList_IllegalValuesIgnored()
        {
            AtlasObservationQuery q = new() { Iconic = "1,abc,99,6,,0" };
            short[] list = q.GetIconicList();
            Assert.Equal(new short[] { 1, 6 }, list);
        }

        [Fact]
        public void QualityList_DefaultsToResearchAndNeedsId()
        {
            Assert.Equal(new short[] { 1, 2 }, new AtlasObservationQuery().GetQualityList());
            AtlasObservationQuery q = new() { Quality = "1" };
            Assert.Equal(new short[] { 1 }, q.GetQualityList());
        }

        [Fact]
        public void GetMonthRange_PlainAndWrapAround()
        {
            AtlasObservationQuery plain = new() { MonthFrom = 3, MonthTo = 5 };
            Assert.Equal((3, 5), plain.GetMonthRange()!.Value);

            AtlasObservationQuery wrap = new() { MonthFrom = 11, MonthTo = 2 };
            (int from, int to)? range = wrap.GetMonthRange();
            Assert.NotNull(range);
            Assert.Equal(11, range!.Value.from);
            Assert.Equal(2, range.Value.to);
        }

        [Fact]
        public void GetMonthRange_FullYearIsNull()
        {
            Assert.Null(new AtlasObservationQuery().GetMonthRange());
            Assert.Null(new AtlasObservationQuery { MonthFrom = 1, MonthTo = 12 }.GetMonthRange());
        }

        [Fact]
        public void GetBBox_ValidAndIllegal()
        {
            AtlasObservationQuery ok = new() { Bbox = "116.1,39.8,116.6,40.1" };
            decimal[]? bbox = ok.GetBBox();
            Assert.NotNull(bbox);
            Assert.Equal(4, bbox!.Length);
            Assert.Equal(116.1m, bbox[0]);

            Assert.Null(new AtlasObservationQuery { Bbox = "abc" }.GetBBox());
            Assert.Null(new AtlasObservationQuery { Bbox = "1,2,3" }.GetBBox());
        }

        [Fact]
        public void GetDateWindow_ParsesIsoDates()
        {
            AtlasObservationQuery q = new() { D1 = "2026-01-01", D2 = "2026-01-31" };
            Assert.Equal(new DateOnly(2026, 1, 1), q.GetDateFrom());
            Assert.Equal(new DateOnly(2026, 1, 31), q.GetDateTo());
        }
    }
}
