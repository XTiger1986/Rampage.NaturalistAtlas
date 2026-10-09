import { computed, ref, watch } from 'vue'
import { defineStore } from 'pinia'
import { useLocalStorage } from '@vueuse/core'
import { DEFAULT_THEME, SEASON_ACCENTS, getThemeMeta, THEMES } from '@/theme/themes'
import type { ThemeId } from '@/theme/themes'
import { buildNaiveOverrides, naiveBaseTheme } from '@/theme/naiveTheme'

/**
 * 主题状态 / Theme store
 *
 * 职责：/ Responsibilities:
 * - 持久化用户选择的主题（localStorage: na.theme）；/ Persist the user's theme choice (localStorage: na.theme);
 * - 把主题写入 <html data-theme>，驱动 tokens.css 的 CSS 变量切换；
 *   / Write the theme to <html data-theme>, driving the CSS variable switch in tokens.css;
 * - 计算 Naive UI 主题与覆盖（供 App.vue 的 NConfigProvider 使用）；
 *   / Compute the Naive UI theme and overrides (consumed by NConfigProvider in App.vue);
 * - 四季·光谱主题：根据"当前季节月份"覆写 --c-accent（时间滑块播放时实时流转）。
 *   / Spectrum (four seasons) theme: overrides --c-accent by the current season month (flows live during time-slider playback).
 *
 * 地图底图样式的切换不在此处，由 map/MapEngine 订阅 theme 变化后自行处理。
 * / Basemap style switching is not here; map/MapEngine subscribes to theme changes and handles it itself.
 */
export const useThemeStore = defineStore('theme', () => {
  /** 当前主题（持久化） / Current theme (persisted) */
  const stored = useLocalStorage<ThemeId>('na.theme', DEFAULT_THEME)
  // 容错：旧版本或手工篡改的非法值回落默认主题 / Fault tolerance: invalid values from old versions or manual tampering fall back to the default theme
  if (!THEMES.some((t) => t.id === stored.value)) {
    stored.value = DEFAULT_THEME
  }
  const theme = computed<ThemeId>({
    get: () => stored.value,
    set: (v) => (stored.value = v)
  })

  /** 季节月份（1~12），默认当前月；时间滑块播放时由 filter store 同步 / Season month (1~12), defaults to the current month; synced from the filter store during time-slider playback */
  const seasonMonth = ref<number>(new Date().getMonth() + 1)

  /** 当前主题元数据 / Current theme metadata */
  const meta = computed(() => getThemeMeta(theme.value))

  /** 当前强调色覆写（仅四季·光谱主题生效） / Current accent color override (Spectrum theme only) */
  const accentOverride = computed<string | undefined>(() =>
    theme.value === 'spectrum' ? SEASON_ACCENTS[seasonMonth.value - 1] : undefined
  )

  /** Naive UI 基础主题 / Naive UI base theme */
  const naiveTheme = computed(() => naiveBaseTheme(theme.value))

  /** Naive UI 主题覆盖 / Naive UI theme overrides */
  const naiveOverrides = computed(() => buildNaiveOverrides(theme.value, accentOverride.value))

  /** 切换主题 / Switch the theme */
  function setTheme(id: ThemeId): void {
    theme.value = id
  }

  /** 设置季节月份（四季·光谱主题强调色随之流转） / Set the season month (the Spectrum accent flows accordingly) */
  function setSeasonMonth(month: number): void {
    seasonMonth.value = Math.min(12, Math.max(1, Math.round(month)))
  }

  // 同步到 DOM：data-theme 属性、强调色变量、浏览器 theme-color
  // / Sync to DOM: data-theme attribute, accent color variable, browser theme-color
  watch(
    [theme, accentOverride],
    ([id, accent]) => {
      const root = document.documentElement
      root.dataset.theme = id
      if (accent) {
        root.style.setProperty('--c-accent', accent)
      } else {
        root.style.removeProperty('--c-accent')
      }
      document.querySelector('meta[name="theme-color"]')?.setAttribute('content', getThemeMeta(id).metaColor)
    },
    { immediate: true }
  )

  return { theme, meta, seasonMonth, accentOverride, naiveTheme, naiveOverrides, setTheme, setSeasonMonth }
})
