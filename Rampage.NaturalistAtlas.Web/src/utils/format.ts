/**
 * 展示层格式化工具 / Presentation-layer formatting utilities
 *
 * 语言规则（设计文档 §8）：文案随界面语言（vue-i18n 当前 locale）；相对时间/数字用
 * Intl 按 locale 格式化；物种名中文界面 中文名 > 英文名 > 学名，英文界面 英文名 > 学名。
 * / Language rules (design doc §8): copy follows the UI language (vue-i18n current locale); relative
 * time/numbers are formatted by Intl per locale; species names: Chinese UI prefers Chinese > English >
 * scientific, English UI prefers English > scientific.
 */
import type { LicenseCode } from '@/api/types'
import { i18n, type AppLocale } from '@/i18n'

/** 当前界面语言（纯函数内取用；测试可直接切 i18n.global.locale） / Current UI language (for use inside pure functions; tests may switch i18n.global.locale directly) */
function locale(): AppLocale {
  return (i18n.global.locale.value as AppLocale) ?? 'zh-CN'
}

/** 授权协议短名（1~8 为通用 CC 缩写无需翻译；0 的保留所有权利文案走 i18n） / License short labels (1~8 are universal CC abbreviations needing no translation; 0's all-rights-reserved copy goes through i18n) */
const LICENSE_LABEL: Record<number, string> = {
  0: '',
  1: 'CC0',
  2: 'CC BY',
  3: 'CC BY-SA',
  4: 'CC BY-NC',
  5: 'CC BY-NC-SA',
  6: 'CC BY-ND',
  7: 'CC BY-NC-ND',
  8: 'PDM'
}

/** 授权协议官方链接 / Official license URLs */
const LICENSE_URL: Record<number, string | undefined> = {
  1: 'https://creativecommons.org/publicdomain/zero/1.0/',
  2: 'https://creativecommons.org/licenses/by/4.0/',
  3: 'https://creativecommons.org/licenses/by-sa/4.0/',
  4: 'https://creativecommons.org/licenses/by-nc/4.0/',
  5: 'https://creativecommons.org/licenses/by-nc-sa/4.0/',
  6: 'https://creativecommons.org/licenses/by-nd/4.0/',
  7: 'https://creativecommons.org/licenses/by-nc-nd/4.0/',
  8: 'https://creativecommons.org/publicdomain/mark/1.0/'
}

/** 授权协议短名 / License short label */
export function licenseLabel(code: LicenseCode | number): string {
  if (code === 0) {
    return i18n.global.t('format.allRightsReserved')
  }
  return LICENSE_LABEL[code] ?? '—'
}

/** 授权协议链接 / License URL */
export function licenseUrl(code: LicenseCode | number): string | undefined {
  return LICENSE_URL[code]
}

/**
 * 版权署名：©观察者 + 协议（产品文档 §9：所有照片展示必须保留作者与协议署名）
 * GBIF 记录同时署名数据发布机构
 * / Attribution: ©observer + license (product doc §9: every photo must keep author and license credit);
 * GBIF records also credit the dataset publisher
 */
export function attribution(userLogin: string | null, license: number, datasetTitle?: string | null): string {
  const who = userLogin ? `© ${userLogin}` : '©'
  const publisher = datasetTitle ? ` · ${datasetTitle}` : ''
  return `${who}${publisher} · ${licenseLabel(license)}`
}

/** 数字千分位（按界面语言） / Thousands-separated number (per UI language) */
export function formatNumber(n: number | null | undefined): string {
  return typeof n === 'number' ? new Intl.NumberFormat(locale()).format(n) : '—'
}

/** 相对时间：刚刚 / N 分钟前 / N 小时前 / N 天前 / 日期（分钟~天走 Intl.RelativeTimeFormat） / Relative time: just now / N minutes ago / N hours ago / N days ago / date (minutes~days via Intl.RelativeTimeFormat) */
export function relativeTime(iso: string | null | undefined): string {
  if (!iso) {
    return '—'
  }
  const t = new Date(iso).getTime()
  const diffSeconds = (Date.now() - t) / 1000
  if (diffSeconds < 60) {
    return i18n.global.t('format.justNow')
  }
  const rtf = new Intl.RelativeTimeFormat(locale(), { numeric: 'auto' })
  if (diffSeconds < 3600) {
    return rtf.format(-Math.floor(diffSeconds / 60), 'minute')
  }
  if (diffSeconds < 86400) {
    return rtf.format(-Math.floor(diffSeconds / 3600), 'hour')
  }
  if (diffSeconds < 86400 * 30) {
    return rtf.format(-Math.floor(diffSeconds / 86400), 'day')
  }
  return new Date(iso).toLocaleDateString(locale())
}

/** 物种展示名：中文界面 中文名 > 英文名 > 学名；英文界面 英文名 > 学名（设计文档 §8） / Species display name: Chinese UI prefers Chinese > English > scientific; English UI prefers English > scientific (design doc §8) */
export function speciesName(s: { commonNameCn?: string | null; commonNameEn?: string | null; scientificName?: string | null }): string {
  const picked = locale() === 'en-US'
    ? (s.commonNameEn || s.scientificName)
    : (s.commonNameCn || s.commonNameEn || s.scientificName)
  return picked || i18n.global.t('format.unidentified')
}

/** 城市展示名：中文界面中文名，英文界面英文名（缺失回退中文名） / City display name: Chinese in the Chinese UI, English in the English UI (falls back to Chinese when missing) */
export function cityName(c: { name?: string | null; nameEn?: string | null }): string {
  return (locale() === 'en-US' ? (c.nameEn || c.name) : c.name) ?? ''
}

/** 月份序号（1~12）→ 本地化短月名（中文 "3月"，英文 "Mar"） / Month number (1~12) → localized short month name ("3月" in Chinese, "Mar" in English) */
export function monthShort(month: number): string {
  return new Intl.DateTimeFormat(locale(), { month: 'short' }).format(new Date(2026, month - 1, 1))
}

/** 旬序号（1~36）→ 标签，如 "3月中旬" / "mid Mar" / Ten-day index (1~36) → label, e.g. "mid Mar" */
export function tenDayLabel(index: number): string {
  const month = Math.ceil(index / 3)
  const part = (['early', 'mid', 'late'] as const)[(index - 1) % 3]
  return i18n.global.t(`tenDay.${part}`, { mon: monthShort(month) })
}
