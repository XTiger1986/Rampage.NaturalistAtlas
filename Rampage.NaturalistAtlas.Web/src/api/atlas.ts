/**
 * 自然地图志公开接口（按领域分组） / Naturalist Atlas public API (grouped by domain)
 *
 * 每个函数一一对应后端 WebApi 的一个端点，参数/返回类型见 ./types.ts。
 * / Each function maps 1:1 to a backend WebApi endpoint; parameter/return types live in ./types.ts.
 * 组件中不要直接调用这些函数，应通过 composables/queries.ts 中的 vue-query 封装使用（带缓存与去重）。
 * / Components must not call these directly; use the vue-query wrappers in composables/queries.ts (caching + dedup).
 */
import { get, post } from './http'
import type {
  CityItem,
  CityOverview,
  EmergenceMode,
  EmergenceResult,
  GeoSearchItem,
  ObservationCard,
  ObservationDetail,
  ObservationPointResult,
  ObservationQuery,
  PageData,
  PhenologyMatrix,
  ReportInput,
  SpeciesDetail,
  SpeciesRankItem,
  SpeciesRankOrder,
  TaxonAncestor,
  WantedTaxonItem
} from './types'

/** 地理：城市与地点搜索 / Geo: cities and place search */
export const geoApi = {
  /** 已上线城市列表 / Live city list */
  cities: () => get<CityItem[]>('/geo/cities'),
  /** 地点模糊搜索（国家/省份/城市，中英文） / Fuzzy place search (country/province/city, Chinese & English) */
  search: (keyword: string, signal?: AbortSignal) => get<GeoSearchItem[]>('/geo/search', { keyword, limit: 12 }, signal)
}

/** 观察记录 / Observations */
export const observationApi = {
  /** 轻量点位（客户端聚合用） / Lightweight points (for client-side clustering) */
  points: (q: ObservationQuery, limit = 8000) =>
    get<ObservationPointResult>('/observation/points', { ...q, limit }),
  /** 卡片分页列表（最新优先；photoOnly = 仅有图记录，照片墙用） / Paginated card list (newest first; photoOnly = records with photos only, for photo walls) */
  list: (q: ObservationQuery, pageIndex = 1, pageSize = 30, photoOnly = false) =>
    get<PageData<ObservationCard>>('/observation/list', { ...q, pageIndex, pageSize, photoOnly }),
  /** 单条详情 / Single record detail */
  detail: (id: string) => get<ObservationDetail>(`/observation/${encodeURIComponent(id)}`)
}

/** 物种 / Species */
export const speciesApi = {
  /** 城市物种榜 / City species leaderboard */
  rank: (q: ObservationQuery & { order?: SpeciesRankOrder; keyword?: string }, pageIndex = 1, pageSize = 30) =>
    get<PageData<SpeciesRankItem>>('/species/rank', { ...q, pageIndex, pageSize }),
  /** 物种详情（本市口径） / Species detail (city-scoped) */
  detail: (taxonId: string, cityId?: number) =>
    get<SpeciesDetail>(`/species/${encodeURIComponent(taxonId)}`, { cityId }),
  /** 物候曲线 / Emergence curve */
  emergence: (taxonId: string, cityId: number | undefined, mode: EmergenceMode, researchOnly = false) =>
    get<EmergenceResult>(`/species/${encodeURIComponent(taxonId)}/emergence`, { cityId, mode, researchOnly }),
  /** 相似物种（同祖先链同胞，本市有记录） / Similar species (siblings on the same ancestor chain with city records) */
  similar: (taxonId: string, cityId: number) =>
    get<SpeciesRankItem[]>(`/species/${encodeURIComponent(taxonId)}/similar`, { cityId })
}

/** 物候 / Phenology */
export const phenologyApi = {
  /** 物种 × 月份矩阵 / Species × month matrix */
  matrix: (q: ObservationQuery & { top?: number }) => get<PhenologyMatrix>('/phenology/matrix', { ...q })
}

/** 统计 / Stats */
export const statsApi = {
  /** 城市总览 / City overview */
  overview: (cityId: number, researchOnly = false) => get<CityOverview>('/stats/overview', { cityId, researchOnly })
}

/** 分类 / Taxonomy */
export const taxonApi = {
  /** 祖先链（界›门›纲›目›科›属 面包屑） / Ancestor chain (kingdom›phylum›class›order›family›genus breadcrumb) */
  ancestors: (taxonId: string) => get<TaxonAncestor[]>(`/taxon/${encodeURIComponent(taxonId)}/ancestors`)
}

/** 集邮缺口 / Collection gaps */
export const gapsApi = {
  /** 周边已记录、本市未记录的物种清单（只读缓存） / Species recorded nearby but not yet in this city (read-only cache) */
  gaps: (cityId: number, iconic?: number[]) =>
    get<WantedTaxonItem[]>('/gaps', { cityId, iconic: iconic && iconic.length > 0 ? iconic : undefined })
}

/** 举报 / Reports */
export const reportApi = {
  /** 提交举报（带幂等键，防止重复点击） / Submit a report (with idempotency key to guard against double clicks) */
  submit: (input: ReportInput, idempotencyKey: string) => post<string>('/report', input, idempotencyKey)
}
