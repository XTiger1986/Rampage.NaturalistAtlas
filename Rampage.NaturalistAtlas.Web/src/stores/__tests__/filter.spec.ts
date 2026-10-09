/** stores/filter 单元测试：跨年区间、旬换算、previewQuery 键一致性（设计文档 §9 任务15 补强） */
import { describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useFilterStore } from '../filter'

describe('filter store', () => {
  it('默认口径：research + needs_id，无类群，全年', () => {
    setActivePinia(createPinia())
    const filter = useFilterStore()
    expect(filter.quality).toEqual([1, 2])
    expect(filter.iconicTaxa).toEqual([])
    expect(filter.isFullYear).toBe(true)
    expect(filter.hasFilter).toBe(false)
  })

  it('toggleTaxon 多选切换', () => {
    setActivePinia(createPinia())
    const filter = useFilterStore()
    filter.toggleTaxon(1)
    expect(filter.iconicTaxa).toEqual([1])
    filter.toggleTaxon(6)
    expect(filter.iconicTaxa).toEqual([1, 6])
    filter.toggleTaxon(1)
    expect(filter.iconicTaxa).toEqual([6])
  })

  it('soloTaxon 只看该类群；再触发恢复全部', () => {
    setActivePinia(createPinia())
    const filter = useFilterStore()
    filter.toggleTaxon(1)
    filter.soloTaxon(9)
    expect(filter.iconicTaxa).toEqual([9])
    filter.soloTaxon(9)
    expect(filter.iconicTaxa).toEqual([])
  })

  it('跨年月份区间（11~2）产出 monthFrom>monthTo', () => {
    setActivePinia(createPinia())
    const filter = useFilterStore()
    filter.monthRange = [11, 2]
    const q = filter.query as { monthFrom?: number; monthTo?: number }
    expect(q.monthFrom).toBe(11)
    expect(q.monthTo).toBe(2)
    expect(filter.hasFilter).toBe(true)
  })

  it('旬模式产出 d1/d2 日期窗且不产月份参数', () => {
    setActivePinia(createPinia())
    const filter = useFilterStore()
    filter.unit = 'tenDay'
    filter.tenDayRange = [4, 9]
    const q = filter.query as Record<string, unknown>
    expect(typeof q.d1).toBe('string')
    expect(typeof q.d2).toBe('string')
    expect(q.monthFrom).toBeUndefined()
    expect(q.monthTo).toBeUndefined()
  })

  it('previewQuery 与 query 键结构一致（播放预取命中同一缓存）', () => {
    setActivePinia(createPinia())
    const filter = useFilterStore()
    filter.toggleTaxon(2)
    filter.monthRange = [3, 5]
    const a = JSON.stringify(filter.query)
    const b = JSON.stringify(filter.previewQuery('month', 3, 5))
    expect(a).toBe(b)
  })

  it('reset 复位到默认口径', () => {
    setActivePinia(createPinia())
    const filter = useFilterStore()
    filter.toggleTaxon(2)
    filter.quality = [1]
    filter.monthRange = [3, 5]
    filter.unit = 'tenDay'
    filter.reset()
    expect(filter.iconicTaxa).toEqual([])
    expect(filter.quality).toEqual([1, 2])
    expect(filter.unit).toBe('month')
    expect(filter.monthRange).toEqual([1, 12])
  })
})
