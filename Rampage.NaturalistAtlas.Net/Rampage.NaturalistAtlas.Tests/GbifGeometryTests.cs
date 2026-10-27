using Rampage.NaturalistAtlas.Collection;

namespace Rampage.NaturalistAtlas.Tests
{
    /// <summary>
    /// GBIF geometry 环向归一化：顺时针外环会让 occurrence/search 返回 HTTP 400。
    /// GBIF geometry ring normalization: a clockwise shell makes occurrence/search return HTTP 400.
    /// </summary>
    public class GbifGeometryTests
    {
        /// <summary>
        /// 首都种子里触发过 400 的顺时针多边形（摩纳哥）。GBIF 报错里给出的改正环即保留起点的反转。
        /// Clockwise polygon from the capital seed that GBIF rejected (Monaco). The correction in the
        /// 400 body is this ring reversed, start vertex kept.
        /// </summary>
        private const string MONACO_CLOCKWISE =
            "POLYGON((7.4297 43.6743,7.4156 43.7257,7.4095 43.7299,7.4363 43.7509,7.4604 43.7222,7.4668 43.7035,7.4334 43.6751,7.4297 43.6743))";

        private const string MONACO_COUNTER_CLOCKWISE =
            "POLYGON((7.4297 43.6743,7.4334 43.6751,7.4668 43.7035,7.4604 43.7222,7.4363 43.7509,7.4095 43.7299,7.4156 43.7257,7.4297 43.6743))";

        [Fact]
        public void Normalize_ClockwiseExterior_ReversesAndKeepsStart()
        {
            Assert.Equal(MONACO_COUNTER_CLOCKWISE, GbifGeometry.NormalizeRingOrientation(MONACO_CLOCKWISE));
        }

        [Fact]
        public void Normalize_AlreadyCounterClockwise_UnchangedIncludingWhitespace()
        {
            Assert.Equal(MONACO_COUNTER_CLOCKWISE, GbifGeometry.NormalizeRingOrientation(MONACO_COUNTER_CLOCKWISE));

            // 已经逆时针的串连空白也原样返回，避免改动本来就能过的城市查询
            // An already-CCW string, whitespace included, is returned as stored
            string spaced = "  polygon ( ( 0 0, 1 0, 1 1, 0 1, 0 0 ) )  ";
            Assert.Equal(spaced, GbifGeometry.NormalizeRingOrientation(spaced));
        }

        [Fact]
        public void Normalize_NegativeCoordinates_ClockwiseShell()
        {
            Assert.Equal(
                "POLYGON((-1 -1,0 -1,0 0,-1 0,-1 -1))",
                GbifGeometry.NormalizeRingOrientation("POLYGON((-1 -1,-1 0,0 0,0 -1,-1 -1))"));
        }

        [Fact]
        public void Normalize_ClockwiseRectangle_BecomesCounterClockwise()
        {
            // GBIF 对轴对齐矩形放行顺时针，但城市外环不是矩形；矩形也一并调成逆时针
            // GBIF still accepts clockwise rectangles; shells are normalized anyway
            Assert.Equal(
                "POLYGON((0 0,1 0,1 1,0 1,0 0))",
                GbifGeometry.NormalizeRingOrientation("POLYGON((0 0,0 1,1 1,1 0,0 0))"));
        }

        [Fact]
        public void Normalize_Holes_ExteriorCounterClockwise_InteriorClockwise()
        {
            // 外环顺时针 + 孔逆时针：GBIF 会先报 clockwise exterior，孔本身也会报 anticlockwise interior
            // Clockwise shell and counter-clockwise hole: GBIF rejects both
            string wrong = "POLYGON((0 0,0 4,4 4,4 0,0 0),(1 1,2 1,2 2,1 2,1 1))";
            string expected = "POLYGON((0 0,4 0,4 4,0 4,0 0),(1 1,1 2,2 2,2 1,1 1))";
            Assert.Equal(expected, GbifGeometry.NormalizeRingOrientation(wrong));

            // 已经符合右手法则的带孔多边形不动 / An already right-hand polygon with a hole stays put
            Assert.Equal(expected, GbifGeometry.NormalizeRingOrientation(expected));
        }

        [Fact]
        public void Normalize_MultiPolygon_NormalizesEachShellAndHole()
        {
            string wrong =
                "MULTIPOLYGON(((0 0,0 1,1 1,1 0,0 0)),((0 0,0 4,4 4,4 0,0 0),(1 1,2 1,2 2,1 2,1 1)))";
            string expected =
                "MULTIPOLYGON(((0 0,1 0,1 1,0 1,0 0)),((0 0,4 0,4 4,0 4,0 0),(1 1,1 2,2 2,2 1,1 1)))";
            Assert.Equal(expected, GbifGeometry.NormalizeRingOrientation(wrong));
            Assert.Equal(expected, GbifGeometry.NormalizeRingOrientation(expected));
        }

