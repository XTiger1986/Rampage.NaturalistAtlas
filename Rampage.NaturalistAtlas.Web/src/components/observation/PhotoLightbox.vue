<script setup lang="ts">
/**
 * 照片大图浮层（全局唯一，由 ui.lightboxId 驱动） / Photo lightbox (single global instance, driven by ui.lightboxId)
 *
 * 展示：大图（large 1024，方图模糊放大作 LQIP 占位）+ 物种三名 + 观察者/日期/鉴定次数 + 版权署名 + 源站链接； / Shows: large image (1024px, blurred square scaled up as LQIP placeholder) + species names + observer/date/identification count + attribution + source link;
 * 键盘 ←/→ 在打开时的列表上下文内切换上一张/下一张（无上下文时忽略）； / Keyboard ←/→ moves prev/next within the list context captured at open (ignored without context);
 * 受威胁物种 / 模糊坐标：显著标注"位置经保护性模糊"（产品文档 §4.1 坐标隐私）； / Threatened species / obscured coordinates: a prominent "location protected by obscuration" notice (product doc §4.1 coordinate privacy);
 * 操作：定位到地图、进入物种详情、举报（本地已举报的观察禁用按钮）。 / Actions: locate on map, open species detail, report (button disabled for locally reported observations).
 */
import { computed, ref, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEventListener } from '@vueuse/core'
import { NButton, NModal, NSpin, NTag } from 'naive-ui'
import { useUiStore } from '@/stores/ui'
import { useObservationDetail } from '@/composables/queries'
import TaxonChip from '@/components/common/TaxonChip.vue'
import LicenseBadge from '@/components/common/LicenseBadge.vue'
import ProgressiveImage from '@/components/common/ProgressiveImage.vue'
import { speciesName } from '@/utils/format'
import { isReported } from '@/utils/reported'
import { mapEngine } from '@/map/MapEngine'

const { t } = useI18n()
const ui = useUiStore()
const router = useRouter()
const route = useRoute()

const { data: obs, isLoading } = useObservationDetail(() => ui.lightboxId)

/** 该观察是否已在本机举报过（localStorage 非响应式，在打开大图/举报框关闭时重估） / Whether this observation was reported on this device (localStorage is non-reactive; re-evaluated when the lightbox/report dialog opens or closes) */
const reported = ref(false)
watch(
  [() => ui.lightboxId, () => ui.reportId],
  ([id]) => {
    reported.value = id !== null && isReported(id)
  },
  { immediate: true }
)

/** 键盘切换：← 上一张 / → 下一张（仅在有列表上下文时生效） / Keyboard navigation: ← prev / → next (only with a list context) */
useEventListener(window, 'keydown', (e) => {
  const ids = ui.lightboxIds
  if (ui.lightboxId === null || !ids) {
    return
  }
  const i = ids.indexOf(ui.lightboxId)
  if (i < 0) {
    return
  }
  if (e.key === 'ArrowLeft' && i > 0) {
    ui.lightboxId = ids[i - 1]
  } else if (e.key === 'ArrowRight' && i < ids.length - 1) {
    ui.lightboxId = ids[i + 1]
  }
})

/** 是否打开 / Whether open */
const show = computed({
  get: () => ui.lightboxId !== null,
  set: (v) => {
    if (!v) ui.closeObservation()
  }
})

/** 坐标是否经过模糊（受威胁物种或观察者设置模糊） / Whether coordinates are obscured (threatened species or the observer's privacy setting) */
const obscured = computed(() => !!obs.value && (obs.value.threatened || obs.value.geoprivacy !== 0))

/** 在地图上定位 / Locate on the map */
function locate(): void {
  if (obs.value) {
    mapEngine.flyTo([obs.value.lng, obs.value.lat], 16)
    ui.closeObservation()
  }
}

/** 进入物种详情 / Open species detail */
function gotoSpecies(): void {
  if (obs.value?.taxonId) {
    router.push({ path: `/species/${obs.value.taxonId}`, query: route.query })
    ui.closeObservation()
  }
}
</script>

