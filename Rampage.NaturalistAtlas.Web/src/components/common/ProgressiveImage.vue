<script setup lang="ts">
/**
 * 渐进加载图片（详情图片通用，设计文档 §5.10 增强） / Progressive image loader (shared by detail images, design doc §5.10 enhancement)
 *
 * 分层反馈：LQIP 方图模糊垫底 → 加载覆盖层（百分比进度条 / 已下载 KB 计数 / 不确定态旋转指示） / Layered feedback: blurred square LQIP placeholder → loading overlay (percent progress bar / downloaded-KB counter / indeterminate spinner)
 * → 就绪后淡入；失败显示重试。进度来自 useProgressiveImage 的流式读取（无 CORS 时自动降级）。 / → fade-in when ready; failures show a retry button. Progress comes from useProgressiveImage's streaming reads (auto-degrades when CORS is unavailable).
 */
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useProgressiveImage } from '@/composables/useProgressiveImage'

const props = withDefaults(
  defineProps<{
    /** 大图地址 / Full-size image URL */
    src: string | null
    /** LQIP 占位（方图 URL，模糊放大垫底） / LQIP placeholder (square URL, blurred and scaled up as backdrop) */
    placeholder?: string | null
    alt?: string
    /** 填充方式 / Fit mode */
    fit?: 'cover' | 'contain'
    /** 稠密模式（小缩略图用：小号指示器，隐藏进度条与文案） / Dense mode (tiny thumbnails: smaller spinner, hides progress bar and text) */
    dense?: boolean
  }>(),
  { placeholder: null, alt: '', fit: 'cover', dense: false }
)

const emit = defineEmits<{ loaded: [] }>()

const { t } = useI18n()
const { status, progress, loadedBytes, displayUrl, retry } = useProgressiveImage(() => props.src)

/** <img> 是否就绪（direct 模式靠 onload；loaded 模式 object URL 也会触发一次 load） / Whether the <img> is ready (direct mode relies on onload; loaded mode fires load once more via the object URL) */
const imgReady = ref(false)
/** 直载失败（img onerror；重试由组件驱动） / Direct-load failure (img onerror; retry driven by the component) */
const failed = ref(false)
watch(
  () => props.src,
  () => {
    imgReady.value = false
    failed.value = false
  }
)

/** 展示中的 <img> 地址 / URL currently shown on the <img> */
const imgSrc = computed(() => displayUrl.value)

/** 是否显示加载覆盖层（图片未就绪且未失败） / Whether to show the loading overlay (image neither ready nor failed) */
const showOverlay = computed(() => !failed.value && !imgReady.value)

/** 失败态（img onerror 后显示重试） / Failure state (shows retry after img onerror) */
const showError = computed(() => failed.value)

/** 进度文案：有百分比用百分比；有字节数用 KB 计数；否则只剩旋转指示 / Progress label: percent when available; KB counter when bytes are known; spinner only otherwise */
const progressLabel = computed(() => {
  if (progress.value != null) {
    return `${Math.round(progress.value * 100)}%`
  }
  if (loadedBytes.value > 0) {
    return t('img.loadedKb', { kb: Math.max(1, Math.round(loadedBytes.value / 1024)) })
  }
  return ''
})

function onImgError(): void {
  if (status.value === 'direct' || status.value === 'loaded') {
    failed.value = true
  }
}

function onRetry(): void {
  imgReady.value = false
  failed.value = false
  retry()
}
</script>

<template>
  <div class="relative h-full w-full overflow-hidden bg-surface-2">
    <!-- LQIP 垫底 / LQIP backdrop -->
    <img
      v-if="placeholder"
      :src="placeholder"
      alt=""
      aria-hidden="true"
      referrerpolicy="no-referrer"
      class="absolute inset-0 h-full w-full scale-[1.25] object-cover opacity-60 blur-[16px]"
    />
    <!-- 正图 / Main image -->
    <img
      v-if="imgSrc"
      :src="imgSrc"
      :alt="alt"
      referrerpolicy="no-referrer"
      class="absolute inset-0 h-full w-full transition-opacity duration-[260ms] ease-out"
      :class="[fit === 'contain' ? 'object-contain' : 'object-cover', imgReady ? 'opacity-100' : 'opacity-0']"
      @load="imgReady = true; emit('loaded')"
      @error="onImgError"
    />
    <!-- 加载覆盖层 / Loading overlay -->
    <div
      v-if="showOverlay"
      class="absolute inset-0 flex flex-col items-center justify-center gap-2 bg-black/25 backdrop-blur-[2px]"
      :class="dense ? 'gap-1' : 'gap-2'"
    >
      <span
        class="animate-spin rounded-full border-white/30 border-t-white/90"
        :class="dense ? 'h-3.5 w-3.5 border' : 'h-7 w-7 border-2'"
      />
      <template v-if="!dense">
        <div class="h-1 w-24 overflow-hidden rounded-full bg-white/25">
          <div
            v-if="progress != null"
            class="h-full rounded-full bg-white/90 transition-[width] duration-150"
            :style="{ width: `${Math.round(progress * 100)}%` }"
          />
          <div v-else class="h-full w-1/3 animate-pulse rounded-full bg-white/70" />
        </div>
        <span v-if="progressLabel" class="text-[11px] text-white/85 tabular">{{ progressLabel }}</span>
      </template>
    </div>
    <!-- 失败态 / Failure state -->
    <div v-else-if="showError" class="absolute inset-0 flex flex-col items-center justify-center gap-1">
      <span class="text-[11px] text-ink-3">{{ t('img.failed') }}</span>
      <button type="button" class="rounded-chip bg-surface-2 px-2 py-1 text-[11px] text-ink-2 hover:text-accent" @click="onRetry">
        {{ t('img.retry') }}
      </button>
    </div>
  </div>
</template>
