<script setup lang="ts">
/**
 * 类群图例筛选器（图例即筛选器，产品文档 §8.2） / Taxon legend filter (the legend is the filter, product doc §8.2)
 *
 * - 单击：多选切换（未选任何类群 = 全部）； / - Click: multi-select toggle (no taxon selected = all);
 * - 双击 / 长按 500ms（触屏无双击习惯，设计文档 §5.2）：只看该类群（再次触发恢复全部）； / - Double-click / 500ms long-press (no double-tap convention on touch, design doc §5.2): solo that taxon (triggering again restores all);
 * - 右侧：鉴定状态切换（研究级 / 待鉴定）与"清除筛选"。 / - Right side: quality grade toggles (research grade / needs ID) and "clear filters".
 * 横向可滚动，移动端手指滑动浏览全部 13 个类群。 / Horizontally scrollable; on mobile a swipe browses all 13 taxa.
 */
import { useI18n } from 'vue-i18n'
import TaxonChip from '@/components/common/TaxonChip.vue'
import { LEGEND_TAXA } from '@/theme/taxonPalette'
import { useFilterStore } from '@/stores/filter'
import { useResponsive } from '@/composables/useResponsive'

const { t } = useI18n()
const filter = useFilterStore()
const { isPhone } = useResponsive()

/** 长按计时器与"吞掉长按后的 click"标记 / Long-press timer and the "swallow the click after long-press" flag */
let pressTimer: number | null = null
let suppressClick = false

/** 类群是否处于"点亮"状态（未选任何类群时全部点亮） / Whether a taxon is "lit" (all lit when none is selected) */
function isOn(code: number): boolean {
  return filter.iconicTaxa.length === 0 || filter.iconicTaxa.includes(code)
}

/** 单击切换（长按触发 solo 后到来的 click 被吞掉，避免状态又被切回） / Click toggle (the click arriving after a long-press solo is swallowed so the state isn't toggled back) */
function onChipClick(code: number): void {
  if (suppressClick) {
    suppressClick = false
    return
  }
  filter.toggleTaxon(code)
}

/** 长按开始：500ms 后触发"只看该类" / Long-press start: solo this taxon after 500ms */
function onPressStart(code: number): void {
  cancelPress()
  pressTimer = window.setTimeout(() => {
    suppressClick = true
    filter.soloTaxon(code)
  }, 500)
}

/** 长按结束/离开/取消：清除计时器 / Long-press end/leave/cancel: clear the timer */
function cancelPress(): void {
  if (pressTimer !== null) {
    clearTimeout(pressTimer)
    pressTimer = null
  }
}

/** 切换鉴定级别（至少保留一个） / Toggle quality grade (at least one stays on) */
function toggleQuality(q: number): void {
  const list = filter.quality
  if (list.includes(q)) {
    if (list.length > 1) {
      filter.quality = list.filter((x) => x !== q)
    }
  } else {
    filter.quality = [...list, q]
  }
}
</script>

<template>
  <div class="glass-panel flex max-w-full items-center gap-2 px-2 py-1.5">
    <div class="scroll-thin flex min-w-0 flex-1 items-center gap-1 overflow-x-auto">
      <!-- 手机断点仅显示字形（设计文档 §7.1）；触控命中区向外扩 8px / Phone breakpoint shows glyphs only (design doc §7.1); touch hit area expanded 8px outward -->
      <TaxonChip
        v-for="tx in LEGEND_TAXA"
        :key="tx.code"
        :code="tx.code"
        :active="isOn(tx.code)"
        :selected="filter.iconicTaxa.includes(tx.code)"
        :compact="isPhone"
        class="hit-expand"
        @click="onChipClick(tx.code)"
        @dblclick="filter.soloTaxon(tx.code)"
        @pointerdown="onPressStart(tx.code)"
        @pointerup="cancelPress"
        @pointerleave="cancelPress"
        @pointercancel="cancelPress"
      />
    </div>
    <span class="h-5 w-px shrink-0 bg-line" />
    <div class="flex shrink-0 items-center gap-1 text-xs">
      <button
        v-for="q in [1, 2]"
        :key="q"
        type="button"
        class="hit-expand rounded-chip px-2 py-1 transition-colors"
        :class="filter.quality.includes(q) ? 'bg-accent-soft text-accent' : 'text-ink-3'"
        @click="toggleQuality(q)"
      >
        {{ q === 1 ? t('filter.research') : t('filter.needsId') }}
      </button>
      <button
        v-if="filter.hasFilter"
        type="button"
        class="hit-expand px-1.5 py-1 text-ink-3 hover:text-ink"
        @click="filter.reset()"
      >
        {{ t('filter.clear') }}
      </button>
    </div>
  </div>
</template>
