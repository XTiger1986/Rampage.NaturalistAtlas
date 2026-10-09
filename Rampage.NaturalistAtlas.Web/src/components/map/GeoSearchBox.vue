<script setup lang="ts">
/**
 * 地点搜索定位（产品文档 §8.3 · 地点搜索定位，设计文档 §5.11） / Geo search & locate (product doc §8.3, design doc §5.11)
 *
 * 输入国家/省份/城市名称（中英文）→ 300ms 防抖联想 → 选中后： / Type a country/province/city name (Chinese or English) → 300ms debounced suggestions → on select:
 * - 城市：若为已上线城市则切换当前城市（地图按 bbox 定位）；否则仅按 bbox 定位； / - City: switch the current city if it's launched (map fits its bbox); otherwise just fit the bbox;
 * - 国家/省份：飞行到定位中心点（国家 zoom 4，省份 zoom 6）。 / - Country/province: fly to the result center (country zoom 4, province zoom 6).
 *
 * 增强：键盘 ↑/↓ 与回车由 NAutoComplete 内置；输入为空时显示"最近搜索"（本地 5 条）； / Extras: arrow keys ↑/↓ and Enter are built into NAutoComplete; empty input shows "recent searches" (5 local entries);
 * 无结果时提示"暂未收录，欢迎反馈"。 / no results shows a "not yet covered, feedback welcome" hint.
 */
import { computed, ref } from 'vue'
import { refDebounced } from '@vueuse/core'
import { useI18n } from 'vue-i18n'
import { NAutoComplete } from 'naive-ui'
import type { AutoCompleteOption } from 'naive-ui'
import { useGeoSearch } from '@/composables/queries'
import { useCityStore } from '@/stores/city'
import { mapEngine } from '@/map/MapEngine'
import { addRecentSearch, getRecentSearches } from '@/utils/recentSearches'
import type { GeoSearchItem } from '@/api/types'

const { t, locale } = useI18n()
const city = useCityStore()

/** 输入值 / Input value */
const keyword = ref('')
/** 防抖后的关键字（驱动查询） / Debounced keyword (drives the query) */
const debounced = refDebounced(keyword, 300)
const { data, isFetching } = useGeoSearch(debounced)

/** 最近搜索（本地记录，最新在前） / Recent searches (local records, newest first) */
const recent = ref(getRecentSearches())

/** 联想选项：value 使用 "层级:id" 保证唯一 / Suggestion option: value uses "level:id" for uniqueness */
function toOption(g: GeoSearchItem): AutoCompleteOption {
  return {
    label: `${locale.value === 'en-US' ? g.nameEn || g.name : g.name}  ·  ${t(`search.${g.level}`)}${g.parentName ? ` · ${g.parentName}` : ''}`,
    value: `${g.level}:${g.id}`
  }
}

/** 选项集：空关键字 → 最近搜索分组；有结果 → 结果；查无结果 → 引导提示 / Option set: empty keyword → recent-search group; results available → results; no results → guidance hint */
const options = computed<AutoCompleteOption[]>(() => {
  if (debounced.value.trim().length === 0) {
    return recent.value.length === 0
      ? []
      : [{ type: 'group', label: t('search.recent'), key: 'recent', children: recent.value.map(toOption) }]
  }
  if (!isFetching.value && (data.value ?? []).length === 0) {
    return [{ label: t('search.noResult'), value: 'none', disabled: true }]
  }
  return (data.value ?? []).map(toOption)
})

/** 下拉是否展示：有输入，或为空但有最近搜索 / Whether the dropdown shows: has input, or empty but recents exist */
function shouldShow(): boolean {
  return keyword.value.trim().length > 0 || recent.value.length > 0
}

/** 选中联想项：定位地图并记录最近搜索 / On suggestion select: locate the map and record a recent search */
function onSelect(value: string | number): void {
  if (value === 'none') {
    return
  }
  const item: GeoSearchItem | undefined = (data.value ?? []).find(
    (g) => `${g.level}:${g.id}` === value
  ) ?? recent.value.find((g) => `${g.level}:${g.id}` === value)
  if (!item) {
    return
  }
  keyword.value = item.name
  addRecentSearch(item)
  recent.value = getRecentSearches()
  if (item.level === 'city' && item.slug && city.cities.some((c) => c.slug === item.slug)) {
    // 已上线城市：切换当前城市（MapCanvas 负责飞行定位） / Launched city: switch the current city (MapCanvas handles the fly-to)
    city.setCity(item.slug)
  } else if (item.bbox) {
    mapEngine.fitBBox(item.bbox)
  } else {
    mapEngine.flyTo(item.center, item.level === 'country' ? 4 : 6)
  }
}
</script>

<template>
  <NAutoComplete
    v-model:value="keyword"
    :options="options"
    :loading="isFetching"
    :placeholder="t('search.placeholder')"
    size="small"
    clearable
    :get-show="shouldShow"
    :input-props="{ 'aria-label': t('search.placeholder') }"
    @select="onSelect"
  />
</template>
