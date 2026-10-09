<script setup lang="ts">
/**
 * 时间滑块 ·「城市苏醒」（产品文档 §8.8，US-05，设计文档 §5.4） / Time slider · "City Awakening" (product doc §8.8, US-05, design doc §5.4)
 *
 * - 档位：月（1~12 双滑块区间，支持跨年）/ 旬（当年 1~36 双滑块区间，点位按 d1/d2 日期窗查询）； / - Units: month (1–12 dual-thumb range, year-wrap supported) / ten-day (1–36 dual-thumb range in-year; points queried by a d1/d2 date window);
 * - 播放：当前粒度下逐档推进单窗口（月 1→12 / 旬 1→36 循环），地图照片卡/热力随之刷新； / - Playback: advances a single window step by step in the current unit (month 1→12 / ten-day 1→36, looping); map photo cards/heatmap refresh along;
 * - 季节剧本：播放到某月时字幕淡入（i18n season.script[1..12]）； / - Season script: the caption fades in as playback reaches a month (i18n season.script[1..12]);
 * - 预取：步进前 prefetch 下一档点位，消除切换等待； / - Prefetch: prefetch the next step's points before stepping to remove switch lag;
 * - 投屏：全屏 + 隐藏内容面板 + 放大字幕（requestFullscreen 失败时静默降级为仅本地态）； / - Presentation: fullscreen + hidden content panel + enlarged captions (silently degrades to a local-only state when requestFullscreen fails);
 * - 四季·光谱主题强调色随当前月流转（theme store seasonMonth）。 / - Seasons-spectrum theme accent color follows the current month (theme store seasonMonth).
 */
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { NSlider } from 'naive-ui'
import { useQueryClient } from '@tanstack/vue-query'
import { observationApi } from '@/api/atlas'
import { useFilterStore } from '@/stores/filter'
import type { TimeUnit } from '@/stores/filter'
import { useThemeStore } from '@/stores/theme'
import { useUiStore } from '@/stores/ui'
import { tenDayMonth } from '@/utils/emergence'

const { t } = useI18n()
const filter = useFilterStore()
const themeStore = useThemeStore()
const ui = useUiStore()
const queryClient = useQueryClient()

/** 每档播放时长（ms） / Per-step playback duration (ms) */
const PLAY_INTERVAL = 1800

/** 是否播放中 / Whether playing */
const playing = ref(false)
let timer: number | undefined

/** 月档滑块值（双向绑定 filter.monthRange） / Month-unit slider value (two-way bound to filter.monthRange) */
const monthValue = computed<[number, number]>({
  get: () => filter.monthRange,
  set: (v) => filter.setMonthRange(Math.min(v[0], v[1]), Math.max(v[0], v[1]))
})

/** 旬档滑块值（双向绑定 filter.tenDayRange） / Ten-day-unit slider value (two-way bound to filter.tenDayRange) */
const tenDayValue = computed<[number, number]>({
  get: () => filter.tenDayRange,
  set: (v) => filter.setTenDayRange(v[0], v[1])
})

/** 月份刻度 / Month marks */
const monthMarks = Object.fromEntries(Array.from({ length: 12 }, (_, i) => [i + 1, `${i + 1}`]))
/** 旬档刻度：每月首旬标月号 / Ten-day marks: month number on each month's first ten-day */
const tenDayMarks = Object.fromEntries(Array.from({ length: 12 }, (_, i) => [i * 3 + 1, `${i + 1}`]))

/** 切换档位 / Switch the unit */
function setUnit(u: TimeUnit): void {
  stop()
  filter.setUnit(u)
}

/** 旬区间标签（如 "4月中旬 – 5月上旬"） / Ten-day range label (e.g. "mid Apr – early May") */
const tenDayLabel = computed(() => {
  const [f, to] = filter.tenDayRange
  if (filter.isFullTenDay) {
    return t('common.all')
  }
  const fmt = (idx: number): string => {
    const { month, period } = tenDayMonth(idx)
    const key = (['early', 'mid', 'late'] as const)[period]
    return t(`tenDay.${key}`, { m: month })
  }
  return f === to ? fmt(f) : `${fmt(f)} – ${fmt(to)}`
})

/** 当前播放/选中的"当前月"（剧本与季节色驱动） / The "current month" playing/selected (drives script text and season color) */
const currentMonth = computed(() => {
  if (filter.unit === 'tenDay') {
    return tenDayMonth(filter.tenDayRange[0]).month
  }
  return filter.isFullYear ? new Date().getMonth() + 1 : filter.monthRange[0]
})

/** 当前月的季节剧本文案 / Season script text for the current month */
const script = computed(() => t(`season.script.${currentMonth.value}`))

/** 预取下一档点位（queryKey 与 useObservationPoints 一致） / Prefetch the next step's points (queryKey matches useObservationPoints) */
function prefetch(u: TimeUnit, from: number, to: number): void {
  const next = filter.previewQuery(u, from, to)
  void queryClient.prefetchQuery({
    queryKey: ['observation', 'points', next],
    queryFn: () => observationApi.points(next)
  })
}

/** 播放/暂停 / Play / pause */
function togglePlay(): void {
  if (playing.value) {
    stop()
    return
  }
  playing.value = true
  if (filter.unit === 'month') {
    let m = filter.isFullYear ? 1 : filter.monthRange[0]
    const step = () => {
      const next = (m % 12) + 1
      prefetch('month', next, next)
      filter.setMonthRange(m, m)
      m = next
    }
    step()
    timer = window.setInterval(step, PLAY_INTERVAL)
  } else {
    let idx = filter.isFullTenDay ? 1 : filter.tenDayRange[0]
    const step = () => {
      const next = (idx % 36) + 1
      prefetch('tenDay', next, next)
      filter.setTenDayRange(idx, idx)
      idx = next
    }
    step()
    timer = window.setInterval(step, PLAY_INTERVAL)
  }
}

