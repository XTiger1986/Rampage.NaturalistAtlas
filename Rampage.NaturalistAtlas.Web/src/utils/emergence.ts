/**
 * 物候纯函数：最佳观察时间窗口、旬日期窗（设计文档 §5.6 / §5.4）
 * / Phenology pure functions: best-observation window and ten-day date windows (design doc §5.6 / §5.4)
 *
 * 全部为无副作用纯函数，便于单元测试（任务 15 覆盖）。
 * / All side-effect-free pure functions, easy to unit test (covered by task 15).
 */
import type { EmergencePoint } from '@/api/types'

/** 最佳观察时间样本量下限（低于该值不生成文案） / Minimum sample size for the best-observation window (below it no copy is generated) */
export const BEST_WINDOW_MIN_SAMPLE = 30

/** 连续区间边界单元的占比门槛（该单元记录数 / 总记录数） / Share threshold for boundary units of the window (unit records / total records) */
const EDGE_SHARE = 0.1

/** 最佳观察时间窗口 / Best-observation time window */
export interface BestWindow {
  /** 起始序号（含；旬 1~36 / 月 1~12，允许跨年环绕如 11 月~2 月） / Start index (inclusive; 1~36 ten-day / 1~12 month, year wrap allowed, e.g. Nov~Feb) */
  fromIdx: number
  /** 结束序号（含） / End index (inclusive) */
  toIdx: number
  /** 窗口内记录占比（0~1） / Record share inside the window (0~1) */
  share: number
}

/**
 * 由物候曲线计算"最佳观察时间"：从峰值单元出发，向两侧扩展出
 * 每个边界单元占比 ≥ 10% 的连续区间（序号环状环绕，支持 11 月~次年 2 月）。
 * 样本量不足时返回 null（页面显示口径说明即可，不生成误导性文案）。
 * / Derive the "best observation time" from the emergence curve: starting at the peak unit, expand both ways
 * into a contiguous range whose boundary units each hold ≥ 10% (indices wrap around, supporting Nov~Feb).
 * Returns null when the sample is too small (the page just shows the caveat, no misleading copy).
 */
export function bestWindow(series: EmergencePoint[]): BestWindow | null {
  const points = series.filter((p) => p.recordCount > 0)
  const total = series.reduce((sum, p) => sum + p.recordCount, 0)
  if (total < BEST_WINDOW_MIN_SAMPLE || points.length === 0) {
    return null
  }
  const size = series.length // 旬 36 / 月 12 / 36 for ten-day, 12 for month
  const count = new Map<number, number>(points.map((p) => [p.index, p.recordCount]))
  const peak = points.reduce((a, b) => (b.recordCount > a.recordCount ? b : a))
  const at = (i: number): number => count.get(((i - 1 + size * 2) % size) + 1) ?? 0

  // 从峰值向两侧扩展：边界单元占比 ≥ 10% 才纳入 / Expand from the peak both ways: a boundary unit joins only at ≥ 10% share
  let from = peak.index
  let to = peak.index
  let windowSum = peak.recordCount
  let extended = true
  while (extended) {
    extended = false
    const prev = ((from - 2 + size * 2) % size) + 1
    const next = (to % size) + 1
    if (prev !== to && at(prev) / total >= EDGE_SHARE) {
      from = prev
      windowSum += at(prev)
      extended = true
    }
    if (next !== from && at(next) / total >= EDGE_SHARE) {
      to = next
      windowSum += at(next)
      extended = true
    }
    // 全环纳入即停 / Stop once the whole ring is included
    if (from === ((to % size) + 1)) {
      break
    }
  }
  return { fromIdx: from, toIdx: to, share: windowSum / total }
}

/**
 * 旬序（1~36）的日期窗（当年）：旬起 1/11/21 日，旬末 10/20/月末（闰年 2 月按当年天数）
 * / Date window of a ten-day index (1~36, current year): starts on day 1/11/21, ends on day 10/20/month-end (February honors leap years)
 * 返回 [d1, d2]（YYYY-MM-DD，含两端） / Returns [d1, d2] (YYYY-MM-DD, both inclusive)
 */
export function tenDayBounds(year: number, idx: number): [string, string] {
  const month = Math.floor((idx - 1) / 3) + 1
  const period = (idx - 1) % 3 // 0/1/2 → 上/中/下旬 / 0/1/2 → early/mid/late part of the month
  const startDay = period * 10 + 1
  const lastDay = new Date(year, month, 0).getDate()
  const endDay = period === 2 ? lastDay : Math.min(period * 10 + 10, lastDay)
  const two = (n: number): string => String(n).padStart(2, '0')
  const d1 = `${year}-${two(month)}-${two(startDay)}`
  const d2 = `${year}-${two(month)}-${two(endDay)}`
  return [d1, d2]
}

/** 旬序标签：如 "4月中旬"（i18n 文案由调用方拼接，这里只给序号语义） / Ten-day index semantics, e.g. "mid April" (the caller assembles i18n copy; only the index meaning lives here) */
export function tenDayMonth(idx: number): { month: number; period: 0 | 1 | 2 } {
  return { month: Math.floor((idx - 1) / 3) + 1, period: (((idx - 1) % 3) as 0 | 1 | 2) }
}
