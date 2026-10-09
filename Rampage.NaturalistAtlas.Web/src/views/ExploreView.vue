<script setup lang="ts">
/**
 * 探索（首页面板）：此刻视野内的最新记录（US-01） / Explore (home panel): the latest records in the current view (US-01)
 *
 * - 列表查询 = 组合筛选 + 当前地图视野 bbox（地图停止移动 400ms 后刷新）； / - List query = combined filters + current map-view bbox (refreshes 400ms after the map stops moving);
 * - 无限滚动：列表底部哨兵进入面板滚动区前沿 200px 即取下一页； / - Infinite scroll: the bottom sentinel triggers the next page 200px before entering the panel's scroll area;
 *   视野/筛选变化时 queryKey 变化，自动重置到第 1 页； / when the view/filters change the queryKey changes and paging auto-resets to page 1;
 * - "按距离排序"为纯前端开关：以当前视野中心 Haversine 升序重排已加载记录，不改接口； / - "Sort by distance" is a pure front-end toggle: re-sorts loaded records ascending by Haversine distance from the view center, no API change;
 * - 卡片悬停联动地图高亮，点击打开大图； / - Card hover highlights on the map; click opens the lightbox;
 * - 全球视角下（缩小/离开城市）提示用户回到城市尺度查看明细。 / - In the global view (zoomed out / off the city) a hint asks the user back to city scale for details.
 */
import { computed, ref } from 'vue'
import { refDebounced } from '@vueuse/core'
import { useI18n } from 'vue-i18n'
import { NCheckbox } from 'naive-ui'
import PanelHeader from '@/components/common/PanelHeader.vue'
import QueryState from '@/components/common/QueryState.vue'
import PhotoCard from '@/components/observation/PhotoCard.vue'
import { useObservationList } from '@/composables/queries'
import { useInfiniteSentinel } from '@/composables/useInfiniteSentinel'
import { useFilterStore } from '@/stores/filter'
import { useMapViewStore } from '@/stores/mapView'
import { useCityStore } from '@/stores/city'
import { formatNumber, cityName } from '@/utils/format'
import type { ObservationCard } from '@/api/types'

const { t } = useI18n()
const filter = useFilterStore()
const mapView = useMapViewStore()
const city = useCityStore()

/** 视野 bbox 防抖（避免拖动过程中频繁请求） / Viewport bbox debounce (avoids request storms while dragging) */
const bounds = refDebounced(computed(() => mapView.bounds), 400)

/** 列表查询：筛选 + 视野（坐标保留 4 位小数，提高缓存命中） / List query: filters + view (coordinates keep 4 decimals to improve cache hits) */
const query = computed(() => ({
  ...filter.query,
  bbox: bounds.value ? bounds.value.map((n) => n.toFixed(4)).join(',') : undefined
}))

/** 面板滚动容器（哨兵的 IntersectionObserver root；侧栏与移动抽屉 provide） / Panel scroll container (the sentinel's IntersectionObserver root; provided by the sidebar and mobile drawer) */
const { data, flat, isLoading, error, refetch, fetchNextPage, hasNextPage, isFetchingNextPage } =
  useObservationList(query, 30)

/** 视野内总记录数（取第 1 页的分页头） / Total records in view (from page 1's pager header) */
const total = computed(() => data.value?.pages[0]?.pager?.recordCount ?? null)

/** 列表底部哨兵（公共 composable：进入滚动区前沿 200px 即取下一页） / Bottom sentinel (shared composable: fetch the next page 200px before entering the scroll area) */
const sentinel = ref<HTMLElement | null>(null)
useInfiniteSentinel(sentinel, () => {
  if (hasNextPage.value && !isFetchingNextPage.value) {
    fetchNextPage()
  }
})

/** 距离排序开关 / Sort-by-distance toggle */
const sortByDistance = ref(false)

/** 展示列表：开关开启时按距当前视野中心 Haversine 升序重排已加载记录 / Displayed list: with the toggle on, loaded records are re-sorted by ascending Haversine distance from the current view center */
const items = computed<ObservationCard[]>(() => {
  const list = flat.value
  if (!sortByDistance.value || !bounds.value) {
    return list
  }
  const [w, s, e, n] = bounds.value
  const cx = (w + e) / 2
  const cy = (s + n) / 2
  return [...list].sort((a, b) => haversineKm(a.lng, a.lat, cx, cy) - haversineKm(b.lng, b.lat, cx, cy))
})

/** 大图键盘切换的列表上下文 / List context for lightbox keyboard navigation */
const ids = computed(() => items.value.map((o) => o.id))

/** Haversine 球面距离（km，仅用于排序，不必精确到米） / Haversine great-circle distance (km; sorting only, no need for meter precision) */
function haversineKm(lng1: number, lat1: number, lng2: number, lat2: number): number {
  const rad = Math.PI / 180
  const dLat = (lat2 - lat1) * rad
  const dLng = (lng2 - lng1) * rad
  const a =
    Math.sin(dLat / 2) ** 2 + Math.cos(lat1 * rad) * Math.cos(lat2 * rad) * Math.sin(dLng / 2) ** 2
  return 6371 * 2 * Math.asin(Math.sqrt(a))
}
</script>

<template>
  <div>
    <PanelHeader :title="cityName(city.current)" :subtitle="t('app.slogan')" />
    <div class="px-5 pt-4 pb-2">
      <div class="flex items-baseline justify-between">
        <h2 class="font-display text-sm font-semibold text-ink">{{ t('explore.latest') }}</h2>
        <span v-if="total != null" class="text-xs text-ink-3">
          {{ t('explore.inView', { n: formatNumber(total) }) }}
        </span>
      </div>
      <div class="mt-1 flex min-h-6 items-center justify-between">
        <NCheckbox v-model:checked="sortByDistance" size="small" class="text-xs">
          {{ t('explore.sortByDistance') }}
        </NCheckbox>
        <span v-if="isFetchingNextPage" class="text-xs text-ink-3">{{ t('common.loading') }}</span>
      </div>
      <p v-if="mapView.inGlobalView" class="mt-1 text-xs text-ink-3">{{ t('map.globalHint') }}</p>
    </div>
    <QueryState :loading="isLoading" :error="error" :empty="items.length === 0" @retry="refetch()">
      <div class="grid grid-cols-2 gap-3 px-5 pb-4">
        <PhotoCard v-for="o in items" :key="o.id" :obs="o" :ids="ids" />
      </div>
      <!-- 无限滚动哨兵 / Infinite-scroll sentinel -->
      <div ref="sentinel" class="h-px w-full" />
    </QueryState>
  </div>
</template>
