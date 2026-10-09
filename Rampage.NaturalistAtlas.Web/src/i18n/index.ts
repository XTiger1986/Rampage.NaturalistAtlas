/**
 * 国际化（vue-i18n，Composition API 模式） / Internationalization (vue-i18n, Composition API mode)
 *
 * - 默认中文；语言选择持久化到 localStorage（na.locale）；
 *   / Chinese by default; the language choice persists to localStorage (na.locale);
 * - 切换语言时同步请求头 Accept-Language（后端本地化）与 <html lang>。
 *   / Switching the language also updates the Accept-Language request header (backend localization) and <html lang>.
 */
import { createI18n } from 'vue-i18n'
import zhCN from './zh-CN'
import enUS from './en-US'
import { setRequestLocale } from '@/api/http'

/** 支持的语言 / Supported languages */
export type AppLocale = 'zh-CN' | 'en-US'

/** 读取持久化语言 / Load the persisted language */
function loadLocale(): AppLocale {
  const saved = localStorage.getItem('na.locale')
  return saved === 'en-US' ? 'en-US' : 'zh-CN'
}

const initial = loadLocale()
setRequestLocale(initial)
// 首载即同步 <html lang>（index.html 静态默认 zh-CN，持久化了其它语言时须纠正）
// / Sync <html lang> on first load (index.html statically defaults to zh-CN; correct it when another language was persisted)
document.documentElement.lang = initial

export const i18n = createI18n({
  legacy: false,
  locale: initial,
  fallbackLocale: 'zh-CN',
  messages: { 'zh-CN': zhCN, 'en-US': enUS }
})

/** 切换语言 / Switch the language */
export function setLocale(locale: AppLocale): void {
  i18n.global.locale.value = locale
  localStorage.setItem('na.locale', locale)
  document.documentElement.lang = locale
  setRequestLocale(locale)
}
