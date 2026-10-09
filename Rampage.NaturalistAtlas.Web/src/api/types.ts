/**
 * 接口契约类型（与后端 Model/Dto/Public/*.cs 一一对应，字段为 camelCase）
 * / API contract types (1:1 with backend Model/Dto/Public/*.cs, fields in camelCase)
 *
 * 修改任一侧契约时，必须同步修改另一侧，并更新《自然地图志前端设计》§6 接口契约。
 * / Any contract change on one side must be mirrored on the other and reflected in "Naturalist Atlas Frontend Design" §6.
 * 枚举字段一律以数字代码传输（与后端 AtlasEnums.cs 一致）。
 * / Enum fields are always transmitted as numeric codes (matching backend AtlasEnums.cs).
 */
import type { IconicTaxonCode } from '@/theme/taxonPalette'

/** 经纬度 [lng, lat] / Longitude-latitude pair [lng, lat] */
export type LngLat = [number, number]

/** 经纬度矩形 [西南经度, 西南纬度, 东北经度, 东北纬度] / Bounding box [west, south, east, north] */
export type BBox = [number, number, number, number]

/** 数据源：1 = iNaturalist，2 = GBIF / Data source: 1 = iNaturalist, 2 = GBIF */
export type AtlasSource = 1 | 2

/** 鉴定级别：1 = research，2 = needs_id，3 = casual（预留） / Quality grade: 1 = research, 2 = needs_id, 3 = casual (reserved) */
export type QualityGrade = 1 | 2 | 3

/** 坐标保护：0 = 开放，1 = 模糊，2 = 私密 / Geoprivacy: 0 = open, 1 = obscured, 2 = private */
export type GeoPrivacy = 0 | 1 | 2

/** 照片授权：0 = 保留所有权利，1 = CC0，2 = CC-BY … 7 = CC-BY-NC-ND，8 = PDM / Photo license: 0 = all rights reserved, 1 = CC0, 2 = CC-BY … 7 = CC-BY-NC-ND, 8 = PDM */
export type LicenseCode = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8

/** 分页信息（Rampage PagerInfo） / Pagination info (Rampage PagerInfo) */
export interface Pager {
  pageIndex: number
  pageSize: number
  recordCount: number | null
  totalPage: number | null
}

/** 分页数据（Rampage PageData<T>） / Paginated data (Rampage PageData<T>) */
export interface PageData<T> {
  pager: Pager | null
  dataList: T[]
}

// ---------------------------------------------------------------- 地理 / Geography

/** 城市（GET /api/geo/cities） / City (GET /api/geo/cities) */
export interface CityItem {
  id: number
  name: string
  nameEn: string | null
  slug: string
  countryId: number
  countryName: string | null
  provinceName: string | null
  /** 城市 bbox / City bounding box */
  bbox: BBox
  /** 定位中心点（由 bbox 推算） / Center point (derived from the bbox) */
  center: LngLat
}

/** 地点搜索结果层级 / Place search result level */
export type GeoLevel = 'country' | 'province' | 'city'

/** 地点搜索结果（GET /api/geo/search） / Place search result (GET /api/geo/search) */
export interface GeoSearchItem {
  level: GeoLevel
  id: number
  name: string
  nameEn: string | null
  /** 上级名称（省份 → 国家；城市 → 省份/国家） / Parent name (province → country; city → province/country) */
  parentName: string | null
  /** 定位中心点 / Center point */
  center: LngLat
  /** 城市级返回 bbox，国家/省份为 null / bbox for city-level results; null for country/province */
  bbox: BBox | null
  /** 城市级：slug（选中后切换当前城市） / City level only: slug (selecting it switches the current city) */
  slug: string | null
}

// ---------------------------------------------------------------- 观察 / Observations

/** 观察组合筛选（observation/list、observation/points、species/rank 等共用） / Shared observation filter (used by observation/list, observation/points, species/rank, etc.) */
export interface ObservationQuery {
  cityId?: number
  /** "w,s,e,n" */
  bbox?: string
  /** 类群代码，逗号分隔 / Iconic taxon codes, comma-separated */
  iconic?: number[]
  /** 月份区间（1~12，可跨年：如 11~2） / Month range (1~12, may wrap the year, e.g. 11~2) */
  monthFrom?: number
  monthTo?: number
  /** 鉴定级别，逗号分隔（默认 research + needs_id） / Quality grades, comma-separated (default: research + needs_id) */
  quality?: number[]
  /** 观察日期窗 YYYY-MM-DD / Observation date window YYYY-MM-DD */
  d1?: string
  d2?: string
  /** 限定物种 / Restrict to a taxon */
  taxonId?: string
}

