/**
 * 照片卡聚合图层（产品核心体验，设计文档 §5.1） / Photo-card clustering layer (the core product experience, design doc §5.1)
 *
 * 渲染策略： / Rendering strategy:
 * - 使用 supercluster 在客户端聚合点位（点位接口一次拉取城市级数据，缩放/平移零请求）； / - Points are clustered client-side with supercluster (the points endpoint fetches city-scale data once; zoom/pan issues zero requests);
 * - 聚合簇与照片卡均为 HTML Marker（便于做照片、光晕、标本签等富样式），按视野 diff 增删， / - Clusters and photo cards are both HTML Markers (easy rich styling: photos, halos, specimen labels), added/removed by viewport diff;
 *   单屏 Marker 数量上限 MAX_MARKERS，超出时 supercluster 半径保证数量可控； / per-screen Marker count is capped at MAX_MARKERS; beyond that the supercluster radius keeps it controlled;
 * - 聚合簇外环为"类群构成色环"（conic-gradient），一眼看清该区域物种构成； / - A cluster's outer ring is a "taxon composition ring" (conic-gradient) showing the local species mix at a glance;
 * - 缩放到 CARD_LABEL_ZOOM 以上时，照片卡展开为带物种名的大卡片。 / - Zooming past CARD_LABEL_ZOOM expands photo cards into large cards bearing species names.
 *
 * 交互：点击聚合簇 → 缩放到其展开级别；点击照片卡 → onOpen(id) 打开大图。 / Interaction: click a cluster → zoom to its expansion level; click a photo card → onOpen(id) opens the lightbox.
 * 性能预算：1 万点建索引 < 100ms；每次 moveend 重绘 < 16ms。 / Performance budget: indexing 10k points < 100ms; each moveend repaint < 16ms.
 */
import Supercluster from 'supercluster'
import maplibregl from 'maplibre-gl'
import type { Map as MlMap } from 'maplibre-gl'
import type { ObservationPoint } from '@/api/types'
import type { ThemeId } from '@/theme/themes'
import { TAXA, taxonColor } from '@/theme/taxonPalette'
import { licenseLabel } from '@/utils/format'
import type { AtlasLayer } from '../MapEngine'
import './markers.css'

/** 叶子点属性 / Leaf point properties */
interface LeafProps {
  id: string
  iconic: number
  photo: string | null
  name: string | null
  license: number | null
}

/** 聚合簇属性：k{类群代码} = 该类群点数 / Cluster properties: k{taxon code} = point count of that taxon */
type ClusterProps = Record<string, number>

/** 单屏 Marker 上限 / Per-screen Marker cap */
const MAX_MARKERS = 400
/** 照片卡展开为带名称大卡片的缩放级别 / Zoom level at which photo cards expand into large labeled cards */
const CARD_LABEL_ZOOM = 15
/** supercluster 最大聚合级别（超过后全部散开） / supercluster max clustering zoom (above it everything unclusters) */
const CLUSTER_MAX_ZOOM = 16

/** 图层构造参数 / Layer constructor options */
export interface ObservationCardLayerOptions {
  /** 点击照片卡 / Photo card clicked */
  onOpen: (id: string) => void
}

export class ObservationCardLayer implements AtlasLayer {
  readonly id = 'observation-cards'

  private map: MlMap | null = null
  private theme: ThemeId = 'night'
  private visible = true
  private index: Supercluster<LeafProps, ClusterProps> | null = null
  /** 当前已渲染 Marker（key: c{clusterId} / p{obsId}） / Currently rendered Markers (key: c{clusterId} / p{obsId}) */
  private readonly markers = new Map<string, maplibregl.Marker>()
  /** 当前高亮的观察 id / Currently highlighted observation id */
  private highlightId: string | null = null
  /** 上次渲染时的缩放档（跨越 CARD_LABEL_ZOOM 时需重建卡片样式） / Label mode at last render (crossing CARD_LABEL_ZOOM rebuilds card styles) */
  private lastLabelMode = false

  constructor(private readonly opts: ObservationCardLayerOptions) {}

