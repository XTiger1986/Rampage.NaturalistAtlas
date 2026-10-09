/**
 * 面板滚动位置记忆（路由切换保持列表位置） / Panel scroll-position memory (keeps list positions across route changes)
 *
 * 面板滚动容器在 SidePanel/MobileSheet 内、路由页面之外——切路由时容器不销毁但内容换掉，
 * 浏览器会把 scrollTop 钳到新内容高度（通常归零）。本 composable 按路由 path 记录 scrollTop，
 * 切换后下一帧恢复目标页的记录值（无记录则归零）。
 * / The panel scroll container lives in SidePanel/MobileSheet, outside the routed page — on route change the
 * container survives but its content is swapped, and the browser clamps scrollTop to the new content height
 * (usually zero). This composable records scrollTop per route path and restores the target page's remembered
 * value on the next frame after switching (zero if none).
 * 与 vue-query 缓存配合（列表数据在缓存期不重拉），实现"切回来列表还在原位"。
 * / Together with the vue-query cache (lists are not refetched while cached), the list is right where you left it when you come back.
 */
import { nextTick, watch } from 'vue'
import { useRoute } from 'vue-router'
import type { Ref } from 'vue'

export function useScrollMemory(scrollEl: Ref<HTMLElement | null>): void {
  const route = useRoute()
  const memory = new Map<string, number>()

  watch(
    () => route.path,
    (to, from) => {
      if (from && scrollEl.value) {
        memory.set(from, scrollEl.value.scrollTop)
      }
      void nextTick().then(() => {
        const el = scrollEl.value
        if (el) {
          el.scrollTop = memory.get(to) ?? 0
        }
      })
    }
  )
}
