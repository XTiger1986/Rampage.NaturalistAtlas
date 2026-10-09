/** utils/format 单元测试（设计文档 §8/§9：展示文案随界面语言） */
import { describe, expect, it } from 'vitest'
import { i18n } from '@/i18n'
import { attribution, formatNumber, licenseLabel, relativeTime, speciesName, tenDayLabel } from '../format'

/** 切语言后取值（speciesName 等纯函数读取 i18n 当前 locale） */
function withLocale<T>(locale: 'zh-CN' | 'en-US', fn: () => T): T {
  const prev = i18n.global.locale.value
  i18n.global.locale.value = locale
  try {
    return fn()
  } finally {
    i18n.global.locale.value = prev
  }
}

describe('speciesName', () => {
  it('中文界面：中文名 > 英文名 > 学名', () => {
    expect(withLocale('zh-CN', () => speciesName({ commonNameCn: '喜鹊', commonNameEn: 'Eurasian Magpie', scientificName: 'Pica serica' }))).toBe('喜鹊')
    expect(withLocale('zh-CN', () => speciesName({ commonNameCn: null, commonNameEn: 'Eurasian Magpie', scientificName: 'Pica serica' }))).toBe('Eurasian Magpie')
    expect(withLocale('zh-CN', () => speciesName({ commonNameCn: '', commonNameEn: null, scientificName: 'Pica serica' }))).toBe('Pica serica')
  })
  it('英文界面：英文名 > 学名（跳过中文名，设计文档 §8）', () => {
    expect(withLocale('en-US', () => speciesName({ commonNameCn: '喜鹊', commonNameEn: 'Eurasian Magpie', scientificName: 'Pica serica' }))).toBe('Eurasian Magpie')
    expect(withLocale('en-US', () => speciesName({ commonNameCn: '喜鹊', commonNameEn: null, scientificName: 'Pica serica' }))).toBe('Pica serica')
  })
  it('全部缺失时兜底文案（随语言）', () => {
    expect(withLocale('zh-CN', () => speciesName({}))).toBe('未鉴定')
    expect(withLocale('en-US', () => speciesName({}))).toBe('Unidentified')
  })
})

describe('formatNumber', () => {
  it('千分位', () => {
    expect(formatNumber(19102)).toBe('19,102')
  })
  it('空值占位', () => {
    expect(formatNumber(null)).toBe('—')
    expect(formatNumber(undefined)).toBe('—')
  })
})

describe('licenseLabel / attribution', () => {
  it('授权短名（CC 缩写跨语言一致；0 走 i18n）', () => {
    expect(licenseLabel(2)).toBe('CC BY')
    expect(licenseLabel(8)).toBe('PDM')
    expect(withLocale('zh-CN', () => licenseLabel(0))).toContain('保留所有权利')
    expect(withLocale('en-US', () => licenseLabel(0))).toContain('All rights reserved')
  })
  it('署名 = ©观察者 + 机构（GBIF） + 协议', () => {
    expect(attribution('monkey', 4)).toBe('© monkey · CC BY-NC')
    expect(attribution('monkey', 4, 'iNaturalist')).toBe('© monkey · iNaturalist · CC BY-NC')
  })
})

describe('relativeTime', () => {
  it('空值占位', () => {
    expect(relativeTime(null)).toBe('—')
  })
  it('近期显示分钟级（Intl.RelativeTimeFormat，随语言）', () => {
    expect(relativeTime(new Date(Date.now() - 5 * 60_000).toISOString())).toBe('5分钟前')
    expect(withLocale('en-US', () => relativeTime(new Date(Date.now() - 5 * 60_000).toISOString()))).toBe('5 minutes ago')
  })
})

describe('tenDayLabel', () => {
  it('旬序 → 本地化标签（随语言）', () => {
    expect(withLocale('zh-CN', () => tenDayLabel(1))).toBe('1月上旬')
    expect(withLocale('zh-CN', () => tenDayLabel(5))).toBe('2月中旬')
    expect(withLocale('zh-CN', () => tenDayLabel(36))).toBe('12月下旬')
    expect(withLocale('en-US', () => tenDayLabel(1))).toBe('Early Jan')
    expect(withLocale('en-US', () => tenDayLabel(2))).toBe('Mid Jan')
    expect(withLocale('en-US', () => tenDayLabel(5))).toBe('Mid Feb')
  })
})
