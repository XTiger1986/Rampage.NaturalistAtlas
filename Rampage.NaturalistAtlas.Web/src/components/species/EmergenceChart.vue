<script setup lang="ts">
/**
 * 物候出现曲线（产品文档 §3.2：逐旬/逐月出现次数；可切换近两年明细口径与全历史统计口径） / Emergence curve (product doc §3.2: counts per ten-day/month; switchable between the recent-two-years detail caliber and the all-time aggregate caliber)
 *
 * - 明细口径：36 旬柱状 + 研究级折线； / - Detail caliber: 36 ten-day bars + research-grade line;
 * - 全历史口径：12 月柱状； / - All-time caliber: 12 monthly bars;
 * - 当前月份以 markArea 高亮，便于回答"现在是不是看它的好时候"。 / - The current month is highlighted with a markArea to answer "is now a good time to see it".
 * 颜色取物种所属类群色；图表颜色在主题切换时重新计算（ECharts 不识别 CSS 变量）。 / Colors come from the species' taxon color; chart colors recompute on theme switch (ECharts can't read CSS variables).
 */
import { computed } from 'vue'
import VChart from 'vue-echarts'
import '@/plugins/echarts'
import { cssVar } from '@/plugins/echarts'
import { useI18n } from 'vue-i18n'
import type { EmergenceResult } from '@/api/types'
import { taxonColor } from '@/theme/taxonPalette'
import { useThemeStore } from '@/stores/theme'
import { monthShort, tenDayLabel } from '@/utils/format'

const props = defineProps<{ data: EmergenceResult; iconicTaxon: number }>()
const themeStore = useThemeStore()
const { t } = useI18n()

const option = computed(() => {
  // 依赖主题：切换主题时重新读取 CSS 变量 / Theme dependency: re-read CSS variables on theme switch
  void themeStore.theme
  void themeStore.accentOverride
  const ink3 = cssVar('--c-ink-3')
  const line = cssVar('--c-line')
  const color = taxonColor(props.iconicTaxon, themeStore.theme)
  const tenDay = props.data.unit === 'tenDay'
  const labels = props.data.series.map((p) => (tenDay ? tenDayLabel(p.index) : t('tenDay.monthLabel', { mon: monthShort(p.index) })))
  const nowIndex = tenDay
    ? (new Date().getMonth()) * 3 + Math.min(2, Math.floor((new Date().getDate() - 1) / 10))
    : new Date().getMonth()
  return {
    grid: { left: 36, right: 12, top: 16, bottom: 28 },
    tooltip: { trigger: 'axis' },
    xAxis: {
      type: 'category',
      data: labels,
      axisLine: { lineStyle: { color: line } },
      // 旬模式每 3 个标签取 1 个并压缩为月名（"3月中旬" → "3月"；英文 "Mid Mar" → "Mar"） / Ten-day mode keeps every 3rd label and compresses it to the month name ("3月中旬" → "3月"; English "Mid Mar" → "Mar")
      axisLabel: { color: ink3, fontSize: 10, interval: tenDay ? 2 : 0, formatter: (v: string) => (tenDay ? v.replace(/(上|中|下)旬$/, '').replace(/^(Early|Mid|Late) /, '') : v) },
      axisTick: { show: false }
    },
    yAxis: {
      type: 'value',
      splitLine: { lineStyle: { color: line } },
      axisLabel: { color: ink3, fontSize: 10 }
    },
    series: [
      {
        name: t('common.records'),
        type: 'bar',
        data: props.data.series.map((p) => p.recordCount),
        itemStyle: { color, borderRadius: [3, 3, 0, 0], opacity: 0.85 },
        barCategoryGap: '25%',
        markArea: {
          silent: true,
          itemStyle: { color: cssVar('--c-accent-soft') },
          data: [[{ xAxis: labels[nowIndex] }, { xAxis: labels[nowIndex] }]]
        }
      },
      ...(tenDay
        ? [
            {
              name: t('filter.research'),
              type: 'line',
              smooth: true,
              symbol: 'none',
              data: props.data.series.map((p) => p.researchCount),
              lineStyle: { color: cssVar('--c-ink-2'), width: 1.5 }
            }
          ]
        : [])
    ]
  }
})
</script>

<template>
  <VChart :option="option" autoresize class="h-52 w-full" />
</template>
