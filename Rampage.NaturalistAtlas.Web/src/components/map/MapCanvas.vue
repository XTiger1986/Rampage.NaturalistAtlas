<script setup lang="ts">
/**
 * 地图画布（常驻底座） / Map canvas (persistent base)
 *
 * 这是 Vue 世界与地图引擎之间唯一的"胶水层"： / The sole "glue layer" between the Vue world and the map engine:
 * - store（主题/底图/视图模式/图层开关/城市/筛选）变化 → 调用 MapEngine 与各图层的命令式 API； / - store changes (theme/basemap/view mode/layer toggles/city/filters) → imperative calls into MapEngine and each layer's API;
 * - 地图交互（moveend）→ 回写 mapView store（视野 bbox、缩放级别）； / - map interaction (moveend) → writes back to the mapView store (viewport bbox, zoom level);
 * - 城市级点位数据（vue-query 缓存）→ 灌入照片卡层与热力层。 / - city-level point data (vue-query cache) → fed into the photo-card and heatmap layers.
 *
 * 图层栈（自下而上，注册顺序即叠放顺序）： / Layer stack (bottom-up; registration order is stacking order):
 *   底图 → 城市遮罩/边框 → 全球密度瓦片 → 本地热力 → 照片卡（HTML Marker，始终最上） /   basemap → city mask/bounds → global density tiles → local heatmap → photo cards (HTML markers, always on top)
 */
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { mapEngine } from '@/map/MapEngine'
import { ObservationCardLayer } from '@/map/layers/ObservationCardLayer'
import { HeatmapLayer } from '@/map/layers/HeatmapLayer'
import { GlobalTileLayer } from '@/map/layers/GlobalTileLayer'
import { CityBoundsLayer } from '@/map/layers/CityBoundsLayer'
import { useThemeStore } from '@/stores/theme'
import { useMapViewStore } from '@/stores/mapView'
import { useCityStore } from '@/stores/city'
import { useFilterStore } from '@/stores/filter'
import { useUiStore } from '@/stores/ui'
import { useObservationPoints } from '@/composables/queries'

const container = ref<HTMLDivElement | null>(null)

const themeStore = useThemeStore()
const mapView = useMapViewStore()
const city = useCityStore()
const filter = useFilterStore()
const ui = useUiStore()

// 图层实例（普通对象，不做响应式） / Layer instances (plain objects, kept non-reactive)
const boundsLayer = new CityBoundsLayer()
const globalLayer = new GlobalTileLayer(
  (inGlobal) => (mapView.inGlobalView = inGlobal),
  (degraded) => (mapView.globalDegraded = degraded)
)
const heatLayer = new HeatmapLayer()
const cardLayer = new ObservationCardLayer({ onOpen: (id) => ui.openObservation(id) })

// 城市级点位（筛选变化自动重新请求；切回已看过的组合命中缓存） / City-level points (auto-refetch on filter change; revisited combos hit the cache)
const { data: pointResult } = useObservationPoints(() => filter.query)

onMounted(() => {
  if (!container.value) {
    return
  }
  const map = mapEngine.init({
    container: container.value,
    center: city.current.center,
    zoom: 9,
    theme: themeStore.theme,
    basemap: mapView.basemap,
    mode: mapView.mode
  })
  mapEngine.addLayer(boundsLayer)
  mapEngine.addLayer(globalLayer)
  mapEngine.addLayer(heatLayer)
  mapEngine.addLayer(cardLayer)

  // 首屏直接定位到城市（无动画） / Position straight onto the city on first paint (no animation)
  mapEngine.fitBBox(city.current.bbox, false)
  boundsLayer.setBBox(city.current.bbox)
  globalLayer.setCityBBox(city.current.bbox)
  applyOverlays()

  // 视野回写（探索列表按视野查询） / View write-back (explore list queries by viewport)
  const syncView = () => {
    mapView.bounds = mapEngine.getBBox()
    mapView.zoom = map.getZoom()
  }
  map.on('moveend', syncView)
  mapEngine.onReady(() => {
    mapView.ready = true
    syncView()
  })
})

onBeforeUnmount(() => {
  mapEngine.destroy()
  mapView.ready = false
})

// ---------------------------------------------------------------- store → 引擎 / store → engine

watch(() => themeStore.theme, (t) => mapEngine.setTheme(t))
watch(() => mapView.basemap, (b) => mapEngine.setBasemap(b))
watch(() => mapView.mode, (m) => mapEngine.setMode(m))
watch(() => mapView.globalProvider, (p) => globalLayer.setProvider(p))

// 城市切换：飞行定位 + 更新范围框与全球视角判定 / City switch: fly-to + refresh the bbox and global-view detection
watch(
  () => city.current.slug + city.current.bbox.join(','),
  () => {
    mapEngine.fitBBox(city.current.bbox)
    boundsLayer.setBBox(city.current.bbox)
    globalLayer.setCityBBox(city.current.bbox)
  }
)

// 点位数据 → 照片卡层 + 热力层 / Point data → photo-card layer + heatmap layer
watch(
  () => pointResult.value?.points,
  (points) => {
    cardLayer.setData(points ?? [])
    heatLayer.setData(points ?? [])
  }
)

// 筛选 → 全球瓦片参数（iNat 瓦片不支持日期窗：旬档展开为覆盖的月份区间） / Filters → global tile params (iNat tiles don't support date windows: ten-day ranges expand to the covering month span)
const globalFilter = computed(() => {
  const q = filter.query
  let monthFrom = q.monthFrom
  let monthTo = q.monthTo
  if (filter.unit === 'tenDay' && !filter.isFullTenDay) {
    const [f, t] = filter.tenDayRange
    monthFrom = Math.floor((f - 1) / 3) + 1
    monthTo = Math.floor((t - 1) / 3) + 1
  }
  return { iconic: q.iconic ?? [], monthFrom, monthTo, quality: q.quality ?? [] }
})
watch(globalFilter, (f) => globalLayer.setFilter(f), { immediate: true })

/** 图层可见性：全球视角下隐藏本地明细层（双视角无缝衔接） / Layer visibility: local detail layers hidden in global view (seamless dual-view handover) */
function applyOverlays(): void {
  const o = mapView.overlays
  const local = !mapView.inGlobalView
  cardLayer.setVisible(o.cards && local)
  heatLayer.setVisible(o.heatmap && local)
  globalLayer.setEnabled(o.global)
  boundsLayer.setVisible(o.cityBounds)
}
watch(() => [mapView.overlays, mapView.inGlobalView], applyOverlays, { deep: true })

// 列表悬停 → 地图卡片高亮 / List hover → map card highlight
watch(() => ui.hoverId, (id) => cardLayer.highlight(id))
</script>

<template>
  <div ref="container" class="h-full w-full" />
</template>
