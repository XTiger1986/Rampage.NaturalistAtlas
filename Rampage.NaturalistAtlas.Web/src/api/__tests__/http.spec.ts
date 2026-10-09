/** http.ts 单元测试：PascalCase → camelCase 递归转换 */
import { describe, expect, it } from 'vitest'
import { camelizeKeys } from '../http'

describe('camelizeKeys', () => {
  it('首字母小写', () => {
    expect(camelizeKeys({ Code: 0, Message: null, Data: { TaxonId: 'x' } })).toEqual({
      code: 0,
      message: null,
      data: { taxonId: 'x' }
    })
  })
  it('数组递归', () => {
    expect(camelizeKeys([{ Id: 1 }, { Id: 2 }])).toEqual([{ id: 1 }, { id: 2 }])
  })
  it('嵌套对象递归', () => {
    const input = { Pager: { PageIndex: 1, DataList: [{ PhotoSquareUrl: null }] } }
    expect(camelizeKeys(input)).toEqual({ pager: { pageIndex: 1, dataList: [{ photoSquareUrl: null }] } })
  })
  it('原始值与 null 原样返回', () => {
    expect(camelizeKeys(42)).toBe(42)
    expect(camelizeKeys(null)).toBe(null)
    expect(camelizeKeys('Pager')).toBe('Pager')
  })
  it('已是 camelCase 的键不受影响', () => {
    expect(camelizeKeys({ cityId: 1 })).toEqual({ cityId: 1 })
  })
})