        [Fact]
        public void Normalize_ZeroAreaSpur_DroppedThenOriented()
        {
            // 重复顶点夹出的零面积尖刺：丢掉尖刺（覆盖范围不变），再把外环调成逆时针。
            // A repeated vertex forms a zero-area spur. Drop it, then orient the shell CCW.
            Assert.Equal(
                "POLYGON((0 0,1 0,3 0,3 3,0 3,0 0))",
                GbifGeometry.NormalizeRingOrientation("POLYGON((0 0,1 0,1 -1,1 0,3 0,3 3,0 3,0 0))"));
            Assert.Equal(
                "POLYGON((0 0,3 0,3 3,0 3,0 1,0 0))",
                GbifGeometry.NormalizeRingOrientation("POLYGON((0 0,0 1,-1 1,0 1,0 3,3 3,3 0,0 0))"));
        }

        [Fact]
        public void Normalize_SelfTouchingRing_ReversesClockwise_KeepsCounterClockwise()
        {
            // 非相邻顶点自触（(2,0) 出现两次）。转向仍可判定，反转后闭合且再次归一化不变。
            // A non-adjacent vertex is repeated. Orientation is still defined; a second pass is a no-op.
            string clockwise = "POLYGON((0 0,0 3,3 3,3 0,2 0,2 1,1 1,1 0,2 0,0 0))";
            string counterClockwise = "POLYGON((0 0,2 0,1 0,1 1,2 1,2 0,3 0,3 3,0 3,0 0))";
            Assert.Equal(counterClockwise, GbifGeometry.NormalizeRingOrientation(clockwise));
            Assert.Equal(counterClockwise, GbifGeometry.NormalizeRingOrientation(counterClockwise));

            // 两瓣在顶点捏合、本身已是逆时针：不改点序 / Two lobes pinching at a vertex, already CCW
            string pinched = "POLYGON((0 0,2 0,1 1,2 2,0 2,1 1,0 0))";
            Assert.Equal(pinched, GbifGeometry.NormalizeRingOrientation(pinched));
        }

        [Fact]
        public void Normalize_Idempotent_AndPreservesNumberTokens()
        {
            string once = GbifGeometry.NormalizeRingOrientation(MONACO_CLOCKWISE);
            Assert.Equal(once, GbifGeometry.NormalizeRingOrientation(once));

            // 科学计数法的原文要留下来，不能被 double 回写改掉
            // Scientific-notation tokens survive; they are not reprinted from the parsed double
            Assert.Equal(
                "POLYGON((0 0,1 0,1 1,0 1e0,0 0))",
                GbifGeometry.NormalizeRingOrientation("POLYGON((0 0,0 1e0,1 1,1 0,0 0))"));
        }

        [Fact]
        public void Normalize_SridPrefix_KeptWhenRingIsReversed()
        {
            Assert.Equal(
                "SRID=4326;POLYGON((0 0,1 0,1 1,0 1,0 0))",
                GbifGeometry.NormalizeRingOrientation("SRID=4326;POLYGON((0 0,0 1,1 1,1 0,0 0))"));
        }

        [Fact]
        public void Normalize_UnparseableOrEmpty_ReturnedUnchanged()
        {
            Assert.Equal(string.Empty, GbifGeometry.NormalizeRingOrientation(null));
            Assert.Equal("  ", GbifGeometry.NormalizeRingOrientation("  "));
            Assert.Equal("POLYGON EMPTY", GbifGeometry.NormalizeRingOrientation("POLYGON EMPTY"));
            Assert.Equal("MULTIPOLYGON EMPTY", GbifGeometry.NormalizeRingOrientation("MULTIPOLYGON EMPTY"));
            Assert.Equal("not a polygon", GbifGeometry.NormalizeRingOrientation("not a polygon"));
            Assert.Equal("POLYGON((1 2, 3))", GbifGeometry.NormalizeRingOrientation("POLYGON((1 2, 3))"));
            // 零面积：两种走向都不是有限多边形，保持原样 / Zero area stays as stored
            Assert.Equal(
                "POLYGON((0 0,1 0,1 0,0 0))",
                GbifGeometry.NormalizeRingOrientation("POLYGON((0 0,1 0,1 0,0 0))"));
        }
    }
}
