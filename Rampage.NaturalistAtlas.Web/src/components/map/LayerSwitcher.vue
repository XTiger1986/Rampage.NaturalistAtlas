<script setup lang="ts">
/**
 * 图层面板：底图选择 + 叠加图层开关 + 全球数据源 / Layer panel: basemap selection + overlay toggles + global data source
 */
import { useI18n } from 'vue-i18n'
import { NButton, NPopover, NRadioGroup, NRadio, NSwitch } from 'naive-ui'
import { useMapViewStore } from '@/stores/mapView'
import type { OverlayToggles } from '@/stores/mapView'
import { BASEMAPS } from '@/map/basemaps'

const { t } = useI18n()
const mapView = useMapViewStore()

/** 叠加图层项 / Overlay layer keys */
const overlayKeys: (keyof OverlayToggles)[] = ['cards', 'heatmap', 'global', 'cityBounds']
</script>

<template>
  <NPopover trigger="click" placement="left-start" :show-arrow="false" raw>
    <template #trigger>
      <NButton class="glass-panel !h-9 !px-3" quaternary size="small">
        <span class="text-xs text-ink-2">{{ t('map.layers') }}</span>
      </NButton>
    </template>
    <div class="glass-panel w-60 space-y-4 p-3 text-sm">
      <section>
        <div class="mb-2 text-xs text-ink-3">{{ t('map.basemap') }}</div>
        <NRadioGroup v-model:value="mapView.basemap" size="small">
          <div class="flex flex-col gap-1.5">
            <NRadio v-for="b in BASEMAPS" :key="b.id" :value="b.id">{{ t(`basemap.${b.id}`) }}</NRadio>
          </div>
        </NRadioGroup>
      </section>
      <section>
        <div class="mb-2 text-xs text-ink-3">{{ t('map.overlays') }}</div>
        <div class="flex flex-col gap-2">
          <label v-for="k in overlayKeys" :key="k" class="flex items-center justify-between">
            <span class="text-ink">{{ t(`map.${k}`) }}</span>
            <NSwitch v-model:value="mapView.overlays[k]" size="small" />
          </label>
        </div>
      </section>
      <section v-if="mapView.overlays.global">
        <div class="mb-2 text-xs text-ink-3">{{ t('map.globalProvider') }}</div>
        <NRadioGroup v-model:value="mapView.globalProvider" size="small">
          <NRadio value="inat">iNaturalist</NRadio>
          <NRadio value="gbif">GBIF</NRadio>
        </NRadioGroup>
      </section>
    </div>
  </NPopover>
</template>
