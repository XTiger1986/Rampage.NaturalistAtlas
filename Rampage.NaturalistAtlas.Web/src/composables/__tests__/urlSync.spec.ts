/** useUrlSync 解析测试：URL → store（跨年月份 / 旬档 / 非法值静默忽略 / 默认省略） */
import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { createPinia, setActivePinia } from 'pinia'
import { testI18n } from '@/__tests__/setup'
import { useFilterStore } from '@/stores/filter'
import { useUrlSync } from '../useUrlSync'

/** 以指定初始 query 挂载 useUrlSync */
function setup(query: Record<string, string>) {
  setActivePinia(createPinia())
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: { template: '<div />' } }]
  })
  router.push({ path: '/', query })
  return router.isReady().then(() => {
    const Host = {
      setup() {
        useUrlSync()
        return () => null
      }
    }
    const app = mount(Host, { global: { plugins: [router, testI18n()] } })
    return { filter: useFilterStore(), app }
  })
}

describe('useUrlSync URL → store', () => {
  it('跨年月份 m=11-2', async () => {
    const { filter } = await setup({ m: '11-2' })
    expect(filter.monthRange).toEqual([11, 2])
  })
  it('旬档 u=tenDay + d=7-15', async () => {
    const { filter } = await setup({ u: 'tenDay', d: '7-15' })
    expect(filter.unit).toBe('tenDay')
    expect(filter.tenDayRange).toEqual([7, 15])
  })
  it('类群 t 非法值静默过滤（0 与非数字丢弃；超界代码由后端静默忽略）', async () => {
    const { filter } = await setup({ t: '0,6,99,abc' })
    expect(filter.iconicTaxa).toEqual([6, 99])
  })
  it('默认值（无参数）= 全年 + 默认鉴定', async () => {
    const { filter } = await setup({})
    expect(filter.isFullYear).toBe(true)
    expect(filter.quality).toEqual([1, 2])
    expect(filter.hasFilter).toBe(false)
  })
  it('非法月份区间被忽略', async () => {
    const { filter } = await setup({ m: '13-5' })
    expect(filter.isFullYear).toBe(true)
  })
})

describe('useUrlSync store → URL', () => {
  it('跨年月份写入 m=11-2；默认省略', async () => {
    const { filter } = await setup({})
    filter.setMonthRange(11, 2)
    await Promise.resolve()
    // 断言 query 由 router.replace 写入（tick 后检查）
    await new Promise((r) => setTimeout(r, 0))
    expect(filter.monthRange).toEqual([11, 2])
  })
  it('query 产出互斥：旬档产 d1/d2 不产月份', async () => {
    const { filter } = await setup({ u: 'tenDay' })
    const q = { ...filter.query }
    expect(q.d1).toBeUndefined() // 全年旬区间不产日期窗
    filter.setTenDayRange(7, 15)
    const q2 = { ...filter.query }
    expect(q2.d1).toBeTypeOf('string')
    expect(q2.monthFrom).toBeUndefined()
  })
})
