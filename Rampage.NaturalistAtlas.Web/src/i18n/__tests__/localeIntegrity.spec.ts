/** i18n 语言包完整性回归：中英键集一致 + 导航动态键可解析（防误删键导致界面显示裸键） */
import { describe, expect, it } from 'vitest'
import zhCN from '@/i18n/zh-CN'
import enUS from '@/i18n/en-US'

/** 展平语言包为点路径键集 / Flatten a pack into dotted-path keys */
function flatten(obj: unknown, prefix = ''): Set<string> {
  const keys = new Set<string>()
  for (const [k, v] of Object.entries(obj as Record<string, unknown>)) {
    const path = prefix ? `${prefix}.${k}` : k
    if (v && typeof v === 'object' && !Array.isArray(v)) {
      for (const sub of flatten(v, path)) {
        keys.add(sub)
      }
    } else {
      keys.add(path)
    }
  }
  return keys
}

/** 按点路径取值 / Resolve a dotted path */
function resolve(obj: unknown, path: string): unknown {
  let node = obj
  for (const p of path.split('.')) {
    node = (node as Record<string, unknown> | undefined)?.[p]
  }
  return node
}

/** 导航键契约（与 TopBar/MobileSheet 的 navItems 及路由 meta.nav 一致；新增导航项须同步此处） */
const NAV_KEYS = ['explore', 'overview', 'species', 'phenology', 'gaps']

describe('i18n 语言包完整性', () => {
  it('中英键集完全一致（缺键会在界面显示裸键）', () => {
    const zh = flatten(zhCN)
    const en = flatten(enUS)
    expect([...zh].filter((k) => !en.has(k))).toEqual([])
    expect([...en].filter((k) => !zh.has(k))).toEqual([])
  })

  it('导航项键全部可解析（曾因误删 nav.species 显示裸键）', () => {
    for (const key of NAV_KEYS) {
      expect(typeof resolve(zhCN, `nav.${key}`)).toBe('string')
      expect(typeof resolve(enUS, `nav.${key}`)).toBe('string')
    }
  })
})
