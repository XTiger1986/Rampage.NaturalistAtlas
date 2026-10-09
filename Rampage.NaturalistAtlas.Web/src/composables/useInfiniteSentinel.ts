/**
 * 无限滚动哨兵（ExploreView / PhotoWall 共用约定） / Infinite-scroll sentinel (shared convention of ExploreView / PhotoWall)
 *
 * 面板滚动容器经 provide/inject（'panelScrollEl'）由 SidePanel/MobileSheet 提供，
 * 哨兵进入滚动区前沿 rootMargin 即触发取下一页；fetcher 自带 hasNextPage/去重判断。
 * / The panel scroll container is provided via provide/inject ('panelScrollEl') by SidePanel/MobileSheet;
 * the next page is fetched once the sentinel enters the leading rootMargin; the fetcher itself guards
 * hasNextPage/dedup.
 */
import { computed, inject, ref } from 'vue'
import type { Ref } from 'vue'
import { useIntersectionObserver } from '@vueuse/core'

export function useInfiniteSentinel(sentinel: Ref<HTMLElement | null>, fetchNext: () => void): void {
  const panelScrollEl = inject<Ref<HTMLElement | null>>('panelScrollEl', ref(null))
  useIntersectionObserver(
    sentinel,
    ([entry]) => {
      if (entry?.isIntersecting) {
        fetchNext()
      }
    },
    { root: computed(() => panelScrollEl.value), rootMargin: '200px' }
  )
}
