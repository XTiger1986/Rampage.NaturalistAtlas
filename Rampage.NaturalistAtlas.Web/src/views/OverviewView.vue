<script setup lang="ts">
/**
 * 城市总览（产品文档 §3.1 城市总览，P0） / City overview (product doc §3.1, P0)
 *
 * 指标卡 + 近 12 个月趋势迷你图 + 类群构成 + 本月新星（含首次达标日期）+ 活跃观察者； / Metric cards + last-12-month trend sparkline + taxon composition + this month's rising stars (with the date they first qualified) + active observers;
 * 可切换"仅研究级"口径（产品文档 §4.3）。 / Switchable "research-grade only" caliber (product doc §4.3).
 * 数据天级更新（后端缓存 10 分钟），页脚注明数据更新时间。 / Data is refreshed daily (backend cache 10 minutes); the footer states the data freshness.
 */
import { ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { NSwitch, NTooltip } from 'naive-ui'
import PanelHeader from '@/components/common/PanelHeader.vue'
import QueryState from '@/components/common/QueryState.vue'
import OverviewCards from '@/components/stats/OverviewCards.vue'
import TaxonDonut from '@/components/stats/TaxonDonut.vue'
import TrendSpark from '@/components/stats/TrendSpark.vue'
import ProgressiveImage from '@/components/common/ProgressiveImage.vue'
import LicenseBadge from '@/components/common/LicenseBadge.vue'
import { useOverview } from '@/composables/queries'
import { useCityStore } from '@/stores/city'
import { formatNumber, relativeTime, speciesName, cityName } from '@/utils/format'

const { t } = useI18n()
const route = useRoute()
const city = useCityStore()

/** 仅研究级口径 / Research-grade-only caliber */
const researchOnly = ref(false)
const { data, isLoading, error, refetch } = useOverview(() => city.currentId, researchOnly)
</script>

<template>
  <div>
    <PanelHeader :title="t('overview.title')" :subtitle="cityName(city.current)">
      <template #actions>
        <label class="flex items-center gap-1.5 text-xs text-ink-2">
          {{ t('overview.researchOnly') }}
          <NSwitch v-model:value="researchOnly" size="small" />
        </label>
      </template>
    </PanelHeader>

    <QueryState :loading="isLoading" :error="error" :empty="data?.totalObservation === 0" @retry="refetch()">
      <div v-if="data" class="space-y-6 px-5 py-4">
        <OverviewCards :data="data" />

        <section v-if="data.monthly12 && data.monthly12.length > 0">
          <h2 class="font-display mb-1 text-sm font-semibold text-ink">{{ t('overview.trend') }}</h2>
          <TrendSpark :data="data.monthly12" />
        </section>

        <section>
          <h2 class="font-display mb-1 text-sm font-semibold text-ink">{{ t('overview.composition') }}</h2>
          <TaxonDonut :data="data.iconicDistribution" />
        </section>

        <section>
          <h2 class="font-display mb-2 flex items-center gap-1 text-sm font-semibold text-ink">
            {{ t('overview.risingStars') }}
            <NTooltip>
              <template #trigger><span class="cursor-help text-ink-3">ⓘ</span></template>
              {{ t('overview.risingStarsTip') }}
            </NTooltip>
          </h2>
          <p v-if="data.risingStars.length === 0" class="text-xs text-ink-3">{{ t('common.empty') }}</p>
          <div class="scroll-thin -mx-5 flex gap-3 overflow-x-auto px-5 pb-2">
            <RouterLink
              v-for="s in data.risingStars"
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
                <div v-if="s.reachedOn" class="text-[10px] text-accent tabular">
                  {{ t('overview.reachedOn', { date: s.reachedOn.slice(5) }) }}
                </div>
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

        <section>
          <h2 class="font-display mb-2 text-sm font-semibold text-ink">{{ t('overview.topObservers') }}</h2>
          <ol class="space-y-1.5">
            <li v-for="(o, i) in data.topObservers" :key="o.userLogin" class="flex items-center gap-2 text-sm">
              <span class="w-5 text-center text-xs text-ink-3 tabular">{{ i + 1 }}</span>
              <span class="flex-1 truncate text-ink">{{ o.userLogin }}</span>
              <span class="text-xs text-ink-2 tabular">{{ formatNumber(o.count) }}</span>
            </li>
          </ol>
        </section>

        <p class="text-center text-[11px] text-ink-3">
          {{ t('overview.freshness', { t: relativeTime(data.newestSourceCreatedAt) }) }}
        </p>
      </div>
    </QueryState>
  </div>
</template>
