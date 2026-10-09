/** 时间滑块组件测试：跨年区间、播放步进、旬档切换（设计文档 §5.4 / §9） */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { VueQueryPlugin } from '@tanstack/vue-query'
import { testI18n } from '@/__tests__/setup'
import TimeSlider from '../TimeSlider.vue'
import { useFilterStore } from '@/stores/filter'

function mountSlider() {
  setActivePinia(createPinia())
  return mount(TimeSlider, {
    global: { plugins: [testI18n(), createPinia(), VueQueryPlugin] },
    attachTo: document.body
  })
}

describe('TimeSlider', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline')))
  })
  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
  })

  it('跨年区间写入 filter（11~2）', () => {
    const wrapper = mountSlider()
    const filter = useFilterStore()
    filter.setMonthRange(11, 2)
    expect(filter.monthRange).toEqual([11, 2])
    expect(filter.query.monthFrom).toBe(11)
    expect(filter.query.monthTo).toBe(2)
    wrapper.unmount()
  })

  it('播放逐月推进（1 月起）', async () => {
    const wrapper = mountSlider()
    const filter = useFilterStore()
    const play = wrapper.findAll('button').find((b) => b.attributes('aria-label')?.includes('城市苏醒'))!
    await play.trigger('click')
    expect(filter.monthRange).toEqual([1, 1])
    vi.advanceTimersByTime(1800)
    expect(filter.monthRange).toEqual([2, 2])
    vi.advanceTimersByTime(1800 * 10)
    expect(filter.monthRange).toEqual([12, 12])
    vi.advanceTimersByTime(1800)
    // 循环回 1 月
    expect(filter.monthRange).toEqual([1, 1])
    wrapper.unmount()
  })

  it('切换旬档后播放按旬推进', async () => {
    const wrapper = mountSlider()
    const filter = useFilterStore()
    const tenDayBtn = wrapper.findAll('button').find((b) => b.text() === '旬')!
    await tenDayBtn.trigger('click')
    expect(filter.unit).toBe('tenDay')
    expect(filter.tenDayRange).toEqual([1, 36])
    const play = wrapper.findAll('button').find((b) => b.attributes('aria-label')?.includes('城市苏醒'))!
    await play.trigger('click')
    expect(filter.tenDayRange).toEqual([1, 1])
    // 旬模式下 query 产出 d1/d2 日期窗，且不产月份区间
    expect(filter.query.d1).toBeTypeOf('string')
    expect(filter.query.monthFrom).toBeUndefined()
    vi.advanceTimersByTime(1800)
    expect(filter.tenDayRange).toEqual([2, 2])
    wrapper.unmount()
  })

  it('播放中显示季节剧本字幕', async () => {
    const wrapper = mountSlider()
    const play = wrapper.findAll('button').find((b) => b.attributes('aria-label')?.includes('城市苏醒'))!
    await play.trigger('click')
    await vi.advanceTimersByTimeAsync(0)
    expect(wrapper.text()).toContain('冬鸟在结冰的湖面聚群')
    wrapper.unmount()
  })
})
