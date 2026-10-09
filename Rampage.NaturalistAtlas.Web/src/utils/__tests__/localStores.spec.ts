/** localStorage 工具测试：最近搜索 + 已举报标记 */
import { beforeEach, describe, expect, it } from 'vitest'
import { addRecentSearch, getRecentSearches } from '../recentSearches'
import { addReported, isReported } from '../reported'
import type { GeoSearchItem } from '@/api/types'

const item = (id: number, name: string): GeoSearchItem => ({
  level: 'city',
  id,
  name,
  nameEn: null,
  parentName: '北京市',
  center: [116.4, 39.9],
  bbox: [116, 39.5, 117, 40.5],
  slug: 'beijing'
})

beforeEach(() => {
  localStorage.clear()
})

describe('recentSearches', () => {
  it('空库返回空表', () => {
    expect(getRecentSearches()).toEqual([])
  })
  it('记录后最新在前', () => {
    addRecentSearch(item(1, '北京'))
    addRecentSearch(item(3, '上海'))
    expect(getRecentSearches().map((x) => x.name)).toEqual(['上海', '北京'])
  })
  it('同一条去重置顶', () => {
    addRecentSearch(item(1, '北京'))
    addRecentSearch(item(3, '上海'))
    addRecentSearch(item(1, '北京'))
    expect(getRecentSearches().map((x) => x.id)).toEqual([1, 3])
  })
  it('最多保留 5 条', () => {
    for (let i = 1; i <= 7; i++) {
      addRecentSearch(item(i, `城市${i}`))
    }
    const list = getRecentSearches()
    expect(list).toHaveLength(5)
    expect(list[0]!.id).toBe(7)
    expect(list[4]!.id).toBe(3)
  })
  it('损坏数据回退空表', () => {
    localStorage.setItem('na.recentSearch', '{not json')
    expect(getRecentSearches()).toEqual([])
  })
})

describe('reported', () => {
  it('标记后可查询', () => {
    expect(isReported('a1')).toBe(false)
    addReported('a1')
    expect(isReported('a1')).toBe(true)
  })
  it('重复标记去重置顶', () => {
    addReported('a1')
    addReported('a2')
    addReported('a1')
    expect(JSON.parse(localStorage.getItem('na.reported')!)).toEqual(['a1', 'a2'])
  })
})
