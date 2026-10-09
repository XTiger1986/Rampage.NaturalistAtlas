/**
 * Naive UI 主题适配 / Naive UI theme adaptation
 *
 * Naive UI 组件内部颜色不读 CSS 变量，因此每套主题需提供一份 themeOverrides，
 * 色值与 tokens.css 保持一致（修改其一必须同步另一处）。
 * / Naive UI components do not read CSS variables, so each theme ships its own themeOverrides with colors
 * matching tokens.css (change one, sync the other).
 * 四季·光谱主题的强调色随月份变化，由 buildNaiveOverrides 的 accent 参数注入。
 * / The Spectrum (four seasons) theme's accent varies by month, injected via buildNaiveOverrides's accent parameter.
 */
import { darkTheme } from 'naive-ui'
import type { GlobalTheme, GlobalThemeOverrides } from 'naive-ui'
import type { ThemeId } from './themes'

/** 各主题基础色（与 tokens.css 同步） / Base colors per theme (kept in sync with tokens.css) */
const BASE: Record<
  ThemeId,
  { accent: string; bg: string; surface: string; ink: string; ink2: string; line: string; radius: string; font: string }
> = {
  journal: {
    accent: '#7a4f2a',
    bg: '#efe6d2',
    surface: '#fbf7ee',
    ink: '#2b2118',
    ink2: '#5a4a38',
    line: '#d6c7a6',
    radius: '4px',
    font: "'Noto Serif SC', 'Songti SC', serif"
  },
  night: {
    accent: '#7cf2b0',
    bg: '#0b1512',
    surface: '#14231f',
    ink: '#e7f3ec',
    ink2: '#a9c2b6',
    line: 'rgba(160,220,190,0.18)',
    radius: '10px',
    font: "'Noto Sans SC', 'PingFang SC', sans-serif"
  },
  spectrum: {
    accent: '#2f9e5b',
    bg: '#f7f9fb',
    surface: '#ffffff',
    ink: '#17202b',
    ink2: '#4a5665',
    line: '#e3e8ee',
    radius: '12px',
    font: "'Noto Sans SC', 'PingFang SC', sans-serif"
  }
}

/** 取 Naive UI 基础主题（暗色主题用 darkTheme，其余用默认亮色 null） / Pick the Naive UI base theme (darkTheme for dark themes, null for the default light theme) */
export function naiveBaseTheme(theme: ThemeId): GlobalTheme | null {
  return theme === 'night' ? darkTheme : null
}

/**
 * 生成 Naive UI 主题覆盖 / Build Naive UI theme overrides
 * @param theme 主题标识 / Theme id
 * @param accentOverride 强调色覆写（四季·光谱主题按月份传入） / Accent color override (passed per month for the Spectrum theme)
 */
export function buildNaiveOverrides(theme: ThemeId, accentOverride?: string): GlobalThemeOverrides {
  const b = BASE[theme]
  const accent = accentOverride ?? b.accent
  return {
    common: {
      primaryColor: accent,
      primaryColorHover: accent,
      primaryColorPressed: accent,
      primaryColorSuppl: accent,
      bodyColor: b.bg,
      cardColor: b.surface,
      modalColor: b.surface,
      popoverColor: b.surface,
      textColorBase: b.ink,
      textColor1: b.ink,
      textColor2: b.ink2,
      borderColor: b.line,
      dividerColor: b.line,
      borderRadius: b.radius,
      fontFamily: "'Noto Sans SC', 'PingFang SC', 'Microsoft YaHei', sans-serif"
    },
    Card: { titleFontSizeMedium: '17px' },
    Typography: { headerFontFamily: b.font } as GlobalThemeOverrides['Typography'],
    Button: theme === 'night' ? { textColorPrimary: '#062014', textColorHoverPrimary: '#062014' } : {},
    Tag: { borderRadius: b.radius }
  }
}
