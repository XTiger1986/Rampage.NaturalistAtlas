/**
 * 数据查询组合式函数（@tanstack/vue-query 封装） / Data-fetch composables (wrappers over @tanstack/vue-query)
 *
 * 约定：/ Conventions:
 * - 组件只通过这里取数，禁止在组件里直接调用 api/atlas.ts；
 *   / Components fetch data only through this module; calling api/atlas.ts directly from components is forbidden;
 * - queryKey 统一为 [领域, 动作, 参数对象]，参数对象变化即自动重新请求并缓存；
 *   / queryKeys are uniformly [domain, action, params object]; changing the params object triggers refetch with caching;
 * - 离线城市（cityId <= 0，后端未连接）一律 enabled=false，界面展示空态而非报错；
 *   / Offline cities (cityId <= 0, backend not connected) always set enabled=false so the UI shows an empty state instead of an error;
 * - 缓存时长：统计类 10 分钟（与后端服务端缓存一致），明细类 2 分钟。
 *   / Cache times: 10 minutes for stats (matching the backend server-side cache), 2 minutes for details.
 */
import { computed, toValue } from 'vue'
import type { MaybeRefOrGetter } from 'vue'
import { keepPreviousData, useInfiniteQuery, useMutation, useQuery } from '@tanstack/vue-query'
import { gapsApi, geoApi, observationApi, phenologyApi, reportApi, speciesApi, statsApi, taxonApi } from '@/api/atlas'
import type {
  EmergenceMode,
  ObservationCard,
  ObservationQuery,
  PageData,
  ReportInput,
  SpeciesRankItem,
  SpeciesRankOrder
} from '@/api/types'

/** 统计类缓存时长 / Stale time for stats queries */
const STAT_STALE = 10 * 60 * 1000
/** 明细类缓存时长 / Stale time for detail queries */
const DETAIL_STALE = 2 * 60 * 1000

/** 城市是否在线可查 / Whether the city is online and queryable */
function cityOk(cityId: number | undefined): boolean {
  return typeof cityId === 'number' && cityId > 0
}

/** 城市级点位（地图照片卡与热力共用） / City-scoped points (shared by map photo cards and the heatmap) */
export function useObservationPoints(query: MaybeRefOrGetter<ObservationQuery>) {
  return useQuery({
    queryKey: computed(() => ['observation', 'points', toValue(query)]),
    queryFn: () => observationApi.points(toValue(query)),
    enabled: computed(() => cityOk(toValue(query).cityId)),
    staleTime: DETAIL_STALE,
    // 切换筛选时保留上一份点位，避免地图闪空 / Keep the previous points while filters change to avoid the map flashing empty
    placeholderData: keepPreviousData
  })
}

/**
 * 观察卡片分页列表（无限加载） / Paginated observation card list (infinite loading)
 *
 * 视野/筛选变化时 queryKey 跟着变，vue-query 自动丢弃旧分页从第 1 页重来（无需手动 reset）；
 * 下一页由调用方在列表底部的哨兵元素进入滚动区时 fetchNextPage 触发。
 * / When viewport/filters change the queryKey changes, and vue-query automatically discards old pages and
 * restarts from page 1 (no manual reset needed); the caller triggers fetchNextPage when the sentinel element
 * at the list bottom enters the scroll area.
 */
export function useObservationList(query: MaybeRefOrGetter<ObservationQuery>, pageSize = 30) {
  const q = useInfiniteQuery({
    queryKey: computed(() => ['observation', 'list', toValue(query), pageSize]),
    queryFn: ({ pageParam }) => observationApi.list(toValue(query), pageParam, pageSize),
    enabled: computed(() => cityOk(toValue(query).cityId)),
    staleTime: DETAIL_STALE,
    placeholderData: keepPreviousData,
    initialPageParam: 1,
    getNextPageParam: (last: PageData<ObservationCard>) =>
      last.pager && last.pager.totalPage != null && last.pager.pageIndex < last.pager.totalPage
        ? last.pager.pageIndex + 1
        : undefined
  })
  /** 已加载全部页的扁平列表 / Flat list of all loaded pages */
  const flat = computed<ObservationCard[]>(() => q.data.value?.pages.flatMap((p) => p.dataList) ?? [])
  return { ...q, flat }
}

