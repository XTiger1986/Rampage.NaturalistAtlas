using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Rampage.NaturalistAtlas.Collection
{
    /// <summary>
    /// GBIF geometry 参数的 WKT 环向归一化。
    /// occurrence/search 按 JTS <c>Orientation.isCCW</c> 校验：外环必须逆时针，内环（孔）必须顺时针
    /// （右手法则）。顺时针外环返回 HTTP 400（<c>Polygon with clockwise exterior ring</c>），
    /// 逆时针内环返回 <c>Polygon with anticlockwise interior ring</c>。
    /// 城市 boundary_wkt 里有一批外环是顺时针（iNat place 几何居多），直接透传会被熔断跳过。
    /// 重复顶点夹出的零面积尖刺会另报 Self-intersection，归一化时丢掉，不改变覆盖范围。
    /// GBIF geometry WKT ring normalizer. Occurrence search checks JTS Orientation.isCCW:
    /// exterior rings must be counter-clockwise and holes clockwise (right-hand rule).
    /// A clockwise shell is HTTP 400, which circuit-skips that city's GBIF backfill.
    /// A zero-area spur on a repeated vertex is dropped; it does not change the covered area.
    /// </summary>
    public static class GbifGeometry
    {
        /// <summary>
        /// 把 POLYGON / MULTIPOLYGON 调成 GBIF 接受的环向。已符合的串原样返回；解析不了的也原样返回（不抛）。
        /// Normalize POLYGON / MULTIPOLYGON ring orientation for GBIF.
        /// Already-correct and unparseable inputs are returned unchanged. Never throws.
        /// </summary>
        /// <param name="wkt">城市边界或 bbox 矩形 WKT / City boundary or bbox rectangle WKT</param>
        /// <returns>可提交给 GBIF geometry 的 WKT / WKT safe to send as the GBIF geometry parameter</returns>
        public static string NormalizeRingOrientation(string? wkt)
        {
            if (string.IsNullOrWhiteSpace(wkt))
            {
                return wkt ?? string.Empty;
            }

            if (!WktReader.TryParse(wkt, out ParsedWkt? parsed) || parsed == null || parsed.Empty)
            {
                return wkt;
            }

            bool changed = false;
            for (int p = 0; p < parsed.Polygons.Count; p++)
            {
                List<List<Coord>> rings = parsed.Polygons[p];
                for (int r = 0; r < rings.Count; r++)
                {
                    // 重复顶点夹出的零面积尖刺会让 GBIF 报 Self-intersection；去掉尖刺不改变覆盖范围。
                    // 两瓣都有面积的捏合环不动几何，只在下面调环向。
                    // A repeated vertex with a zero-area spur is a GBIF self-intersection. Dropping the spur
                    // does not change the covered area. A pinch whose both lobes have area is left intact.
                    List<Coord> simplified = DropZeroAreaSpurs(rings[r]);
                    if (!SameRing(simplified, rings[r]))
                    {
                        rings[r] = simplified;
                        changed = true;
                    }

                    // 每个多边形的第一环是外环，其余是孔 / First ring is the shell, the rest are holes
                    bool wantCounterClockwise = r == 0;
                    if (!NeedsReverse(rings[r], wantCounterClockwise))
                    {
                        continue;
                    }

                    rings[r] = ReverseKeepingStart(rings[r]);
                    changed = true;
                }
            }

            if (!changed)
            {
                return wkt;
            }

            return Emit(parsed);
        }

        /// <summary>
        /// 去掉零面积尖刺：环上非相邻的重复顶点把环拆成两段，其中一段面积约为 0 时丢掉那段。
        /// 连续重复点一并去掉。两段都有面积（真正的捏合）则保持原环。
        /// Drop a zero-area spur created by a repeated non-adjacent vertex, and drop consecutive duplicates.
        /// A pinch where both loops enclose area is kept as stored.
        /// </summary>
        private static List<Coord> DropZeroAreaSpurs(List<Coord> ring)
        {
            if (ring.Count < 4 || !SamePoint(ring[0], ring[ring.Count - 1]))
            {
                return ring;
            }

            List<Coord> pts = new List<Coord>(ring.Count - 1);
            for (int i = 0; i < ring.Count - 1; i++)
            {
                pts.Add(ring[i]);
            }

            bool edited = false;
            bool progressed = true;
            while (progressed)
            {
                progressed = false;
                Dictionary<(long XBits, long YBits), int> seen = new Dictionary<(long XBits, long YBits), int>();
                for (int i = 0; i < pts.Count; i++)
                {
                    (long XBits, long YBits) key = PointKey(pts[i]);
                    if (!seen.TryGetValue(key, out int first))
                    {
                        seen[key] = i;
                        continue;
                    }

                    if (i - first <= 1)
                    {
                        pts.RemoveAt(i);
                        edited = true;
                        progressed = true;
                        break;
                    }

                    double spur = Math.Abs(SignedAreaRange(pts, first, i));
                    double rest = Math.Abs(SignedAreaWrap(pts, i, first));
                    if (spur <= 1e-12 && rest > 1e-12)
                    {
                        pts.RemoveRange(first + 1, i - first);
                        edited = true;
                        progressed = true;
                        break;
                    }

                    if (rest <= 1e-12 && spur > 1e-12)
                    {
                        List<Coord> keep = new List<Coord>(i - first);
                        for (int k = first; k < i; k++)
                        {
                            keep.Add(pts[k]);
                        }

                        pts = keep;
                        edited = true;
                        progressed = true;
                        break;
                    }

                    seen[key] = i;
                }
            }

            if (!edited || pts.Count < 3)
            {
                return ring;
            }

            pts.Add(pts[0]);
            return pts;
        }

        /// <summary>闭合切片 first..last（两端是同一顶点）的带符号面积 / Signed area of the closed slice first..last</summary>
        private static double SignedAreaRange(List<Coord> pts, int first, int last)
        {
            int count = last - first + 1;
            int[] indices = new int[count];
            for (int i = 0; i < count; i++)
            {
                indices[i] = first + i;
            }

            return SignedAreaIndices(pts, indices);
        }

        /// <summary>从 from 绕过环首回到 to（两端是同一顶点）的带符号面积 / Signed area of the wrap-around slice</summary>
        private static double SignedAreaWrap(List<Coord> pts, int from, int to)
        {
            int count = (pts.Count - from) + to + 1;
            int[] indices = new int[count];
            int n = 0;
            for (int k = from; k < pts.Count; k++)
            {
                indices[n++] = k;
            }

            for (int k = 0; k <= to; k++)
            {
                indices[n++] = k;
            }

            return SignedAreaIndices(pts, indices);
        }

        private static double SignedAreaIndices(List<Coord> pts, int[] indices)
        {
            if (indices.Length < 3)
            {
                return 0;
            }

            double sum = 0;
            for (int i = 0; i < indices.Length - 1; i++)
            {
                Coord a = pts[indices[i]];
                Coord b = pts[indices[i + 1]];
                sum += (a.X * b.Y) - (b.X * a.Y);
            }

            return sum / 2.0;
        }

        private static (long XBits, long YBits) PointKey(Coord coord)
        {
            return (BitConverter.DoubleToInt64Bits(coord.X), BitConverter.DoubleToInt64Bits(coord.Y));
        }

        private static bool SameRing(List<Coord> left, List<Coord> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i].Text, right[i].Text, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 外环要逆时针、孔要顺时针。isCCW 对两种走向都判否（自触/平顶退化）时回退带符号面积，面积为零则不动。
        /// Shells should be CCW and holes CW. When isCCW rejects both directions (self-touch or a flat cap),
        /// fall back to signed area; a zero-area ring is left alone.
        /// </summary>
        private static bool NeedsReverse(List<Coord> ring, bool wantCounterClockwise)
        {
            Coord[] closed = Close(ring);
            RingFacing facing = Classify(closed);
            if (facing == RingFacing.Indeterminate)
            {
                return false;
            }

            return wantCounterClockwise ? facing == RingFacing.Clockwise : facing == RingFacing.CounterClockwise;
        }

        /// <summary>
        /// 与 GBIF <c>SearchTypeValidator.isLinearRingCCW</c> 同一规则：JTS <c>Orientation.isCCW</c>。
        /// 它对合法环与顶点最高处的局部转向一致；自交/自触环可能失真，所以再试反转，两边都失败才用面积。
        /// Same predicate GBIF uses (JTS Orientation.isCCW). Self-touching rings can fool it,
        /// so a ring is clockwise only when the reversed copy is CCW; otherwise signed area breaks the tie.
        /// </summary>
        private static RingFacing Classify(Coord[] closed)
        {
            if (closed.Length - 1 < 3)
            {
                return RingFacing.Indeterminate;
            }

            if (IsCounterClockwise(closed))
            {
                return RingFacing.CounterClockwise;
            }

            if (IsCounterClockwise(ReverseClosed(closed)))
            {
                return RingFacing.Clockwise;
            }

            double area = SignedArea(closed);
            if (area > 1e-12)
            {
                return RingFacing.CounterClockwise;
            }

            if (area < -1e-12)
            {
                return RingFacing.Clockwise;
            }

            return RingFacing.Indeterminate;
        }

        /// <summary>
        /// JTS Orientation.isCCW 的移植：在环的最高点（先上升后下降）看局部转向；平顶看顶边走向。
        /// Port of JTS Orientation.isCCW: local turn at the highest point reached by a rising segment.
        /// A flat cap uses the direction of the top edge. Too few points or a flat ring returns false.
        /// </summary>
        private static bool IsCounterClockwise(Coord[] ring)
        {
            int nPts = ring.Length - 1;
            if (nPts < 3)
            {
                return false;
            }

            Coord upHi = ring[0];
            double prevY = upHi.Y;
            Coord upLow = default;
            bool hasUpLow = false;
            int iUpHi = 0;
            for (int i = 1; i <= nPts; i++)
            {
                double py = ring[i].Y;
                if (py > prevY && py >= upHi.Y)
                {
                    upHi = ring[i];
                    iUpHi = i;
                    upLow = ring[i - 1];
                    hasUpLow = true;
                }

                prevY = py;
            }

            if (iUpHi == 0 || !hasUpLow)
            {
                return false;
            }

            int iDownLow = iUpHi;
            do
            {
                iDownLow = (iDownLow + 1) % nPts;
            }
            while (iDownLow != iUpHi && ring[iDownLow].Y == upHi.Y);

            Coord downLow = ring[iDownLow];
            int iDownHi = iDownLow > 0 ? iDownLow - 1 : nPts - 1;
            Coord downHi = ring[iDownHi];
            if (SamePoint(upHi, downHi))
            {
                if (SamePoint(upLow, upHi) || SamePoint(downLow, upHi) || SamePoint(upLow, downLow))
                {
                    return false;
                }

                return OrientationIndex(upLow, upHi, downLow) > 0;
            }

            // 平顶：顶边向西（downHi 在 upHi 左侧）才是逆时针 / Flat cap is CCW when the top edge runs west
            return downHi.X - upHi.X < 0;
        }

        /// <summary>三点转向：逆时针为正 / Triangle orientation: positive is counter-clockwise</summary>
        private static int OrientationIndex(Coord p1, Coord p2, Coord q)
        {
            double det = ((p2.X - p1.X) * (q.Y - p1.Y)) - ((p2.Y - p1.Y) * (q.X - p1.X));
            if (det > 0)
            {
                return 1;
            }

            if (det < 0)
            {
                return -1;
            }

            return 0;
        }

        /// <summary>标准鞋带面积，正值为逆时针 / Shoelace area; positive is counter-clockwise</summary>
        private static double SignedArea(Coord[] closed)
        {
            double sum = 0;
            for (int i = 0; i < closed.Length - 1; i++)
            {
                sum += (closed[i].X * closed[i + 1].Y) - (closed[i + 1].X * closed[i].Y);
            }

            return sum / 2.0;
        }

        private static Coord[] ReverseClosed(Coord[] closed)
        {
            Coord[] rev = new Coord[closed.Length];
            for (int i = 0; i < closed.Length; i++)
            {
                rev[i] = closed[closed.Length - 1 - i];
            }

            return rev;
        }

        /// <summary>
        /// 反转环但保留起点，使同一几何的两次反转回到原序。未闭合的环顺手闭合（GBIF 要求首尾相同）。
        /// Reverse a ring while keeping its start vertex, so a second reverse restores the original order.
        /// An open ring is closed, which GBIF requires.
        /// </summary>
        private static List<Coord> ReverseKeepingStart(List<Coord> ring)
        {
            Coord[] closed = Close(ring);
            int lastBody = closed.Length - 2;
            List<Coord> result = new List<Coord>(closed.Length);
            result.Add(closed[0]);
            for (int i = lastBody; i >= 1; i--)
            {
                result.Add(closed[i]);
            }

            result.Add(closed[0]);
            return result;
        }

        private static Coord[] Close(List<Coord> ring)
        {
            if (ring.Count == 0)
            {
                return Array.Empty<Coord>();
            }

            if (ring.Count >= 2 && SamePoint(ring[0], ring[ring.Count - 1]))
            {
                Coord[] copy = new Coord[ring.Count];
                for (int i = 0; i < ring.Count; i++)
                {
                    copy[i] = ring[i];
                }

                return copy;
            }

            Coord[] closed = new Coord[ring.Count + 1];
            for (int i = 0; i < ring.Count; i++)
            {
                closed[i] = ring[i];
            }

            closed[closed.Length - 1] = ring[0];
            return closed;
        }

        private static bool SamePoint(Coord a, Coord b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        private static string Emit(ParsedWkt parsed)
        {
            StringBuilder sb = new StringBuilder();
            if (parsed.Prefix.Length > 0)
            {
                sb.Append(parsed.Prefix);
            }

            sb.Append(parsed.Multi ? "MULTIPOLYGON(" : "POLYGON(");
            for (int p = 0; p < parsed.Polygons.Count; p++)
            {
                if (p > 0)
                {
                    sb.Append(',');
                }

                if (parsed.Multi)
                {
                    sb.Append('(');
                }

                List<List<Coord>> rings = parsed.Polygons[p];
                for (int r = 0; r < rings.Count; r++)
                {
                    if (r > 0)
                    {
                        sb.Append(',');
                    }

                    AppendRing(sb, rings[r]);
                }

                if (parsed.Multi)
                {
                    sb.Append(')');
                }
            }

            sb.Append(')');
            return sb.ToString();
        }

        private static void AppendRing(StringBuilder sb, List<Coord> ring)
        {
            sb.Append('(');
            for (int i = 0; i < ring.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(ring[i].Text);
            }

            sb.Append(')');
        }

        private enum RingFacing
        {
            CounterClockwise,
            Clockwise,
            Indeterminate
        }

        private struct Coord
        {
            public string Text;
            public double X;
            public double Y;
        }

        private sealed class ParsedWkt
        {
            public string Prefix = string.Empty;
            public bool Multi;
            public bool Empty;
            public List<List<List<Coord>>> Polygons = new List<List<List<Coord>>>();
        }

        /// <summary>
        /// 只认本库会写出的 WKT：可选 SRID 前缀、POLYGON / MULTIPOLYGON、环内空格分隔的经纬度。
        /// 多出来的 Z/M 保留在原文里，不参与转向判断。
        /// Accepts the WKT this project stores: optional SRID prefix, POLYGON / MULTIPOLYGON,
        /// longitude latitude pairs. Extra Z/M ordinates are kept in the text and ignored for orientation.
        /// </summary>
        private sealed class WktReader
        {
            private readonly string _text;
            private int _index;

            private WktReader(string text)
            {
                _text = text;
            }

            public static bool TryParse(string text, out ParsedWkt? parsed)
            {
                parsed = null;
                WktReader reader = new WktReader(text);
                int keywordAt = reader.FindGeometryKeyword(out bool multi);
                if (keywordAt < 0)
                {
                    return false;
                }

                string prefix = text.Substring(0, keywordAt).Trim();
                if (!IsAllowedPrefix(prefix))
                {
                    return false;
                }

                reader._index = keywordAt;
                if (!reader.MatchKeyword(multi ? "MULTIPOLYGON" : "POLYGON"))
                {
                    return false;
                }

                ParsedWkt result = new ParsedWkt
                {
                    Prefix = prefix,
                    Multi = multi
                };
                if (reader.MatchKeyword("EMPTY"))
                {
                    reader.SkipWs();
                    if (reader._index != text.Length)
                    {
                        return false;
                    }

                    result.Empty = true;
                    parsed = result;
                    return true;
                }

                if (multi)
                {
                    if (!reader.TryConsume('('))
                    {
                        return false;
                    }

                    do
                    {
                        if (!reader.TryReadPolygon(out List<List<Coord>>? rings) || rings == null)
                        {
                            return false;
                        }

                        result.Polygons.Add(rings);
                    }
                    while (reader.TryConsume(','));

                    if (!reader.TryConsume(')'))
                    {
                        return false;
                    }
                }
                else if (!reader.TryReadPolygon(out List<List<Coord>>? single) || single == null)
                {
                    return false;
                }
                else
                {
                    result.Polygons.Add(single);
                }

                reader.SkipWs();
                if (reader._index != text.Length)
                {
                    return false;
                }

                parsed = result;
                return true;
            }

            private static bool IsAllowedPrefix(string prefix)
            {
                if (prefix.Length == 0)
                {
                    return true;
                }

                int i = 0;
                if (prefix.Length < 6 || !prefix.AsSpan(0, 4).Equals("SRID", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                i = 4;
                while (i < prefix.Length && char.IsWhiteSpace(prefix[i]))
                {
                    i++;
                }

                if (i >= prefix.Length || prefix[i] != '=')
                {
                    return false;
                }

                i++;
                while (i < prefix.Length && char.IsWhiteSpace(prefix[i]))
                {
                    i++;
                }

                int digits = 0;
                while (i < prefix.Length && char.IsDigit(prefix[i]))
                {
                    i++;
                    digits++;
                }

                if (digits == 0)
                {
                    return false;
                }

                while (i < prefix.Length && char.IsWhiteSpace(prefix[i]))
                {
                    i++;
                }

                return i == prefix.Length - 1 && prefix[i] == ';';
            }

            private int FindGeometryKeyword(out bool multi)
            {
                int multiAt = IndexOfKeyword("MULTIPOLYGON");
                if (multiAt >= 0)
                {
                    multi = true;
                    return multiAt;
                }

                int polyAt = IndexOfKeyword("POLYGON");
                multi = false;
                return polyAt;
            }

            private int IndexOfKeyword(string keyword)
            {
                for (int i = 0; i + keyword.Length <= _text.Length; i++)
                {
                    if (i > 0 && char.IsLetterOrDigit(_text[i - 1]))
                    {
                        continue;
                    }

                    if (string.Compare(_text, i, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        continue;
                    }

                    int after = i + keyword.Length;
                    if (after < _text.Length && char.IsLetterOrDigit(_text[after]))
                    {
                        continue;
                    }

                    return i;
                }

                return -1;
            }

            private bool TryReadPolygon(out List<List<Coord>>? rings)
            {
                rings = null;
                if (!TryConsume('('))
                {
                    return false;
                }

                List<List<Coord>> parsed = new List<List<Coord>>();
                do
                {
                    if (!TryReadRing(out List<Coord>? ring) || ring == null)
                    {
                        return false;
                    }

                    parsed.Add(ring);
                }
                while (TryConsume(','));

                if (!TryConsume(')'))
                {
                    return false;
                }

                rings = parsed;
                return true;
            }

            private bool TryReadRing(out List<Coord>? ring)
            {
                ring = null;
                if (!TryConsume('('))
                {
                    return false;
                }

                List<Coord> coords = new List<Coord>();
                do
                {
                    if (!TryReadCoord(out Coord coord))
                    {
                        return false;
                    }

                    coords.Add(coord);
                }
                while (TryConsume(','));

                if (!TryConsume(')'))
                {
                    return false;
                }

                ring = coords;
                return true;
            }

            private bool TryReadCoord(out Coord coord)
            {
                coord = default;
                if (!TryReadNumber(out double x, out int start, out int end))
                {
                    return false;
                }

                if (!TryReadNumber(out double y, out _, out end))
                {
                    return false;
                }

                while (TryReadNumber(out _, out _, out end))
                {
                    // Z / M：留在原文，转向只用前两维 / Keep Z/M in the text; orientation uses x, y only
                }

                coord = new Coord
                {
                    Text = CollapseWhitespace(_text.Substring(start, end - start)),
                    X = x,
                    Y = y
                };
                return true;
            }

            private bool TryReadNumber(out double value, out int start, out int end)
            {
                value = 0;
                SkipWs();
                start = _index;
                end = _index;
                int i = _index;
                if (i < _text.Length && (_text[i] == '+' || _text[i] == '-'))
                {
                    i++;
                }

                int digits = 0;
                while (i < _text.Length && char.IsDigit(_text[i]))
                {
                    i++;
                    digits++;
                }

                if (i < _text.Length && _text[i] == '.')
                {
                    i++;
                    while (i < _text.Length && char.IsDigit(_text[i]))
                    {
                        i++;
                        digits++;
                    }
                }

                if (digits == 0)
                {
                    return false;
                }

                if (i < _text.Length && (_text[i] == 'e' || _text[i] == 'E'))
                {
                    int exponent = i + 1;
                    if (exponent < _text.Length && (_text[exponent] == '+' || _text[exponent] == '-'))
                    {
                        exponent++;
                    }

                    int exponentDigits = 0;
                    while (exponent < _text.Length && char.IsDigit(_text[exponent]))
                    {
                        exponent++;
                        exponentDigits++;
                    }

                    if (exponentDigits == 0)
                    {
                        return false;
                    }

                    i = exponent;
                }

                string token = _text.Substring(start, i - start);
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    return false;
                }

                end = i;
                _index = i;
                return true;
            }

            private bool MatchKeyword(string keyword)
            {
                SkipWs();
                if (_index + keyword.Length > _text.Length)
                {
                    return false;
                }

                if (string.Compare(_text, _index, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    return false;
                }

                int after = _index + keyword.Length;
                if (after < _text.Length && char.IsLetterOrDigit(_text[after]))
                {
                    return false;
                }

                _index = after;
                return true;
            }

            private bool TryConsume(char expected)
            {
                SkipWs();
                if (_index < _text.Length && _text[_index] == expected)
                {
                    _index++;
                    return true;
                }

                return false;
            }

            private void SkipWs()
            {
                while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
                {
                    _index++;
                }
            }

            private static string CollapseWhitespace(string raw)
            {
                StringBuilder sb = new StringBuilder(raw.Length);
                bool pendingSpace = false;
                for (int i = 0; i < raw.Length; i++)
                {
                    if (char.IsWhiteSpace(raw[i]))
                    {
                        pendingSpace = sb.Length > 0;
                        continue;
                    }

                    if (pendingSpace)
                    {
                        sb.Append(' ');
                        pendingSpace = false;
                    }

                    sb.Append(raw[i]);
                }

                return sb.ToString();
            }
        }
    }
}
