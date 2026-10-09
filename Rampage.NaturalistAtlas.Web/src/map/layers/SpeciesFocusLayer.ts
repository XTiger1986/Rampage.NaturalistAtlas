/**
 * 物种聚焦图层（物种详情页，产品文档 §8.9） / Species focus layer (species detail page, product doc §8.9)
 *
 * 进入物种详情时启用，离开时注销： / Enabled on entering species detail, unregistered on leaving:
 * - 全球分布范围：iNaturalist 物种 → v1/taxon_ranges 瓦片（分布范围多边形）； / - Global range: iNaturalist taxa → v1/taxon_ranges tiles (range polygons);
 *                 GBIF 物种 → GBIF 密度瓦片按 taxonKey 过滤； / GBIF taxa → GBIF density tiles filtered by taxonKey;
 * - 本市分布点：本站明细点位，类群色圆点 + 光晕。 / - Local points: this site's detail points as taxon-colored dots + halos.
 */
import type { Map as MlMap, GeoJSONSource } from 'maplibre-gl'
import type { FeatureCollection, Point } from 'geojson'
import type { ObservationPoint, TaxonInfo } from '@/api/types'
import type { ThemeId } from '@/theme/themes'
import { taxonColor } from '@/theme/taxonPalette'
import type { AtlasLayer } from '../MapEngine'
import { firstSymbolLayerId } from '../viewModes'

const RANGE_SOURCE = 'atlas-species-range'
const RANGE_LAYER = 'atlas-species-range'
const POINT_SOURCE = 'atlas-species-points'
const POINT_HALO = 'atlas-species-halo'
const POINT_LAYER = 'atlas-species-points'

export class SpeciesFocusLayer implements AtlasLayer {
  readonly id = 'species-focus'

  private map: MlMap | null = null
  private theme: ThemeId = 'night'
  private taxon: TaxonInfo | null = null
  private points: FeatureCollection<Point> = { type: 'FeatureCollection', features: [] }

  /** 设置聚焦物种与本市分布点 / Set the focused taxon and its local points */
  setSpecies(taxon: TaxonInfo, points: ObservationPoint[]): void {
    this.taxon = taxon
    this.points = {
      type: 'FeatureCollection',
      features: points.map((p) => ({ type: 'Feature', geometry: { type: 'Point', coordinates: [p.lng, p.lat] }, properties: { id: p.id } }))
    }
    if (this.map) {
      // 物种变化：重建范围瓦片（URL 含物种 id），刷新点位 / Taxon changed: rebuild the range tiles (the URL embeds the taxon id) and refresh the points
      this.unmount(this.map)
      this.mount(this.map)
    }
  }

  setTheme(theme: ThemeId): void {
    this.theme = theme
  }

  /** 分布范围瓦片 URL / Range tile URL */
  private rangeTiles(): { tiles: string[]; tileSize: number } | null {
    const t = this.taxon
    if (!t) {
      return null
    }
    const color = encodeURIComponent(taxonColor(t.iconicTaxon, this.theme))
    if (t.source === 1) {
      return { tiles: [`https://api.inaturalist.org/v1/taxon_ranges/${t.sourceTaxonId}/{z}/{x}/{y}.png?color=${color}`], tileSize: 256 }
    }
    return {
      tiles: [`https://api.gbif.org/v2/map/occurrence/density/{z}/{x}/{y}@1x.png?srs=EPSG:3857&style=purpleYellow.point&taxonKey=${t.sourceTaxonId}`],
      tileSize: 512
    }
  }

  mount(map: MlMap): void {
    this.map = map
    const before = firstSymbolLayerId(map)
    const range = this.rangeTiles()
    if (range && !map.getSource(RANGE_SOURCE)) {
      map.addSource(RANGE_SOURCE, { type: 'raster', ...range, maxzoom: 12, attribution: 'iNaturalist / GBIF' })
      map.addLayer({ id: RANGE_LAYER, type: 'raster', source: RANGE_SOURCE, paint: { 'raster-opacity': 0.55 } }, before)
    }
    if (!map.getSource(POINT_SOURCE)) {
      map.addSource(POINT_SOURCE, { type: 'geojson', data: this.points })
    } else {
      ;(map.getSource(POINT_SOURCE) as GeoJSONSource).setData(this.points)
    }
    const color = taxonColor(this.taxon?.iconicTaxon ?? 14, this.theme)
    if (!map.getLayer(POINT_HALO)) {
      map.addLayer({
        id: POINT_HALO,
        type: 'circle',
        source: POINT_SOURCE,
        paint: { 'circle-radius': 12, 'circle-color': color, 'circle-opacity': 0.18, 'circle-blur': 0.8 }
      })
    }
    if (!map.getLayer(POINT_LAYER)) {
      map.addLayer({
        id: POINT_LAYER,
        type: 'circle',
        source: POINT_SOURCE,
        paint: {
          'circle-radius': ['interpolate', ['linear'], ['zoom'], 8, 3, 15, 6],
          'circle-color': color,
          'circle-stroke-color': this.theme === 'night' ? '#0b1512' : '#ffffff',
          'circle-stroke-width': 1.2
        }
      })
    }
  }

  unmount(map: MlMap): void {
    for (const id of [POINT_LAYER, POINT_HALO, RANGE_LAYER]) {
      if (map.getLayer(id)) {
        map.removeLayer(id)
      }
    }
    for (const id of [POINT_SOURCE, RANGE_SOURCE]) {
      if (map.getSource(id)) {
        map.removeSource(id)
      }
    }
  }
}