/** 轻量点位（GET /api/observation/points，供客户端聚合） / Lightweight point (GET /api/observation/points, for client-side clustering) */
export interface ObservationPoint {
  id: string
  lng: number
  lat: number
  iconicTaxon: IconicTaxonCode
  qualityGrade: QualityGrade
  taxonId: string | null
  /** 展示名（中文常用名 > 学名） / Display name (Chinese common name > scientific name) */
  name: string | null
  photoSquareUrl: string | null
  /** 授权协议（地图照片卡微标署名；作者与完整署名在大图/详情链路） / License (badge credit on map photo cards; author and full attribution live in the lightbox/detail flow) */
  license: LicenseCode
  observedOn: string
}

/** 点位响应（带截断标记） / Points response (with truncation flag) */
export interface ObservationPointResult {
  points: ObservationPoint[]
  /** 符合条件总数 / Total matching count */
  total: number
  /** 是否因上限被截断（前端提示"放大查看更多"） / Whether truncated by the limit (UI prompts "zoom in for more") */
  truncated: boolean
}

/** 观察卡片（GET /api/observation/list 的列表项） / Observation card (list item of GET /api/observation/list) */
export interface ObservationCard {
  id: string
  source: AtlasSource
  taxonId: string | null
  scientificName: string | null
  commonNameCn: string | null
  commonNameEn: string | null
  iconicTaxon: IconicTaxonCode
  qualityGrade: QualityGrade
  /** 观察者本地日期 YYYY-MM-DD / Observer's local date YYYY-MM-DD */
  observedOn: string
  /** 观察时刻（ISO 8601，带 Z），可空 / Observation instant (ISO 8601 with Z), nullable */
  observedAt: string | null
  lng: number
  lat: number
  geoprivacy: GeoPrivacy
  photoSquareUrl: string | null
  photoMediumUrl: string | null
  license: LicenseCode
  userLogin: string | null
  /** GBIF 数据发布机构（iNat 为 null） / GBIF dataset publisher (null for iNat) */
  datasetTitle: string | null
  /** 受威胁物种（坐标经保护性模糊） / Threatened species (coordinates conservation-obscured) */
  threatened: boolean
}

/** 观察详情（GET /api/observation/{id}） / Observation detail (GET /api/observation/{id}) */
export interface ObservationDetail extends ObservationCard {
  sourceObservationId: string
  photoLargeUrl: string | null
  positionalAccuracy: number | null
  identificationsCount: number
  recordBasis: number | null
  cityName: string | null
  /** 源站原始页面链接 / Original record URL on the source site */
  sourceUrl: string | null
}

/** 举报提交（POST /api/report） / Report submission (POST /api/report) */
export interface ReportInput {
  observationId: string
  /** 0=其他 1=不当内容 2=错误鉴定 3=隐私问题 4=垃圾信息 / 0=other 1=inappropriate content 2=misidentification 3=privacy issue 4=spam */
  reason: 0 | 1 | 2 | 3 | 4
  content?: string
  contact?: string
}

// ---------------------------------------------------------------- 物种 / Species

/** 物种榜排序：record = 按记录数，observer = 按观察者数，rare = 稀缺榜（记录数升序，口径固定研究级） / Rank order: record = by record count, observer = by observer count, rare = rarity board (ascending record count, research-grade only) */
export type SpeciesRankOrder = 'record' | 'observer' | 'rare'

/** 物种榜条目（GET /api/species/rank） / Leaderboard entry (GET /api/species/rank) */
export interface SpeciesRankItem {
  taxonId: string
  scientificName: string | null
  commonNameCn: string | null
  commonNameEn: string | null
  iconicTaxon: IconicTaxonCode
  threatened: boolean
  recordCount: number
  observerCount: number
  photoSquareUrl: string | null
  photoMediumUrl: string | null
  /** 代表图授权协议（与代表图同一行；版权署名用，可缺失） / Cover photo license (same row as the cover photo; for attribution, may be missing) */
  photoLicense?: LicenseCode | null
  /** 代表图作者（与代表图同一行；版权署名用，可缺失） / Cover photo author (same row as the cover photo; for attribution, may be missing) */
  photoUserLogin?: string | null
  lastObservedOn: string | null
  /** 首次达标日期（仅新星语境返回；其余场景为 null/缺失） / Date the threshold was first met (returned only for rising stars; null/absent otherwise) */
  reachedOn?: string | null
}

/** 分类档案 / Taxon profile */
export interface TaxonInfo {
  taxonId: string
  source: AtlasSource
  sourceTaxonId: string
  rank: number
  scientificName: string | null
  commonNameCn: string | null
  commonNameEn: string | null
  iconicTaxon: IconicTaxonCode
  threatened: boolean
  ancestry: string | null
}

