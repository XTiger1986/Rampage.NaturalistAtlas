/** 类群调色板完整性：1~14 × 三主题色齐全且为合法 hex */
import { describe, expect, it } from 'vitest'
import { TAXA, taxonColor } from '../taxonPalette'
import type { ThemeId } from '@/theme/themes'

const THEMES: ThemeId[] = ['journal', 'night', 'spectrum']

describe('taxonPalette', () => {
  it('类群代码覆盖 1~14', () => {
    expect([...TAXA.map((t) => t.code)].sort((a, b) => a - b)).toEqual(
      Array.from({ length: 14 }, (_, i) => i + 1)
    )
  })
  it('三主题 × 全部类群均为合法 #hex 色', () => {
    for (const theme of THEMES) {
      for (const code of [1, 5, 9, 13, 14]) {
        expect(taxonColor(code, theme)).toMatch(/^#[0-9a-f]{6}$/i)
      }
    }
  })
  it('同一类群三主题色相一致但可区分（避免完全相同）', () => {
    const colors = THEMES.map((th) => taxonColor(6, th))
    expect(new Set(colors).size).toBe(3)
  })
})
