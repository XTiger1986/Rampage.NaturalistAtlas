<script setup lang="ts">
/**
 * 移动端底部抽屉（设计文档 §7.2） / Mobile bottom sheet (design doc §7.2)
 *
 * 三档：peek（露出标签栏与标题，地图占满）/ half（半屏）/ full（全屏阅读）。 / Three snaps: peek (tab bar + title visible, map fills the screen) / half (half-screen) / full (full-screen reading).
 * 实现：容器高度恒为 full 档，档位切换用 translateY(T)（GPU 合成，避免改 height 触发重排）； / Implementation: the container height is fixed at the full snap; snap switches use translateY(T) (GPU-composited, avoiding reflow from height changes);
 * 拖动把手实时换算 T，松手按"最近 100ms 位移速度"判定： / the drag handle maps to T in real time; on release, velocity over the last 100ms decides:
 * - |v| > 0.5px/ms：按甩动方向直接进/退一档（惯性）； / - |v| > 0.5px/ms: advance/retreat one snap in the fling direction (momentum);
 * - 否则：就近吸附。 / - otherwise: snap to the nearest stop.
 * 大图打开时自动收到 peek，关闭后恢复原档位；顶部为导航标签栏（移动端导航入口）。 / The sheet auto-collapses to peek while the lightbox is open and restores its snap on close; the top hosts the nav tab bar (mobile navigation entry).
 */
import { computed, onBeforeUnmount, onMounted, provide, ref, watch } from 'vue'
import type { Ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useUiStore } from '@/stores/ui'
import type { SheetSnap } from '@/stores/ui'
import GeoSearchBox from '@/components/map/GeoSearchBox.vue'
import { useScrollMemory } from '@/composables/useScrollMemory'

const { t } = useI18n()
const route = useRoute()
const ui = useUiStore()

/** 各档位高度（占视口高度比例） / Snap heights (fraction of viewport height) */
const SNAP_HEIGHT: Record<SheetSnap, number> = { peek: 0.18, half: 0.5, full: 0.88 }

/** 长按/甩动速度判定阈值（px/ms） / Fling velocity threshold for snap decisions (px/ms) */
const FLING_VELOCITY = 0.5
/** 速度采样窗口（ms） / Velocity sampling window (ms) */
const VELOCITY_WINDOW = 100

/** 内容滚动容器：提供给页面内无限滚动哨兵作 IntersectionObserver root（见 ExploreView） / Content scroll container: provided to in-page infinite-scroll sentinels as their IntersectionObserver root (see ExploreView) */
const scrollEl = ref<HTMLElement | null>(null)
provide<Ref<HTMLElement | null>>('panelScrollEl', scrollEl)
/** 路由切换保持列表滚动位置 / Preserve list scroll position across route changes */
useScrollMemory(scrollEl)

/** 档位顺序（甩动时进/退一档用） / Snap order (for stepping one snap on fling) */
const ORDER: SheetSnap[] = ['peek', 'half', 'full']

/** 视口高度（拖拽换算基准；dvh 与 innerHeight 在动态地址栏下有细微偏差，换算统一用 innerHeight） / Viewport height (drag math baseline; dvh and innerHeight differ slightly under the dynamic URL bar, so math uses innerHeight throughout) */
const vh = ref(window.innerHeight)
function onResize(): void {
  vh.value = window.innerHeight
}

/** 松手：速度判定 → 跳档或就近吸附（把手拖拽与内容区下拉共用同一收尾） / Release: velocity check → jump a snap or snap to nearest (handle drags and content pull-down share this finish path) */
function finishDrag(): void {
  if (dragOffset.value === null) {
    return
  }
  // 最近 100ms 采样窗口首尾差求速度：v > 0 = 上甩（展开），v < 0 = 下甩（收起） / Velocity from the first/last samples in the last-100ms window: v > 0 = fling up (expand), v < 0 = fling down (collapse)
  const first = samples[0]
  const last = samples[samples.length - 1]
  const v = first && last && last[0] > first[0] ? (first[1] - last[1]) / (last[0] - first[0]) : 0

  const idx = ORDER.indexOf(ui.sheetSnap)
  let next: SheetSnap
  if (Math.abs(v) > FLING_VELOCITY && idx >= 0) {
    next = ORDER[Math.max(0, Math.min(ORDER.length - 1, v > 0 ? idx + 1 : idx - 1))]
  } else {
    // 就近吸附：反解当前位移对应的档位比例（snapHeight / full） / Nearest snap: invert the current offset back to a snap ratio (snapHeight / full)
    const snapRatio = 1 - dragOffset.value / (SNAP_HEIGHT.full * vh.value)
    const nearest = (Object.entries(SNAP_HEIGHT) as [SheetSnap, number][]).reduce((a, b) =>
      Math.abs(b[1] - snapRatio) < Math.abs(a[1] - snapRatio) ? b : a
    )
    next = nearest[0]
  }
  ui.sheetSnap = next
  dragOffset.value = null
}

