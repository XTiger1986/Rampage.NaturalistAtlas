/**
 * 底图注册表（设计文档 §2 底图选型） / Basemap registry (design doc §2 basemap selection)
 *
 * 选型原则：优先 WGS-84 坐标系底图，与观察点坐标（WGS-84）直接对齐，杜绝 GCJ-02 偏移。 / Selection principle: prefer WGS-84 basemaps so they align directly with observation coordinates (WGS-84), ruling out GCJ-02 offsets.
 *
 * | id       | 服务                         | 类型     | key       | 说明 | / | id | service | type | key | notes |
 * |----------|------------------------------|----------|-----------|------|
 * | ofm      | OpenFreeMap（OpenMapTiles）  | 矢量     | 无需      | 默认；三套主题各用一套基础样式 + 补丁；自带 building 图层供 2.5D 挤出 | / | ofm | OpenFreeMap (OpenMapTiles) | vector | none | default; one base style per theme plus patches; ships a building layer for 2.5D extrusion |
 * | tianditu | 天地图（国家地理信息公共服务）| 栅格     | 免费申请  | CGCS2000≈WGS-84，带审图号，国内公开上线推荐；配置 VITE_TIANDITU_KEY 后出现 | / | tianditu | national geospatial public service of China | raster | free key | CGCS2000≈WGS-84 with an approved map number, recommended for public launch in mainland China; appears once VITE_TIANDITU_KEY is configured |
 * | imagery  | Esri World Imagery           | 栅格影像 | 无需      | 卫星影像；须保留署名 | / | imagery | Esri World Imagery | raster imagery | none | satellite imagery; attribution must be kept |
 *
 * 合规提示：面向中国大陆公众公开发布的地图须使用带审图号的底图（如天地图），OpenFreeMap 适合开发与海外访问。 / Compliance note: maps published to the public in mainland China must use basemaps with an approved map number (e.g. Tianditu); OpenFreeMap suits development and overseas access.
 */
import type { StyleSpecification, LayerSpecification } from 'maplibre-gl'
import type { ThemeId } from '@/theme/themes'

/** 底图标识 / Basemap identifier */
export type BasemapId = 'ofm' | 'tianditu' | 'imagery'

/** 底图元数据 / Basemap metadata */
export interface BasemapMeta {
  id: BasemapId
  /** 是否为矢量底图（矢量底图支持主题补丁与建筑挤出） / Whether it is a vector basemap (vector basemaps support theme patches and building extrusion) */
  vector: boolean
}

/** 天地图 key（未配置则不注册天地图） / Tianditu API key (Tianditu is not registered when unset) */
const TIANDITU_KEY = import.meta.env.VITE_TIANDITU_KEY?.trim() ?? ''

/** 可用底图列表（按配置动态裁剪） / Available basemaps (trimmed dynamically by configuration) */
export const BASEMAPS: BasemapMeta[] = [
  { id: 'ofm', vector: true },
  ...(TIANDITU_KEY ? [{ id: 'tianditu' as const, vector: false }] : []),
  { id: 'imagery', vector: false }
]

/** OpenFreeMap 各主题基础样式 / OpenFreeMap base style per theme */
const OFM_STYLE: Record<ThemeId, string> = {
  journal: 'https://tiles.openfreemap.org/styles/positron',
  night: 'https://tiles.openfreemap.org/styles/dark',
  spectrum: 'https://tiles.openfreemap.org/styles/liberty'
}

/** 矢量底图的主题补丁色 / Theme patch colors for vector basemaps */
interface ThemePatch {
  background: string
  water: string
  green: string
  building: string
}

/** 各主题补丁色（与 tokens.css 的色调保持一致） / Patch colors per theme (kept in tune with tokens.css) */
const PATCH: Record<ThemeId, ThemePatch> = {
  // 纸本：米色陆地、灰青水体、橄榄绿地 / journal: beige land, gray-teal water, olive-green vegetation
  journal: { background: '#efe6d2', water: '#c3d1cc', green: '#dcdcbc', building: '#d8ccb2' },
  // 夜观：墨绿夜色陆地、深青水体、幽暗林地 / night: dark-green night land, deep-teal water, dim woodland
  night: { background: '#0b1512', water: '#08222a', green: '#0f2219', building: '#1a2c27' },
  // 光谱：清爽浅灰陆地、明亮水体、嫩绿 / spectrum: airy light-gray land, bright water, tender green
  spectrum: { background: '#f4f6f3', water: '#bfe0f1', green: '#d7ecd0', building: '#e4e7ea' }
}

/** 取主题补丁色（建筑挤出等图层也会用到） / Get the theme patch colors (also used by building extrusion and friends) */
export function themePatch(theme: ThemeId): ThemePatch {
  return PATCH[theme]
}

