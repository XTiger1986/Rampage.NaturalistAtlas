/** useProgressiveImage 单元测试：流式进度 / 无 Content-Length / CORS 失败回退 / 切换中止 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick, ref } from 'vue'
import { useProgressiveImage } from '../useProgressiveImage'

/** 构造分块流响应 */
function streamResponse(chunks: number[], total: number | null, status = 200) {
  const stream = new ReadableStream<Uint8Array>({
    start(controller) {
      for (const size of chunks) {
        controller.enqueue(new Uint8Array(size).fill(1))
      }
      controller.close()
    }
  })
  const headers = new Headers()
  if (total != null) {
    headers.set('content-length', String(total))
  }
  return { ok: status >= 200 && status < 300, status, headers, body: stream, clone: () => streamResponse([], 0) }
}

describe('useProgressiveImage', () => {
  beforeEach(() => {
    // jsdom 未实现 createObjectURL
    URL.createObjectURL = vi.fn(() => 'blob:mock-url')
    URL.revokeObjectURL = vi.fn()
  })
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('流式下载：暴露 Content-Length 时进度递增至 1，产出 object URL', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(streamResponse([100, 200, 50], 350)))
    const { status, progress, loadedBytes, displayUrl } = useProgressiveImage(ref('https://x/a.jpg'))
    await vi.waitFor(() => expect(status.value).toBe('loaded'))
    expect(progress.value).toBe(1)
    expect(loadedBytes.value).toBe(350)
    expect(displayUrl.value).toBe('blob:mock-url')
  })

  it('未暴露 Content-Length：progress 恒为 null，字节数仍累计', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(streamResponse([1024, 2048], null)))
    const { status, progress, loadedBytes } = useProgressiveImage(ref('https://x/b.jpg'))
    await vi.waitFor(() => expect(status.value).toBe('loaded'))
    expect(progress.value).toBe(1) // 完成后置 1
    expect(loadedBytes.value).toBe(3072)
  })

  it('fetch 失败（如 CORS）：回退 direct 模式，展示原地址', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('CORS blocked')))
    const { status, displayUrl } = useProgressiveImage(ref('https://x/c.jpg'))
    await vi.waitFor(() => expect(status.value).toBe('direct'))
    expect(displayUrl.value).toBe('https://x/c.jpg')
  })

  it('HTTP 非 2xx：同样回退 direct（img 标签可自行失败）', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(streamResponse([], 0, 404)))
    const { status, displayUrl } = useProgressiveImage(ref('https://x/d.jpg'))
    await vi.waitFor(() => expect(status.value).toBe('direct'))
    expect(displayUrl.value).toBe('https://x/d.jpg')
  })

  it('src 变化：上一请求被中止且状态对新地址重置', async () => {
    let calls = 0
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async () => {
      calls++
      return streamResponse([10], 10)
    }))
    const src = ref('https://switch-host/e1.jpg')
    const { status, displayUrl } = useProgressiveImage(src)
    await nextTick()
    src.value = 'https://switch-host/e2.jpg'
    await vi.waitFor(() => expect(status.value).toBe('loaded'))
    expect(displayUrl.value).toBe('blob:mock-url')
    expect(calls).toBe(2)
  })

  it('src 为 null：保持 loading 且不发请求', async () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
    const { status, displayUrl } = useProgressiveImage(ref(null))
    await nextTick()
    expect(fetchMock).not.toHaveBeenCalled()
    expect(status.value).toBe('loading')
    expect(displayUrl.value).toBeNull()
  })

  it('主机被 CORS 拒过后：同主机后续图片直接走直载（不再 fetch）', async () => {
    const fetchMock = vi.fn().mockRejectedValueOnce(new TypeError('CORS')).mockResolvedValue(streamResponse([10], 10))
    vi.stubGlobal('fetch', fetchMock)
    const a = useProgressiveImage(ref('https://samesite-host/x1.jpg'))
    await vi.waitFor(() => expect(a.status.value).toBe('direct'))
    const b = useProgressiveImage(ref('https://samesite-host/x2.jpg'))
    await vi.waitFor(() => expect(b.status.value).toBe('direct'))
    expect(b.displayUrl.value).toBe('https://samesite-host/x2.jpg')
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})