/**
 * 物种照片墙（瀑布流无限加载）：仅取该物种在当前城市的有图记录
 * 与 useObservationList 同端点不同参数（taxonId + photoOnly），缓存天然分离
 * / Species photo wall (masonry infinite loading): only photo records of this species in the current city;
 * same endpoint as useObservationList with different params (taxonId + photoOnly), so caches stay naturally separate
 */
export function useSpeciesPhotos(
  taxonId: MaybeRefOrGetter<string | null>,
  cityId: MaybeRefOrGetter<number>,
  pageSize = 30
) {
  const q = useInfiniteQuery({
    queryKey: computed(() => ['observation', 'photos', toValue(taxonId), toValue(cityId), pageSize]),
    queryFn: ({ pageParam }) =>
      observationApi.list(
        { cityId: toValue(cityId), taxonId: toValue(taxonId)! },
        pageParam,
        pageSize,
        true
      ),
    enabled: computed(() => !!toValue(taxonId) && cityOk(toValue(cityId))),
    staleTime: DETAIL_STALE,
    placeholderData: keepPreviousData,
    initialPageParam: 1,
    getNextPageParam: (last: PageData<ObservationCard>) =>
      last.pager && last.pager.totalPage != null && last.pager.pageIndex < last.pager.totalPage
        ? last.pager.pageIndex + 1
        : undefined
  })
  const flat = computed<ObservationCard[]>(() => q.data.value?.pages.flatMap((p) => p.dataList) ?? [])
  return { ...q, flat }
}

/** 观察详情 / Observation detail */
export function useObservationDetail(id: MaybeRefOrGetter<string | null>) {
  return useQuery({
    queryKey: computed(() => ['observation', 'detail', toValue(id)]),
    queryFn: () => observationApi.detail(toValue(id)!),
    enabled: computed(() => !!toValue(id)),
    staleTime: DETAIL_STALE
  })
}

/** 城市物种榜 / City species leaderboard */
export function useSpeciesRank(
  query: MaybeRefOrGetter<ObservationQuery & { order: SpeciesRankOrder; keyword?: string }>,
  page: MaybeRefOrGetter<number>,
  pageSize = 30
) {
  return useQuery({
    queryKey: computed(() => ['species', 'rank', toValue(query), toValue(page), pageSize]),
    queryFn: () => speciesApi.rank(toValue(query), toValue(page), pageSize),
    enabled: computed(() => cityOk(toValue(query).cityId)),
    staleTime: STAT_STALE,
    placeholderData: keepPreviousData
  })
}

/** 物种详情 / Species detail */
export function useSpeciesDetail(taxonId: MaybeRefOrGetter<string | null>, cityId: MaybeRefOrGetter<number>) {
  return useQuery({
    queryKey: computed(() => ['species', 'detail', toValue(taxonId), toValue(cityId)]),
    queryFn: () => speciesApi.detail(toValue(taxonId)!, cityOk(toValue(cityId)) ? toValue(cityId) : undefined),
    enabled: computed(() => !!toValue(taxonId)),
    staleTime: STAT_STALE
  })
}

/** 物候曲线 / Emergence curve */
export function useEmergence(
  taxonId: MaybeRefOrGetter<string | null>,
  cityId: MaybeRefOrGetter<number>,
  mode: MaybeRefOrGetter<EmergenceMode>,
  researchOnly: MaybeRefOrGetter<boolean>
) {
  return useQuery({
    queryKey: computed(() => ['species', 'emergence', toValue(taxonId), toValue(cityId), toValue(mode), toValue(researchOnly)]),
    queryFn: () =>
      speciesApi.emergence(
        toValue(taxonId)!,
        cityOk(toValue(cityId)) ? toValue(cityId) : undefined,
        toValue(mode),
        toValue(researchOnly)
      ),
    enabled: computed(() => !!toValue(taxonId)),
    staleTime: STAT_STALE
  })
}

