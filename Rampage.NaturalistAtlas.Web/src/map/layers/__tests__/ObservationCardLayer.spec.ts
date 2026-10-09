/** ObservationCardLayer 纯函数单测：XSS 转义与数量缩写（产品核心体验的底线保障） */
import { describe, expect, it, vi } from 'vitest'

// 模块级 mock：maplibre-gl 在 jsdom 下导入即崩（gl-matrix 依赖未初始化的运行时）
vi.mock('maplibre-gl', () => ({ default: {} }))

import { escapeHtml, formatCount } from '../ObservationCardLayer'

describe('escapeHtml', () => {
  it('转义五个危险字符', () => {
    expect(escapeHtml('<img src=x onerror=alert(1)>')).toBe(
      '&lt;img src=x onerror=alert(1)&gt;'
    )
    expect(escapeHtml('a&b')).toBe('a&amp;b')
    expect(escapeHtml('"x"')).toBe('&quot;x&quot;')
    expect(escapeHtml("'x'")).toBe('&#39;x&#39;')
  })

  it('普通物种名原样通过', () => {
    expect(escapeHtml('Pica serica 喜鹊')).toBe('Pica serica 喜鹊')
  })

  it('空串安全', () => {
    expect(escapeHtml('')).toBe('')
  })
})

describe('formatCount', () => {
  it('千以下原样', () => {
    expect(formatCount(0)).toBe('0')
    expect(formatCount(999)).toBe('999')
  })
  it('千位缩写一位小数', () => {
    expect(formatCount(1234)).toBe('1.2k')
    expect(formatCount(9999)).toBe('10.0k')
  })
  it('万位取整千', () => {
    expect(formatCount(161049)).toBe('161k')
  })
})
