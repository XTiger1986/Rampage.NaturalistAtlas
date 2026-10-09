<script setup lang="ts">
/**
 * 主题切换器：三套视觉方案（A 博物志·纸本 / B 夜观·萤火 / C 四季·光谱） / Theme switcher: three visual schemes (A Journal paper / B Night firefly / C Seasons spectrum)
 * 每项展示三色预览（背景 / 面板 / 强调）+ 名称 + 一句描述 / Each entry shows a three-color swatch (background / panel / accent) + name + a one-line description
 */
import { useI18n } from 'vue-i18n'
import { NButton, NPopover } from 'naive-ui'
import { THEMES } from '@/theme/themes'
import { useThemeStore } from '@/stores/theme'

const { t } = useI18n()
const themeStore = useThemeStore()
</script>

<template>
  <NPopover trigger="click" placement="bottom-end" :show-arrow="false" raw>
    <template #trigger>
      <NButton quaternary circle size="small" :title="t('theme.title')">
        <!-- 当前主题三色圆点 / Current theme's three-color dots -->
        <span class="flex -space-x-1">
          <span
            v-for="(c, i) in themeStore.meta.swatch"
            :key="i"
            class="h-3 w-3 rounded-full border border-line"
            :style="{ background: c }"
          />
        </span>
      </NButton>
    </template>
    <div class="glass-panel w-64 p-2">
      <div class="px-2 pt-1 pb-2 text-xs text-ink-3">{{ t('theme.title') }}</div>
      <button
        v-for="th in THEMES"
        :key="th.id"
        type="button"
        class="flex w-full items-center gap-3 rounded-card px-2 py-2 text-left transition-colors"
        :class="themeStore.theme === th.id ? 'bg-accent-soft' : 'hover:bg-surface-2'"
        @click="themeStore.setTheme(th.id)"
      >
        <span class="grid h-9 w-9 shrink-0 grid-cols-2 overflow-hidden rounded-chip border border-line">
          <span class="row-span-2" :style="{ background: th.swatch[0] }" />
          <span :style="{ background: th.swatch[1] }" />
          <span :style="{ background: th.swatch[2] }" />
        </span>
        <span class="min-w-0">
          <span class="block text-sm font-medium text-ink">{{ th.code }} · {{ t(`theme.${th.id}`) }}</span>
          <span class="block truncate text-xs text-ink-3">{{ t(`theme.${th.id}Desc`) }}</span>
        </span>
      </button>
    </div>
  </NPopover>
</template>