/**
 * 栅格底图的主题调色（raster-* paint 属性） / Theme grading for raster basemaps (raster-* paint properties)
 * 夜观主题把影像/天地图压暗并降饱和，使发光类群点更突出 / The night theme dims and desaturates imagery/Tianditu so the glowing taxon dots stand out
 */
function rasterPaint(theme: ThemeId): Record<string, number> {
  switch (theme) {
    case 'night':
      return { 'raster-brightness-max': 0.55, 'raster-saturation': -0.45, 'raster-contrast': 0.1 }
    case 'journal':
      return { 'raster-saturation': -0.55, 'raster-brightness-min': 0.12, 'raster-hue-rotate': 12 }
    default:
      return {}
  }
}

/** 天地图 WMTS 瓦片模板（Web 墨卡托 _w 系列，t0~t7 负载均衡） / Tianditu WMTS tile templates (Web Mercator _w series, t0~t7 load balancing) */
function tiandituTiles(layer: 'vec' | 'cva' | 'img' | 'cia'): string[] {
  return [0, 1, 2, 3, 4, 5, 6, 7].map(
    (n) =>
      `https://t${n}.tianditu.gov.cn/${layer}_w/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=${layer}` +
      `&STYLE=default&TILEMATRIXSET=w&FORMAT=tiles&TILEMATRIX={z}&TILEROW={y}&TILECOL={x}&tk=${TIANDITU_KEY}`
  )
}

/** 构造栅格底图样式 / Build a raster basemap style */
function rasterStyle(theme: ThemeId, sources: Record<string, { tiles: string[]; attribution: string; maxzoom?: number }>): StyleSpecification {
  const paint = rasterPaint(theme)
  return {
    version: 8,
    sources: Object.fromEntries(
      Object.entries(sources).map(([id, s]) => [id, { type: 'raster', tileSize: 256, ...s }])
    ),
    layers: [
      { id: 'background', type: 'background', paint: { 'background-color': PATCH[theme].background } },
      ...Object.keys(sources).map(
        (id) => ({ id: `basemap-${id}`, type: 'raster', source: id, paint }) as LayerSpecification
      )
    ]
  }
}

/**
 * 取底图样式（URL 或样式对象） / Resolve the basemap style (a URL or a style object)
 * @param id 底图 / the basemap
 * @param theme 主题（决定 OpenFreeMap 基础样式与栅格调色） / the theme (decides the OpenFreeMap base style and raster grading)
 */
export function resolveBasemapStyle(id: BasemapId, theme: ThemeId): string | StyleSpecification {
  switch (id) {
    case 'tianditu':
      return rasterStyle(theme, {
        // 上线前按天地图官网要求在署名中补充当年审图号 / Before launch, append the current-year map approval number to the attribution as Tianditu requires
        tdtVec: { tiles: tiandituTiles('vec'), attribution: '© 天地图', maxzoom: 18 },
        tdtCva: { tiles: tiandituTiles('cva'), attribution: '', maxzoom: 18 }
      })
    case 'imagery':
      return rasterStyle(theme, {
        esri: {
          tiles: ['https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}'],
          attribution: 'Imagery © Esri, Maxar, Earthstar Geographics',
          maxzoom: 19
        }
      })
    case 'ofm':
    default:
      return OFM_STYLE[theme]
  }
}

/**
 * 矢量样式主题补丁（作为 setStyle 的 transformStyle 使用，在样式应用前改写） / Theme patch for vector styles (used as setStyle transformStyle; rewrites before the style is applied)
 * 按图层类型 + source-layer 识别背景、水体、绿地，改写其颜色，其余图层保持基础样式。 / Detects background, water and green areas by layer type + source-layer and rewrites their colors; other layers keep the base style.
 */
export function patchVectorStyle(style: StyleSpecification, theme: ThemeId): StyleSpecification {
  const p = PATCH[theme]
  const layers = style.layers.map((layer) => {
    const l = { ...layer } as LayerSpecification & { 'source-layer'?: string; paint?: Record<string, unknown> }
    const srcLayer = l['source-layer']
    if (l.type === 'background') {
      l.paint = { ...(l.paint ?? {}), 'background-color': p.background }
    } else if (l.type === 'fill' && srcLayer === 'water') {
      l.paint = { ...(l.paint ?? {}), 'fill-color': p.water }
    } else if (l.type === 'fill' && (srcLayer === 'park' || (srcLayer === 'landcover' && /wood|grass|forest/.test(l.id)))) {
      l.paint = { ...(l.paint ?? {}), 'fill-color': p.green }
    }
    return l as LayerSpecification
  })
  return { ...style, layers }
}

/** 底图是否为矢量（OpenMapTiles schema） / Whether the basemap is vector (OpenMapTiles schema) */
export function isVectorBasemap(id: BasemapId): boolean {
  return BASEMAPS.find((b) => b.id === id)?.vector ?? false
}
