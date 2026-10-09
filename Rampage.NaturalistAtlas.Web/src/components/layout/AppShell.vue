<script setup lang="ts">
/**
 * 应用外壳（设计文档 §4.3 布局） / App shell (design doc §4.3 layout)
 *
 * 层级（自下而上）： / Layer stack (bottom-up):
 *   MapCanvas（常驻全屏地图，z-map） / MapCanvas (persistent fullscreen map, z-map)
 *   MapHud（图例筛选 / 时间滑块 / 视图与图层工具，z-overlay） / MapHud (legend filter / time slider / view & layer tools, z-overlay)
 *   SidePanel（桌面）或 MobileSheet（移动）承载路由页面（z-panel） / SidePanel (desktop) or MobileSheet (mobile) hosting routed pages (z-panel)
 *   TopBar（z-topbar） / TopBar (z-topbar)
 *   PhotoLightbox / ReportDialog（模态，z-modal） / PhotoLightbox / ReportDialog (modals, z-modal)
 */
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import TopBar from './TopBar.vue'
import SidePanel from './SidePanel.vue'
import MobileSheet from './MobileSheet.vue'
import MapCanvas from '@/components/map/MapCanvas.vue'
import MapHud from '@/components/map/MapHud.vue'
import PhotoLightbox from '@/components/observation/PhotoLightbox.vue'
import ReportDialog from '@/components/observation/ReportDialog.vue'
import { useResponsive } from '@/composables/useResponsive'
import { useUrlSync } from '@/composables/useUrlSync'
import { useCityStore } from '@/stores/city'
import { useUiStore } from '@/stores/ui'

const { isDesktop } = useResponsive()
const city = useCityStore()
const ui = useUiStore()

// URL ⇄ store 同步（须在城市列表加载前执行，以便首屏 ?city= 生效） / URL ⇄ store sync (must run before the city list loads so first-paint ?city= takes effect)
useUrlSync()

onMounted(() => {
  city.load()
})
</script>

<template>
  <div class="relative h-full w-full overflow-hidden">
    <MapCanvas class="absolute inset-0" style="z-index: var(--z-map)" />

    <MapHud />

    <!-- 投屏模式隐藏内容面板，仅保留地图 + HUD（时间滑块承担退出） / Presentation mode hides content panels, keeping only map + HUD (the time slider handles exit) -->
    <SidePanel v-if="isDesktop && !ui.presenting">
      <RouterView v-slot="{ Component, route }">
        <Transition name="panel" mode="out-in">
          <component :is="Component" :key="route.path" />
        </Transition>
      </RouterView>
    </SidePanel>
    <MobileSheet v-else-if="!ui.presenting">
      <RouterView v-slot="{ Component, route }">
        <component :is="Component" :key="route.path" />
      </RouterView>
    </MobileSheet>

    <TopBar />

    <PhotoLightbox />
    <ReportDialog />
  </div>
</template>
