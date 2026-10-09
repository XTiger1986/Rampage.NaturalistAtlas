/**
 * URL 同步：城市 + 组合筛选 ⇄ 路由查询参数 / URL sync: city + combined filters ⇄ route query params
 *
 * 参数约定（短键，便于分享与投屏）：/ Param conventions (short keys for easy sharing and casting):
 * - city=beijing       当前城市 slug / current city slug
 * - t=1,6,9            类群代码 / iconic taxon codes
 * - u=tenDay           旬档（月份档省略）/ ten-day mode (omitted in month mode)
 * - m=3-5              月份区间（旬档/全年省略）/ month range (omitted in ten-day mode or full year)
 * - d=7-15             旬区间（月份档/全年省略；与 m 互斥）/ ten-day range (omitted in month mode or full year; mutually exclusive with m)
 * - q=1                鉴定级别（默认 1,2 省略）/ quality grades (default 1,2 omitted)
 *
 * 首次进入时 URL → store；之后 store → URL（router.replace，不产生历史记录）。
 * / On first entry URL → store; afterwards store → URL via router.replace (no history entries).
 */
import { watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useCityStore } from '@/stores/city'
import { useFilterStore } from '@/stores/filter'
import type { TimeUnit } from '@/stores/filter'

/** 解析逗号分隔的数字列表 / Parse a comma-separated number list */
function parseList(v: unknown): number[] {
  return typeof v === 'string' && v !== ''
    ? v.split(',').map(Number).filter((n) => Number.isInteger(n) && n > 0)
    : []
}

/** 解析 "a-b" 数字区间（各自 clamp 到 [min,max]） / Parse an "a-b" number range (each end clamped to [min,max]) */
function parseRange(v: unknown, min: number, max: number): [number, number] | null {
  if (typeof v !== 'string' || !/^\d{1,2}-\d{1,2}$/.test(v)) {
    return null
  }
  const [a, b] = v.split('-').map(Number) as [number, number]
  if (a < min || a > max || b < min || b > max) {
    return null
  }
  return [a, b]
}

export function useUrlSync(): void {
  const route = useRoute()
  const router = useRouter()
  const city = useCityStore()
  const filter = useFilterStore()

  // ---- URL → store（首次） / URL → store (first entry)
  const q = route.query
  if (typeof q.city === 'string') {
    city.currentSlug = q.city
  }
  const t = parseList(q.t)
  if (t.length > 0) {
    filter.iconicTaxa = t
  }
  if (q.u === 'tenDay') {
    filter.unit = 'tenDay' as TimeUnit
    const d = parseRange(q.d, 1, 36)
    if (d) {
      filter.setTenDayRange(d[0], d[1])
    }
  } else {
    const m = parseRange(q.m, 1, 12)
    if (m) {
      filter.setMonthRange(m[0], m[1])
    }
  }
  const qual = parseList(q.q)
  if (qual.length > 0) {
    filter.quality = qual
  }

  // ---- store → URL（m 与 d 互斥：旬档写 d 省略 m，月份档反之）/ store → URL (m and d are mutually exclusive: ten-day mode writes d and omits m, month mode the reverse)
  watch(
    () => [
      city.currentSlug,
      filter.iconicTaxa.join(','),
      filter.unit,
      filter.monthRange.join('-'),
      filter.tenDayRange.join('-'),
      filter.quality.join(',')
    ] as const,
    ([slug, taxa, unit, months, tenDays, quality]) => {
      const next: Record<string, string | undefined> = {
        ...(route.query as Record<string, string | undefined>),
        city: slug,
        t: taxa || undefined,
        u: unit === 'tenDay' ? 'tenDay' : undefined,
        m: unit === 'tenDay' || months === '1-12' ? undefined : months,
        d: unit === 'tenDay' && tenDays !== '1-36' ? tenDays : undefined,
        q: quality === '1,2' ? undefined : quality
      }
      router.replace({ query: next })
    }
  )
}
