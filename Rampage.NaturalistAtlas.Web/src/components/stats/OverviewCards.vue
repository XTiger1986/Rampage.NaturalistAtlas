<script setup lang="ts">
/**
 * 城市总览指标卡：总记录 / 研究级 / 物种数 / 观察者 / 本月记录 / City overview stat cards: total records / research grade / species count / observers / this month's records
 * 数字随数据到达滚动入场（useTransition 数值插值；reduced-motion 由全局 CSS 兜底缩短时长）。 / Numbers roll in as data arrives (useTransition numeric tween; global CSS shortens the duration under reduced-motion).
 */
import { computed } from 'vue'
import { useTransition } from '@vueuse/core'
import { useI18n } from 'vue-i18n'
import type { CityOverview } from '@/api/types'
import { formatNumber } from '@/utils/format'

const props = defineProps<{ data: CityOverview }>()
const { t } = useI18n()

/** 五个指标各一条滚动插值（数据刷新时从旧值过渡到新值） / One rolling tween per metric (old value transitions to the new one on data refresh) */
const animTotal = useTransition(() => props.data.totalObservation, { duration: 600 })
const animSpecies = useTransition(() => props.data.speciesCount, { duration: 600 })
const animResearch = useTransition(() => props.data.researchCount, { duration: 600 })
const animObservers = useTransition(() => props.data.observerCount, { duration: 600 })
const animMonth = useTransition(() => props.data.monthObservation, { duration: 600 })

/** 指标项（读 .value 使插值变化驱动本 computed 重算） / Metric items (reading .value lets tween changes drive this computed) */
const items = computed(() => [
  { key: 'total', anim: animTotal.value, big: true },
  { key: 'speciesCount', anim: animSpecies.value, big: true },
  { key: 'research', anim: animResearch.value },
  { key: 'observerCount', anim: animObservers.value },
  { key: 'thisMonth', anim: animMonth.value }
])
</script>

<template>
  <!-- 容器断点（@container 在面板滚动容器上）：面板 420px 内 2 列，容器 ≥448px 才 3 列 / Container breakpoints (@container on the panel scroll container): 2 columns within the 420px panel, 3 columns only at ≥448px container -->
  <div class="grid grid-cols-2 gap-2.5 @[28rem]:grid-cols-3">
    <div
      v-for="it in items"
      :key="it.key"
      class="rounded-card border border-line bg-surface px-3 py-2.5"
      :class="it.big ? 'col-span-1 sm:col-span-1' : ''"
    >
      <div class="text-[11px] text-ink-3">{{ t(`overview.${it.key}`) }}</div>
      <div class="font-display mt-0.5 tabular" :class="it.big ? 'text-2xl font-bold text-accent' : 'text-lg font-semibold text-ink'">
        {{ formatNumber(Math.round(it.anim)) }}
      </div>
    </div>
  </div>
</template>
