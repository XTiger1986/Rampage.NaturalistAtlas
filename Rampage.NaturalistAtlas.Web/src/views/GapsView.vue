<script setup lang="ts">
/**
 * 集邮缺口（US-08，设计文档 §5.9） / Collection gaps (US-08, design doc §5.9)
 *
 * 周边已记录、本市未记录的物种清单（数据来自采集端每周全量刷新的 city_wanted_taxon 缓存， / Species recorded nearby but not yet in this city (data from the city_wanted_taxon cache that the collector fully refreshes weekly;
 * 请求链路不直连源站）：代表图 + 名称 + 类群芯片 + "周边已记录 N 次" + 去 iNaturalist 记录外链； / the request path never hits the upstream directly): representative photo + name + taxon chip + "recorded N times nearby" + an outlink to record it on iNaturalist;
 * 类群筛选与地图图例联动（filter.iconicTaxa）；空态鼓励"成为本城第一位记录者"。 / taxon filtering links with the map legend (filter.iconicTaxa); the empty state encourages you to "be this city's first recorder".
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import PanelHeader from '@/components/common/PanelHeader.vue'
import QueryState from '@/components/common/QueryState.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import TaxonChip from '@/components/common/TaxonChip.vue'
import ProgressiveImage from '@/components/common/ProgressiveImage.vue'
import LicenseBadge from '@/components/common/LicenseBadge.vue'
import { useGaps } from '@/composables/queries'
import { useFilterStore } from '@/stores/filter'
import { useCityStore } from '@/stores/city'
import { formatNumber, speciesName, cityName } from '@/utils/format'

const { t } = useI18n()
const filter = useFilterStore()
const city = useCityStore()

const { data, isLoading, error, refetch } = useGaps(() => city.currentId, () => filter.iconicTaxa)

const items = computed(() => data.value ?? [])
const filtered = computed(() =>
  filter.iconicTaxa.length === 0 ? items.value : items.value.filter((g) => filter.iconicTaxa.includes(g.iconicTaxon))
)
</script>

<template>
  <div>
    <PanelHeader :title="t('gaps.title')" :subtitle="cityName(city.current)" />
    <p class="px-5 pt-3 pb-1 text-[11px] leading-relaxed text-ink-3">{{ t('gaps.desc') }}</p>
    <QueryState :loading="isLoading" :error="error" :empty="filtered.length === 0" @retry="refetch()">
      <template #empty>
        <EmptyState :title="t('gaps.emptyTitle')" :desc="t('gaps.emptyDesc')" />
      </template>
      <ul class="space-y-2 px-5 pb-6">
        <li
          v-for="g in filtered"
          :key="g.sourceTaxonId"
          class="flex items-center gap-3 rounded-card border border-line bg-surface p-2"
        >
          <div v-if="g.photoSquareUrl" class="h-12 w-12 shrink-0 overflow-hidden rounded-chip">
            <ProgressiveImage :src="g.photoSquareUrl" :alt="speciesName(g)" dense />
          </div>
          <span v-else class="h-12 w-12 shrink-0 rounded-chip bg-surface-2" />
          <div class="min-w-0 flex-1">
            <div class="flex items-center gap-1.5">
              <span class="truncate text-sm font-semibold text-ink">{{ speciesName(g) }}</span>
              <TaxonChip :code="g.iconicTaxon" compact />
            </div>
            <p class="truncate text-[11px] text-ink-3 italic">{{ g.scientificName }}</p>
            <p class="text-[11px] text-ink-3">
              <template v-if="g.nearbyCount > 0">{{ t('gaps.nearby', { n: formatNumber(g.nearbyCount) }) }}</template>
              <template v-else>{{ t('gaps.nearbyUnknown') }}</template>
            </p>
            <!-- 缺口代表图版权署名（源站 wanted 照片无作者名，仅协议） / License attribution for the gap's representative photo (upstream wanted photos carry no author name, license only) -->
            <LicenseBadge v-if="g.photoSquareUrl && g.photoLicense != null" :license="g.photoLicense" compact />
          </div>
          <a
            :href="`https://www.inaturalist.org/taxa/${g.sourceTaxonId}`"
            target="_blank"
            rel="noopener"
            class="shrink-0 rounded-chip bg-accent-soft px-2 py-1 text-[11px] text-accent transition-colors hover:bg-accent hover:text-accent-ink"
          >
            {{ t('gaps.record') }} ↗
          </a>
        </li>
      </ul>
      <p class="px-5 pb-6 text-center text-[11px] text-ink-3">{{ t('gaps.source') }}</p>
    </QueryState>
  </div>
</template>
