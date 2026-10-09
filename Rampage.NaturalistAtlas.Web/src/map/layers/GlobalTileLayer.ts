/**
 * 全球视角图层（双视角地图，产品文档 §8.5，设计文档 §5.3） / Global-view layer (dual-view map, product doc §8.5, design doc §5.3)
 *
 * 城市视角用本站明细（照片卡/热力）；当 缩放 < min(GLOBAL_ZOOM, 城市完整显示级别 − 1.5) 或 视野中心离开当前城市 bbox 时， / The city view uses this site's own details (photo cards/heatmap); when zoom < min(GLOBAL_ZOOM, city full-display zoom − 1.5) or the view center leaves the current city bbox,
 * 自动切换为数据源官方密度瓦片（全球视角），两者以透明度渐变无缝衔接。 / it switches automatically to the data source's official density tiles (global view); the two crossfade seamlessly via opacity.
 *
 * 数据源： / Data sources:
 * - iNaturalist v1 瓦片：https://api.inaturalist.org/v1/heatmap/{z}/{x}/{y}.png（接受与搜索一致的筛选参数） / - iNaturalist v1 tiles: https://api.inaturalist.org/v1/heatmap/{z}/{x}/{y}.png (accepts the same filter params as search)
 *   注意：地图瓦片端点在 v1，v2 不提供瓦片。 / Note: the map tile endpoint is v1; v2 offers no tiles.
 * - GBIF v2 密度瓦片：https://api.gbif.org/v2/map/occurrence/density/{z}/{x}/{y}@1x.png / GBIF v2 density tiles, same URL as shown
 *
 * 瓦片由浏览器直接请求源站（不经本站后端），不占用采集端的请求预算；源站自带 CDN 缓存（ttl 参数）。 / Tiles are requested from the origin directly by the browser (not via our backend), spending none of the collector's request budget; the origin has its own CDN caching (the ttl parameter).
 */
import type { Map as MlMap } from 'maplibre-gl'
import type { BBox } from '@/api/types'
import type { ThemeId } from '@/theme/themes'
import type { AtlasLayer } from '../MapEngine'
import { firstSymbolLayerId } from '../viewModes'

/** 全球数据源 / Global data provider */
export type GlobalProvider = 'inat' | 'gbif'

/** 全球视角缩放阈值上限：无论城市多小，低于该级别都进入全球视角 / Upper bound of the global-view zoom threshold: below this level the global view always applies, however small the city */
export const GLOBAL_ZOOM = 8

/** 瓦片加载失败次数超过该值即降级隐藏本层（避免满屏破图，设计文档 §5.3） / Beyond this many tile failures the layer degrades and hides (avoids a screenful of broken tiles, design doc §5.3) */
const TILE_ERROR_LIMIT = 5

/** 相对城市"完整显示"缩放级别再缩小多少级后进入全球视角（大城市完整显示时本身就可能 < 8） / How many zoom levels below the city's "full display" zoom the global view starts (large cities may already fit below zoom 8) */
const GLOBAL_ZOOM_OFFSET = 1.5

const SOURCE_ID = 'atlas-global'
const LAYER_ID = 'atlas-global'

/** 本站类群代码 → iNaturalist iconic_taxa 名称 / Our taxon codes → iNaturalist iconic_taxa names */
const INAT_ICONIC: Record<number, string> = {
  1: 'Aves',
  2: 'Mammalia',
  3: 'Amphibia',
  4: 'Reptilia',
  5: 'Actinopterygii',
  6: 'Insecta',
  7: 'Arachnida',
  8: 'Mollusca',
  9: 'Plantae',
  10: 'Fungi',
  11: 'Animalia',
  12: 'Protozoa',
  13: 'Chromista'
}

/** GBIF 各主题配色方案（GBIF 地图 API 内置 style） / GBIF color scheme per theme (built-in styles of the GBIF map API) */
const GBIF_STYLE: Record<ThemeId, string> = {
  night: 'greenHeat.point',
  journal: 'orangeHeat.point',
  spectrum: 'blueHeat.point'
}

/** 全球图层筛选参数 / Filter params for the global layer */
export interface GlobalFilter {
  iconic: number[]
  monthFrom?: number
  monthTo?: number
  quality: number[]
}

/** 展开月份区间（支持跨年，如 11~2 → 11,12,1,2）；导出供单元测试 / Expand a month range (wrap-year supported, e.g. 11~2 → 11,12,1,2); exported for unit tests */
export function expandMonths(from?: number, to?: number): number[] {
  if (!from || !to) {
    return []
  }
  const out: number[] = []
  let m = from
  for (let i = 0; i < 12; i++) {
    out.push(m)
    if (m === to) {
      break
    }
    m = (m % 12) + 1
  }
  return out
}

