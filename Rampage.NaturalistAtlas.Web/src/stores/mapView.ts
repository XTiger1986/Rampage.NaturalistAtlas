import { ref } from 'vue'
import { defineStore } from 'pinia'
import { useLocalStorage } from '@vueuse/core'
import type { BBox } from '@/api/types'
import type { BasemapId } from '@/map/basemaps'
import type { ViewMode } from '@/map/viewModes'
import type { GlobalProvider } from '@/map/layers/GlobalTileLayer'

/** 叠加图层开关 / Overlay layer toggles */
export interface OverlayToggles {
  /** 照片卡聚合层 / Photo-card cluster layer */
  cards: boolean
  /** 本地热力层 / Local heatmap layer */
  heatmap: boolean
  /** 全球视角（数据源官方瓦片，缩小或离开城市时自动显示） / Global view (official source tiles, auto-shown when zoomed out or away from the city) */
  global: boolean
  /** 城市范围框 / City bounds outline */
  cityBounds: boolean
}

/**
 * 地图视图状态（只存"可序列化的意图"，不存地图实例） / Map view state (stores only serializable intent, never the map instance)
 *
 * MapLibre 实例由 map/MapEngine.ts 以普通对象持有，不进入 Vue 响应式系统（避免 Proxy 包裹大对象导致卡顿）。
 * / The MapLibre instance is held as a plain object in map/MapEngine.ts, outside Vue's reactivity
 * (avoids jank from proxying a large object).
 * 组件修改此 store → MapCanvas.vue 监听后调用 MapEngine 执行；地图交互结果（视野范围、缩放）回写此 store。
 * / Components mutate this store → MapCanvas.vue listens and drives MapEngine; map interaction results
 * (bounds, zoom) are written back here.
 */
export const useMapViewStore = defineStore('mapView', () => {
  /** 视图模式（持久化） / View mode (persisted) */
  const mode = useLocalStorage<ViewMode>('na.viewMode', '2d')
  /** 底图（持久化） / Basemap (persisted) */
  const basemap = useLocalStorage<BasemapId>('na.basemap', 'ofm')
  /** 全球数据源（持久化） / Global data provider (persisted) */
  const globalProvider = useLocalStorage<GlobalProvider>('na.globalProvider', 'inat')
  /** 叠加图层开关（持久化） / Overlay layer toggles (persisted) */
  const overlays = useLocalStorage<OverlayToggles>(
    'na.overlays',
    { cards: true, heatmap: false, global: true, cityBounds: true },
    { mergeDefaults: true }
  )

  /** 当前视野范围（moveend 回写，探索列表按视野查询） / Current viewport bounds (written on moveend; the explore list queries by viewport) */
  const bounds = ref<BBox | null>(null)
  /** 当前缩放级别 / Current zoom level */
  const zoom = ref(9)
  /** 是否处于全球视角（由 GlobalTileLayer 判定后回写，用于提示） / Whether in global view (written back by GlobalTileLayer, used for hints) */
  const inGlobalView = ref(false)
  /** 全球瓦片是否因源站连续失败被降级隐藏（GlobalTileLayer 回写） / Whether global tiles were degraded/hidden after repeated source failures (written by GlobalTileLayer) */
  const globalDegraded = ref(false)
  /** 地图是否已就绪（首个样式加载完成） / Whether the map is ready (first style load finished) */
  const ready = ref(false)

  /** 设置视图模式 / Set the view mode */
  function setMode(m: ViewMode): void {
    mode.value = m
  }

  return { mode, basemap, globalProvider, overlays, bounds, zoom, inGlobalView, globalDegraded, ready, setMode }
})
