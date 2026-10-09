import { ref } from 'vue'
import { defineStore } from 'pinia'

/** 移动端底部抽屉档位：peek = 露出标题，half = 半屏，full = 全屏 / Mobile bottom sheet snap: peek = title visible, half = half screen, full = full screen */
export type SheetSnap = 'peek' | 'half' | 'full'

/**
 * 界面状态（与业务数据无关的开关） / UI state (toggles unrelated to business data)
 */
export const useUiStore = defineStore('ui', () => {
  /** 桌面端侧栏是否展开 / Whether the desktop side panel is expanded */
  const panelOpen = ref(true)
  /** 移动端底部抽屉档位 / Mobile bottom sheet snap position */
  const sheetSnap = ref<SheetSnap>('peek')
  /** 当前打开大图的观察 id（null = 未打开） / Observation id shown in the lightbox (null = closed) */
  const lightboxId = ref<string | null>(null)
  /** 打开大图时的列表上下文 id（供键盘 ←/→ 在同列表内切换；null = 无上下文） / List context ids for the lightbox (for ←/→ keyboard navigation within the same list; null = no context) */
  const lightboxIds = ref<string[] | null>(null)
  /** 当前举报的观察 id（null = 未打开举报框） / Observation id being reported (null = report dialog closed) */
  const reportId = ref<string | null>(null)
  /** 地图上高亮的观察 id（列表悬停联动） / Highlighted observation id on the map (linked to list hover) */
  const hoverId = ref<string | null>(null)
  /** 投屏模式（时间滑块全屏播放，隐藏面板放大字幕） / Presenting mode (fullscreen time-slider playback with hidden panels and enlarged captions) */
  const presenting = ref(false)

  /** 打开大图（可携带列表上下文，供大图内键盘切换上一张/下一张） / Open the lightbox (optionally with list context for previous/next keyboard navigation) */
  function openObservation(id: string, ids?: string[]): void {
    lightboxId.value = id
    lightboxIds.value = ids && ids.length > 0 ? ids : null
  }

  /** 关闭大图 / Close the lightbox */
  function closeObservation(): void {
    lightboxId.value = null
    lightboxIds.value = null
  }

  /** 打开举报框 / Open the report dialog */
  function openReport(id: string): void {
    reportId.value = id
  }

  /** 切换侧栏 / Toggle the side panel */
  function togglePanel(): void {
    panelOpen.value = !panelOpen.value
  }

  return { panelOpen, sheetSnap, lightboxId, lightboxIds, reportId, hoverId, presenting, openObservation, closeObservation, openReport, togglePanel }
})
