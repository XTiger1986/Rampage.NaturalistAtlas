<script setup lang="ts">
/**
 * 视图模式切换：2D / 2.5D / 3D（分段按钮） / View mode switch: 2D / 2.5D / 3D (segmented buttons)
 * 切换后由 MapCanvas 调用 MapEngine.setMode，带 1.2s 相机过渡动画。 / On switch, MapCanvas calls MapEngine.setMode with a 1.2s camera transition.
 */
import { useI18n } from 'vue-i18n'
import { NTooltip } from 'naive-ui'
import { useMapViewStore } from '@/stores/mapView'
import type { ViewMode } from '@/map/viewModes'

const { t } = useI18n()
const mapView = useMapViewStore()

/** 模式列表 / Mode list */
const modes: { id: ViewMode; label: string; tip: string }[] = [
  { id: '2d', label: 'map.mode2d', tip: 'map.mode2dTip' },
  { id: '2.5d', label: 'map.mode25d', tip: 'map.mode25dTip' },
  { id: '3d', label: 'map.mode3d', tip: 'map.mode3dTip' }
]
</script>

<template>
  <div class="glass-panel flex p-1" role="radiogroup" aria-label="view mode">
    <NTooltip v-for="m in modes" :key="m.id" placement="bottom">
      <template #trigger>
        <button
          type="button"
          role="radio"
          :aria-checked="mapView.mode === m.id"
          class="rounded-chip px-2.5 py-1 text-xs font-semibold tracking-wide transition-colors"
          :class="mapView.mode === m.id ? 'bg-accent text-accent-ink' : 'text-ink-2 hover:text-ink'"
          @click="mapView.setMode(m.id)"
        >
          {{ t(m.label) }}
        </button>
      </template>
      {{ t(m.tip) }}
    </NTooltip>
  </div>
</template>
