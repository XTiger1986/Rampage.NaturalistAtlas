import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import type { ObservationQuery } from '@/api/types'
import { useCityStore } from './city'
import { tenDayBounds } from '@/utils/emergence'

/** 时间粒度：month = 月份区间（默认），tenDay = 旬区间（当年 1~36） / Time granularity: month = month range (default), tenDay = ten-day range (1~36 of the current year) */
export type TimeUnit = 'month' | 'tenDay'

/**
 * 组合筛选状态（类群 × 月份/旬 × 鉴定状态） / Combined filter state (iconic taxon × month/ten-day × quality grade)
 *
 * - 地图、探索列表、物种榜、物候矩阵共用同一份筛选，切换页面筛选不丢失；
 *   / Map, explore list, species leaderboard and phenology matrix share one filter set, so switching pages keeps it;
 * - 与 URL 查询参数同步（t=类群, m=月份区间, q=鉴定级别, u=旬档, d=旬区间），便于分享与投屏（见 useUrlSync.ts）；
 *   / Synced with URL query params (t=taxa, m=month range, q=quality, u=ten-day mode, d=ten-day range) for sharing and casting (see useUrlSync.ts);
 * - 月份与旬互斥产出：BuildWhere 会把月份区间与 d1/d2 日期窗 AND 叠加，
 *   旬模式下只产 d1/d2、不产 monthFrom/To，反之亦然（设计文档 §5.4）；
 *   / Month and ten-day outputs are mutually exclusive: BuildWhere ANDs the month range with the d1/d2 date window;
 *   ten-day mode emits only d1/d2 without monthFrom/To, and vice versa (design doc §5.4);
 * - 产品验收：切换后地图与列表 1 秒内响应——点位接口按"城市 + 筛选"缓存，切换回已看过的组合即时命中。
 *   / Acceptance: map and list respond within 1s after switching — the points endpoint is cached per city + filter,
 *   so revisited combos hit instantly.
 */
