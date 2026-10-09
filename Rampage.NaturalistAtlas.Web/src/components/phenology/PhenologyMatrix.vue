<script setup lang="ts">
/**
 * 物候矩阵：物种 × 月份热力表（产品文档 §8.10，US-07，设计文档 §5.7） / Phenology matrix: species × month heatmap table (product doc §8.10, US-07, design doc §5.7)
 *
 * 每行按"该物种自身最大月"归一化着色（行内归一），突出季节节奏而非绝对数量； / Each row is colored normalized against that species' own peak month (per-row normalization), highlighting seasonal rhythm over absolute counts;
 * 单元格颜色取物种所属类群色，透明度表示强度。点击行 → emit select(taxonId)。 / Cell color comes from the species' taxon color; opacity encodes intensity. Row click → emit select(taxonId).
 *
 * 排序：默认按总量降序；点击月份列头按该月记录数降序；"按峰值月排序"把季节节奏排成对角线。 / Sorting: total descending by default; clicking a month header sorts by that month's count; "sort by peak month" lines the seasonal rhythms up as a diagonal.
 * 虚拟滚动：useVirtualList 容器化（接口 top ≤100，虚拟化为上量预留；行高固定、名称截断不换行）。 / Virtual scrolling: useVirtualList containerization (API top ≤100; virtualization reserved for scale-up; fixed row height, names truncated, never wrapped).
 */
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useVirtualList } from '@vueuse/core'
import type { PhenologyRow } from '@/api/types'
import { taxonColor } from '@/theme/taxonPalette'
import { useThemeStore } from '@/stores/theme'
import { speciesName } from '@/utils/format'

type SortMode = 'total' | 'month' | 'peak'

const props = defineProps<{ rows: PhenologyRow[]; selectedId?: string | null }>()
defineEmits<{ select: [taxonId: string] }>()

const { t } = useI18n()
const themeStore = useThemeStore()
const months = Array.from({ length: 12 }, (_, i) => i + 1)
const currentMonth = new Date().getMonth() + 1

/** 行高（虚拟滚动固定值：text-xs 16px + py-1 上下共 8px + 余量） / Row height (fixed for virtual scrolling: text-xs 16px + 8px total py-1 + slack) */
const ROW_HEIGHT = 26

/** 排序状态 / Sort state */
const sortMode = ref<SortMode>('total')
const sortMonth = ref<number | null>(null)

/** 峰值月（该行最大记录数的月份） / Peak month (month with the row's max count) */
function peakMonth(r: PhenologyRow): number {
  let best = 0
  let bestVal = -1
  r.counts.forEach((c, i) => {
    if (c > bestVal) {
      bestVal = c
      best = i + 1
    }
  })
  return best
}

/** 排序后的行（峰值月排序：月升序 + 总量降序 tiebreak —— 季节节奏呈对角线） / Sorted rows (peak sort: month ascending + total descending tiebreak — seasonal rhythms form a diagonal) */
const sortedRows = computed(() => {
  const list = [...props.rows]
  if (sortMode.value === 'month' && sortMonth.value != null) {
    const m = sortMonth.value - 1
    list.sort((a, b) => b.counts[m] - a.counts[m] || b.total - a.total || a.taxonId.localeCompare(b.taxonId))
  } else if (sortMode.value === 'peak') {
    list.sort((a, b) => peakMonth(a) - peakMonth(b) || b.total - a.total || a.taxonId.localeCompare(b.taxonId))
  }
  return list
})

/** 预计算每行的归一化强度与颜色 / Precompute each row's normalized intensity and color */
const cells = computed(() =>
  sortedRows.value.map((r) => {
    const max = Math.max(1, ...r.counts)
    return { row: r, color: taxonColor(r.iconicTaxon, themeStore.theme), levels: r.counts.map((c) => c / max) }
  })
)

/** 虚拟滚动（行数少时行为与全量渲染一致） / Virtual scrolling (behaves like full rendering at small row counts) */
const { list, containerProps, wrapperProps } = useVirtualList(cells, { itemHeight: ROW_HEIGHT, overscan: 10 })

/** 列头点击：按该月降序；再点同月复位总量排序 / Header click: descending by that month; clicking the same month again restores total sorting */
function headerClick(m: number): void {
  if (sortMode.value === 'month' && sortMonth.value === m) {
    sortMode.value = 'total'
    sortMonth.value = null
  } else {
    sortMode.value = 'month'
    sortMonth.value = m
  }
}

/** 峰值月排序开关 / Toggle peak-month sorting */
function togglePeak(): void {
  if (sortMode.value === 'peak') {
    sortMode.value = 'total'
  } else {
    sortMode.value = 'peak'
    sortMonth.value = null
  }
}
</script>

<template>
  <div class="text-xs">
    <!-- 列头 / Column headers -->
    <div class="sticky top-0 z-10 grid grid-cols-[minmax(0,1fr)_repeat(12,20px)_auto] items-center gap-0.5 bg-glass px-5 py-1.5 backdrop-blur">
      <span class="flex items-center gap-1 text-ink-3">
        {{ t('phenology.speciesCol') }}
        <button
          type="button"
          class="rounded-chip px-1 py-0.5 transition-colors"
          :class="sortMode === 'peak' ? 'bg-accent-soft text-accent' : 'text-ink-3 hover:text-ink'"
          :title="t('phenology.sortPeak')"
          @click="togglePeak"
        >
          ↘{{ t('phenology.sortPeakShort') }}
        </button>
      </span>
      <button
        v-for="m in months"
        :key="m"
        type="button"
        class="text-center tabular transition-colors"
        :class="[
          sortMode === 'month' && sortMonth === m ? 'font-bold text-accent' : m === currentMonth ? 'font-bold text-ink-2' : 'text-ink-3 hover:text-ink'
        ]"
        :title="t('phenology.sortByMonth')"
        @click="headerClick(m)"
      >
        {{ m }}
      </button>
      <span />
    </div>
    <!-- 行（虚拟滚动容器） / Rows (virtual scroll container) -->
    <div v-bind="containerProps" class="max-h-[62dvh]">
      <div v-bind="wrapperProps">
        <button
          v-for="{ data: c, index } in list"
          :key="c.row.taxonId"
          type="button"
          class="grid w-full grid-cols-[minmax(0,1fr)_repeat(12,20px)_auto] items-center gap-0.5 px-5 text-left transition-colors hover:bg-surface-2"
          :style="{ height: `${ROW_HEIGHT}px` }"
          :class="selectedId === c.row.taxonId ? 'bg-accent-soft' : ''"
          :aria-rowindex="index + 1"
          @click="$emit('select', c.row.taxonId)"
        >
          <span class="truncate pr-2 text-ink" :title="c.row.scientificName ?? ''">{{ speciesName(c.row) }}</span>
          <span
            v-for="(lv, i) in c.levels"
            :key="i"
            class="h-4 rounded-[3px]"
            :style="{ background: c.color, opacity: lv === 0 ? 0.06 : 0.15 + lv * 0.85 }"
            :title="`${i + 1}${t('filter.monthUnit')}：${c.row.counts[i]}`"
          />
          <span class="w-8 shrink-0 text-right text-[10px] text-ink-3 tabular">{{ c.row.total }}</span>
        </button>
      </div>
    </div>
  </div>
</template>