<template>
  <NModal v-model:show="show" :auto-focus="false" transform-origin="center">
    <div class="glass-panel mx-3 flex max-h-[92dvh] w-[min(980px,96vw)] flex-col overflow-hidden md:flex-row">
      <NSpin v-if="isLoading || !obs" class="m-auto p-16" />
      <template v-else>
        <!-- 大图（渐进加载：LQIP 垫底 + 进度反馈，就绪后淡入）。 / Main image (progressive loading: LQIP backdrop + progress feedback, fade-in when ready).
             容器必须有确定高度（h-[..dvh] 而非 max-h）：ProgressiveImage 内部为 h-full 百分比链， / The container needs a definite height (h-[..dvh], not max-h): ProgressiveImage is an h-full percentage chain,
             父级高度不定时整块坍缩为 0 高——图片与加载指示器都是绝对定位会一起消失 / with an indefinite parent height the whole block collapses to 0 — the absolutely-positioned image and spinner vanish together -->
        <div class="relative flex min-h-[260px] flex-1 items-center justify-center bg-black">
          <div class="relative h-[60dvh] w-full md:h-[88dvh]">
            <ProgressiveImage
              :src="obs.photoLargeUrl ?? obs.photoMediumUrl ?? obs.photoSquareUrl ?? null"
              :placeholder="obs.photoSquareUrl"
              :alt="speciesName(obs)"
              fit="contain"
              class="!bg-transparent"
            />
          </div>
          <!-- 上下文导航提示 / Context navigation hint -->
          <div
            v-if="ui.lightboxIds && ui.lightboxIds.length > 1"
            class="absolute top-2 left-2 z-10 rounded-chip bg-black/45 px-2 py-0.5 text-[11px] text-white/85 backdrop-blur"
          >
            ← / →
          </div>
          <div class="absolute right-0 bottom-0 left-0 z-10 bg-gradient-to-t from-black/70 to-transparent px-3 pt-6 pb-2">
            <LicenseBadge class="!text-white/80" :user-login="obs.userLogin" :license="obs.license" :dataset-title="obs.datasetTitle" />
          </div>
        </div>
        <!-- 信息栏 / Info sidebar -->
        <aside class="scroll-thin flex w-full shrink-0 flex-col gap-3 overflow-y-auto p-5 md:w-80">
          <div class="flex items-start justify-between gap-2">
            <TaxonChip :code="obs.iconicTaxon" />
            <NButton quaternary circle size="small" :aria-label="t('common.close')" :title="t('common.close')" @click="ui.closeObservation()">✕</NButton>
          </div>
          <div>
            <h2 class="font-display text-xl font-bold text-ink">{{ speciesName(obs) }}</h2>
            <p v-if="obs.commonNameEn && obs.commonNameCn" class="text-sm text-ink-2">{{ obs.commonNameEn }}</p>
            <p v-if="obs.scientificName" class="text-sm text-ink-3 italic">{{ obs.scientificName }}</p>
          </div>
          <div class="flex flex-wrap gap-1.5">
            <NTag size="small" :type="obs.qualityGrade === 1 ? 'success' : 'default'" round>
              {{ obs.qualityGrade === 1 ? t('filter.research') : t('filter.needsId') }}
            </NTag>
            <NTag v-if="obs.threatened" size="small" type="error" round>{{ t('species.threatened') }}</NTag>
          </div>
          <p v-if="obscured" class="rounded-card border border-warn/40 bg-warn/10 px-3 py-2 text-xs text-ink-2">
            {{ t('map.obscured') }}
          </p>
          <dl class="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1.5 text-sm">
            <dt class="text-ink-3">{{ t('obs.observer') }}</dt>
            <dd class="truncate text-ink">{{ obs.userLogin ?? '—' }}</dd>
            <dt class="text-ink-3">{{ t('obs.observedOn') }}</dt>
            <dd class="text-ink tabular">{{ obs.observedOn }}</dd>
            <dt class="text-ink-3">{{ t('obs.identifications') }}</dt>
            <dd class="text-ink tabular">{{ obs.identificationsCount }}</dd>
            <template v-if="obs.datasetTitle">
              <dt class="text-ink-3">{{ t('obs.publisher') }}</dt>
              <dd class="truncate text-ink" :title="obs.datasetTitle">{{ obs.datasetTitle }}</dd>
            </template>
            <dt class="text-ink-3">{{ t('obs.source') }}</dt>
            <dd class="text-ink">{{ obs.source === 1 ? 'iNaturalist' : 'GBIF' }}</dd>
          </dl>
          <div class="mt-auto flex flex-wrap gap-2 pt-2">
            <NButton size="small" type="primary" @click="locate">{{ t('obs.locate') }}</NButton>
            <NButton v-if="obs.taxonId" size="small" @click="gotoSpecies">{{ t('common.viewDetail') }}</NButton>
            <NButton v-if="obs.sourceUrl" size="small" tag="a" :href="obs.sourceUrl" target="_blank">{{ t('obs.viewSource') }}</NButton>
            <NButton v-if="reported" size="small" quaternary disabled>{{ t('obs.reported') }}</NButton>
            <NButton v-else size="small" quaternary type="error" @click="ui.openReport(obs.id)">{{ t('obs.report') }}</NButton>
          </div>
        </aside>
      </template>
    </div>
  </NModal>
</template>
