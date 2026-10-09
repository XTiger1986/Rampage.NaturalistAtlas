/**
 * 远程图片渐进加载（设计文档 §5.10 增强） / Progressive loading of remote images (design doc §5.10 enhancement)
 *
 * 详情图片来自源站 CDN（static.inaturalist.org / inaturalist-open-data.s3…），跨境加载可能较慢。
 * / Detail images come from source-site CDNs (static.inaturalist.org / inaturalist-open-data.s3…), which can be slow cross-border.
 * 策略（逐级降级，永远有可用反馈）：/ Strategy (graceful degradation, always some usable feedback):
 * 1. fetch + ReadableStream 流式读取——源站允许 CORS 时可得真实进度：
 *    响应头暴露 Content-Length → 百分比；未暴露（如 S3 只暴露 ETag）→ 已下载 KB 计数；
 *    / 1. fetch + ReadableStream streaming — real progress when the source allows CORS:
 *    Content-Length exposed → percentage; not exposed (e.g. S3 exposes only ETag) → downloaded-KB counter;
 * 2. fetch 失败（无 CORS / 网络层拒绝）→ 回退 direct 模式：直接 <img> 加载（img 不受 CORS 限制），
 *    组件层展示不确定态指示器，img onload 后淡入；
 *    / 2. fetch fails (no CORS / network refusal) → fall back to direct mode: plain <img> loading (img is not
 *    CORS-bound); the component shows an indeterminate indicator and fades in on img onload;
 * 3. img 加载也失败 → error 态，可点击重试。/ 3. img loading also fails → error state, click to retry.
 */
import { onBeforeUnmount, ref, watch } from 'vue'
import { toValue } from 'vue'
import type { MaybeRefOrGetter } from 'vue'

export type ImageLoadStatus = 'loading' | 'loaded' | 'direct' | 'error'

/**
 * 会话级"无 CORS"主机记忆：该主机 fetch 一旦被 CORS 拒绝，后续同主机直接走直载，
 * 避免每张图都白付一次失败请求（浏览器控制台的 CORS 报错也随之收敛到首个）
 * Session-scoped memory of CORS-blocked hosts: after one rejection the host goes straight to
 * direct <img> mode, sparing a wasted request (and console noise) per image
 */
const corsBlockedHosts = new Set<string>()

/** 图片渐进加载状态 / Progressive image loading state */
export interface ProgressiveImageState {
  /** loading=流式下载中；loaded=object URL 就绪；direct=回退 <img> 直载；error=失败 / loading=streaming download; loaded=object URL ready; direct=fallback <img> load; error=failed */
  status: ImageLoadStatus
  /** 下载进度 0~1（仅响应头暴露 Content-Length 时有值，否则 null） / Download progress 0~1 (only when Content-Length is exposed; otherwise null) */
  progress: number | null
  /** 已下载字节数 / Bytes downloaded so far */
  loadedBytes: number
  /** 展示地址（loaded=object URL；direct=原地址；loading/error=null） / Display URL (loaded=object URL; direct=original URL; loading/error=null) */
  displayUrl: string | null
}

/**
 * 渐进加载一张远程图片；src 变化自动中止上一张并重新开始
 * / Progressively load one remote image; a src change aborts the previous load and restarts
 */
export function useProgressiveImage(src: MaybeRefOrGetter<string | null | undefined>) {
  const status = ref<ImageLoadStatus>('loading')
  const progress = ref<number | null>(null)
  const loadedBytes = ref(0)
  const displayUrl = ref<string | null>(null)

  let controller: AbortController | null = null
  let objectUrl: string | null = null

  function cleanup(): void {
    controller?.abort()
    controller = null
    if (objectUrl) {
      URL.revokeObjectURL(objectUrl)
      objectUrl = null
    }
  }

  async function start(url: string): Promise<void> {
    cleanup()
    status.value = 'loading'
    progress.value = null
    loadedBytes.value = 0
    displayUrl.value = null
    let host: string | null = null
    try {
      host = new URL(url).host
    } catch {
      host = null
    }
    if (host && corsBlockedHosts.has(host)) {
      // 该主机已知无 CORS：直接走直载，不再白付一次失败请求 / Host already known CORS-blocked: go straight to direct loading, skip the wasted request
      status.value = 'direct'
      displayUrl.value = url
      return
    }
    controller = new AbortController()
    const signal = controller.signal
    try {
      const res = await fetch(url, { signal })
      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`)
      }
      if (!res.body) {
        // 无流支持的环境：回退直载 / Environment without stream support: fall back to direct loading
        status.value = 'direct'
        displayUrl.value = url
        return
      }
      const totalHeader = res.headers.get('content-length')
      const total = totalHeader != null && /^\d+$/.test(totalHeader) ? Number(totalHeader) : null
      const reader = res.body.getReader()
      const chunks: BlobPart[] = []
      let received = 0
      for (;;) {
        const { done, value } = await reader.read()
        if (done) {
          break
        }
        if (signal.aborted) {
          return
        }
        chunks.push(value)
        received += value.byteLength
        loadedBytes.value = received
        if (total != null && total > 0) {
          progress.value = Math.min(0.99, received / total)
        }
      }
      const blob = new Blob(chunks, { type: res.headers.get('content-type') ?? undefined })
      objectUrl = URL.createObjectURL(blob)
      displayUrl.value = objectUrl
      progress.value = 1
      status.value = 'loaded'
    } catch {
      if (signal.aborted) {
        return
      }
      // CORS 拒绝 / 网络层失败：img 标签不受 CORS 限制，回退直载并记住该主机 / CORS refusal / network failure: <img> is not CORS-bound, fall back to direct loading and remember the host
      if (host) {
        corsBlockedHosts.add(host)
      }
      status.value = 'direct'
      displayUrl.value = url
      progress.value = null
    }
  }

  watch(
    () => toValue(src),
    (url) => {
      if (url) {
        void start(url)
      } else {
        cleanup()
        status.value = 'loading'
        progress.value = null
        loadedBytes.value = 0
        displayUrl.value = null
      }
    },
    { immediate: true }
  )

  onBeforeUnmount(cleanup)

  /** 失败后重试 / Retry after failure */
  function retry(): void {
    const url = toValue(src)
    if (url) {
      void start(url)
    }
  }

  return { status, progress, loadedBytes, displayUrl, retry }
}
