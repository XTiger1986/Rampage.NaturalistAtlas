/**
 * 后端接口访问基础封装 / Base HTTP layer for backend API access
 *
 * 约定（对应产品文档 §7 与后端 Rampage.Model.R）：/ Conventions (product doc §7 and backend Rampage.Model.R):
 * - 统一响应结构 { Code, Message, Data }（后端 PascalCase，本模块统一转为 camelCase），code = 0 表示成功；
 *   / Unified response envelope { Code, Message, Data } (backend PascalCase, converted to camelCase here); code = 0 means success;
 * - 列表接口分页结构为 PageData：{ pager: { pageIndex, pageSize, recordCount, totalPage }, dataList: [] }；
 *   / List endpoints paginate via PageData: { pager: { pageIndex, pageSize, recordCount, totalPage }, dataList: [] };
 * - 请求头携带 Accept-Language（后端本地化）与 X-Timezone（访客时区，后端按需换算展示时间）；
 *   / Requests carry Accept-Language (backend localization) and X-Timezone (visitor timezone, for display-time conversion);
 * - 接口基地址来自 VITE_API_BASE_URL；开发期为跨域直连（5173 → 5204），后端已开启 CORS（AllowLocalhost）。
 *   / Base URL comes from VITE_API_BASE_URL; in development it connects cross-origin (5173 → 5204) with backend CORS enabled (AllowLocalhost).
 * 错误文案随界面语言（直接引入语言包数据模块，避免与 i18n 实例互相 import 成环）。
 * / Error messages follow the UI language (locale data modules imported directly to avoid a circular import with the i18n instance).
 */
import zhCN from '@/i18n/zh-CN'
import enUS from '@/i18n/en-US'

/** 后端服务基地址（去掉可能存在的尾部斜杠，便于拼接路径） / Backend base URL (trailing slashes stripped for easy path concatenation) */
const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/+$/, '')

/** 访客时区（IANA 名称，如 Asia/Shanghai） / Visitor timezone (IANA name, e.g. Asia/Shanghai) */
const CLIENT_TIME_ZONE = Intl.DateTimeFormat().resolvedOptions().timeZone

/** 当前界面语言（由 i18n 模块在切换语言时写入） / Current UI language (written by the i18n module on locale switch) */
let currentLocale = 'zh-CN'

/** 按当前界面语言取文案（语言包数据模块直读，不经 i18n 实例） / Resolve message text for the current UI language (reads locale data directly, bypassing the i18n instance) */
function tr(key: 'networkError' | 'requestFailed'): string {
  return (currentLocale === 'en-US' ? enUS : zhCN).common[key]
}

/** 设置请求语言（i18n 切换语言时调用） / Set the request language (called when i18n switches locale) */
export function setRequestLocale(locale: string): void {
  currentLocale = locale
}

/** 后端统一响应结构 / Unified backend response envelope */
export interface ApiResult<T> {
  code: number
  message: string | null
  data: T
}

/** 业务/HTTP 错误 / Business/HTTP error */
export class ApiError extends Error {
  /** 业务码或 HTTP 状态码 / Business code or HTTP status code */
  readonly code: number

  constructor(code: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.code = code
  }
}

/**
 * 递归把对象键名首字母转小写（PascalCase → camelCase） / Recursively lowercase the first letter of object keys (PascalCase → camelCase)
 *
 * Rampage 框架 JSON 保留 C# 属性原名（PascalCase），幂等回放缓存也按 PascalCase 序列化，
 * 因此后端不改命名策略，统一在此转换，前端类型一律按 camelCase 书写。
 * / The Rampage framework keeps C# property names (PascalCase) in JSON and its idempotency replay cache is
 * PascalCase-serialized too, so the backend naming policy stays untouched, the conversion happens here, and
 * frontend types are written in camelCase.
 */
export function camelizeKeys(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map(camelizeKeys)
  }
  if (value !== null && typeof value === 'object') {
    const out: Record<string, unknown> = {}
    for (const [key, v] of Object.entries(value as Record<string, unknown>)) {
      out[key.charAt(0).toLowerCase() + key.slice(1)] = camelizeKeys(v)
    }
    return out
  }
  return value
}

/** 通用请求：处理 HTTP 状态与统一响应结构，成功时解包 data 返回 / Generic request: handles HTTP status and the unified envelope, unwrapping data on success */
async function request<T>(url: string, init?: RequestInit): Promise<T> {
  let res: Response
  try {
    res = await fetch(url, {
      ...init,
      headers: {
        'Content-Type': 'application/json',
        'Accept-Language': currentLocale,
        'X-Timezone': CLIENT_TIME_ZONE,
        ...(init?.headers ?? {})
      }
    })
  } catch {
    // 网络层失败（后端未启动 / CORS 拒绝）统一转为业务错误，便于界面提示
    // / Network-level failure (backend down / CORS refused) normalized into a business error for UI messaging
    throw new ApiError(-1, tr('networkError'))
  }
  if (!res.ok) {
    throw new ApiError(res.status, `HTTP ${res.status} ${res.statusText}`)
  }
  const body = camelizeKeys(await res.json()) as ApiResult<T>
  if (body.code !== 0) {
    throw new ApiError(body.code, body.message ?? tr('requestFailed'))
  }
  return body.data
}

/** 查询参数值类型（undefined / null / 空数组 的键会被忽略；数组以逗号拼接） / Query param value type (keys with undefined/null/empty-array values are skipped; arrays are comma-joined) */
export type QueryValue = string | number | boolean | undefined | null | ReadonlyArray<string | number>
export type QueryParams = Record<string, QueryValue>

/** 拼接查询串 / Build the query string */
export function buildQuery(params?: QueryParams): string {
  if (!params) {
    return ''
  }
  const qs = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null) {
      continue
    }
    if (Array.isArray(value)) {
      if (value.length > 0) {
        qs.append(key, value.join(','))
      }
      continue
    }
    qs.append(key, String(value))
  }
  const s = qs.toString()
  return s !== '' ? `?${s}` : ''
}

/** GET 请求：自动拼接查询串 / GET request: appends the query string automatically */
export function get<T>(path: string, params?: QueryParams, signal?: AbortSignal): Promise<T> {
  return request<T>(`${API_BASE_URL}/api${path}${buildQuery(params)}`, { signal })
}

/**
 * POST 请求：JSON 提交 / POST request: JSON submission
 * @param idempotencyKey 幂等键（后端 IdempotencyActionFilter 读取，重复提交直接回放首次结果）
 *   / Idempotency key (read by the backend IdempotencyActionFilter; duplicate submissions replay the first result)
 */
export function post<T>(path: string, body?: unknown, idempotencyKey?: string): Promise<T> {
  return request<T>(`${API_BASE_URL}/api${path}`, {
    method: 'POST',
    body: body === undefined ? undefined : JSON.stringify(body),
    headers: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : undefined
  })
}
