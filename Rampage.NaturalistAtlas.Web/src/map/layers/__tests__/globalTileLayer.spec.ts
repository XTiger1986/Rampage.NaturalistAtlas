/** GlobalTileLayer 月份展开测试（跨年区间，设计文档 §9） */
import { describe, expect, it, vi } from 'vitest'

// 只测纯函数：屏蔽地图引擎与视图模式模块（它们引入完整 maplibre-gl）
vi.mock('maplibre-gl', () => ({}))
vi.mock('../../viewModes', () => ({ firstSymbolLayerId: () => '' }))

import { expandMonths } from '../GlobalTileLayer'

describe('expandMonths', () => {
  it('跨年区间 11~2 → 11,12,1,2', () => {
    expect(expandMonths(11, 2)).toEqual([11, 12, 1, 2])
  })
  it('普通区间 3~5', () => {
    expect(expandMonths(3, 5)).toEqual([3, 4, 5])
  })
  it('全年 1~12', () => {
    expect(expandMonths(1, 12)).toEqual([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12])
  })
  it('单月', () => {
    expect(expandMonths(7, 7)).toEqual([7])
  })
  it('缺失端点返回空（不过滤）', () => {
    expect(expandMonths(undefined, 3)).toEqual([])
    expect(expandMonths(3, undefined)).toEqual([])
  })
})
