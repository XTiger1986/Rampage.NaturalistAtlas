/** CSV 生成测试：转义 / BOM / 行尾 */
import { describe, expect, it } from 'vitest'
import { toCsv } from '../csv'

describe('toCsv', () => {
  it('表头与行以逗号拼接，UTF-8 BOM 开头', () => {
    const csv = toCsv(
      [
        ['a', '列A'],
        ['b', '列B']
      ],
      [{ a: 1, b: 'x' }]
    )
    expect(csv.startsWith('﻿')).toBe(true)
    expect(csv).toContain('列A,列B\r\n1,x')
  })
  it('含逗号/引号/换行的值加引号转义', () => {
    const csv = toCsv([['v', 'V']], [{ v: 'a,"b"\nc' }])
    expect(csv).toContain('"a,""b""\nc"')
  })
  it('null / undefined 输出空单元格', () => {
    const csv = toCsv(
      [
        ['x', 'X'],
        ['y', 'Y']
      ],
      [{ x: null, y: undefined }]
    )
    expect(csv.endsWith(',\r\n') || csv.endsWith(',')).toBe(true)
  })
})
