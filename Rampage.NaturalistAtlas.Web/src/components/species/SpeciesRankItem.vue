<script setup lang="ts">
/**
 * 物种榜单行：名次 + 缩略图 + 三名 + 记录数/观察者数 + 相对占比条 / Species rank row: rank + thumbnail + three names + record/observer count + relative ratio bar
 */
import { computed } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { SpeciesRankItem, SpeciesRankOrder } from '@/api/types'
import TaxonChip from '@/components/common/TaxonChip.vue'
import ProgressiveImage from '@/components/common/ProgressiveImage.vue'
import LicenseBadge from '@/components/common/LicenseBadge.vue'
import { formatNumber, speciesName } from '@/utils/format'
import { taxonColor } from '@/theme/taxonPalette'
import { useThemeStore } from '@/stores/theme'

const props = defineProps<{
  item: SpeciesRankItem
  /** 名次（从 1 开始） / Rank (1-based) */
  rank: number
  /** 当前排序口径（决定主数字） / Current sort caliber (decides the main number) */
  order: SpeciesRankOrder
  /** 榜首数值（计算占比条长度） / Top-ranked value (sizes the ratio bar) */
  max: number
}>()

const { t } = useI18n()
const route = useRoute()
const themeStore = useThemeStore()

/** 主数值（稀缺榜按记录数展示；max=0 时隐藏占比条） / Main value (rarity ranking shows record count; max=0 hides the ratio bar) */
const value = computed(() =>
  props.order === 'observer' ? props.item.observerCount : props.item.recordCount
)
/** 占比条宽度（稀缺榜升序无"榜首基准"语义，父组件传 0 隐藏） / Ratio bar width (ascending rarity ranking has no "top baseline" semantics; the parent passes 0 to hide) */
const ratio = computed(() => (props.max > 0 ? Math.max(4, (value.value / props.max) * 100) : 0))
</script>

<template>
  <RouterLink
    :to="{ path: `/species/${item.taxonId}`, query: route.query }"
    class="group flex items-center gap-3 px-5 py-2.5 transition-colors hover:bg-surface-2"
  >
    <span class="font-display w-6 shrink-0 text-center text-sm font-bold tabular" :class="rank <= 3 ? 'text-accent' : 'text-ink-3'">
      {{ rank }}
    </span>
    <div v-if="item.photoSquareUrl" class="h-11 w-11 shrink-0 overflow-hidden rounded-chip">
      <ProgressiveImage :src="item.photoSquareUrl" :alt="speciesName(item)" dense />
    </div>
    <span v-else class="h-11 w-11 shrink-0 rounded-chip bg-surface-2" />
    <div class="min-w-0 flex-1">
      <div class="flex items-center gap-1.5">
        <span class="font-display truncate text-sm font-semibold text-ink group-hover:text-accent">{{ speciesName(item) }}</span>
        <TaxonChip :code="item.iconicTaxon" compact />
      </div>
      <p class="truncate text-[11px] text-ink-3 italic">{{ item.scientificName }}</p>
      <LicenseBadge v-if="item.photoSquareUrl && item.photoLicense != null" :license="item.photoLicense" compact />
      <div v-if="ratio > 0" class="mt-1 h-1 overflow-hidden rounded-full bg-surface-2">
        <div class="h-full rounded-full" :style="{ width: `${ratio}%`, background: taxonColor(item.iconicTaxon, themeStore.theme) }" />
      </div>
    </div>
    <div class="shrink-0 text-right">
      <div class="text-sm font-semibold text-ink tabular">{{ formatNumber(value) }}</div>
      <div class="text-[10px] text-ink-3">{{ order === 'observer' ? t('common.observers') : t('common.records') }}</div>
    </div>
  </RouterLink>
</template>
