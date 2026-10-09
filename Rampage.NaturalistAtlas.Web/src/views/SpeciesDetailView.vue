<script setup lang="ts">
/**
 * 物种详情（US-06，P0）：分类面包屑 + 照片墙（瀑布流无限加载）+ 本市分布点 + 官方分布范围底图 + 全年出现节奏曲线 / Species detail (US-06, P0): taxonomy breadcrumb + photo wall (waterfall with infinite loading) + local points + official range basemap + year-round occurrence rhythm curve
 *
 * 进入时在地图上启用 SpeciesFocusLayer（分布范围瓦片 + 本市分布点），并将视野适配到分布点； / On entry the SpeciesFocusLayer is enabled on the map (range tiles + local points) and the view is fitted to the points;
 * 离开时注销该图层，地图恢复探索状态； / on leave the layer is unregistered and the map returns to explore state;
 * "最佳观察时间"由物候曲线纯前端计算（峰值 + 边界占比 ≥10% 的连续区间，样本量不足时不生成）。 / The "best observation window" is computed purely on the front end from the phenology curve (peak + a consecutive span with ≥10% share; not produced when the sample is too small).
 */
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { NButton, NRadioButton, NRadioGroup, NTag } from 'naive-ui'
import QueryState from '@/components/common/QueryState.vue'
import TaxonChip from '@/components/common/TaxonChip.vue'
import ProgressiveImage from '@/components/common/ProgressiveImage.vue'
import LicenseBadge from '@/components/common/LicenseBadge.vue'

import PhotoWall from '@/components/species/PhotoWall.vue'
import EmergenceChart from '@/components/species/EmergenceChart.vue'
import { useEmergence, useSimilarSpecies, useSpeciesDetail, useTaxonAncestors } from '@/composables/queries'
import { useCityStore } from '@/stores/city'
import { mapEngine } from '@/map/MapEngine'
import { SpeciesFocusLayer } from '@/map/layers/SpeciesFocusLayer'
import { formatNumber, speciesName } from '@/utils/format'
import { bestWindow } from '@/utils/emergence'
import { monthShort, tenDayLabel } from '@/utils/format'
import type { EmergenceMode } from '@/api/types'

const { t, locale } = useI18n()
const route = useRoute()
const city = useCityStore()

const taxonId = computed(() => (typeof route.params.id === 'string' ? route.params.id : null))
const { data, isLoading, error, refetch } = useSpeciesDetail(taxonId, () => city.currentId)

/** 祖先链面包屑（未同步链段后端已跳过；空链不渲染） / Ancestor-chain breadcrumb (the backend skips unsynced ranks; an empty chain renders nothing) */
const { data: ancestors } = useTaxonAncestors(taxonId)

/** 相似物种（同祖先链同胞，本市有记录） / Similar species (siblings on the same ancestor chain with records in this city) */
const { data: similar } = useSimilarSpecies(taxonId, () => city.currentId)

/** 物候口径 / Phenology caliber */
const mode = ref<EmergenceMode>('detail')
const { data: emergence } = useEmergence(taxonId, () => city.currentId, mode, false)

/** 阶元名称（rank 0~11 → i18n） / Rank names (rank 0~11 → i18n) */
const RANK_KEYS: Record<number, string> = {
  0: 'unknown',
  1: 'kingdom',
  2: 'phylum',
  3: 'class',
  4: 'order',
  5: 'family',
  6: 'genus',
  7: 'species',
  8: 'subspecies',
  9: 'variety',
  10: 'form',
  11: 'hybrid'
}

/** 物候单元序号 → 本地化标签（月："4月"/"Apr"；旬："4月中旬"/"Mid Apr"） / Phenology unit index → localized label (month: "4月"/"Apr"; ten-day: "4月中旬"/"Mid Apr") */
function unitLabel(idx: number, unit: 'tenDay' | 'month'): string {
  return unit === 'month' ? t('tenDay.monthLabel', { mon: monthShort(idx) }) : tenDayLabel(idx)
}

