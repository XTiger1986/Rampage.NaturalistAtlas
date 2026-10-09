/**
 * 本地"已举报"标记（localStorage 持久化） / Local "reported" markers (persisted in localStorage)
 *
 * 举报成功后标记该观察，界面据此禁用举报按钮，避免同一浏览器重复提交；
 * 服务端仍以 report 表的实际记录为准，这里只是体验层去重（设计文档 §5.10）。
 * / After a successful report the observation is marked and the UI disables its report button, preventing
 * repeat submissions in the same browser; the server still trusts the report table — this is UX-level
 * dedup only (design doc §5.10).
 */
const KEY = 'na.reported'
/** 标记上限（超出丢弃最旧的，防无限增长） / Marker cap (oldest dropped beyond it, preventing unbounded growth) */
const LIMIT = 500

function load(): string[] {
  try {
    const raw = localStorage.getItem(KEY)
    const parsed = raw ? (JSON.parse(raw) as unknown) : []
    return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === 'string') : []
  } catch {
    return []
  }
}

function save(ids: string[]): void {
  try {
    localStorage.setItem(KEY, JSON.stringify(ids))
  } catch {
    // 隐私模式/存储被禁时静默失败，仅失去本地标记 / Fail silently in private mode or when storage is disabled; only the local marker is lost
  }
}

/** 该观察是否已在本机举报过 / Whether this observation was reported on this device */
export function isReported(id: string): boolean {
  return load().includes(id)
}

/** 标记观察为已举报（置顶去重） / Mark an observation as reported (moved to top, deduped) */
export function addReported(id: string): void {
  const ids = load().filter((x) => x !== id)
  ids.unshift(id)
  save(ids.slice(0, LIMIT))
}