export const useFilterStore = defineStore('filter', () => {
  /** 选中的类群代码（空数组 = 全部类群） / Selected iconic taxon codes (empty array = all taxa) */
  const iconicTaxa = ref<number[]>([])
  /** 时间粒度 / Time granularity */
  const unit = ref<TimeUnit>('month')
  /** 月份区间 [起, 止]（1~12；允许跨年如 [11, 2]；[1, 12] = 全年） / Month range [from, to] (1~12; year wrap allowed, e.g. [11, 2]; [1, 12] = full year) */
  const monthRange = ref<[number, number]>([1, 12])
  /** 旬区间 [起, 止]（当年 1~36；[1, 36] = 全年；不支持跨年） / Ten-day range [from, to] (1~36 of the current year; [1, 36] = full year; no year wrap) */
  const tenDayRange = ref<[number, number]>([1, 36])
  /** 鉴定级别（默认 research + needs_id，产品文档 §4.3 默认口径） / Quality grades (default research + needs_id per product doc §4.3) */
  const quality = ref<number[]>([1, 2])

  /** 是否为全年（未做月份筛选） / Whether full year (no month filtering) */
  const isFullYear = computed(() => monthRange.value[0] === 1 && monthRange.value[1] === 12)
  /** 是否为全年（旬口径） / Whether full year (ten-day scope) */
  const isFullTenDay = computed(() => tenDayRange.value[0] === 1 && tenDayRange.value[1] === 36)

  /** 是否存在非默认筛选 / Whether any non-default filter is active */
  const hasFilter = computed(
    () =>
      iconicTaxa.value.length > 0 ||
      (unit.value === 'month' ? !isFullYear.value : !isFullTenDay.value) ||
      quality.value.length !== 2
  )

  /** 旬序 → 起止日期窗（当年） / Ten-day indices → start/end date window (current year) */
  function tenDayWindow(from: number, to: number): { d1: string; d2: string } | Record<string, never> {
    if (from === 1 && to === 36) {
      return {}
    }
    const year = new Date().getFullYear()
    return { d1: tenDayBounds(year, from)[0], d2: tenDayBounds(year, to)[1] }
  }

  /** 月份区间片段（全年返回空对象） / Month range fragment (empty object for full year) */
  function monthWindow(from: number, to: number): Partial<ObservationQuery> {
    return from === 1 && to === 12 ? {} : { monthFrom: from, monthTo: to }
  }

  /**
   * 转换为接口查询参数（带当前城市） / Convert to API query params (with the current city)
   * 注意：返回新对象，vue-query 以其序列化结果作为缓存键
   * / Note: returns a fresh object; vue-query uses its serialized form as the cache key
   */
  const city = useCityStore()
  const query = computed<ObservationQuery>(() => {
    return {
      cityId: city.currentId,
      iconic: [...iconicTaxa.value].sort((a, b) => a - b),
      ...(unit.value === 'tenDay'
        ? tenDayWindow(tenDayRange.value[0], tenDayRange.value[1])
        : monthWindow(monthRange.value[0], monthRange.value[1])),
      quality: [...quality.value].sort()
    }
  })

  /**
   * 预览某粒度/某区间的查询对象（时间滑块播放预取下一档用；键结构与 query 完全一致）
   * / Preview the query object for a granularity/range (used to prefetch the next step during time-slider playback; key structure identical to query)
   */
  function previewQuery(u: TimeUnit, from: number, to: number): ObservationQuery {
    return {
      cityId: city.currentId,
      iconic: [...iconicTaxa.value].sort((a, b) => a - b),
      ...(u === 'tenDay' ? tenDayWindow(from, to) : monthWindow(from, to)),
      quality: [...quality.value].sort()
    }
  }

  /** 切换单个类群（图例点击） / Toggle one iconic taxon (legend click) */
  function toggleTaxon(code: number): void {
    const i = iconicTaxa.value.indexOf(code)
    if (i >= 0) {
      iconicTaxa.value.splice(i, 1)
    } else {
      iconicTaxa.value.push(code)
    }
  }

  /** 只看某一类群（图例双击 / 长按） / Solo one iconic taxon (legend double-click / long press) */
  function soloTaxon(code: number): void {
    iconicTaxa.value = iconicTaxa.value.length === 1 && iconicTaxa.value[0] === code ? [] : [code]
  }

  /** 设置月份区间 / Set the month range */
  function setMonthRange(from: number, to: number): void {
    monthRange.value = [from, to]
  }

  /** 设置旬区间 / Set the ten-day range */
  function setTenDayRange(from: number, to: number): void {
    tenDayRange.value = [Math.min(from, to), Math.max(from, to)]
  }

  /** 切换时间粒度（月份 ⇄ 旬），把当前区间按月 ⇄ 旬近似换算，避免切换后筛选突变 / Switch time granularity (month ⇄ ten-day), roughly converting the current range so the filter does not jump */
  function setUnit(u: TimeUnit): void {
    if (u === unit.value) {
      return
    }
    unit.value = u
    if (u === 'tenDay') {
      const [f, t] = monthRange.value
      setTenDayRange(f <= t ? (f - 1) * 3 + 1 : 1, f <= t ? t * 3 : 36)
    } else {
      const [f, t] = tenDayRange.value
      setMonthRange(Math.floor((f - 1) / 3) + 1, Math.floor((t - 1) / 3) + 1)
    }
  }

  /** 恢复默认筛选 / Reset to default filters */
  function reset(): void {
    iconicTaxa.value = []
    unit.value = 'month'
    monthRange.value = [1, 12]
    tenDayRange.value = [1, 36]
    quality.value = [1, 2]
  }

  return {
    iconicTaxa, unit, monthRange, tenDayRange, quality, isFullYear, isFullTenDay, hasFilter, query,
    previewQuery, toggleTaxon, soloTaxon, setMonthRange, setTenDayRange, setUnit, reset
  }
})
