<script setup lang="ts">
/**
 * 类群构成环形图（颜色取类群调色板；点击扇区 → 地图只看该类群） / Taxon composition donut (colors from the taxon palette; clicking a slice → solo that taxon on the map)
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import VChart from 'vue-echarts'
import '@/plugins/echarts'
import { cssVar } from '@/plugins/echarts'
import type { IconicCount } from '@/api/types'
import { getTaxon } from '@/theme/taxonPalette'
import { useThemeStore } from '@/stores/theme'
import { useFilterStore } from '@/stores/filter'

const props = defineProps<{ data: IconicCount[] }>()
const { t } = useI18n()
const themeStore = useThemeStore()
const filter = useFilterStore()

const option = computed(() => {
  void themeStore.theme
  const ink = cssVar('--c-ink-2')
  return {
    tooltip: { trigger: 'item', formatter: '{b}：{c}（{d}%）' },
    series: [
      {
        type: 'pie',
        radius: ['52%', '78%'],
        center: ['50%', '50%'],
        itemStyle: { borderColor: cssVar('--c-surface'), borderWidth: 2, borderRadius: themeStore.theme === 'journal' ? 0 : 4 },
        label: { color: ink, fontSize: 11, formatter: '{b}' },
        labelLine: { length: 6, length2: 6 },
        data: props.data.map((d) => {
          const tx = getTaxon(d.iconicTaxon)
          return { name: t(`taxon.${tx.key}`), value: d.count, code: tx.code, itemStyle: { color: tx.colors[themeStore.theme] } }
        })
      }
    ]
  }
})

/** 点击扇区：只看该类群 / Slice click: solo this taxon */
function onClick(e: { data?: unknown }): void {
  const code = (e.data as { code?: number } | null | undefined)?.code
  if (code) {
    filter.soloTaxon(code)
  }
}
</script>

<template>
  <VChart :option="option" autoresize class="h-56 w-full" @click="onClick" />
</template>
