/**
 * 物候矩阵 PNG 导出（设计文档 §5.7 P2） / Phenology matrix PNG export (design doc §5.7 P2)
 *
 * 矩阵是 CSS Grid（非 ECharts），导出走原生 canvas 重绘：零依赖、无跨域照片 taint， / The matrix is a CSS Grid (not ECharts), so export redraws it on a native canvas: zero dependencies, no cross-origin photo taint,
 * 颜色从 CSS 令牌取值与界面主题一致；2x 缩放保证投屏清晰度。 / colors read from CSS tokens to match the UI theme; 2x scale keeps projections sharp.
 */
import type { PhenologyRow } from '@/api/types'
import { taxonColor } from '@/theme/taxonPalette'
import type { ThemeId } from '@/theme/themes'

/** 布局常量（逻辑像素，绘制统一 ×2） / Layout constants (logical px; drawing scales ×2 uniformly) */
const L = {
  padX: 16,
  padTop: 54,
  padBottom: 30,
  nameW: 170,
  cell: 22,
  cellGap: 3,
  rowH: 26,
  fontSizeTitle: 15,
  fontSizeHead: 11,
  fontSizeRow: 11,
  fontSizeFoot: 10
}

/** 绘制并触发下载 / Draw and trigger the download */
export function exportMatrixPng(
  rows: PhenologyRow[],
  opts: { title: string; caliberText: string; theme: ThemeId; filename: string }
): void {
  const n = rows.length
  const width = L.padX * 2 + L.nameW + 12 * (L.cell + L.cellGap)
  const height = L.padTop + n * L.rowH + L.padBottom
  const scale = 2

  const canvas = document.createElement('canvas')
  canvas.width = width * scale
  canvas.height = height * scale
  const ctx = canvas.getContext('2d')!
  ctx.scale(scale, scale)

  const style = getComputedStyle(document.documentElement)
  const ink = style.getPropertyValue('--c-ink').trim() || '#2b2118'
  const ink3 = style.getPropertyValue('--c-ink-3').trim() || '#8c7a62'
  const surface = style.getPropertyValue('--c-surface').trim() || '#ffffff'
  const accent = style.getPropertyValue('--c-accent').trim() || '#2f9e5b'

  // 背景 / Background
  ctx.fillStyle = surface
  ctx.fillRect(0, 0, width, height)

  // 标题与口径 / Title and data caliber
  ctx.fillStyle = ink
  ctx.font = `bold ${L.fontSizeTitle}px sans-serif`
  ctx.fillText(opts.title, L.padX, 26)
  ctx.fillStyle = ink3
  ctx.font = `${L.fontSizeFoot}px sans-serif`
  ctx.fillText(opts.caliberText, L.padX, 42)

  // 列头 1~12 / Column headers 1–12
  ctx.font = `${L.fontSizeHead}px sans-serif`
  const headY = L.padTop - 6
  for (let m = 1; m <= 12; m++) {
    const x = L.padX + L.nameW + (m - 1) * (L.cell + L.cellGap)
    ctx.fillStyle = ink3
    ctx.textAlign = 'center'
    ctx.fillText(String(m), x + L.cell / 2, headY)
  }
  ctx.textAlign = 'left'

  // 行 / Rows
  rows.forEach((r, i) => {
    const y = L.padTop + i * L.rowH
    // 行头物种名（超长截断） / Row-leading species name (truncated when too long)
    ctx.fillStyle = ink
    ctx.font = `${L.fontSizeRow}px sans-serif`
    const name = r.commonNameCn || r.scientificName || '—'
    ctx.fillText(truncate(ctx, name, L.nameW - 14), L.padX, y + 15)

    const max = Math.max(1, ...r.counts)
    r.counts.forEach((c, m) => {
      const x = L.padX + L.nameW + m * (L.cell + L.cellGap)
      const level = c / max
      ctx.globalAlpha = level === 0 ? 0.06 : 0.15 + level * 0.85
      ctx.fillStyle = taxonColor(r.iconicTaxon, opts.theme)
      roundedRect(ctx, x, y + 2, L.cell, 16, 3)
      ctx.fill()
    })
    ctx.globalAlpha = 1
    // 行尾总量 / Row-trailing total
    ctx.fillStyle = ink3
    ctx.textAlign = 'right'
    ctx.fillText(String(r.total), L.padX + L.nameW + 12 * (L.cell + L.cellGap) - L.cellGap, y + 15)
    ctx.textAlign = 'left'
  })

  // 页脚署名 / Footer attribution
  ctx.fillStyle = ink3
  ctx.font = `${L.fontSizeFoot}px sans-serif`
  ctx.fillText('自然地图志 Naturalist Atlas · naturalistatlas.1986z.com', L.padX, height - 10)
  ctx.fillStyle = accent

  canvas.toBlob((blob) => {
    if (!blob) {
      return
    }
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = opts.filename
    a.click()
    URL.revokeObjectURL(url)
  }, 'image/png')
}

/** 按最大宽度截断文本 / Truncate text to a maximum width */
function truncate(ctx: CanvasRenderingContext2D, text: string, maxWidth: number): string {
  if (ctx.measureText(text).width <= maxWidth) {
    return text
  }
  let out = text
  while (out.length > 1 && ctx.measureText(out + '…').width > maxWidth) {
    out = out.slice(0, -1)
  }
  return out + '…'
}

/** 圆角矩形路径 / Rounded-rectangle path */
function roundedRect(ctx: CanvasRenderingContext2D, x: number, y: number, w: number, h: number, r: number): void {
  ctx.beginPath()
  ctx.moveTo(x + r, y)
  ctx.arcTo(x + w, y, x + w, y + h, r)
  ctx.arcTo(x + w, y + h, x, y + h, r)
  ctx.arcTo(x, y + h, x, y, r)
  ctx.arcTo(x, y, x + w, y, r)
  ctx.closePath()
}
