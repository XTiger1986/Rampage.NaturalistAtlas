/**
 * 本地热力图层（城市视角，P1） / Local heatmap layer (city view, P1)
 *
 * 数据与照片卡共用同一份点位（observation/points），GPU 端渲染，10 万点量级流畅。 / Shares the same points endpoint with photo cards (observation/points), renders on the GPU, smooth at the 100k-point scale.
 * 色带按主题区分：夜观为"萤火"青绿→金黄，纸本为赭石系，光谱为冷暖渐变。 / Color ramps differ per theme: night is a "firefly" teal→gold ramp, journal an ochre family, spectrum a cool-to-warm gradient.
 */
import type { Map as MlMap, GeoJSONSource } from 'maplibre-gl'
import type { FeatureCollection, Point } from 'geojson'
import type { ObservationPoint } from '@/api/types'
import type { ThemeId } from '@/theme/themes'
import type { AtlasLayer } from '../MapEngine'
import { firstSymbolLayerId } from '../viewModes'

const SOURCE_ID = 'atlas-heat'
const LAYER_ID = 'atlas-heat'

/** 各主题热力色带（密度 0 → 1） / Heatmap color ramps per theme (density 0 → 1) */
const RAMPS: Record<ThemeId, string[]> = {
  night: ['rgba(0,0,0,0)', '#0f5a4a', '#1fb88a', '#7cf2b0', '#e8ff8a', '#fff6c2'],
  journal: ['rgba(0,0,0,0)', '#d9c9a3', '#c69a5a', '#a8663a', '#7a3f22', '#4a2414'],
  spectrum: ['rgba(0,0,0,0)', '#9bd3f5', '#4fb3a0', '#f2c14e', '#f07a3a', '#d6336c']
}

export class HeatmapLayer implements AtlasLayer {
  readonly id = 'heatmap'

  private theme: ThemeId = 'night'
  private visible = false
  private data: FeatureCollection<Point> = { type: 'FeatureCollection', features: [] }
  private map: MlMap | null = null

  /** 灌入点位 / Load points */
  setData(points: ObservationPoint[]): void {
    this.data = {
      type: 'FeatureCollection',
      features: points.map((p) => ({
        type: 'Feature',
        geometry: { type: 'Point', coordinates: [p.lng, p.lat] },
        properties: {}
      }))
    }
    ;(this.map?.getSource(SOURCE_ID) as GeoJSONSource | undefined)?.setData(this.data)
  }

  /** 显示/隐藏 / Show/hide */
  setVisible(visible: boolean): void {
    this.visible = visible
    if (this.map?.getLayer(LAYER_ID)) {
      this.map.setLayoutProperty(LAYER_ID, 'visibility', visible ? 'visible' : 'none')
    }
  }

  setTheme(theme: ThemeId): void {
    this.theme = theme
  }

  mount(map: MlMap): void {
    this.map = map
    if (!map.getSource(SOURCE_ID)) {
      map.addSource(SOURCE_ID, { type: 'geojson', data: this.data })
    }
    if (!map.getLayer(LAYER_ID)) {
      const ramp = RAMPS[this.theme]
      map.addLayer(
        {
          id: LAYER_ID,
          type: 'heatmap',
          source: SOURCE_ID,
          maxzoom: 17,
          layout: { visibility: this.visible ? 'visible' : 'none' },
          paint: {
            'heatmap-weight': 1,
            'heatmap-intensity': ['interpolate', ['linear'], ['zoom'], 8, 0.6, 15, 2],
            'heatmap-radius': ['interpolate', ['linear'], ['zoom'], 8, 10, 15, 28],
            'heatmap-opacity': ['interpolate', ['linear'], ['zoom'], 14, 0.85, 17, 0],
            'heatmap-color': [
              'interpolate',
              ['linear'],
              ['heatmap-density'],
              0, ramp[0]!, 0.15, ramp[1]!, 0.35, ramp[2]!, 0.55, ramp[3]!, 0.8, ramp[4]!, 1, ramp[5]!
            ]
          }
        },
        firstSymbolLayerId(map)
      )
    }
  }

  unmount(map: MlMap): void {
    if (map.getLayer(LAYER_ID)) {
      map.removeLayer(LAYER_ID)
    }
    if (map.getSource(SOURCE_ID)) {
      map.removeSource(SOURCE_ID)
    }
    this.map = null
  }
}
