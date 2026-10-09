<script setup lang="ts">
/**
 * 照片卡片（列表/照片墙通用） / Photo card (shared by the list and photo wall)
 *
 * 内容（产品文档 §3.2 验收）：照片、物种名（中/英/学名）、观察者、拍摄时间、版权署名；点击出大图。 / Content (product doc §3.2 acceptance): photo, species name (zh/en/scientific), observer, date, attribution; click opens the lightbox.
 * 默认 ProgressiveImage 渐进加载（流式进度 + LQIP 垫底；无 CORS 图床自动降级直载+指示器）； / ProgressiveImage by default (streaming progress + LQIP backdrop; auto-degrades to direct load + spinner on hosts without CORS);
 * progressive=false 回退原生 lazy <img>。 / progressive=false falls back to a native lazy <img>.
 * 主题形态： / Theme variants:
 * - journal：标本签——白纸 + 墨线框 + 底部红色标签线，学名斜体宋体； / - journal: specimen label — white paper + ink-line frame + red label rule at the bottom, scientific name in italic serif;
 * - night：照片满铺 + 底部渐变遮罩文字； / - night: full-bleed photo + bottom gradient scrim over the text;
 * - spectrum：大圆角 + 柔和阴影。 / - spectrum: large corner radius + soft shadow.
 * 悬停时联动地图高亮对应照片卡（ui.hoverId）。 / Hovering links to the map and highlights the matching card (ui.hoverId).
 */
import { computed } from 'vue'
import type { ObservationCard } from '@/api/types'
import TaxonChip from '@/components/common/TaxonChip.vue'
import LicenseBadge from '@/components/common/LicenseBadge.vue'
import ProgressiveImage from '@/components/common/ProgressiveImage.vue'
import { speciesName } from '@/utils/format'
import { useUiStore } from '@/stores/ui'

const props = withDefaults(
  defineProps<{
    obs: ObservationCard
    /** 列表上下文 id（点击打开大图时携带，供大图内键盘 ←/→ 切换；可选） / List-context ids (carried into the lightbox for keyboard ←/→ navigation; optional) */
    ids?: string[]
    /** 渐进加载（进度反馈 + LQIP；默认开启，false 时用原生 lazy 图） / Progressive loading (progress feedback + LQIP; on by default, false uses a native lazy image) */
    progressive?: boolean
  }>(),
  { progressive: true }
)
const ui = useUiStore()

/** 主名称 / Primary name */
const title = computed(() => speciesName(props.obs))
</script>

<template>
  <article
    class="photo-card group cursor-pointer overflow-hidden"
    role="button"
    tabindex="0"
    :aria-label="title"
    @click="ui.openObservation(obs.id, ids)"
    @keydown.enter.prevent="ui.openObservation(obs.id, ids)"
    @keydown.space.prevent="ui.openObservation(obs.id, ids)"
    @mouseenter="ui.hoverId = obs.id"
    @mouseleave="ui.hoverId = null"
  >
    <div class="relative aspect-[4/3] overflow-hidden bg-surface-2">
      <ProgressiveImage
        v-if="progressive && (obs.photoMediumUrl || obs.photoSquareUrl)"
        :src="obs.photoMediumUrl ?? obs.photoSquareUrl"
        :placeholder="obs.photoSquareUrl"
        :alt="title"
        fit="cover"
        class="transition-transform duration-500 group-hover:scale-105"
      />
      <img
        v-else-if="obs.photoMediumUrl || obs.photoSquareUrl"
        :src="obs.photoMediumUrl ?? obs.photoSquareUrl!"
        :alt="title"
        loading="lazy"
        referrerpolicy="no-referrer"
        class="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
      />
      <TaxonChip class="!absolute top-1.5 left-1.5" :code="obs.iconicTaxon" compact />
      <span
        v-if="obs.qualityGrade === 1"
        class="absolute top-1.5 right-1.5 rounded-chip bg-black/45 px-1.5 py-0.5 text-[10px] text-white backdrop-blur"
      >
        RG
      </span>
    </div>
    <div class="photo-card__label space-y-0.5 px-2.5 py-2">
      <h3 class="font-display truncate text-sm font-semibold text-ink">{{ title }}</h3>
      <p v-if="obs.scientificName" class="truncate text-[11px] text-ink-3 italic">{{ obs.scientificName }}</p>
      <p class="flex items-center justify-between text-[11px] text-ink-3">
        <span class="truncate">{{ obs.userLogin ?? '—' }}</span>
        <span class="shrink-0 tabular">{{ obs.observedOn }}</span>
      </p>
      <LicenseBadge :user-login="obs.userLogin" :license="obs.license" :dataset-title="obs.datasetTitle" />
    </div>
  </article>
</template>

<style scoped>
.photo-card {
  background: var(--c-surface);
  border: var(--card-border);
  border-radius: var(--radius-md);
  box-shadow: var(--card-shadow);
  transition: transform var(--dur-fast) var(--ease-out);
}
.photo-card:hover {
  transform: translateY(-2px);
}
/* 纸本：标本签红线 / Paper theme: specimen-label red rule */
[data-theme='journal'] .photo-card__label {
  border-top: 1px solid var(--c-line-strong);
  box-shadow: inset 0 -2px 0 var(--card-label-line);
}
[data-theme='journal'] .photo-card p.italic {
  font-family: var(--font-serif);
}
</style>