export class GlobalTileLayer implements AtlasLayer {
  readonly id = 'global-tiles'

  private map: MlMap | null = null
  private theme: ThemeId = 'night'
  private provider: GlobalProvider = 'inat'
  private enabled = true
  private filter: GlobalFilter = { iconic: [], quality: [1, 2] }
  private cityBBox: BBox | null = null
  /** 当前是否处于全球视角 / Whether currently in the global view */
  private inGlobal = false
  /** 瓦片加载失败计数（换源/换筛选时重置） / Tile failure counter (reset when the provider or filters change) */
  private errorCount = 0
  /** 是否已因瓦片连续失败降级隐藏 / Whether hidden by degradation after repeated tile failures */
  private degraded = false

  /**
   * @param onGlobalChange 全球视角进入/退出回调（MapCanvas 据此隐藏照片卡并提示） / callback on global-view enter/exit (MapCanvas hides photo cards and shows a hint accordingly)
   * @param onDegraded 瓦片连续失败降级/恢复回调（MapHud 据此提示） / callback on degrade/recover after repeated tile failures (MapHud shows a hint accordingly)
   */
  constructor(
    private readonly onGlobalChange: (inGlobal: boolean) => void,
    private readonly onDegraded: (degraded: boolean) => void = () => {}
  ) {}

  /** 切换数据源 / Switch the data provider */
  setProvider(p: GlobalProvider): void {
    if (p !== this.provider) {
      this.provider = p
      this.rebuild()
    }
  }

  /** 同步筛选（瓦片 URL 随筛选变化） / Sync filters (the tile URL changes with them) */
  setFilter(f: GlobalFilter): void {
    this.filter = f
    this.rebuild()
  }

  /** 设置当前城市范围（判定是否离开城市） / Set the current city bounds (used to detect leaving the city) */
  setCityBBox(bbox: BBox | null): void {
    this.cityBBox = bbox
    this.evaluate()
  }

  /** 总开关 / Master on/off switch */
  setEnabled(enabled: boolean): void {
    this.enabled = enabled
    this.evaluate()
  }

  setTheme(theme: ThemeId): void {
    this.theme = theme
  }

  mount(map: MlMap): void {
    if (this.map !== map) {
      map.on('moveend', this.evaluate)
      map.on('error', this.onMapError)
    }
    this.map = map
    this.addSourceAndLayer()
    this.evaluate()
  }

  unmount(map: MlMap): void {
    map.off('moveend', this.evaluate)
    map.off('error', this.onMapError)
    this.removeSourceAndLayer()
    this.map = null
  }

  /** 生成瓦片 URL 模板 / Build the tile URL template */
  private tileUrl(): { tiles: string[]; tileSize: number; attribution: string } {
    if (this.provider === 'gbif') {
      const qs = new URLSearchParams({ srs: 'EPSG:3857', style: GBIF_STYLE[this.theme] })
      return {
        tiles: [`https://api.gbif.org/v2/map/occurrence/density/{z}/{x}/{y}@1x.png?${qs.toString()}`],
        tileSize: 512,
        attribution: '<a href="https://www.gbif.org" target="_blank">GBIF</a>'
      }
    }
    const qs = new URLSearchParams({ ttl: '86400' })
    const grades = this.filter.quality.map((q) => (q === 1 ? 'research' : q === 2 ? 'needs_id' : 'casual'))
    if (grades.length > 0) {
      qs.set('quality_grade', grades.join(','))
    }
    const iconic = this.filter.iconic.map((c) => INAT_ICONIC[c]).filter(Boolean)
    if (iconic.length > 0) {
      qs.set('iconic_taxa', iconic.join(','))
    }
    const months = expandMonths(this.filter.monthFrom, this.filter.monthTo)
    if (months.length > 0 && months.length < 12) {
      qs.set('month', months.join(','))
    }
    // 夜观主题用亮色点，其余用深色点 / Night theme uses bright dots; the others use dark dots
    qs.set('color', this.theme === 'night' ? '#7cf2b0' : this.theme === 'journal' ? '#7a4f2a' : '#2f9e5b')
    return {
      tiles: [`https://api.inaturalist.org/v1/heatmap/{z}/{x}/{y}.png?${qs.toString()}`],
      tileSize: 256,
      attribution: '<a href="https://www.inaturalist.org" target="_blank">iNaturalist</a>'
    }
  }

