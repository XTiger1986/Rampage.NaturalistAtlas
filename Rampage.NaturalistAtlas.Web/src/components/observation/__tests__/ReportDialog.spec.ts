/** 举报对话框组件测试：原因校验 + 幂等键每次打开重生成（设计文档 §9）
 * NModal 内容经 Teleport 渲染到 document.body，断言基于 document.body。
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { VueQueryPlugin } from '@tanstack/vue-query'
import { testI18n } from '@/__tests__/setup'
import ReportDialog from '../ReportDialog.vue'
import { useUiStore } from '@/stores/ui'

// useMessage 需要消息提供者；测试中直接部分 mock naive-ui
vi.mock('naive-ui', async (importOriginal) => {
  const actual = await importOriginal<typeof import('naive-ui')>()
  return {
    ...actual,
    useMessage: () => ({ success: vi.fn(), error: vi.fn(), warning: vi.fn(), info: vi.fn() })
  }
})

/** 捕获 useSubmitReport 提交参数（重点：幂等键） */
const submitted: Array<{ input: { observationId: string; reason: number }; key: string }> = []
vi.mock('@/composables/queries', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/composables/queries')>()
  return {
    ...actual,
    useSubmitReport: () => ({
      mutateAsync: async (args: { input: { observationId: string; reason: number }; key: string }) => {
        submitted.push(args)
        return 'ok'
      },
      isPending: false
    })
  }
})

function mountDialog() {
  setActivePinia(createPinia())
  return mount(ReportDialog, {
    global: { plugins: [testI18n(), createPinia(), VueQueryPlugin] },
    attachTo: document.body
  })
}

function bodyButtons(): HTMLButtonElement[] {
  return [...document.body.querySelectorAll('button')]
}

function submitBtn(): HTMLButtonElement | undefined {
  return bodyButtons().find((b) => b.textContent === '提交')
}

async function openReport(id: string): Promise<void> {
  useUiStore().openReport(id)
  await vi.waitFor(() => expect(document.body.textContent).toContain('举报此观察'))
}

describe('ReportDialog', () => {
  let wrapper: ReturnType<typeof mountDialog>

  beforeEach(() => {
    wrapper = mountDialog()
    submitted.length = 0
  })
  afterEach(() => wrapper.unmount())

  it('默认原因（不当内容）可直接提交', async () => {
    await openReport('obs-1')
    expect(submitBtn()?.disabled).toBe(false)
    submitBtn()!.click()
    await vi.waitFor(() => expect(submitted).toHaveLength(1))
    expect(submitted[0]!.input.observationId).toBe('obs-1')
    expect(submitted[0]!.input.reason).toBe(1)
  })

  it('选"其他"必须填写说明才能提交', async () => {
    await openReport('obs-2')
    const radio = document.body.querySelector<HTMLInputElement>('input[type="radio"]')!
    radio.click()
    await vi.waitFor(() => expect(submitBtn()?.disabled).toBe(true))
  })

  it('每次打开生成新的幂等键', async () => {
    await openReport('obs-3')
    submitBtn()!.click()
    await vi.waitFor(() => expect(submitted).toHaveLength(1))
    useUiStore().reportId = null
    await vi.waitFor(() => expect(document.body.textContent).not.toContain('举报此观察'))
    await openReport('obs-3')
    submitBtn()!.click()
    await vi.waitFor(() => expect(submitted).toHaveLength(2))
    expect(submitted[0]!.key).not.toBe(submitted[1]!.key)
  })
})
