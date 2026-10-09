/**
 * 测试环境公共装配
 *
 * - mountWithI18n：用真实 zh-CN 语言包挂载组件（顺带守住 i18n key 完整性）；
 * - 其余按用例就地 vi.mock（maplibre / naive-ui useMessage 等）。
 */
import { mount, type ComponentMountingOptions } from '@vue/test-utils'
import type { Component } from 'vue'
import { createI18n } from 'vue-i18n'
import { createPinia, setActivePinia } from 'pinia'
import zhCN from '@/i18n/zh-CN'

/** 测试用 i18n 实例（真实 zh-CN） */
export function testI18n() {
  return createI18n({
    legacy: false,
    locale: 'zh-CN',
    fallbackLocale: 'zh-CN',
    messages: { 'zh-CN': zhCN }
  })
}

/** 挂载组件（自带 i18n 与全新 Pinia） */
export function mountWithI18n<T extends Component>(
  component: T,
  options: ComponentMountingOptions<T> = {}
) {
  setActivePinia(createPinia())
  return mount(component, {
    global: {
      plugins: [testI18n(), createPinia()],
      ...(options.global ?? {})
    },
    ...options
  })
}