  /** 灌入点位并重建聚合索引 / Load points and rebuild the cluster index */
  setData(points: ObservationPoint[]): void {
    const index = new Supercluster<LeafProps, ClusterProps>({
      radius: 64,
      maxZoom: CLUSTER_MAX_ZOOM,
      // 聚合时统计各类群数量，用于构成色环 / Count points per taxon while clustering, for the composition ring
      map: (p) => ({ [`k${p.iconic}`]: 1 }),
      reduce: (acc, p) => {
        for (const k in p) {
          acc[k] = (acc[k] ?? 0) + (p[k] ?? 0)
        }
      }
    })
    index.load(
      points.map((p) => ({
        type: 'Feature' as const,
        geometry: { type: 'Point' as const, coordinates: [p.lng, p.lat] },
        properties: { id: p.id, iconic: p.iconicTaxon, photo: p.photoSquareUrl, name: p.name, license: p.license ?? null }
      }))
    )
    this.index = index
    this.clear()
    this.render()
  }

  /** 显示/隐藏 / Show/hide */
  setVisible(visible: boolean): void {
    this.visible = visible
    if (!visible) {
      this.clear()
    } else {
      this.render()
    }
  }

  /** 主题变化：Marker 颜色随主题变化，需整体重建 / Theme change: Marker colors follow the theme, so everything is rebuilt */
  setTheme(theme: ThemeId): void {
    this.theme = theme
    this.clear()
    this.render()
  }

  /** 高亮指定观察（列表悬停联动） / Highlight an observation (list-hover linkage) */
  highlight(id: string | null): void {
    if (this.highlightId) {
      this.markers.get(`p${this.highlightId}`)?.getElement().classList.remove('is-active')
    }
    this.highlightId = id
    if (id) {
      this.markers.get(`p${id}`)?.getElement().classList.add('is-active')
    }
  }

  mount(map: MlMap): void {
    // Marker 与事件不随样式重载丢失，只需首次绑定 / Markers and events survive style reloads; bind them only once
    if (this.map === map) {
      return
    }
    this.map = map
    map.on('moveend', this.render)
    this.render()
  }

  unmount(map: MlMap): void {
    map.off('moveend', this.render)
    this.clear()
    this.map = null
  }

  /** 清空全部 Marker / Remove all Markers */
  private clear(): void {
    for (const m of this.markers.values()) {
      m.remove()
    }
    this.markers.clear()
  }

  /** 按当前视野 diff 渲染 Marker / Render Markers by diffing against the current viewport */
  private readonly render = (): void => {
    const map = this.map
    if (!map || !this.index || !this.visible) {
      return
    }
    const zoom = map.getZoom()
    const labelMode = zoom >= CARD_LABEL_ZOOM
    if (labelMode !== this.lastLabelMode) {
      // 跨越标签缩放档：卡片形态变化，重建全部 Marker / Crossed the label zoom band: card shape changes, so rebuild all Markers
      this.lastLabelMode = labelMode
      this.clear()
    }
    const b = map.getBounds()
    const features = this.index
      .getClusters([b.getWest(), b.getSouth(), b.getEast(), b.getNorth()], Math.floor(zoom))
      .slice(0, MAX_MARKERS)

    const next = new Set<string>()
    for (const f of features) {
      const props = f.properties as (LeafProps | ClusterProps) & { cluster?: boolean; cluster_id?: number; point_count?: number }
      const key = props.cluster ? `c${props.cluster_id}` : `p${(props as LeafProps).id}`
      next.add(key)
      if (this.markers.has(key)) {
        continue
      }
      const [lng, lat] = f.geometry.coordinates as [number, number]
      const el = props.cluster
        ? this.createClusterEl(props as ClusterProps, props.point_count ?? 0, props.cluster_id!, [lng, lat])
        : this.createCardEl(props as LeafProps, labelMode)
      const marker = new maplibregl.Marker({ element: el, anchor: props.cluster ? 'center' : 'bottom' })
        .setLngLat([lng, lat])
        .addTo(map)
      this.markers.set(key, marker)
    }
    // 移除离开视野/聚合关系改变的 Marker / Drop Markers that left the viewport or whose clustering changed
    for (const [key, m] of this.markers) {
      if (!next.has(key)) {
        m.remove()
        this.markers.delete(key)
      }
    }
  }

