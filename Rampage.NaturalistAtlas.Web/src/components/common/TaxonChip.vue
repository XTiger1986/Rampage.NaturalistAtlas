<script setup lang="ts">
/**
 * 类群芯片：单字图标 + 名称，颜色取自类群调色板（随主题变化） / Taxon chip: single-glyph icon + name, colored from the taxon palette (theme-aware)
 * 用于图例筛选器、物种卡片角标、列表标签。 / Used in the legend filter, species card badges, and list labels.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { getTaxon } from '@/theme/taxonPalette'
import { useThemeStore } from '@/stores/theme'

const props = withDefaults(
  defineProps<{
    /** 类群代码 / Taxon code */
    code: number
    /** 是否点亮（未点亮时降低透明度） / Whether lit (dims when not lit) */
    active?: boolean
    /** 是否被显式选中（显示描边） / Whether explicitly selected (shows an outline) */
    selected?: boolean
    /** 紧凑模式：只显示单字图标 / Compact mode: glyph icon only */
    compact?: boolean
  }>(),
  { active: true, selected: false, compact: false }
)

defineEmits<{ click: [MouseEvent]; dblclick: [MouseEvent] }>()

const { t } = useI18n()
const themeStore = useThemeStore()

const taxon = computed(() => getTaxon(props.code))
const color = computed(() => taxon.value.colors[themeStore.theme])
</script>

<template>
  <button
    type="button"
    class="inline-flex shrink-0 items-center gap-1 rounded-chip border py-0.5 text-xs whitespace-nowrap transition-all select-none"
    :class="[compact ? 'px-0.5' : 'pr-2 pl-0.5', active ? 'opacity-100' : 'opacity-35 grayscale']"
    :style="{
      borderColor: selected ? color : 'transparent',
      background: selected ? `color-mix(in srgb, ${color} 14%, transparent)` : 'transparent'
    }"
    :aria-pressed="selected"
    :title="t(`taxon.${taxon.key}`)"
    @click="$emit('click', $event)"
    @dblclick="$emit('dblclick', $event)"
  >
    <span
      class="grid h-5 w-5 place-items-center rounded-chip text-[11px] font-bold text-white"
      :style="{ background: color, boxShadow: themeStore.theme === 'night' ? `0 0 8px ${color}` : 'none' }"
    >
      {{ taxon.glyph }}
    </span>
    <span v-if="!compact" class="text-ink-2">{{ t(`taxon.${taxon.key}`) }}</span>
  </button>
</template>
