/** utils/emergence 纯函数单元测试（旬日期窗闰年敏感 + 最佳观察窗口环绕） */
import { describe, expect, it } from 'vitest'
import { bestWindow, tenDayBounds, tenDayMonth } from '../emergence'
import type { EmergencePoint } from '@/api/types'

function series(counts: number[]): EmergencePoint[] {
  return counts.map((c, i) => ({ index: i + 1, recordCount: c, researchCount: c }))
}

describe('tenDayBounds', () => {
  it('上旬起于 1 日', () => {
    expect(tenDayBounds(2026, 1)).toEqual(['2026-01-01', '2026-01-10'])
  })
  it('中旬止于 20 日', () => {
    expect(tenDayBounds(2026, 5)).toEqual(['2026-02-11', '2026-02-20'])
  })
  it('下旬止于月末（闰年 2 月 29）', () => {
    expect(tenDayBounds(2028, 6)).toEqual(['2028-02-21', '2028-02-29'])
  })
  it('平年 2 月下旬止于 28', () => {
    expect(tenDayBounds(2026, 6)).toEqual(['2026-02-21', '2026-02-28'])
  })
  it('大旬末（31 天月）', () => {
    expect(tenDayBounds(2026, 28)).toEqual(['2026-10-01', '2026-10-10'])
    expect(tenDayBounds(2026, 30)).toEqual(['2026-10-21', '2026-10-31'])
  })
  it('12 月下旬止于 31', () => {
    expect(tenDayBounds(2026, 36)).toEqual(['2026-12-21', '2026-12-31'])
  })
})

describe('tenDayMonth', () => {
  it('旬序 → (月, 旬)', () => {
    expect(tenDayMonth(1)).toEqual({ month: 1, period: 0 })
    expect(tenDayMonth(11)).toEqual({ month: 4, period: 1 })
    expect(tenDayMonth(36)).toEqual({ month: 12, period: 2 })
  })
})

describe('bestWindow', () => {
  it('样本量不足返回 null', () => {
    const small = series(Array.from({ length: 36 }, (_, i) => (i === 0 ? 29 : 0)))
    expect(bestWindow(small)).toBeNull()
  })
  it('单峰区间：从峰值扩展到边界占比 <10%', () => {
    // 旬 11/12（4 月中/下旬）各 50，其余为 0：峰值 4 月中旬，邻旬 50%≥10% 纳入；两侧 0% 截断
    const counts = Array.from({ length: 36 }, (_, i) => (i === 10 || i === 11 ? 50 : 0))
    const w = bestWindow(series(counts))!
    expect(w.fromIdx).toBe(11)
    expect(w.toIdx).toBe(12)
    expect(w.share).toBe(1)
  })
  it('跨年环绕：11 月~次年 2 月的月度曲线', () => {
    const counts = [30, 30, 0, 0, 0, 0, 0, 0, 0, 0, 30, 30] // 1,2,11,12 月
    const w = bestWindow(series(counts))!
    expect(w.fromIdx).toBe(11)
    expect(w.toIdx).toBe(2)
  })
  it('窗口占比 = 窗口内合计 / 总量（边界 <10% 截断）', () => {
    const counts = [45, 45, 0, 0, 0, 0, 0, 0, 0, 0, 5, 5]
    const w = bestWindow(series(counts))!
    // 峰值 1 月 45% 纳入、2 月 45% 纳入；12 月 5% <10% 截断 → 窗口 1~2 月共 90%
    expect(w.fromIdx).toBe(1)
    expect(w.toIdx).toBe(2)
    expect(w.share).toBeCloseTo(0.9)
  })
})
