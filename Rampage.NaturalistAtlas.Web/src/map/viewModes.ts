/**
 * 2D / 2.5D / 3D 视图模式（设计文档 §4.2 视图模式状态机） / 2D / 2.5D / 3D view modes (design doc §4.2 view-mode state machine)
 *
 * 单引擎（MapLibre GL v5）实现三种模式，切换无需重建地图： / One engine (MapLibre GL v5) implements all three modes; switching never rebuilds the map:
 *
 * | 模式 | 投影      | 倾角   | 地形 DEM | 山体阴影 | 建筑挤出 | 天空/大气 | 旋转 | / | mode | projection | pitch | terrain DEM | hillshade | building extrusion | sky/atmosphere | rotate |
 * |------|-----------|--------|----------|----------|----------|-----------|------|
 * | 2d   | mercator  | 0（锁定）| 关     | 关       | 关       | 关        | 禁用 | / | 2d | mercator | 0 (locked) | off | off | off | off | disabled |
 * | 2.5d | mercator  | 55°    | 关       | 关       | 开       | 关        | 允许 | / | 2.5d | mercator | 55° | off | off | on | off | allowed |
 * | 3d   | globe     | 缩放≥6 时 60° | 开（×1.4）| 开 | 开      | 开        | 允许 | / | 3d | globe | 60° at zoom ≥ 6 | on (×1.4) | on | on | on | allowed |
 *
 * 3D 模式下 globe 投影在高缩放级别会自动过渡为墨卡托观感，因此"地球仪 → 城市地形"是连续的。 / In 3D the globe projection automatically blends into a Mercator look at high zoom, so "globe → city terrain" is continuous.
 * 地形数据：AWS Terrain Tiles（Terrarium 编码，免费无需 key）。 / Terrain data: AWS Terrain Tiles (Terrarium encoding, free, no key needed).
 */
import type { Map as MlMap, SkySpecification } from 'maplibre-gl'
import type { ThemeId } from '@/theme/themes'
import { themePatch } from './basemaps'

/** 视图模式 / View mode */
export type ViewMode = '2d' | '2.5d' | '3d'

/** 自有图层/数据源 id（统一 atlas- 前缀，避免与底图冲突） / Owned layer/source ids (uniform atlas- prefix to avoid clashing with basemaps) */
export const DEM_SOURCE = 'atlas-dem'
const HILLSHADE_SOURCE = 'atlas-hillshade-dem'
const HILLSHADE_LAYER = 'atlas-hillshade'
const BUILDING_LAYER = 'atlas-building-3d'

/** Terrarium 高程瓦片 / Terrarium elevation tiles */
const TERRARIUM_TILES = ['https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png']

/** 地形夸张系数 / Terrain exaggeration factor */
const TERRAIN_EXAGGERATION = 1.4

/** 各主题天空/大气配置 / Sky/atmosphere configuration per theme */
function skyOf(theme: ThemeId): SkySpecification {
  if (theme === 'night') {
    return {
      'sky-color': '#07131a',
      'horizon-color': '#15342f',
      'fog-color': '#0b1512',
      'sky-horizon-blend': 0.6,
      'horizon-fog-blend': 0.5,
      'fog-ground-blend': 0.4,
      'atmosphere-blend': ['interpolate', ['linear'], ['zoom'], 0, 1, 8, 0.6, 12, 0]
    }
  }
  if (theme === 'journal') {
    return {
      'sky-color': '#e6dcc4',
      'horizon-color': '#f3ecdc',
      'fog-color': '#efe6d2',
      'sky-horizon-blend': 0.5,
      'horizon-fog-blend': 0.6,
      'fog-ground-blend': 0.5,
      'atmosphere-blend': ['interpolate', ['linear'], ['zoom'], 0, 0.8, 10, 0]
    }
  }
  return {
    'sky-color': '#9fcdf0',
    'horizon-color': '#e8f3fb',
    'fog-color': '#f4f6f3',
    'sky-horizon-blend': 0.5,
    'horizon-fog-blend': 0.6,
    'fog-ground-blend': 0.5,
    'atmosphere-blend': ['interpolate', ['linear'], ['zoom'], 0, 1, 10, 0]
  }
}

/** 取第一个 symbol 图层 id（自有面/栅格图层插在其下，保证地名注记在最上层） / Get the first symbol layer id (owned fill/raster layers insert below it so place labels stay on top) */
export function firstSymbolLayerId(map: MlMap): string | undefined {
  return map.getStyle().layers.find((l) => l.type === 'symbol')?.id
}

