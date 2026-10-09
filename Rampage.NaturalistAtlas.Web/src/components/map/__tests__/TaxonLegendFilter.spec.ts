/** 类群图例筛选器组件测试：单击多选 / 双击 solo / 长按 solo（触屏，设计文档 §5.2） */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mountWithI18n } from '@/__tests__/setup'
import TaxonLegendFilter from '../TaxonLegendFilter.vue'
import { useFilterStore } from '@/stores/filter'

/** 鸟类芯片（title 为本地化类群名） */
function birdChip(wrapper: ReturnType<typeof mountWithI18n>) {
  const el = wrapper.findAll('button').find((b) => b.attributes('title') === '鸟类')
  if (!el) {
    throw new Error('bird chip not found')
  }
  return el
}

describe('TaxonLegendFilter', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('单击切换多选', () => {
    const wrapper = mountWithI18n(TaxonLegendFilter)
    const filter = useFilterStore()
    birdChip(wrapper).trigger('click')
    expect(filter.iconicTaxa).toEqual([1])
    birdChip(wrapper).trigger('click')
    expect(filter.iconicTaxa).toEqual([])
  })

  it('双击只看该类群，再次双击恢复', () => {
    const wrapper = mountWithI18n(TaxonLegendFilter)
    const filter = useFilterStore()
    birdChip(wrapper).trigger('dblclick')
    expect(filter.iconicTaxa).toEqual([1])
    birdChip(wrapper).trigger('dblclick')
    expect(filter.iconicTaxa).toEqual([])
  })

  it('长按 500ms 触发 solo，且随后的 click 被吞掉', () => {
    const wrapper = mountWithI18n(TaxonLegendFilter)
    const filter = useFilterStore()
    const el = birdChip(wrapper)
    el.trigger('pointerdown', { pointerId: 1 })
    vi.advanceTimersByTime(550)
    el.trigger('pointerup', { pointerId: 1 })
    expect(filter.iconicTaxa).toEqual([1])
    // 长按后的 click 不应把 solo 切回
    el.trigger('click')
    expect(filter.iconicTaxa).toEqual([1])
  })

  it('未到 500ms 松手不触发 solo', () => {
    const wrapper = mountWithI18n(TaxonLegendFilter)
    const filter = useFilterStore()
    const el = birdChip(wrapper)
    el.trigger('pointerdown', { pointerId: 1 })
    vi.advanceTimersByTime(200)
    el.trigger('pointerup', { pointerId: 1 })
    expect(filter.iconicTaxa).toEqual([])
  })
})
