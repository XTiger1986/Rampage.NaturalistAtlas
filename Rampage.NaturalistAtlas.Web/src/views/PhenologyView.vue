<script setup lang="ts">
/**
 * 物候日历（US-03 / US-07） / Phenology calendar (US-03 / US-07)
 *
 * - 矩阵：物种 × 月份热力表（跟随全局类群/鉴定筛选；月份筛选不作用于矩阵本身）， / - Matrix: species × month heat table (follows the global taxon/verification filters; the month filter does not apply to the matrix itself),
 *   列头/峰值月排序与 Top N 选择在矩阵组件内（设计文档 §5.7）； / column headers/peak-month sorting and the Top N selector live inside the matrix component (design doc §5.7);
 * - 曲线：点击矩阵行选中物种，下方展示该物种逐旬/逐月出现曲线（选中物种写入 URL ?taxon=）； / - Curve: clicking a matrix row selects the species and shows its ten-day/monthly occurrence curve below (the selection is written to URL ?taxon=);
 * - 口径说明常驻，防止把观察热度误读为物候（产品文档 §10 风险）。 / - The caliber note stays visible so observation effort is not misread as phenology (product doc §10 risk).
 * 「城市苏醒」动画入口在地图底部时间滑块。 / The "city awakening" animation lives in the time slider at the bottom of the map.
 */
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { NButton, NRadioButton, NRadioGroup, NSelect, NSwitch } from 'naive-ui'
import PanelHeader from '@/components/common/PanelHeader.vue'
import QueryState from '@/components/common/QueryState.vue'
import PhenologyMatrix from '@/components/phenology/PhenologyMatrix.vue'
import EmergenceChart from '@/components/species/EmergenceChart.vue'
import { exportMatrixPng } from '@/components/phenology/matrixPng'
import { useEmergence, usePhenologyMatrix } from '@/composables/queries'
import { useFilterStore } from '@/stores/filter'
import { useCityStore } from '@/stores/city'
import { useThemeStore } from '@/stores/theme'
import { formatNumber, speciesName, cityName } from '@/utils/format'
import type { EmergenceMode } from '@/api/types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const filter = useFilterStore()
const city = useCityStore()

/** 矩阵物种数（设计文档 §5.7：30 / 50 / 100） / Matrix species count (design doc §5.7: 30 / 50 / 100) */
const topN = ref(30)
const topOptions = [30, 50, 100].map((n) => ({ label: `Top ${n}`, value: n }))

/** 矩阵查询：去掉月份/旬筛选（矩阵本身即全年视角），取前 N 个物种 / Matrix query: strips month/ten-day filters (the matrix is itself a full-year view) and takes the top N species */
const matrixQuery = computed(() => ({
  ...filter.query,
  monthFrom: undefined,
  monthTo: undefined,
  d1: undefined,
  d2: undefined,
  top: topN.value
}))
const { data: matrix, isLoading, error, refetch } = usePhenologyMatrix(matrixQuery)

/** 选中物种（URL 同步） / Selected species (synced with the URL) */
const selectedId = computed(() => (typeof route.query.taxon === 'string' ? route.query.taxon : null))
function select(id: string): void {
  router.replace({ query: { ...route.query, taxon: id } })
}
const selectedRow = computed(() => matrix.value?.rows.find((r) => r.taxonId === selectedId.value) ?? null)

const mode = ref<EmergenceMode>('detail')
const researchOnly = ref(false)
const { data: emergence } = useEmergence(selectedId, () => city.currentId, mode, researchOnly)
const themeStore = useThemeStore()

/** 导出矩阵 PNG（原生 canvas 重绘，颜色与当前主题一致） / Export the matrix as PNG (redrawn on a native canvas, colors matching the current theme) */
function exportPng(): void {
  if (!matrix.value || matrix.value.rows.length === 0) {
    return
  }
  const date = new Date().toISOString().slice(0, 10).replace(/-/g, '')
  exportMatrixPng(matrix.value.rows, {
    title: `${cityName(city.current)} · ${t('phenology.matrix')} Top ${topN.value}`,
    caliberText: t('phenology.exportCaliber', {
      quality: researchOnly.value ? t('filter.research') : `${t('filter.research')}+${t('filter.needsId')}`
    }),
    theme: themeStore.theme,
    filename: `${city.currentSlug}-phenology-top${topN.value}-${date}.png`
  })
}
</script>

<template>
  <div>
    <PanelHeader :title="t('phenology.title')" :subtitle="cityName(city.current)" />

    <!-- 曲线区 / Curve section -->
    <section class="border-b border-line px-5 py-4">
      <div class="mb-2 flex items-center justify-between gap-2">
        <h2 class="font-display truncate text-sm font-semibold text-ink">
          {{ selectedRow ? speciesName(selectedRow) : t('phenology.curve') }}
        </h2>
        <label class="flex shrink-0 items-center gap-1 text-xs text-ink-2">
          {{ t('filter.research') }}
          <NSwitch v-model:value="researchOnly" size="small" />
        </label>
      </div>
      <NRadioGroup v-model:value="mode" size="small" class="mb-2">
        <NRadioButton value="detail">{{ t('phenology.detailMode') }}</NRadioButton>
        <NRadioButton value="history">{{ t('phenology.historyMode') }}</NRadioButton>
      </NRadioGroup>
      <template v-if="selectedId && emergence">
        <EmergenceChart :data="emergence" :iconic-taxon="selectedRow?.iconicTaxon ?? 14" />
        <p class="text-[11px] text-ink-3">{{ t('phenology.sampleSize', { n: formatNumber(emergence.sampleSize) }) }}</p>
      </template>
      <p v-else class="py-6 text-center text-xs text-ink-3">{{ t('phenology.pickSpecies') }}</p>
      <p class="mt-1 text-[11px] leading-relaxed text-ink-3">{{ t('phenology.caliberNote') }}</p>
    </section>

    <!-- 矩阵区 / Matrix section -->
    <section class="pt-3 pb-6">
      <div class="mb-1 flex items-center justify-between px-5">
        <h2 class="font-display text-sm font-semibold text-ink">{{ t('phenology.matrix') }}</h2>
        <div class="flex items-center gap-1">
          <NSelect v-model:value="topN" :options="topOptions" size="tiny" class="w-24" />
          <NButton size="tiny" @click="exportPng">PNG</NButton>
        </div>
      </div>
      <QueryState :loading="isLoading" :error="error" :empty="(matrix?.rows.length ?? 0) === 0" :rows="8" @retry="refetch()">
        <PhenologyMatrix :rows="matrix!.rows" :selected-id="selectedId" @select="select" />
      </QueryState>
    </section>
  </div>
</template>
