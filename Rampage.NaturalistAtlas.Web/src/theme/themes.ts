/**
 * 三套视觉方案的元数据（设计文档 §3） / Metadata of the three visual schemes (design doc §3)
 *
 * 一套主题 = 三个维度同步切换：/ One theme = three dimensions switched in sync:
 * 1. CSS 设计令牌：tokens.css 中 `[data-theme="xxx"]` 下的 CSS 变量（界面颜色、字体、圆角、阴影、毛玻璃）；
 *    / 1. CSS design tokens: CSS variables under [data-theme="xxx"] in tokens.css (colors, fonts, radii, shadows, glass effects);
 * 2. Naive UI 主题：naiveTheme.ts 中的 themeOverrides（组件库内部颜色与圆角）；
 *    / 2. Naive UI theme: themeOverrides in naiveTheme.ts (component-internal colors and radii);
 * 3. 底图样式：map/basemaps.ts 中按主题选择 OpenFreeMap 基础样式 + 样式补丁（背景/水体/绿地色）。
 *    / 3. Basemap style: per-theme OpenFreeMap base style + patches in map/basemaps.ts (background/water/green colors).
 */

/** 主题标识 / Theme id */
export type ThemeId = 'journal' | 'night' | 'spectrum'

/** 主题元数据 / Theme metadata */
export interface ThemeMeta {
  /** 主题标识（写入 <html data-theme>） / Theme id (written to <html data-theme>) */
  id: ThemeId
  /** 方案编号（设计文档中的 A/B/C） / Scheme code (A/B/C in the design doc) */
  code: 'A' | 'B' | 'C'
  /** 是否暗色（决定 Naive UI 基础主题与地图天空色） / Whether dark (drives the Naive UI base theme and map sky color) */
  dark: boolean
  /** 浏览器地址栏主题色（meta theme-color） / Browser address-bar theme color (meta theme-color) */
  metaColor: string
  /** 主题切换器中的预览色块（背景 / 面板 / 强调） / Preview swatches in the theme switcher (background / panel / accent) */
  swatch: [string, string, string]
}

/** 主题列表（顺序即切换器顺序） / Theme list (order matches the switcher) */
export const THEMES: ThemeMeta[] = [
  {
    id: 'journal',
    code: 'A',
    dark: false,
    metaColor: '#f3ecdc',
    swatch: ['#f3ecdc', '#fbf7ee', '#7a4f2a']
  },
  {
    id: 'night',
    code: 'B',
    dark: true,
    metaColor: '#0b1512',
    swatch: ['#0b1512', '#14231f', '#7cf2b0']
  },
  {
    id: 'spectrum',
    code: 'C',
    dark: false,
    metaColor: '#f7f9fb',
    swatch: ['#f7f9fb', '#ffffff', '#2f9e5b']
  }
]

/** 默认主题：B 夜观·萤火 / Default theme: B Night Fireflies */
export const DEFAULT_THEME: ThemeId = 'night'

/** 按标识取主题元数据 / Get theme metadata by id */
export function getThemeMeta(id: ThemeId): ThemeMeta {
  return THEMES.find((t) => t.id === id) ?? THEMES[1]!
}

/**
 * 四季·光谱主题的月份强调色（1~12 月） / Monthly accent colors of the Spectrum theme (Jan~Dec)
 * 冬霜蓝（12~2 月）→ 春樱粉（3~5 月）→ 夏林绿（6~8 月）→ 秋枫橙（9~11 月），相邻月份渐变过渡
 * / Winter frost blue (Dec~Feb) → spring cherry pink (Mar~May) → summer forest green (Jun~Aug) →
 * autumn maple orange (Sep~Nov), blending between adjacent months
 */
export const SEASON_ACCENTS: string[] = [
  '#4f86e8', // 1 月 冬霜蓝 / Jan winter frost blue
  '#6a8fe6', // 2 月 冬末 / Feb late winter
  '#e46f9c', // 3 月 春樱粉 / Mar cherry pink
  '#e7609a', // 4 月 樱盛 / Apr cherry peak
  '#8fbf4a', // 5 月 春末新绿 / May fresh late-spring green
  '#2f9e5b', // 6 月 夏林绿 / Jun summer forest green
  '#1f9a6e', // 7 月 盛夏 / Jul midsummer
  '#3e9a4a', // 8 月 夏末 / Aug late summer
  '#d9912f', // 9 月 初秋 / Sep early autumn
  '#e0762f', // 10 月 秋枫橙 / Oct maple orange
  '#c75a2a', // 11 月 深秋 / Nov late autumn
  '#5b7fd6' // 12 月 初冬 / Dec early winter
]