  /** 添加瓦片源与图层 / Add the tile source and layer */
  private addSourceAndLayer(): void {
    const map = this.map
    if (!map || map.getSource(SOURCE_ID)) {
      return
    }
    const t = this.tileUrl()
    map.addSource(SOURCE_ID, { type: 'raster', tiles: t.tiles, tileSize: t.tileSize, attribution: t.attribution, maxzoom: 14 })
    map.addLayer(
      {
        id: LAYER_ID,
        type: 'raster',
        source: SOURCE_ID,
        layout: { visibility: 'none' },
        paint: { 'raster-opacity': 0, 'raster-opacity-transition': { duration: 600, delay: 0 }, 'raster-fade-duration': 200 }
      },
      firstSymbolLayerId(map)
    )
  }

  /** 移除瓦片源与图层 / Remove the tile source and layer */
  private removeSourceAndLayer(): void {
    const map = this.map
    if (!map) {
      return
    }
    if (map.getLayer(LAYER_ID)) {
      map.removeLayer(LAYER_ID)
    }
    if (map.getSource(SOURCE_ID)) {
      map.removeSource(SOURCE_ID)
    }
  }

  /** 参数变化后重建瓦片源 / Rebuild the tile source after parameters change */
  private rebuild(): void {
    if (!this.map) {
      return
    }
    this.removeSourceAndLayer()
    this.addSourceAndLayer()
    // 新 URL 重新观察：重置失败计数与降级态 / Re-observe under the new URL: reset the failure counter and degraded state
    if (this.degraded) {
      this.degraded = false
      this.onDegraded(false)
    }
    this.errorCount = 0
    this.evaluate()
  }

  /**
   * 瓦片错误观察：MapLibre 的 error 事件无 sourceId（仅 error.message）， / Tile error watcher: MapLibre's error event carries no sourceId (only error.message),
   * 按当前源站域名匹配本层瓦片错误；忽略样式重载/瓦片回收产生的 abort。 / so this layer's tile errors are matched by the current origin host; aborts from style reloads/tile eviction are ignored.
   */
  private readonly onMapError = (e: unknown): void => {
    const msg = ((e as { error?: { message?: string } })?.error?.message ?? '').toLowerCase()
    if (!msg || msg.includes('abort')) {
      return
    }
    const host = this.provider === 'gbif' ? 'api.gbif.org' : 'api.inaturalist.org'
    if (!msg.includes(host)) {
      return
    }
    this.errorCount++
    if (this.errorCount > TILE_ERROR_LIMIT && !this.degraded) {
      this.degraded = true
      this.onDegraded(true)
      this.evaluate()
    }
  }

  /** 判定当前是否处于全球视角，并渐变切换透明度 / Decide whether the view is currently global and crossfade the opacity */
  private readonly evaluate = (): void => {
    const map = this.map
    if (!map || !map.getLayer(LAYER_ID)) {
      return
    }
    const zoom = map.getZoom()
    const c = map.getCenter()
    const b = this.cityBBox
    const outside = b ? c.lng < b[0] || c.lng > b[2] || c.lat < b[1] || c.lat > b[3] : true
    // 阈值取 min(GLOBAL_ZOOM, 城市完整显示级别 − 偏移)，保证城市默认视野始终是城市视角； / Threshold = min(GLOBAL_ZOOM, city full-display zoom − offset), so the default city view always stays in city view;
    // cameraForBounds 依赖当前画布尺寸，每次判定时现算（开销可忽略），窗口缩放后自然生效 / cameraForBounds depends on the current canvas size, so it is recomputed on each check (negligible cost) and adapts naturally after window resizes
    let threshold = GLOBAL_ZOOM
    if (b) {
      const fitZoom = map.cameraForBounds([[b[0], b[1]], [b[2], b[3]]], { padding: 48 })?.zoom
      if (fitZoom !== undefined) {
        threshold = Math.min(GLOBAL_ZOOM, fitZoom - GLOBAL_ZOOM_OFFSET)
      }
    }
    const inGlobal = zoom < threshold || outside
    const show = this.enabled && inGlobal && !this.degraded
    map.setLayoutProperty(LAYER_ID, 'visibility', show ? 'visible' : 'none')
    map.setPaintProperty(LAYER_ID, 'raster-opacity', show ? 0.9 : 0)
    if (inGlobal !== this.inGlobal) {
      this.inGlobal = inGlobal
      this.onGlobalChange(inGlobal)
    }
  }
}
