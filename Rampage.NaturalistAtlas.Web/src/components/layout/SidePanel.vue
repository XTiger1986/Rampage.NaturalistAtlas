<script setup lang="ts">
/**
 * 桌面端左侧浮动面板 / Desktop floating left panel
 * 承载路由页面；可收起为窄条，把地图完整让给用户（投屏讲解场景）。 / Hosts routed pages; collapses to a narrow strip to give the map back to the user (presentation scenarios).
 * 滚动容器声明为容器查询上下文（@container），页面内网格/列数按面板实际宽度断点。 / The scroll container is declared as a container-query context (@container) so in-page grids/column counts break on the panel's actual width.
 */
import { NButton } from 'naive-ui'
import { provide, ref } from 'vue'
import type { Ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useUiStore } from '@/stores/ui'
import { useScrollMemory } from '@/composables/useScrollMemory'

const ui = useUiStore()
const { t } = useI18n()

/** 内容滚动容器：提供给页面内无限滚动哨兵作 IntersectionObserver root（见 ExploreView） / Content scroll container: provided to in-page infinite-scroll sentinels as their IntersectionObserver root (see ExploreView) */
const scrollEl = ref<HTMLElement | null>(null)
provide<Ref<HTMLElement | null>>('panelScrollEl', scrollEl)
/** 路由切换保持列表滚动位置 / Preserve list scroll position across route changes */
useScrollMemory(scrollEl)
</script>

<template>
  <aside
    class="glass-panel pointer-events-auto absolute left-3 flex flex-col overflow-hidden transition-[width,opacity] duration-300"
    :style="{
      zIndex: 'var(--z-panel)',
      top: 'calc(var(--topbar-h) + 24px + env(safe-area-inset-top))',
      bottom: '12px',
      width: ui.panelOpen ? 'var(--panel-w)' : '44px'
    }"
  >
    <!-- 收起/展开 / Collapse / expand -->
    <NButton
      quaternary
      circle
      size="small"
      class="!absolute top-2 right-2 z-10"
      :title="ui.panelOpen ? t('common.collapse') : t('common.expand')"
      :aria-label="ui.panelOpen ? t('common.collapse') : t('common.expand')"
      @click="ui.togglePanel()"
    >
      <span class="text-ink-2">{{ ui.panelOpen ? '‹' : '›' }}</span>
    </NButton>
    <div v-show="ui.panelOpen" ref="scrollEl" class="@container scroll-thin min-h-0 flex-1 overflow-y-auto">
      <slot />
    </div>
  </aside>
</template>
