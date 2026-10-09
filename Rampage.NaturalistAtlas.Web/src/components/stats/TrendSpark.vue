<script setup lang="ts">
/**
 * 近 12 个月记录趋势迷你图（设计文档 §5.8） / Last-12-months record trend sparkline (design doc §5.8)
 *
 * ECharts Line 小图（面积填充 + 当月标记点）；颜色取设计令牌（主题切换时重算）。 / ECharts line sparkline (area fill + current-month mark); colors from design tokens (recomputed on theme switch).
 */
import { computed } from 'vue'
import VChart from 'vue-echarts'
import '@/plugins/echarts'
import { cssVar } from '@/plugins/echarts'
import type { MonthlyCount } from '@/api/types'
import { useThemeStore } from '@/stores/theme'
import { formatNumber } from '@/utils/format'

const props = defineProps<{ data: MonthlyCount[] }>()

const themeStore = useThemeStore()

const option = computed(() => {
  // 依赖主题：切换主题时重新读取 CSS 变量 / Theme dependency: re-read CSS variables on theme switch
  void themeStore.theme
  const accent = cssVar('--c-accent')
  const ink3 = cssVar('--c-ink-3')
  const line = cssVar('--c-line')
  const n = props.data.length
  return {
    grid: { left: 8, right: 8, top: 10, bottom: 18 },
    tooltip: {
      trigger: 'axis',
      formatter: (params: unknown): string => {
        const p = (params as Array<{ name: string; value: number }>)[0]
        return `${p.name}<br/>${formatNumber(p.value)}`
      }
    },
    xAxis: {
      type: 'category',
      data: props.data.map((m) => m.month.slice(2)),
      axisLine: { lineStyle: { color: line } },
      axisLabel: { color: ink3, fontSize: 9, interval: n > 8 ? 2 : 0 },
      axisTick: { show: false }
    },
    yAxis: { type: 'value', show: false },
    series: [
      {
        type: 'line',
        data: props.data.map((m) => m.count),
        lineStyle: { color: accent, width: 2 },
        itemStyle: { color: accent },
        areaStyle: { color: cssVar('--c-accent-soft'), opacity: 0.9 },
        symbolSize: 4,
        showSymbol: false
      }
    ]
  }
})
</script>

<template>
  <VChart :option="option" autoresize class="h-24 w-full" />
</template>
