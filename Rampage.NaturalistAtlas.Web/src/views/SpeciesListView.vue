<script setup lang="ts">
/**
 * 城市物种榜（US-04，P0） / City species leaderboard (US-04, P0)
 *
 * 排序三档：记录数 / 观察者数 / 稀缺榜（记录数升序，口径固定研究级）； / Three sort orders: by records / by observers / rare board (records ascending, caliber fixed to research grade);
 * 跟随全局组合筛选（类群 × 月份 × 鉴定状态）；支持物种名搜索与分页； / Follows the global combined filters (taxon × month × verification); supports species-name search and pagination;
 * CSV 导出当前筛选下前 500 条；列表头注明统计口径。 / CSV export of the current filter's top 500; the list header notes the statistical caliber.
 */
import { computed, ref, watch } from 'vue'
import { refDebounced } from '@vueuse/core'
import { useI18n } from 'vue-i18n'
import { NButton, NInput, NPagination, NRadioButton, NRadioGroup, useMessage } from 'naive-ui'
import PanelHeader from '@/components/common/PanelHeader.vue'
import QueryState from '@/components/common/QueryState.vue'
import SpeciesRankItem from '@/components/species/SpeciesRankItem.vue'
import { fetchSpeciesRankForExport, useSpeciesRank } from '@/composables/queries'
import { useFilterStore } from '@/stores/filter'
import { useCityStore } from '@/stores/city'
import { toCsv, downloadCsv } from '@/utils/csv'
import { formatNumber, cityName } from '@/utils/format'
import type { SpeciesRankItem as SpeciesRankRow, SpeciesRankOrder } from '@/api/types'

const PAGE_SIZE = 30

const { t } = useI18n()
const message = useMessage()
const filter = useFilterStore()
const city = useCityStore()

const order = ref<SpeciesRankOrder>('record')
const keyword = ref('')
const debouncedKeyword = refDebounced(keyword, 300)
const page = ref(1)
/** 导出进行中 / Export in progress */
const exporting = ref(false)

const query = computed(() => ({ ...filter.query, order: order.value, keyword: debouncedKeyword.value.trim() || undefined }))
// 条件变化回到第一页 / Reset to page 1 when conditions change
watch(query, () => (page.value = 1))

const { data, isLoading, error, refetch } = useSpeciesRank(query, page, PAGE_SIZE)

/** 榜首数值（占比条基准）：第一页首条，翻页后沿用；稀缺榜为升序，占比条无意义，传 0 隐藏 / Top value (share-bar baseline): page 1's first row, kept across pages; the rare board is ascending so the share bar is meaningless — pass 0 to hide it */
const max = computed(() => {
  if (order.value === 'rare') {
    return 0
  }
  const first = data.value?.dataList[0]
  return first ? (order.value === 'record' ? first.recordCount : first.observerCount) : 0
})

/** 统计口径说明（产品文档 §4.3） / Statistical caliber note (product doc §4.3) */
const caliber = computed(() => {
  const quality = filter.quality.includes(1) && filter.quality.includes(2)
    ? t('filter.research') + '+' + t('filter.needsId')
    : filter.quality.includes(1) ? t('filter.research') : t('filter.needsId')
  const q = order.value === 'rare' ? t('filter.research') : quality
  const m = filter.isFullYear
    ? t('phenology.allYear')
    : `${filter.monthRange[0]}${t('filter.monthUnit')}~${filter.monthRange[1]}${t('filter.monthUnit')}`
  return t('species.caliber', { city: cityName(city.current), quality: q, months: m })
})