/** 内容区"滚到顶后继续下拉"接管抽屉（设计文档 §7.2 承诺）：触屏手势经非被动 touchmove / Content "pull down past scroll top" takes over the sheet (design doc §7.2 promise): touch gestures intercepted via non-passive touchmove
 *  拦截——指针事件方案在 touch-action:pan-y 下会被浏览器 pointercancel 抢走，无法中途接管 / — a pointer-events approach is taken over by browser pointercancel under touch-action:pan-y, blocking mid-gesture takeover */
let touchStartY = 0
let touchDragging = false

function onScrollTouchStart(e: TouchEvent): void {
  if (dragOffset.value !== null) {
    return
  }
  touchStartY = e.touches[0].clientY
  touchDragging = false
}

function onScrollTouchMove(e: TouchEvent): void {
  if (dragOffset.value !== null) {
    // 已接管：驱动抽屉位移并阻断原生滚动 / Taken over: drive the sheet offset and block native scrolling
    const y = e.touches[0].clientY
    const now = performance.now()
    samples.push([now, y])
    while (samples.length > 2 && now - samples[0][0] > VELOCITY_WINDOW) {
      samples.shift()
    }
    const max = (SNAP_HEIGHT.full - SNAP_HEIGHT.peek) * vh.value
    dragOffset.value = Math.max(0, Math.min(max, startT + (y - startY)))
    e.preventDefault()
    return
  }
  if (touchDragging) {
    return
  }
  const el = scrollEl.value
  if (el && el.scrollTop <= 0 && e.touches[0].clientY - touchStartY > 8) {
    // 滚动已在顶部且继续下拉：转为抽屉拖拽 / Already scrolled to top and still pulling down: switch to sheet drag
    touchDragging = true
    startY = touchStartY
    startT = offsetOf(ui.sheetSnap)
    dragOffset.value = startT
    samples = [[performance.now(), touchStartY]]
    e.preventDefault()
  }
}

function onScrollTouchEnd(): void {
  touchDragging = false
  finishDrag()
}

onMounted(() => {
  window.addEventListener('resize', onResize)
  const el = scrollEl.value
  if (el && 'ontouchstart' in window) {
    el.addEventListener('touchstart', onScrollTouchStart, { passive: true })
    el.addEventListener('touchmove', onScrollTouchMove, { passive: false })
    el.addEventListener('touchend', onScrollTouchEnd)
    el.addEventListener('touchcancel', onScrollTouchEnd)
  }
})
onBeforeUnmount(() => {
  window.removeEventListener('resize', onResize)
  const el = scrollEl.value
  if (el) {
    el.removeEventListener('touchstart', onScrollTouchStart)
    el.removeEventListener('touchmove', onScrollTouchMove)
    el.removeEventListener('touchend', onScrollTouchEnd)
    el.removeEventListener('touchcancel', onScrollTouchEnd)
  }
})

/** 某档位的 translateY（px）：full=0，peek≈0.70×vh / translateY (px) for a snap: full=0, peek≈0.70×vh */
function offsetOf(snap: SheetSnap): number {
  return (SNAP_HEIGHT.full - SNAP_HEIGHT[snap]) * vh.value
}

/** 当前档位的 translateY（拖拽中为临时值） / Current snap's translateY (temporary value while dragging) */
const dragOffset = ref<number | null>(null)
const translateY = computed(() => (dragOffset.value !== null ? dragOffset.value : offsetOf(ui.sheetSnap)))

/** 拖拽状态 / Drag state */
let startY = 0
let startT = 0
/** 速度采样（最近 100ms 的 [t, y]） / Velocity samples ([t, y] within the last 100ms) */
let samples: Array<[number, number]> = []

