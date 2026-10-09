<script setup lang="ts">
/**
 * 地图平视显示层（HUD）：地图之上的浮动控件布局容器 / Map HUD: layout container for floating controls above the map
 *
 * - 顶部居中：类群图例筛选（图例即筛选器） / - Top center: taxon legend filter (the legend is the filter)
 * - 右上：视图模式切换 + 图层面板 / - Top right: view mode switch + layer panel
 * - 底部居中：时间滑块（月份筛选 / 「城市苏醒」播放） / - Bottom center: time slider (month filter / "City Awakening" playback)
 * - 提示条：全球视角 / 点位截断 / - Hint bars: global view / point truncation
 *
 * 桌面端左侧面板展开时，HUD 整体右移让位，避免遮挡。 / When the desktop left panel is open the whole HUD shifts right to avoid being covered.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import TaxonLegendFilter from './TaxonLegendFilter.vue'
import ViewModeSwitch from './ViewModeSwitch.vue'
import LayerSwitcher from './LayerSwitcher.vue'
import TimeSlider from './TimeSlider.vue'
import { useResponsive } from '@/composables/useResponsive'
import { formatNumber } from '@/utils/format'
import { useUiStore } from '@/stores/ui'
import { useMapViewStore } from '@/stores/mapView'
import { useFilterStore } from '@/stores/filter'
import { useCityStore } from '@/stores/city'
import { mapEngine } from '@/map/MapEngine'
import { useObservationPoints } from '@/composables/queries'

const { t } = useI18n()
const { isDesktop } = useResponsive()
const ui = useUiStore()
const mapView = useMapViewStore()
const filter = useFilterStore()
const city = useCityStore()

// 与 MapCanvas 使用同一 queryKey，vue-query 自动去重，不会重复请求 / Shares the queryKey with MapCanvas, so vue-query dedupes — no duplicate request
const { data: pointResult } = useObservationPoints(() => filter.query)

/** HUD 左边界（桌面面板展开时让位） / HUD left edge (yields when the desktop panel is open) */
const leftOffset = computed(() => (isDesktop.value && ui.panelOpen ? 'calc(var(--panel-w) + 24px)' : '12px'))
</script>

<template>
  <div
    class="pointer-events-none absolute right-[max(0.75rem,env(safe-area-inset-right))] bottom-3 flex flex-col"
    :style="{ zIndex: 'var(--z-overlay)', top: 'calc(var(--topbar-h) + 24px)', left: leftOffset }"
  >
    <!-- 顶部：图例筛选 + 右侧工具（窄屏折行：图例独占一行，工具组换行靠右） / Top: legend filter + right tools (narrow screens wrap: legend takes a full row, tools wrap right-aligned) -->
    <div class="flex flex-col items-stretch gap-2 min-[640px]:flex-row min-[640px]:items-start min-[640px]:gap-3">
      <div class="pointer-events-auto min-w-0 flex-1">
        <TaxonLegendFilter />
      </div>
      <div class="pointer-events-auto flex shrink-0 flex-row justify-end gap-2 min-[640px]:flex-col min-[640px]:items-end">
        <ViewModeSwitch />
        <LayerSwitcher />
      </div>
    </div>

    <!-- 提示条 / Hint bars -->
    <div class="mt-2 flex flex-col items-center gap-2">
      <Transition name="panel">
        <button
          v-if="mapView.inGlobalView && mapView.overlays.global"
          type="button"
          class="glass-panel pointer-events-auto rounded-panel px-3 py-1.5 text-xs text-ink-2 transition-colors hover:text-accent"
          :title="t('map.globalBar', { city: city.current.name })"
          @click="mapEngine.fitBBox(city.current.bbox)"
        >
          {{ t('map.globalBar', { city: city.current.name }) }}
        </button>
      </Transition>
      <Transition name="panel">
        <div
          v-if="mapView.inGlobalView && mapView.globalDegraded"
          class="glass-panel px-3 py-1.5 text-xs text-warn"
        >
          {{ t('map.globalDegraded') }}
        </div>
      </Transition>
      <Transition name="fade">
        <div
          v-if="pointResult?.truncated && !mapView.inGlobalView"
          class="glass-panel px-3 py-1.5 text-xs text-warn"
        >
          {{ t('map.truncated', { n: pointResult.points.length, total: formatNumber(pointResult.total) }) }}
        </div>
      </Transition>
    </div>

    <!-- 底部：时间滑块（移动端让出底部抽屉 peek 高度） / Bottom: time slider (mobile leaves room for the bottom sheet's peek height) -->
    <div class="mt-auto flex justify-center" :class="isDesktop ? 'mb-8' : 'mb-[20dvh]'">
      <div class="pointer-events-auto w-full max-w-2xl">
        <TimeSlider />
      </div>
    </div>
  </div>
</template>
