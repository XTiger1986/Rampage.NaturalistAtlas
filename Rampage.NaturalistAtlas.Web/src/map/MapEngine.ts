/**
 * 地图引擎（设计文档 §4 地图内核） / Map engine (design doc §4 map kernel)
 *
 * 职责： / Responsibilities:
 * - 持有唯一的 MapLibre 实例（刻意不进入 Vue 响应式系统）； / - Holds the single MapLibre instance (deliberately kept out of Vue reactivity);
 * - 管理"底图 × 主题"样式切换：setStyle 后自动恢复视图模式与所有自有图层； / - Manages "basemap × theme" style switching: after setStyle, the view mode and all owned layers are restored automatically;
 * - 统一的自有图层注册机制（AtlasLayer），图层在每次样式加载后被重新 mount； / - Unified owned-layer registry (AtlasLayer); layers are re-mounted after every style load;
 * - 提供定位能力：fitBBox / flyTo。 / - Navigation helpers: fitBBox / flyTo.
 *
 * 为什么需要重新 mount：MapLibre 的 setStyle 会清空所有 source/layer（含自有图层）， / Why remounting is needed: MapLibre setStyle wipes every source/layer (owned ones included),
 * 因此自有图层必须实现幂等的 mount()，由引擎在 style.load 后统一调用。 / so owned layers must implement an idempotent mount(), invoked by the engine on style.load.
 * HTML Marker（照片卡）不属于样式，不受 setStyle 影响。 / HTML Markers (photo cards) are not part of the style and are unaffected by setStyle.
 */
import maplibregl from 'maplibre-gl'
import type { Map as MlMap, StyleSpecification } from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import type { ThemeId } from '@/theme/themes'
import type { BBox, LngLat } from '@/api/types'
import { isVectorBasemap, patchVectorStyle, resolveBasemapStyle } from './basemaps'
import type { BasemapId } from './basemaps'
import { applyViewMode } from './viewModes'
import type { ViewMode } from './viewModes'

/** 自有图层接口 / Owned layer interface */
export interface AtlasLayer {
  /** 图层唯一标识 / Unique layer identifier */
  readonly id: string
  /** 样式（重新）加载后调用：添加 source/layer/事件，须幂等 / Called after the style (re)loads: add sources/layers/events; must be idempotent */
  mount(map: MlMap): void
  /** 从地图移除（组件卸载时） / Remove from the map (on component unmount) */
  unmount(map: MlMap): void
  /** 主题变化通知（可选；样式重载前调用，mount 时应使用新主题） / Optional theme-change notice (called before the style reloads; mount should use the new theme) */
  setTheme?(theme: ThemeId): void
}

/** 引擎初始化参数 / Engine init options */
export interface EngineInitOptions {
  container: HTMLElement
  center: LngLat
  zoom: number
  theme: ThemeId
  basemap: BasemapId
  mode: ViewMode
}

/** 空白样式（初始化占位，随后立即 setStyle） / Empty style (init placeholder; a real setStyle follows immediately) */
const EMPTY_STYLE: StyleSpecification = { version: 8, sources: {}, layers: [] }

class MapEngine {
  /** MapLibre 实例 / MapLibre instance */
  private map: MlMap | null = null
  /** 当前主题 / Current theme */
  private theme: ThemeId = 'night'
  /** 当前底图 / Current basemap */
  private basemap: BasemapId = 'ofm'
  /** 当前视图模式 / Current view mode */
  private mode: ViewMode = '2d'
  /** 已注册的自有图层（按注册顺序 mount，即图层栈自下而上顺序） / Registered owned layers (mounted in registration order, i.e. bottom-up layer-stack order) */
  private readonly layers = new Map<string, AtlasLayer>()
  /** 样式是否已加载完成 / Whether the style has finished loading */
  private styleReady = false
  /** 样式加载完成回调（MapCanvas 用于标记 ready） / Style-ready callbacks (MapCanvas marks readiness with them) */
  private readonly readyHandlers = new Set<() => void>()

  /** 取地图实例（未初始化时为 null） / Get the map instance (null before init) */
  get instance(): MlMap | null {
    return this.map
  }