/** CSV 导出：当前筛选下前 500 条（5 页 × 100 并行拉取，前端生成） / CSV export: top 500 under the current filter (5 pages × 100 fetched in parallel, generated on the front end) */
async function exportCsv(): Promise<void> {
  if (exporting.value) {
    return
  }
  exporting.value = true
  try {
    const rows = await fetchSpeciesRankForExport(query.value)
    const csv = toCsv<Record<string, string | number | null>>(
      [
        ['scientificName', t('species.csv.scientificName')],
        ['commonNameCn', t('species.csv.commonNameCn')],
        ['iconic', t('species.csv.iconic')],
        ['recordCount', t('species.csv.recordCount')],
        ['observerCount', t('species.csv.observerCount')],
        ['lastObservedOn', t('species.csv.lastObservedOn')]
      ],
      rows.map(toCsvRow)
    )
    const date = new Date().toISOString().slice(0, 10).replace(/-/g, '')
    downloadCsv(`${city.currentSlug}-species-${order.value}-${date}.csv`, csv)
  } catch (err) {
    // 拉取失败须给出反馈（否则按钮恢复原状、用户无从得知失败原因） / Fetch failures must surface feedback (otherwise the button just resets and the user never learns why)
    message.error(err instanceof Error ? err.message : t('common.requestFailed'))
  } finally {
    exporting.value = false
  }
}

/** 榜单行 → CSV 行（类群名本地化） / Leaderboard row → CSV row (taxon name localized) */
function toCsvRow(r: SpeciesRankRow): Record<string, string | number | null> {
  return {
    scientificName: r.scientificName,
    commonNameCn: r.commonNameCn,
    iconic: t(`taxon.${iconicKey(r.iconicTaxon)}`),
    recordCount: r.recordCount,
    observerCount: r.observerCount,
    lastObservedOn: r.lastObservedOn
  }
}

/** 类群代码 → i18n 键（与 taxonPalette TAXA 命名一致） / Taxon code → i18n key (names match taxonPalette's TAXA) */
function iconicKey(code: number): string {
  const names: Record<number, string> = {
    1: 'Birds', 2: 'Mammals', 3: 'Amphibians', 4: 'Reptiles', 5: 'RayFinnedFishes', 6: 'Insects',
    7: 'Arachnids', 8: 'Mollusks', 9: 'Plants', 10: 'Fungi', 11: 'OtherAnimals', 12: 'Protozoans',
    13: 'Chromista', 14: 'Unknown'
  }
  return names[code] ?? 'Unknown'
}

/** 总物种数 / Total species count */
const total = computed(() => data.value?.pager?.recordCount ?? null)
</script>

<template>
  <div>
    <PanelHeader :title="t('species.title')" :subtitle="cityName(city.current)">
      <template #actions>
        <NButton size="tiny" :loading="exporting" @click="exportCsv">{{ t('species.export') }}</NButton>
      </template>
    </PanelHeader>
    <div class="flex items-center gap-2 px-5 py-3">
      <NRadioGroup v-model:value="order" size="small">
        <NRadioButton value="record">{{ t('species.byRecord') }}</NRadioButton>
        <NRadioButton value="observer">{{ t('species.byObserver') }}</NRadioButton>
        <NRadioButton value="rare">{{ t('species.byRare') }}</NRadioButton>
      </NRadioGroup>
      <NInput v-model:value="keyword" size="small" clearable :placeholder="t('species.keyword')" class="flex-1" />
    </div>
    <p v-if="order === 'rare'" class="px-5 pb-1 text-[11px] text-accent">{{ t('species.rareTip') }}</p>
    <p class="px-5 pb-2 text-[11px] text-ink-3">
      {{ caliber }}
      <template v-if="total != null"> · {{ t('explore.inView', { n: formatNumber(total) }) }}</template>
    </p>
    <QueryState :loading="isLoading" :error="error" :empty="(data?.dataList.length ?? 0) === 0" @retry="refetch()">
      <div class="pb-2">
        <SpeciesRankItem
          v-for="(it, i) in data!.dataList"
          :key="it.taxonId"
          :item="it"
          :rank="(page - 1) * PAGE_SIZE + i + 1"
          :order="order"
          :max="max"
        />
      </div>
      <div v-if="(data?.pager?.totalPage ?? 1) > 1" class="flex justify-center px-5 pb-6">
        <NPagination v-model:page="page" :page-count="data!.pager!.totalPage ?? 1" size="small" :page-slot="6" />
      </div>
    </QueryState>
  </div>
</template>