/** 物候矩阵 / Phenology matrix */
export function usePhenologyMatrix(query: MaybeRefOrGetter<ObservationQuery & { top?: number }>) {
  return useQuery({
    queryKey: computed(() => ['phenology', 'matrix', toValue(query)]),
    queryFn: () => phenologyApi.matrix(toValue(query)),
    enabled: computed(() => cityOk(toValue(query).cityId)),
    staleTime: STAT_STALE,
    placeholderData: keepPreviousData
  })
}

/** 城市总览 / City overview */
export function useOverview(cityId: MaybeRefOrGetter<number>, researchOnly: MaybeRefOrGetter<boolean>) {
  return useQuery({
    queryKey: computed(() => ['stats', 'overview', toValue(cityId), toValue(researchOnly)]),
    queryFn: () => statsApi.overview(toValue(cityId), toValue(researchOnly)),
    enabled: computed(() => cityOk(toValue(cityId))),
    staleTime: STAT_STALE
  })
}

/** 地点搜索（关键字防抖由调用方处理） / Place search (keyword debouncing handled by the caller) */
export function useGeoSearch(keyword: MaybeRefOrGetter<string>) {
  return useQuery({
    queryKey: computed(() => ['geo', 'search', toValue(keyword).trim()]),
    queryFn: ({ signal }) => geoApi.search(toValue(keyword).trim(), signal),
    enabled: computed(() => toValue(keyword).trim().length > 0),
    staleTime: STAT_STALE
  })
}

/** 分类祖先链（面包屑） / Taxon ancestor chain (breadcrumb) */
export function useTaxonAncestors(taxonId: MaybeRefOrGetter<string | null>) {
  return useQuery({
    queryKey: computed(() => ['taxon', 'ancestors', toValue(taxonId)]),
    queryFn: () => taxonApi.ancestors(toValue(taxonId)!),
    enabled: computed(() => !!toValue(taxonId)),
    staleTime: STAT_STALE
  })
}

/**
 * 拉取榜单当前筛选下的前 500 条（CSV 导出用，5 页 × 100 并行）
 * 走本模块以遵守"取数只走 queries.ts"；调用方负责 CSV 组装
 * / Fetch the top 500 leaderboard rows under the current filter (for CSV export, 5 pages × 100 in parallel);
 * routed through this module to honor "all fetching goes through queries.ts"; the caller assembles the CSV
 */
export async function fetchSpeciesRankForExport(
  query: ObservationQuery & { order?: SpeciesRankOrder; keyword?: string }
): Promise<SpeciesRankItem[]> {
  const pages = await Promise.all(
    Array.from({ length: 5 }, (_, i) => speciesApi.rank(query, i + 1, 100))
  )
  return pages.flatMap((p) => p.dataList)
}

/** 集邮缺口（类群筛选与图例联动） / Collection gaps (iconic filter linked with the legend) */
export function useGaps(cityId: MaybeRefOrGetter<number>, iconic: MaybeRefOrGetter<number[]>) {
  return useQuery({
    queryKey: computed(() => ['gaps', 'list', toValue(cityId), [...toValue(iconic)].sort((a, b) => a - b)]),
    queryFn: () => gapsApi.gaps(toValue(cityId), toValue(iconic)),
    enabled: computed(() => cityOk(toValue(cityId))),
    staleTime: STAT_STALE,
    placeholderData: keepPreviousData
  })
}

/** 相似物种（同祖先链同胞，本市有记录） / Similar species (siblings on the same ancestor chain with city records) */
export function useSimilarSpecies(taxonId: MaybeRefOrGetter<string | null>, cityId: MaybeRefOrGetter<number>) {
  return useQuery({
    queryKey: computed(() => ['species', 'similar', toValue(taxonId), toValue(cityId)]),
    queryFn: () => speciesApi.similar(toValue(taxonId)!, toValue(cityId)),
    enabled: computed(() => !!toValue(taxonId) && cityOk(toValue(cityId))),
    staleTime: STAT_STALE
  })
}

/** 提交举报 / Submit a report */
export function useSubmitReport() {
  return useMutation({
    mutationFn: ({ input, key }: { input: ReportInput; key: string }) => reportApi.submit(input, key)
  })
}