/** 停止播放 / Stop playback */
function stop(): void {
  playing.value = false
  window.clearInterval(timer)
}

/** 投屏模式：全屏 + 隐藏面板（失败静默降级） / Presentation mode: fullscreen + hidden panel (silent fallback on failure) */
async function togglePresenting(): Promise<void> {
  if (ui.presenting) {
    ui.presenting = false
    if (document.fullscreenElement) {
      await document.exitFullscreen().catch(() => undefined)
    }
    return
  }
  ui.presenting = true
  await document.documentElement.requestFullscreen?.().catch(() => undefined)
}

// 季节色：以当前月驱动四季·光谱主题强调色 / Season color: the current month drives the Seasons-spectrum theme accent
watch(currentMonth, (m) => themeStore.setSeasonMonth(m), { immediate: true })

onBeforeUnmount(() => {
  stop()
  if (ui.presenting) {
    ui.presenting = false
  }
})
</script>

<template>
  <div
    class="glass-panel px-3 py-2"
    :class="ui.presenting ? 'mx-auto w-full max-w-4xl px-6 py-4' : 'flex items-center gap-3'"
  >
    <div :class="ui.presenting ? 'flex items-center gap-3' : 'contents'">
      <button
        type="button"
        class="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-accent text-accent-ink transition-transform hover:scale-105"
        :title="playing ? t('filter.pause') : t('filter.play')"
        :aria-label="playing ? t('filter.pause') : t('filter.play')"
        @click="togglePlay"
      >
        <!-- 播放/暂停图标 / Play/pause icon -->
        <svg v-if="!playing" viewBox="0 0 24 24" class="ml-0.5 h-4 w-4 fill-current"><path d="M8 5v14l11-7z" /></svg>
        <svg v-else viewBox="0 0 24 24" class="h-4 w-4 fill-current"><path d="M6 5h4v14H6zM14 5h4v14h-4z" /></svg>
      </button>

      <!-- 档位：月 | 旬 / Unit: month | ten-day -->
      <div class="flex shrink-0 items-center rounded-chip bg-surface-2 p-0.5 text-[11px]">
        <button
          v-for="u in ['month', 'tenDay'] as TimeUnit[]"
          :key="u"
          type="button"
          class="rounded-chip px-1.5 py-0.5 transition-colors"
          :class="filter.unit === u ? 'bg-surface font-medium text-accent' : 'text-ink-3'"
          @click="setUnit(u)"
        >
          {{ t(`filter.unit_${u}`) }}
        </button>
      </div>

      <!-- 滑块 / Slider -->
      <div class="min-w-0 flex-1 px-1">
        <NSlider
          v-if="filter.unit === 'month'"
          v-model:value="monthValue"
          range
          :min="1"
          :max="12"
          :step="1"
          :marks="monthMarks"
          :tooltip="false"
          @dragstart="stop"
        />
        <NSlider
          v-else
          v-model:value="tenDayValue"
          range
          :min="1"
          :max="36"
          :step="1"
          :marks="tenDayMarks"
          :tooltip="false"
          @dragstart="stop"
        />
      </div>

      <!-- 区间标签 / Range label -->
      <div class="font-display w-16 shrink-0 text-right text-sm text-ink tabular sm:w-24">
        <template v-if="filter.unit === 'month'">
          <template v-if="filter.isFullYear">{{ t('common.all') }}</template>
          <template v-else-if="filter.monthRange[0] === filter.monthRange[1]">
            {{ filter.monthRange[0] }}{{ t('filter.monthUnit') }}
          </template>
          <template v-else>{{ filter.monthRange[0] }}–{{ filter.monthRange[1] }}{{ t('filter.monthUnit') }}</template>
        </template>
        <template v-else>
          <span class="text-xs">{{ tenDayLabel }}</span>
        </template>
      </div>

      <!-- 投屏 / Presentation -->
      <button
        type="button"
        class="grid h-8 w-8 shrink-0 place-items-center rounded-full text-ink-2 transition-colors hover:text-accent"
        :title="ui.presenting ? t('filter.presentExit') : t('filter.present')"
        :aria-label="ui.presenting ? t('filter.presentExit') : t('filter.present')"
        @click="togglePresenting"
      >
        <svg viewBox="0 0 24 24" class="h-4 w-4 fill-current">
          <path v-if="!ui.presenting" d="M3 3h8v2H5v6H3V3zm10 0h8v8h-2V5h-6V3zM3 13h2v6h6v2H3v-8zm16 0h2v8h-8v-2h6v-6z" />
          <path v-else d="M9 3v2H5v4H3V3h6zm6 0h6v6h-2V5h-4V3zM3 13h2v4h4v2H3v-6zm16 0h2v6h-6v-2h4v-4z" />
        </svg>
      </button>
    </div>

    <!-- 季节剧本字幕（播放中显示；投屏放大） / Season script caption (visible while playing; enlarged in presentation mode) -->
    <Transition name="panel">
      <p
        v-if="playing"
        class="font-display mt-2 text-center text-ink-2"
        :class="ui.presenting ? 'text-2xl' : 'text-xs'"
      >
        {{ script }}
      </p>
    </Transition>
  </div>
</template>
