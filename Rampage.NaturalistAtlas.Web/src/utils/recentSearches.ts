/**
 * 地点搜索的"最近搜索"记录（localStorage 持久化，设计文档 §5.11）
 * / "Recent searches" for place search (persisted in localStorage, design doc §5.11)
 *
 * 存最近 5 条去重的搜索结果（GeoSearchItem 原样序列化），输入框为空时作为联想首屏。
 * / Stores the last 5 deduplicated results (GeoSearchItem serialized as-is), shown as suggestions when the input is empty.
 */
import type { GeoSearchItem } from '@/api/types'

const KEY = 'na.recentSearch'
const LIMIT = 5

function isItem(x: unknown): x is GeoSearchItem {
  const it = x as GeoSearchItem
  return (
    typeof it === 'object' && it !== null && typeof it.id === 'number' && typeof it.level === 'string'
  )
}

/** 读取最近搜索（最新在前；解析失败回退空） / Load recent searches (newest first; empty on parse failure) */
export function getRecentSearches(): GeoSearchItem[] {
  try {
    const raw = localStorage.getItem(KEY)
    const parsed = raw ? (JSON.parse(raw) as unknown) : []
    return Array.isArray(parsed) ? parsed.filter(isItem) : []
  } catch {
    return []
  }
}

/** 记录一次搜索（按 level:id 去重置顶，最多保留 5 条） / Record a search (deduped and moved to top by level:id, keeps at most 5) */
export function addRecentSearch(item: GeoSearchItem): void {
  const list = getRecentSearches().filter((x) => `${x.level}:${x.id}` !== `${item.level}:${item.id}`)
  list.unshift(item)
  try {
    localStorage.setItem(KEY, JSON.stringify(list.slice(0, LIMIT)))
  } catch {
    // 隐私模式/存储被禁时静默失败 / Fail silently in private mode or when storage is disabled
  }
}
