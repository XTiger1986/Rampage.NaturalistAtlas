/**
 * 响应式断点 / Responsive breakpoints
 * - 桌面（≥1024px）：左侧浮动面板 + 顶部导航；/ Desktop (≥1024px): left floating panel + top navigation;
 * - 移动（<1024px）：底部抽屉面板 + 精简顶栏。/ Mobile (<1024px): bottom sheet panel + compact top bar.
 */
import { useBreakpoints } from '@vueuse/core'

const breakpoints = useBreakpoints({ md: 768, lg: 1024 })

export function useResponsive() {
  /** 是否桌面布局 / Whether the desktop layout is active */
  const isDesktop = breakpoints.greaterOrEqual('lg')
  /** 是否手机（<768） / Whether it is a phone (<768) */
  const isPhone = breakpoints.smaller('md')
  return { isDesktop, isPhone }
}