function onPointerDown(e: PointerEvent): void {
  startY = e.clientY
  startT = offsetOf(ui.sheetSnap)
  dragOffset.value = startT
  samples = [[performance.now(), e.clientY]]
  ;(e.target as HTMLElement).setPointerCapture(e.pointerId)
}

function onPointerMove(e: PointerEvent): void {
  if (dragOffset.value === null) {
    return
  }
  const now = performance.now()
  samples.push([now, e.clientY])
  // 只保留最近 100ms 的样本 / Keep only samples from the last 100ms
  while (samples.length > 2 && now - samples[0][0] > VELOCITY_WINDOW) {
    samples.shift()
  }
  const max = (SNAP_HEIGHT.full - SNAP_HEIGHT.peek) * vh.value
  dragOffset.value = Math.max(0, Math.min(max, startT + (e.clientY - startY)))
}

/** 松手（把手路径）：共用 finishDrag 收尾 / Release (handle path): shares finishDrag as the finish */
function onPointerUp(): void {
  finishDrag()
}

// 大图打开时收起抽屉到 peek，关闭后恢复（设计文档 §7.2） / Collapse the sheet to peek while the lightbox is open, restore on close (design doc §7.2)
let snapBeforeLightbox: SheetSnap | null = null
watch(
  () => ui.lightboxId,
  (id) => {
    if (id !== null && ui.sheetSnap !== 'peek') {
      snapBeforeLightbox = ui.sheetSnap
      ui.sheetSnap = 'peek'
    } else if (id === null && snapBeforeLightbox !== null) {
      ui.sheetSnap = snapBeforeLightbox
      snapBeforeLightbox = null
    }
  }
)

const navItems = ['explore', 'overview', 'species', 'phenology'] as const
const navPath: Record<(typeof navItems)[number], string> = {
  explore: '/',
  overview: '/overview',
  species: '/species',
  phenology: '/phenology'
}
</script>

<template>
  <section
    class="glass-panel pointer-events-auto absolute right-[max(0px,env(safe-area-inset-right))] bottom-0 left-[max(0px,env(safe-area-inset-left))] flex flex-col pb-[env(safe-area-inset-bottom)] !rounded-b-none"
    :class="dragOffset === null ? 'transition-transform duration-300 ease-out' : ''"
    :style="{
      zIndex: 'var(--z-panel)',
      height: `${SNAP_HEIGHT.full * 100}dvh`,
      transform: `translateY(${translateY}px)`
    }"
  >
    <!-- 拖动把手（视觉 24px，命中区向外扩 8px） / Drag handle (24px visual, hit area expanded 8px outward) -->
    <div
      class="hit-expand flex h-6 shrink-0 cursor-grab touch-none items-center justify-center"
      @pointerdown="onPointerDown"
      @pointermove="onPointerMove"
      @pointerup="onPointerUp"
      @pointercancel="onPointerUp"
    >
      <span class="h-1 w-10 rounded-full bg-line-strong" />
    </div>
    <!-- 标签栏 + 搜索（标签触控目标 ≥44px） / Tab bar + search (tab touch targets ≥44px) -->
    <div class="flex shrink-0 items-center gap-1 px-3 pb-2">
      <RouterLink
        v-for="k in navItems"
        :key="k"
        :to="{ path: navPath[k], query: route.query }"
        class="inline-flex min-h-11 items-center rounded-chip px-2.5 text-sm"
        :class="route.meta.nav === k ? 'bg-accent-soft font-medium text-accent' : 'text-ink-2'"
        @click="ui.sheetSnap = ui.sheetSnap === 'peek' ? 'half' : ui.sheetSnap"
      >
        {{ t(`nav.${k}`) }}
      </RouterLink>
    </div>
    <div class="shrink-0 px-3 pb-2">
      <GeoSearchBox />
    </div>
    <!-- 内容滚动：滚到顶后继续下拉会被接管为抽屉拖拽（onScrollTouchMove）；contain 防滚动链穿透； / Content scroll: pulling past scroll top is taken over as sheet drag (onScrollTouchMove); overscroll-contain stops scroll chaining;
         @container 供页面内网格按面板实际宽度断点 / @container lets in-page grids break on the panel's actual width -->
    <div ref="scrollEl" class="@container scroll-thin min-h-0 flex-1 overflow-y-auto overscroll-contain">
      <slot />
    </div>
  </section>
</template>
