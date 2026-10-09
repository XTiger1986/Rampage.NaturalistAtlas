import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { geoApi } from '@/api/atlas'
import type { BBox, CityItem } from '@/api/types'

/**
 * 离线兜底城市（后端未连接时使用，保证地图可浏览） / Offline fallback cities (used when the backend is unreachable, keeping the map browsable)
 * id 为负数表示离线项：数据类接口会被跳过（queries.ts 中 enabled 判断 cityId > 0）。
 * / Negative ids mark offline entries: data queries are skipped (queries.ts checks cityId > 0 via enabled).
 */
const FALLBACK_CITIES: CityItem[] = [
  mk(-1, '北京', 'Beijing', 'beijing', [115.42, 39.44, 117.51, 41.06]),
  mk(-2, '上海', 'Shanghai', 'shanghai', [120.85, 30.67, 122.2, 31.87]),
  mk(-3, '广州', 'Guangzhou', 'guangzhou', [112.95, 22.56, 114.05, 23.93]),
  mk(-4, '深圳', 'Shenzhen', 'shenzhen', [113.75, 22.44, 114.63, 22.86]),
  mk(-5, '杭州', 'Hangzhou', 'hangzhou', [118.34, 29.19, 120.72, 30.57]),
  mk(-6, '成都', 'Chengdu', 'chengdu', [102.99, 30.09, 104.9, 31.44]),
  mk(-7, '昆明', 'Kunming', 'kunming', [102.17, 24.39, 103.67, 26.55]),
  mk(-8, '香港', 'Hong Kong', 'hong-kong', [113.83, 22.15, 114.44, 22.57])
]

/** 构造兜底城市项（中心点由 bbox 推算） / Build a fallback city item (center derived from the bbox) */
function mk(id: number, name: string, nameEn: string, slug: string, bbox: BBox): CityItem {
  return {
    id,
    name,
    nameEn,
    slug,
    countryId: 0,
    countryName: null,
    provinceName: null,
    bbox,
    center: [(bbox[0] + bbox[2]) / 2, (bbox[1] + bbox[3]) / 2]
  }
}

/**
 * 城市状态 / City store
 * - 启动时拉取 /api/geo/cities；失败则使用离线兜底列表并标记 offline；
 *   / Fetches /api/geo/cities on startup; on failure falls back to the offline list and sets offline;
 * - 当前城市以 slug 标识，与 URL 查询参数 ?city= 双向同步（见 composables/useUrlSync.ts）。
 *   / The current city is identified by slug and kept in two-way sync with the ?city= URL query param (see composables/useUrlSync.ts).
 */
export const useCityStore = defineStore('city', () => {
  /** 可选城市列表 / Selectable city list */
  const cities = ref<CityItem[]>(FALLBACK_CITIES)
  /** 当前城市 slug / Current city slug */
  const currentSlug = ref<string>('beijing')
  /** 是否离线（后端未连接） / Whether offline (backend unreachable) */
  const offline = ref(true)
  /** 是否已完成加载 / Whether loading has finished */
  const loaded = ref(false)

  /** 当前城市 / Current city */
  const current = computed<CityItem>(
    () => cities.value.find((c) => c.slug === currentSlug.value) ?? cities.value[0]!
  )

  /** 当前城市 id（离线时为负数） / Current city id (negative when offline) */
  const currentId = computed(() => current.value.id)

  /** 拉取城市列表（应用启动时调用一次） / Fetch the city list (called once at app startup) */
  async function load(): Promise<void> {
    try {
      const list = await geoApi.cities()
      if (list.length > 0) {
        cities.value = list
        offline.value = false
        // 当前 slug 不在列表中时回落第一个城市 / Fall back to the first city when the current slug is not in the list
        if (!list.some((c) => c.slug === currentSlug.value)) {
          currentSlug.value = list[0]!.slug
        }
      }
    } catch {
      offline.value = true
    } finally {
      loaded.value = true
    }
  }

  /** 切换城市 / Switch the city */
  function setCity(slug: string): void {
    if (cities.value.some((c) => c.slug === slug)) {
      currentSlug.value = slug
    }
  }

  return { cities, currentSlug, current, currentId, offline, loaded, load, setCity }
})