  /** 创建聚合簇元素：类群构成色环 + 数量 / Build a cluster element: taxon composition ring + count */
  private createClusterEl(props: ClusterProps, count: number, clusterId: number, lngLat: [number, number]): HTMLElement {
    // 按数量对数缩放直径 34~76px / Diameter scales logarithmically with count, 34~76px
    const size = Math.round(34 + Math.min(42, Math.log10(count + 1) * 16))
    // 构成色环：按类群占比拼接 conic-gradient / Composition ring: stitch a conic-gradient from per-taxon shares
    let acc = 0
    const stops: string[] = []
    let dominant = { code: 14, n: 0 }
    for (const t of TAXA) {
      const n = props[`k${t.code}`] ?? 0
      if (n <= 0) {
        continue
      }
      if (n > dominant.n) {
        dominant = { code: t.code, n }
      }
      const from = (acc / count) * 360
      acc += n
      const to = (acc / count) * 360
      stops.push(`${taxonColor(t.code, this.theme)} ${from}deg ${to}deg`)
    }
    const el = document.createElement('div')
    el.className = 'atlas-cluster'
    el.style.width = `${size}px`
    el.style.height = `${size}px`
    el.style.setProperty('--ring', `conic-gradient(${stops.join(',')})`)
    el.style.setProperty('--cluster-color', taxonColor(dominant.code, this.theme))
    el.innerHTML = `<span class="atlas-cluster__count">${formatCount(count)}</span>`
    el.addEventListener('click', (e) => {
      e.stopPropagation()
      const zoom = this.index?.getClusterExpansionZoom(clusterId) ?? (this.map?.getZoom() ?? 10) + 2
      this.map?.easeTo({ center: lngLat, zoom: Math.min(zoom, 18), duration: 600 })
    })
    return el
  }

  /** 创建照片卡元素 / Build a photo card element */
  private createCardEl(p: LeafProps, labelMode: boolean): HTMLElement {
    const color = taxonColor(p.iconic, this.theme)
    const el = document.createElement('button')
    el.type = 'button'
    // 新建卡片带入场动效（设计文档 §5.1）；render diff 复用的 Marker 不重建、不重播 / New cards get the enter animation (design doc §5.1); Markers reused by the render diff are neither rebuilt nor replayed
    el.className = `atlas-card${labelMode ? ' atlas-card--label' : ''} atlas-card--enter`
    el.style.setProperty('--taxon', color)
    if (p.id === this.highlightId) {
      el.classList.add('is-active')
    }
    const img = p.photo
      ? `<img class="atlas-card__img" src="${escapeAttr(p.photo)}" alt="" loading="lazy" referrerpolicy="no-referrer" />`
      : `<span class="atlas-card__img atlas-card__img--empty"></span>`
    const label = labelMode && p.name ? `<span class="atlas-card__name">${escapeHtml(p.name)}</span>` : ''
    // 版权微标（产品文档 §9）：协议代码角标，悬停 title 提示；完整署名在点击打开的大图链路
    // License micro-badge: a corner chip with the code; full attribution lives in the lightbox path
    const licChip = p.photo && p.license != null && p.license >= 0
      ? `<span class="atlas-card__lic" title="${escapeAttr(licenseLabel(p.license))}">${p.license === 0 ? '©' : escapeHtml(licenseLabel(p.license))}</span>`
      : ''
    el.innerHTML = `${img}${label}${licChip}<span class="atlas-card__pin"></span>`
    // 远程图片加载反馈：未就绪期间 CSS 类群色脉冲底图，就绪/失败换类收尾（见 markers.css） / Remote image loading feedback: a pulsing taxon-colored base shows until ready; load/error swaps the class to finish (see markers.css)
    const imgEl = el.querySelector<HTMLImageElement>('img.atlas-card__img')
    if (imgEl) {
      if (imgEl.complete) {
        imgEl.classList.add('atlas-img-ready')
      } else {
        imgEl.addEventListener('load', () => imgEl.classList.add('atlas-img-ready'), { once: true })
        imgEl.addEventListener('error', () => imgEl.classList.add('atlas-img-error'), { once: true })
      }
    }
    el.title = p.name ?? ''
    el.addEventListener('click', (e) => {
      e.stopPropagation()
      this.opts.onOpen(p.id)
    })
    return el
  }
}

/** 数量缩写：1234 → 1.2k / Count abbreviation: 1234 → 1.2k */
export function formatCount(n: number): string {
  return n >= 10000 ? `${Math.round(n / 1000)}k` : n >= 1000 ? `${(n / 1000).toFixed(1)}k` : String(n)
}

/** HTML 转义（物种名来自外部数据源，必须转义） / HTML-escape (species names come from external sources and must be escaped) */
export function escapeHtml(s: string): string {
  return s.replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]!)
}

/** 属性值转义 / Attribute-value escaping */
function escapeAttr(s: string): string {
  return escapeHtml(s)
}
