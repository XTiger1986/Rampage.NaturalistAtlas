<script setup lang="ts">
/**
 * 应用根组件 / Application root component
 * - NConfigProvider：注入 Naive UI 主题（随 theme store 切换）与中文语言包；
 *   / NConfigProvider: injects the Naive UI theme (switched by the theme store) and locale packs;
 * - Message/Dialog Provider：供全局提示使用；/ Message/Dialog providers: for global toasts and dialogs;
 * - AppShell：地图常驻 + 面板 + 浮层的整体布局。/ AppShell: overall layout of the persistent map + panels + overlays.
 */
import { computed } from 'vue'
import { NConfigProvider, NDialogProvider, NMessageProvider, zhCN, dateZhCN, enUS, dateEnUS } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import AppShell from '@/components/layout/AppShell.vue'
import { useThemeStore } from '@/stores/theme'

const themeStore = useThemeStore()
const { locale } = useI18n()

/** Naive UI 语言包随界面语言切换 / Naive UI locale pack follows the UI language */
const naiveLocale = computed(() => (locale.value === 'en-US' ? enUS : zhCN))
const naiveDateLocale = computed(() => (locale.value === 'en-US' ? dateEnUS : dateZhCN))
</script>

<template>
  <NConfigProvider
    :theme="themeStore.naiveTheme"
    :theme-overrides="themeStore.naiveOverrides"
    :locale="naiveLocale"
    :date-locale="naiveDateLocale"
    class="h-full"
  >
    <NMessageProvider placement="top">
      <NDialogProvider>
        <AppShell />
      </NDialogProvider>
    </NMessageProvider>
  </NConfigProvider>
</template>