/** 确保 DEM 数据源与山体阴影、建筑挤出图层存在（幂等；样式重载后需重新调用） / Ensure the DEM sources plus the hillshade and building-extrusion layers exist (idempotent; re-invoke after style reloads) */
function ensureTerrainAssets(map: MlMap, theme: ThemeId): void {
  if (!map.getSource(DEM_SOURCE)) {
    map.addSource(DEM_SOURCE, {
      type: 'raster-dem',
      tiles: TERRARIUM_TILES,
      encoding: 'terrarium',
      tileSize: 256,
      maxzoom: 15,
      attribution: 'Terrain © Mapzen, AWS Open Data'
    })
  }
  // 山体阴影使用独立数据源（MapLibre 建议地形与阴影分开，避免分辨率相互影响） / Hillshade uses its own source (MapLibre recommends keeping terrain and hillshade separate so resolutions do not interfere)
  if (!map.getSource(HILLSHADE_SOURCE)) {
    map.addSource(HILLSHADE_SOURCE, {
      type: 'raster-dem',
      tiles: TERRARIUM_TILES,
      encoding: 'terrarium',
      tileSize: 256,
      maxzoom: 15
    })
  }
  if (!map.getLayer(HILLSHADE_LAYER)) {
    const night = theme === 'night'
    map.addLayer(
      {
        id: HILLSHADE_LAYER,
        type: 'hillshade',
        source: HILLSHADE_SOURCE,
        layout: { visibility: 'none' },
        paint: {
          'hillshade-exaggeration': night ? 0.45 : 0.35,
          'hillshade-shadow-color': night ? '#000000' : '#5a4a38',
          'hillshade-highlight-color': night ? '#2c4a42' : '#ffffff',
          'hillshade-accent-color': night ? '#0b1512' : '#8c7a62'
        }
      },
      firstSymbolLayerId(map)
    )
  }
  // 建筑挤出：仅矢量底图（OpenMapTiles 的 building 图层）可用 / Building extrusion: available only on vector basemaps (the OpenMapTiles building layer)
  if (!map.getLayer(BUILDING_LAYER) && map.getSource('openmaptiles')) {
    map.addLayer(
      {
        id: BUILDING_LAYER,
        type: 'fill-extrusion',
        source: 'openmaptiles',
        'source-layer': 'building',
        minzoom: 13,
        layout: { visibility: 'none' },
        paint: {
          'fill-extrusion-color': themePatch(theme).building,
          'fill-extrusion-height': ['coalesce', ['get', 'render_height'], 8],
          'fill-extrusion-base': ['coalesce', ['get', 'render_min_height'], 0],
          'fill-extrusion-opacity': ['interpolate', ['linear'], ['zoom'], 13, 0, 14.5, theme === 'night' ? 0.75 : 0.85]
        }
      },
      firstSymbolLayerId(map)
    )
  }
}

/** 设置图层可见性（图层不存在时忽略） / Set layer visibility (ignored when the layer does not exist) */
function setVisible(map: MlMap, layerId: string, visible: boolean): void {
  if (map.getLayer(layerId)) {
    map.setLayoutProperty(layerId, 'visibility', visible ? 'visible' : 'none')
  }
}

/**
 * 应用视图模式 / Apply a view mode
 * @param map 地图实例（样式须已加载） / map instance (the style must be loaded)
 * @param mode 目标模式 / target mode
 * @param theme 当前主题（天空色、阴影色） / current theme (sky and hillshade colors)
 * @param animate 是否动画过渡（样式重载后的恢复调用传 false） / whether to animate (pass false for the restore call after a style reload)
 */
export function applyViewMode(map: MlMap, mode: ViewMode, theme: ThemeId, animate = true): void {
  ensureTerrainAssets(map, theme)
  const duration = animate ? 1200 : 0

  if (mode === '2d') {
    map.setTerrain(null)
    map.setProjection({ type: 'mercator' })
    setVisible(map, HILLSHADE_LAYER, false)
    setVisible(map, BUILDING_LAYER, false)
    map.dragRotate.disable()
    map.touchZoomRotate.disableRotation()
    map.easeTo({ pitch: 0, bearing: 0, duration })
    // 动画结束后锁定倾角，防止键盘/手势再次倾斜 / Lock pitch once the animation ends so keyboard/gestures cannot tilt again
    map.once('moveend', () => map.setMaxPitch(0))
    return
  }

  map.setMaxPitch(85)
  map.dragRotate.enable()
  map.touchZoomRotate.enableRotation()
  setVisible(map, BUILDING_LAYER, true)

  if (mode === '2.5d') {
    map.setTerrain(null)
    map.setProjection({ type: 'mercator' })
    setVisible(map, HILLSHADE_LAYER, false)
    map.easeTo({ pitch: 55, duration })
    return
  }

  // 3d：地球仪 + 地形 + 天空 / 3d: globe + terrain + sky
  map.setProjection({ type: 'globe' })
  map.setTerrain({ source: DEM_SOURCE, exaggeration: TERRAIN_EXAGGERATION })
  map.setSky(skyOf(theme))
  setVisible(map, HILLSHADE_LAYER, true)
  map.easeTo({ pitch: map.getZoom() >= 6 ? 60 : 0, duration })
}