/** 最佳观察时间文案（样本量不足时为 null） / Best-observation-window text (null when the sample is too small) */
const bestWindowText = computed<string | null>(() => {
  const e = emergence.value
  if (!e || e.series.length === 0) {
    return null
  }
  const w = bestWindow(e.series)
  if (!w) {
    return null
  }
  return t('species.bestWindow', {
    from: unitLabel(w.fromIdx, e.unit),
    to: unitLabel(w.toIdx, e.unit),
    pct: Math.round(w.share * 100)
  })
})

// 地图聚焦图层 / Map focus layer
const focusLayer = new SpeciesFocusLayer()
mapEngine.addLayer(focusLayer)
onBeforeUnmount(() => mapEngine.removeLayer(focusLayer.id))

watch(
  data,
  (d) => {
    if (!d) {
      return
    }
    focusLayer.setSpecies(d.taxon, d.points)
    // 视野适配到本市分布点 / Fit the view to the local points
    if (d.points.length > 0) {
      const lngs = d.points.map((p) => p.lng)
      const lats = d.points.map((p) => p.lat)
      mapEngine.fitBBox([Math.min(...lngs), Math.min(...lats), Math.max(...lngs), Math.max(...lats)])
    }
  },
  { immediate: true }
)
</script>

<template>
  <div>
    <QueryState :loading="isLoading" :error="error" :empty="!data" :rows="6" @retry="refetch()">
      <template v-if="data">
        <!-- 头图（渐进加载：LQIP + 进度反馈） / Hero image (progressive loading: LQIP + progress feedback) -->
        <div class="relative aspect-[16/9] overflow-hidden bg-surface-2">
          <ProgressiveImage
            v-if="data.photos[0]?.photoMediumUrl"
            :src="data.photos[0].photoMediumUrl"
            :placeholder="data.photos[0].photoSquareUrl"
            alt=""
            fit="cover"
          />
          <div class="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/70 via-black/10 to-transparent" />
          <RouterLink
            :to="{ path: '/species', query: route.query }"
            class="absolute top-3 left-3 rounded-chip bg-black/40 px-2 py-1 text-xs text-white backdrop-blur"
          >
            ← {{ t('species.title') }}
          </RouterLink>
          <div class="absolute right-5 bottom-3 left-5 text-white">
            <TaxonChip :code="data.taxon.iconicTaxon" class="!text-white" />
            <h1 class="font-display mt-1 text-2xl font-bold">{{ speciesName(data.taxon) }}</h1>
            <p class="text-sm text-white/80 italic">{{ data.taxon.scientificName }}</p>
            <!-- 头图版权署名（产品文档 §9；头图即照片墙首图，作者与协议同源） / Hero attribution (product doc §9; the hero is the photo wall's first photo, same author and license) -->
            <LicenseBadge
              v-if="data.photos[0]"
              :license="data.photos[0].license"
              :user-login="data.photos[0].userLogin"
              :dataset-title="data.photos[0].datasetTitle"
              class="!text-white/70 hover:!text-white"
            />
          </div>
        </div>

        <div class="space-y-6 px-5 py-4">
          <!-- 分类阶元面包屑 / Taxonomy rank breadcrumb -->
          <nav v-if="ancestors && ancestors.length > 0" class="text-[11px] leading-5 text-ink-3" aria-label="taxonomy">
            <template v-for="(a, i) in ancestors" :key="a.taxonId">
              <span v-if="i > 0" class="mx-0.5">›</span>
              <span :title="a.scientificName ?? ''">
                <span class="text-ink-2">{{ t(`taxonRank.${RANK_KEYS[a.rank] ?? 'unknown'}`) }}</span>
                {{ a.commonNameCn || (locale === 'en-US' ? a.commonNameEn : null) || a.scientificName }}
              </span>
            </template>
          </nav>

          <!-- 关键数字 / Key figures -->
          <div class="flex flex-wrap items-center gap-2 text-xs">
            <NTag size="small" round>{{ formatNumber(data.recordCount) }} {{ t('common.records') }}</NTag>
            <NTag size="small" round>{{ formatNumber(data.observerCount) }} {{ t('common.observers') }}</NTag>
            <NTag v-if="data.taxon.threatened" size="small" type="error" round>{{ t('species.threatened') }}</NTag>
            <span class="text-ink-3">{{ t('species.firstSeen') }} {{ data.firstObservedOn ?? '—' }}</span>
            <span class="text-ink-3">{{ t('species.lastSeen') }} {{ data.lastObservedOn ?? '—' }}</span>
          </div>

          <!-- 全年节奏 / Year-round rhythm -->
          <section>
            <div class="mb-1 flex items-center justify-between">
              <h2 class="font-display text-sm font-semibold text-ink">{{ t('species.rhythm') }}</h2>
              <NRadioGroup v-model:value="mode" size="small">
                <NRadioButton value="detail">{{ t('phenology.detailMode') }}</NRadioButton>
                <NRadioButton value="history">{{ t('phenology.historyMode') }}</NRadioButton>
              </NRadioGroup>
            </div>
            <template v-if="emergence">
              <p v-if="bestWindowText" class="mb-1 rounded-card bg-accent-soft px-2.5 py-1.5 text-xs text-accent">
                {{ bestWindowText }}
              </p>
              <EmergenceChart :data="emergence" :iconic-taxon="data.taxon.iconicTaxon" />
              <p class="text-[11px] text-ink-3">
                {{ t('phenology.sampleSize', { n: formatNumber(emergence.sampleSize) }) }} · {{ t('phenology.caliberNote') }}
              </p>
            </template>
          </section>

          <!-- 照片墙（瀑布流无限加载） / Photo wall (waterfall with infinite loading) -->
          <section>
            <h2 class="font-display mb-2 text-sm font-semibold text-ink">{{ t('species.photoWall') }}</h2>
            <PhotoWall v-if="city.currentId > 0" :taxon-id="taxonId!" :city-id="city.currentId" />
          </section>

          <!-- 相似物种 / Similar species -->
          <section v-if="similar && similar.length > 0">
            <h2 class="font-display mb-2 text-sm font-semibold text-ink">{{ t('species.similar') }}</h2>
            <div class="scroll-thin -mx-5 flex gap-3 overflow-x-auto px-5 pb-2">
              <RouterLink
                v-for="s in similar"
                :key="s.taxonId"
                :to="{ path: `/species/${s.taxonId}`, query: route.query }"
                class="w-28 shrink-0 overflow-hidden rounded-card border border-line bg-surface"
              >
                <div v-if="s.photoMediumUrl ?? s.photoSquareUrl" class="aspect-square w-full">
                  <ProgressiveImage
                    :src="(s.photoMediumUrl ?? s.photoSquareUrl)!"
                    :placeholder="s.photoSquareUrl"
                    :alt="speciesName(s)"
                  />
                </div>
                <div class="px-2 py-1.5">
                  <div class="truncate text-xs font-semibold text-ink">{{ speciesName(s) }}</div>
                  <div class="text-[10px] text-ink-3 tabular">{{ formatNumber(s.recordCount) }} {{ t('common.records') }}</div>
                  <LicenseBadge
                    v-if="(s.photoMediumUrl ?? s.photoSquareUrl) && s.photoLicense != null"
                    :license="s.photoLicense"
                    :user-login="s.photoUserLogin"
                    compact
                  />
                </div>
              </RouterLink>
            </div>
          </section>

          <NButton
            v-if="data.taxon.source === 1"
            block
            size="small"
            tag="a"
            :href="`https://www.inaturalist.org/taxa/${data.taxon.sourceTaxonId}`"
            target="_blank"
          >
            iNaturalist ↗
          </NButton>
        </div>
      </template>
    </QueryState>
  </div>
</template>
