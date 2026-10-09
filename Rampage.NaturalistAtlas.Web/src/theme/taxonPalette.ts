/**
 * 类群调色板（全站唯一色源） / Taxon palette (the site's single color source)
 *
 * - code 与后端枚举 IconicTaxon（Model/Enums/AtlasEnums.cs）一一对应：1~13 为展示类群，14 = 未知； / - code maps 1:1 to the backend IconicTaxon enum (Model/Enums/AtlasEnums.cs): 1~13 are displayed taxa, 14 = unknown;
 * - 三套主题共享同一色相序列，仅调整明度/饱和度，保证"图例即筛选器"在任何主题下语义一致； / - the three themes share one hue sequence, adjusting only lightness/saturation, so "legend as filter" stays semantically consistent across themes;
 * - 地图图层（聚合簇、点、热力）、图例、图表均从此处取色，禁止在组件内硬编码类群颜色。 / - map layers (clusters, dots, heatmap), the legend and charts all take colors from here; hard-coding taxon colors in components is forbidden.
 */
import type { ThemeId } from './themes'

/** 类群代码（与后端 IconicTaxon 枚举值一致） / Taxon codes (matching the backend IconicTaxon enum values) */
export type IconicTaxonCode = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14

/** 类群定义 / Taxon definition */
export interface TaxonDef {
  /** 类群代码 / Taxon code */
  code: IconicTaxonCode
  /** 枚举名（与后端 EnumFieldAttribute 一致，用于 i18n key 与 URL 参数） / Enum key (matching the backend EnumFieldAttribute; used for i18n keys and URL params) */
  key: string
  /** 单字图标（图例芯片、聚合簇内使用） / Single-glyph icon (used in legend chips and clusters) */
  glyph: string
  /** 各主题下的类群色 / Taxon color per theme */
  colors: Record<ThemeId, string>
}

/**
 * 类群定义表（顺序即图例顺序：先动物大类，后植物/真菌，再微小类群） / Taxon table (order is the legend order: major animal groups first, then plants/fungi, then tiny groups)
 * 色值说明： / Color notes:
 * - journal（博物志·纸本）：矿物颜料色——群青、赭石、石绿、朱砂、藤黄……低饱和、耐看； / - journal (naturalist's paper): mineral-pigment colors — ultramarine, ochre, malachite, cinnabar, gamboge… low saturation, easy on the eyes;
 * - night（夜观·萤火）：高明度荧光色，在深色底图上自带"发光"感； / - night (firefly watching): high-lightness fluorescent colors that feel self-luminous on dark basemaps;
 * - spectrum（四季·光谱）：标准饱和色，明亮底图上对比清晰。 / - spectrum (four seasons): standard saturated colors, crisp contrast on bright basemaps.
 */
export const TAXA: TaxonDef[] = [
  { code: 1, key: 'Birds', glyph: '鸟', colors: { journal: '#3d6e9e', night: '#5eb3ff', spectrum: '#3b82f6' } },
  { code: 2, key: 'Mammals', glyph: '兽', colors: { journal: '#8b5a3c', night: '#e8a877', spectrum: '#a0522d' } },
  { code: 3, key: 'Amphibians', glyph: '蛙', colors: { journal: '#3f8f86', night: '#3ee6c8', spectrum: '#14b8a6' } },
  { code: 4, key: 'Reptiles', glyph: '蜥', colors: { journal: '#7d8b3a', night: '#b6f24a', spectrum: '#65a30d' } },
  { code: 5, key: 'RayFinnedFishes', glyph: '鱼', colors: { journal: '#4f5b93', night: '#8b8cff', spectrum: '#6366f1' } },
  { code: 6, key: 'Insects', glyph: '虫', colors: { journal: '#c8702a', night: '#ffa24a', spectrum: '#f97316' } },
  { code: 7, key: 'Arachnids', glyph: '蛛', colors: { journal: '#a8423c', night: '#ff5f7a', spectrum: '#e11d48' } },
  { code: 8, key: 'Mollusks', glyph: '螺', colors: { journal: '#b8913a', night: '#ffd35a', spectrum: '#eab308' } },
  { code: 9, key: 'Plants', glyph: '植', colors: { journal: '#4d8a4a', night: '#5cf28a', spectrum: '#22c55e' } },
  { code: 10, key: 'Fungi', glyph: '菌', colors: { journal: '#a6537a', night: '#ff6ec7', spectrum: '#ec4899' } },
  { code: 11, key: 'OtherAnimals', glyph: '动', colors: { journal: '#7b7d80', night: '#b3c2d6', spectrum: '#64748b' } },
  { code: 12, key: 'Protozoans', glyph: '原', colors: { journal: '#6d5a92', night: '#b58cff', spectrum: '#8b5cf6' } },
  { code: 13, key: 'Chromista', glyph: '藻', colors: { journal: '#8a6d3b', night: '#d9a55a', spectrum: '#a16207' } },
  { code: 14, key: 'Unknown', glyph: '?', colors: { journal: '#8c8c8c', night: '#8894a6', spectrum: '#94a3b8' } }
]

/** 图例/筛选器展示的类群（不含"未知"） / Taxa shown in the legend/filter (excluding "Unknown") */
export const LEGEND_TAXA: TaxonDef[] = TAXA.filter((t) => t.code !== 14)

/** 按代码索引的类群字典 / Taxon dictionary indexed by code */
const TAXON_MAP = new Map<number, TaxonDef>(TAXA.map((t) => [t.code, t]))

/** 取类群定义（未知代码回落到"未知"） / Get a taxon definition (unknown codes fall back to "Unknown") */
export function getTaxon(code: number | null | undefined): TaxonDef {
  return TAXON_MAP.get(code ?? 14) ?? TAXA[TAXA.length - 1]!
}

/** 取指定主题下的类群色 / Get the taxon color under a given theme */
export function taxonColor(code: number | null | undefined, theme: ThemeId): string {
  return getTaxon(code).colors[theme]
}