  /** 初始化地图 / Initialize the map */
  init(opts: EngineInitOptions): MlMap {
    this.destroy()
    this.theme = opts.theme
    this.basemap = opts.basemap
    this.mode = opts.mode

    const map = new maplibregl.Map({
      container: opts.container,
      style: EMPTY_STYLE,
      center: opts.center,
      zoom: opts.zoom,
      // 视图模式在样式加载后应用；2D 初始锁定倾角 / View mode is applied after the style loads; 2D locks pitch from the start
      maxPitch: opts.mode === '2d' ? 0 : 85,
      attributionControl: false,
      // 瓦片请求失败（如海外服务偶发不稳）不打断交互，仅在控制台提示 / Tile request failures (e.g. flaky overseas services) never interrupt interaction; only a console notice
      cancelPendingTileRequestsWhileZooming: true
    })
    this.map = map

    // 控件：缩放/指北针（右下）、比例尺（左下）、紧凑署名（右下） / Controls: zoom/compass (bottom-right), scale bar (bottom-left), compact attribution (bottom-right)
    map.addControl(new maplibregl.NavigationControl({ visualizePitch: true }), 'bottom-right')
    map.addControl(new maplibregl.ScaleControl({ maxWidth: 110 }), 'bottom-left')
    map.addControl(new maplibregl.AttributionControl({ compact: true }), 'bottom-right')

    // 每次样式加载完成：恢复视图模式 → 重新挂载自有图层 → 通知就绪 / On every style load: restore view mode → remount owned layers → notify readiness
    map.on('style.load', () => {
      this.styleReady = true
      applyViewMode(map, this.mode, this.theme, false)
      for (const layer of this.layers.values()) {
        layer.mount(map)
      }
      for (const h of this.readyHandlers) {
        h()
      }
    })
    map.on('error', (e) => {
      // 单个瓦片/资源失败不影响整体，记录即可 / A single tile/resource failure does not affect the whole map; just log it
      console.warn('[map]', e.error?.message ?? e)
    })

    this.loadStyle()
    return map
  }

  /** 按当前"底图 × 主题"加载样式 / Load the style for the current "basemap × theme" */
  private loadStyle(): void {
    const map = this.map
    if (!map) {
      return
    }
    this.styleReady = false
    const style = resolveBasemapStyle(this.basemap, this.theme)
    const theme = this.theme
    const vector = isVectorBasemap(this.basemap)
    map.setStyle(style, {
      diff: false,
      // 矢量底图在样式应用前打主题补丁（背景/水体/绿地色） / Vector basemaps get the theme patch (background/water/green colors) before the style is applied
      transformStyle: vector ? (_prev, next) => patchVectorStyle(next, theme) : undefined
    })
  }

  /** 切换主题（重载底图样式，自有图层随后自动重新挂载） / Switch theme (basemap style reloads; owned layers remount automatically afterwards) */
  setTheme(theme: ThemeId): void {
    if (theme === this.theme) {
      return
    }
    this.theme = theme
    for (const layer of this.layers.values()) {
      layer.setTheme?.(theme)
    }
    this.loadStyle()
  }

  /** 切换底图 / Switch basemap */
  setBasemap(basemap: BasemapId): void {
    if (basemap === this.basemap) {
      return
    }
    this.basemap = basemap
    this.loadStyle()
  }

  /** 切换视图模式（带动画） / Switch view mode (animated) */
  setMode(mode: ViewMode): void {
    this.mode = mode
    if (this.map && this.styleReady) {
      applyViewMode(this.map, mode, this.theme, true)
    }
  }

  /** 注册自有图层（样式已就绪则立即挂载） / Register an owned layer (mounted immediately if the style is ready) */
  addLayer(layer: AtlasLayer): void {
    this.layers.set(layer.id, layer)
    layer.setTheme?.(this.theme)
    if (this.map && this.styleReady) {
      layer.mount(this.map)
    }
  }

  /** 注销自有图层 / Unregister an owned layer */
  removeLayer(id: string): void {
    const layer = this.layers.get(id)
    if (layer && this.map) {
      layer.unmount(this.map)
    }
    this.layers.delete(id)
  }

  /** 订阅样式就绪（返回取消订阅函数） / Subscribe to style-ready (returns an unsubscribe function) */
  onReady(handler: () => void): () => void {
    this.readyHandlers.add(handler)
    if (this.styleReady) {
      handler()
    }
    return () => this.readyHandlers.delete(handler)
  }

  /** 视野适配到 bbox（城市定位） / Fit the viewport to a bbox (city locator) */
  fitBBox(bbox: BBox, animate = true): void {
    this.map?.fitBounds(
      [
        [bbox[0], bbox[1]],
        [bbox[2], bbox[3]]
      ],
      // essential:false —— 尊重 prefers-reduced-motion（设计文档 §7.3：减动效偏好时相机瞬移）
      // essential:false respects prefers-reduced-motion (design doc §7.3: instant camera under reduced motion)
      { padding: 48, duration: animate ? 1600 : 0, essential: false }
    )
  }

  /** 飞行到指定点（国家/省份定位、观察点定位） / Fly to a point (country/province locator, observation locator) */
  flyTo(center: LngLat, zoom?: number): void {
    this.map?.flyTo({ center, zoom: zoom ?? this.map.getZoom(), speed: 1.4, curve: 1.5, essential: false })
  }

  /** 当前视野 bbox / Current viewport bbox */
  getBBox(): BBox | null {
    const b = this.map?.getBounds()
    return b ? [b.getWest(), b.getSouth(), b.getEast(), b.getNorth()] : null
  }

  /** 销毁地图（释放 WebGL 资源） / Destroy the map (release WebGL resources) */
  destroy(): void {
    if (this.map) {
      for (const layer of this.layers.values()) {
        layer.unmount(this.map)
      }
      this.map.remove()
    }
    this.map = null
    this.styleReady = false
  }
}

/** 全局唯一地图引擎 / The single global map engine */
export const mapEngine = new MapEngine()
