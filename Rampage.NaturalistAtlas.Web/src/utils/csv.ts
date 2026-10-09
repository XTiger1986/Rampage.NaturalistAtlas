/**
 * CSV 生成与下载（UTF-8 BOM，Excel 友好；\r\n 行尾） / CSV generation and download (UTF-8 BOM, Excel-friendly; \r\n line endings)
 */

/** 字段转义：含逗号/引号/换行的值加引号并双写引号 / Escape a cell: values containing commas/quotes/newlines get quoted with doubled quotes */
function escapeCell(v: string | number | null | undefined): string {
  const s = v == null ? '' : String(v)
  return /[",\r\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s
}

/**
 * 生成 CSV 文本 / Build CSV text
 * @param headers 表头（键 → 列名） / Column headers (key → label)
 * @param rows 行数据 / Row data
 */
export function toCsv<T extends Record<string, string | number | null | undefined>>(
  headers: Array<[keyof T & string, string]>,
  rows: T[]
): string {
  const head = headers.map(([, label]) => escapeCell(label)).join(',')
  const body = rows.map((r) => headers.map(([key]) => escapeCell(r[key])).join(',')).join('\r\n')
  return `﻿${head}\r\n${body}`
}

/** 触发浏览器下载 / Trigger a browser download */
export function downloadCsv(filename: string, csv: string): void {
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}