/** 分类祖先链节点（GET /api/taxon/{id}/ancestors）：rank 0~11 映射见 i18n taxonRank / Ancestor chain node (GET /api/taxon/{id}/ancestors); rank 0~11 mapped in i18n taxonRank */
export interface TaxonAncestor {
  taxonId: string
  rank: number
  scientificName: string | null
  commonNameCn: string | null
  commonNameEn: string | null
}

/** 集邮缺口条目（GET /api/gaps）：周边已记录、本市未记录的分类快照 / Collection gap entry (GET /api/gaps): taxon snapshot recorded nearby but not yet in this city */
export interface WantedTaxonItem {
  /** 源站分类 id（本市无记录，未入本站 taxon 表） / Source-site taxon id (no city records, not in our taxon table) */
  sourceTaxonId: string
  rank: number
  scientificName: string | null
  commonNameCn: string | null
  commonNameEn: string | null
  iconicTaxon: IconicTaxonCode
  /** 周边记录次数（源站口径，可为 0） / Nearby record count (source-site scope, may be 0) */
  nearbyCount: number
  photoSquareUrl: string | null
  photoMediumUrl: string | null
  /** 代表图授权协议（源站 wanted 照片无作者名，仅协议） / Cover photo license (source-site wanted photos carry no author, license only) */
  photoLicense?: LicenseCode | null
}

/** 物种详情（GET /api/species/{id}） / Species detail (GET /api/species/{id}) */
export interface SpeciesDetail {
  taxon: TaxonInfo
  /** 本市记录数 / 观察者数 / City record count & observer count */
  recordCount: number
  observerCount: number
  firstObservedOn: string | null
  lastObservedOn: string | null
  /** 照片墙（最新有图记录） / Photo wall (latest records with photos) */
  photos: ObservationCard[]
  /** 本市分布点 / Distribution points in this city */
  points: ObservationPoint[]
}

/** 物候口径：detail = 本站明细（近两年，逐旬），history = 全历史统计（逐月） / Emergence mode: detail = this-site detail (recent two years, by ten-day period), history = all-time statistics (monthly) */
export type EmergenceMode = 'detail' | 'history'

/** 物候曲线单点 / Single point of the emergence curve */
export interface EmergencePoint {
  /** 序号：逐旬 1~36 / 逐月 1~12 / Index: 1~36 by ten-day period / 1~12 by month */
  index: number
  recordCount: number
  researchCount: number
}

/** 物候曲线（GET /api/species/{id}/emergence） / Emergence curve (GET /api/species/{id}/emergence) */
export interface EmergenceResult {
  mode: EmergenceMode
  /** 粒度：tenDay = 逐旬，month = 逐月 / Granularity: tenDay = by ten-day period, month = monthly */
  unit: 'tenDay' | 'month'
  /** 样本量（总记录数，页面须注明） / Sample size (total records; pages must disclose it) */
  sampleSize: number
  series: EmergencePoint[]
}

/** 物候矩阵行 / Phenology matrix row */
export interface PhenologyRow {
  taxonId: string
  scientificName: string | null
  commonNameCn: string | null
  iconicTaxon: IconicTaxonCode
  total: number
  /** 1~12 月记录数（长度固定 12） / Record counts for Jan~Dec (fixed length 12) */
  counts: number[]
}

/** 物候矩阵（GET /api/phenology/matrix） / Phenology matrix (GET /api/phenology/matrix) */
export interface PhenologyMatrix {
  rows: PhenologyRow[]
}

// ---------------------------------------------------------------- 总览 / Overview

/** 类群构成项 / Iconic taxon count item */
export interface IconicCount {
  iconicTaxon: IconicTaxonCode
  count: number
}

/** 活跃观察者 / Active observer */
export interface ObserverCount {
  userLogin: string
  count: number
}

/** 近 12 个月趋势点 / 12-month trend point */
export interface MonthlyCount {
  /** YYYY-MM */
  month: string
  count: number
}

/** 城市总览（GET /api/stats/overview） / City overview (GET /api/stats/overview) */
export interface CityOverview {
  cityId: number
  cityName: string | null
  totalObservation: number
  researchCount: number
  speciesCount: number
  observerCount: number
  /** 本月观察数 / Observations this month */
  monthObservation: number
  /** 近 12 个月记录趋势（后端补 0；可选字段防缓存过渡期） / 12-month record trend (zero-filled by the backend; optional to survive cache transitions) */
  monthly12?: MonthlyCount[]
  iconicDistribution: IconicCount[]
  /** 本月新星：本月首次累计达到 ≥5 条 research 级记录的物种 / Rising stars: species that first reached ≥5 research-grade records this month */
  risingStars: SpeciesRankItem[]
  topObservers: ObserverCount[]
  /** 最新一条观察的源站发布时间（ISO 8601） / Source-site publish time of the newest observation (ISO 8601) */
  newestSourceCreatedAt: string | null
}
