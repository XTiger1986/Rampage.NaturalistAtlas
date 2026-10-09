<script setup lang="ts">
/**
 * 顶部导航栏 / Top navigation bar
 * 桌面：品牌 + 导航 + 地点搜索 + 城市选择 + 主题/语言 / Desktop: brand + nav + geo search + city select + theme/language
 * 移动：品牌 + 城市选择 + 主题（导航移入底部抽屉的标签栏） / Mobile: brand + city select + theme (nav moves into the bottom sheet's tab bar)
 */
import { RouterLink, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { NButton, NTooltip } from 'naive-ui'
import CitySelect from './CitySelect.vue'
import ThemeSwitcher from './ThemeSwitcher.vue'
import GeoSearchBox from '@/components/map/GeoSearchBox.vue'
import { useResponsive } from '@/composables/useResponsive'
import { setLocale } from '@/i18n'

const { t, locale } = useI18n()
const route = useRoute()
const { isDesktop } = useResponsive()

/** 导航项 / Nav items */
const navItems = [
  { key: 'explore', to: '/' },
  { key: 'overview', to: '/overview' },
  { key: 'species', to: '/species' },
  { key: 'phenology', to: '/phenology' },
  { key: 'gaps', to: '/gaps' }
] as const

/** 切换中英文 / Toggle Chinese/English */
function toggleLocale(): void {
  setLocale(locale.value === 'zh-CN' ? 'en-US' : 'zh-CN')
}
</script>

<template>
  <header
    class="glass-panel pointer-events-auto absolute right-[max(0.75rem,env(safe-area-inset-right))] left-[max(0.75rem,env(safe-area-inset-left))] flex h-[var(--topbar-h)] items-center gap-3 px-3"
    :style="{ zIndex: 'var(--z-topbar)', top: 'max(0.75rem, env(safe-area-inset-top))' }"
  >
    <!-- 品牌 / Brand -->
    <RouterLink :to="{ path: '/', query: route.query }" class="flex shrink-0 items-center gap-2">
      <img src="/favicon.svg" alt="" class="h-7 w-7" />
      <div class="leading-tight">
        <div class="font-display text-[15px] font-bold text-ink">{{ t('app.name') }}</div>
        <div class="hidden text-[10px] tracking-wider text-ink-3 uppercase sm:block">{{ t('app.nameEn') }}</div>
      </div>
    </RouterLink>

    <!-- 导航（桌面） / Navigation (desktop) -->
    <nav v-if="isDesktop" class="ml-2 flex items-center gap-1">
      <RouterLink
        v-for="item in navItems"
        :key="item.key"
        :to="{ path: item.to, query: route.query }"
        class="rounded-chip px-3 py-1.5 text-sm transition-colors"
        :class="route.meta.nav === item.key ? 'bg-accent-soft font-medium text-accent' : 'text-ink-2 hover:text-ink'"
      >
        {{ t(`nav.${item.key}`) }}
      </RouterLink>
    </nav>

    <div class="ml-auto flex items-center gap-2">
      <GeoSearchBox v-if="isDesktop" class="w-64" />
      <CitySelect />
      <ThemeSwitcher />
      <!-- 语言切换：移动端也保留（紧凑按钮，§8 未限定桌面） / Language toggle: kept on mobile too (compact button; §8 doesn't restrict it to desktop) -->
      <NTooltip v-if="isDesktop">
        <template #trigger>
          <NButton quaternary size="small" @click="toggleLocale">{{ locale === 'zh-CN' ? 'EN' : '中' }}</NButton>
        </template>
        中文 / English
      </NTooltip>
      <NButton v-else quaternary size="tiny" class="!px-2" @click="toggleLocale">
        {{ locale === 'zh-CN' ? 'EN' : '中' }}
      </NButton>
    </div>
  </header>
</template>
