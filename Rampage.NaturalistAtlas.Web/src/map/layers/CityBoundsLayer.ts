/**
 * 城市范围图层 / City bounds layer
 *
 * 绘制当前城市 bbox：虚线边框 + 城市外侧轻遮罩（"聚光灯"效果，引导视线聚焦城市）。 / Draws the current city bbox: a dashed outline plus a light mask outside the city (a "spotlight" effect that draws the eye to the city).
 * 遮罩为"全球外环 − 城市内环"的带洞多边形。 / The mask is a polygon with a hole — the world outer ring minus the city inner ring.
 */
import type { Map as MlMap, GeoJSONSource } from 'maplibre-gl'
import type { Feature, Polygon } from 'geojson'
import type { BBox } from '@/api/types'
import type { ThemeId } from '@/theme/themes'
import type { AtlasLayer } from '../MapEngine'
import { firstSymbolLayerId } from '../viewModes'

const SOURCE_ID = 'atlas-city-bounds'
const MASK_LAYER = 'atlas-city-mask'
const LINE_LAYER = 'atlas-city-line'

/** 各主题线色/遮罩色 / Line/mask colors per theme */
const STYLE: Record<ThemeId, { line: string; mask: string; maskOpacity: number }> = {
  night: { line: '#7cf2b0', mask: '#000000', maskOpacity: 0.35 },
  journal: { line: '#7a4f2a', mask: '#3b2f22', maskOpacity: 0.12 },
  spectrum: { line: '#2f9e5b', mask: '#17202b', maskOpacity: 0.1 }
}

/** bbox → 城市矩形环 / bbox → city rectangle ring */
function ring(b: BBox): number[][] {
  return [
    [b[0], b[1]],
    [b[2], b[1]],
    [b[2], b[3]],
    [b[0], b[3]],
    [b[0], b[1]]
  ]
}

export class CityBoundsLayer implements AtlasLayer {
  readonly id = 'city-bounds'

  private map: MlMap | null = null
  private theme: ThemeId = 'night'
  private bbox: BBox | null = null
  private visible = true

  /** 设置城市范围 / Set the city bounds */
  setBBox(bbox: BBox | null): void {
    this.bbox = bbox
    ;(this.map?.getSource(SOURCE_ID) as GeoJSONSource | undefined)?.setData(this.geojson())
  }

  /** 显示/隐藏 / Show/hide */
  setVisible(visible: boolean): void {
    this.visible = visible
    for (const id of [MASK_LAYER, LINE_LAYER]) {
      if (this.map?.getLayer(id)) {
        this.map.setLayoutProperty(id, 'visibility', visible ? 'visible' : 'none')
      }
    }
  }

  setTheme(theme: ThemeId): void {
    this.theme = theme
  }

  /** 生成 GeoJSON：遮罩（带洞）+ 边框 / Build the GeoJSON: mask (with hole) + outline */
  private geojson(): Feature<Polygon> {
    const coords = this.bbox ? [ring([-180, -85, 180, 85]), ring(this.bbox).reverse()] : [ring([0, 0, 0, 0])]
    return { type: 'Feature', geometry: { type: 'Polygon', coordinates: coords }, properties: {} }
  }

  mount(map: MlMap): void {
    this.map = map
    if (!map.getSource(SOURCE_ID)) {
      map.addSource(SOURCE_ID, { type: 'geojson', data: this.geojson() })
    }
    const s = STYLE[this.theme]
    const before = firstSymbolLayerId(map)
    const visibility = this.visible ? 'visible' : 'none'
    if (!map.getLayer(MASK_LAYER)) {
      map.addLayer(
        {
          id: MASK_LAYER,
          type: 'fill',
          source: SOURCE_ID,
          layout: { visibility },
          // 缩小到全球尺度时遮罩淡出，避免整张地球变暗 / The mask fades out at global scale so the whole globe does not go dark
          paint: { 'fill-color': s.mask, 'fill-opacity': ['interpolate', ['linear'], ['zoom'], 5, 0, 8, s.maskOpacity] }
        },
        before
      )
    }
    if (!map.getLayer(LINE_LAYER)) {
      map.addLayer(
        {
          id: LINE_LAYER,
          type: 'line',
          source: SOURCE_ID,
          layout: { visibility, 'line-join': 'round' },
          paint: { 'line-color': s.line, 'line-width': 1.5, 'line-dasharray': [3, 2], 'line-opacity': 0.8 }
        },
        before
      )
    }
  }

  unmount(map: MlMap): void {
    for (const id of [LINE_LAYER, MASK_LAYER]) {
      if (map.getLayer(id)) {
        map.removeLayer(id)
      }
    }
    if (map.getSource(SOURCE_ID)) {
      map.removeSource(SOURCE_ID)
    }
    this.map = null
  }
}
