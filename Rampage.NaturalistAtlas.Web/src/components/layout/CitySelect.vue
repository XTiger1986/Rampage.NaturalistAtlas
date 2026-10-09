<script setup lang="ts">
/**
 * 城市选择器 / City selector
 * 选中后：city store 更新 → MapCanvas 飞行到城市 bbox → 各数据查询按新 cityId 重新请求 / On select: city store updates → MapCanvas flies to the city bbox → data queries refetch with the new cityId
 * 城市多于 30 个时下拉按国家分组（设计文档 §5.11）。 / With more than 30 cities the dropdown groups by country (design doc §5.11).
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { NSelect, NTooltip } from 'naive-ui'
import type { SelectGroupOption, SelectOption } from 'naive-ui'
import { useCityStore } from '@/stores/city'

const { t, locale } = useI18n()
const city = useCityStore()

/** 城市多于该数时按国家分组 / Group by country when cities exceed this count */
const GROUP_THRESHOLD = 30

/** 下拉选项（英文界面显示英文名） / Dropdown options (English UI shows English names) */
const options = computed<(SelectOption | SelectGroupOption)[]>(() => {
  const items = city.cities.map((c) => ({
    label: locale.value === 'en-US' ? c.nameEn || c.name : c.name,
    value: c.slug,
    country: c.countryName ?? ''
  }))
  if (items.length <= GROUP_THRESHOLD) {
    return items.map(({ label, value }) => ({ label, value }))
  }
  const groups = new Map<string, SelectOption[]>()
  for (const it of items) {
    const list = groups.get(it.country) ?? []
    list.push({ label: it.label, value: it.value })
    groups.set(it.country, list)
  }
  return [...groups.entries()].map(([country, children]) => ({
    type: 'group' as const,
    label: country || t('common.all'),
    key: `country:${country}`,
    children
  }))
})
</script>

<template>
  <div class="flex items-center gap-1">
    <NSelect
      :value="city.currentSlug"
      :options="options"
      filterable
      size="small"
      class="w-28"
      :placeholder="t('city.select')"
      @update:value="(v: string) => city.setCity(v)"
    />
    <!-- 离线提示：后端未连接时使用内置城市列表 / Offline hint: built-in city list used while the backend is unreachable -->
    <NTooltip v-if="city.loaded && city.offline">
      <template #trigger>
        <span class="inline-block h-2 w-2 rounded-full bg-warn" />
      </template>
      {{ t('city.offline') }}<br />{{ t('common.backendHint') }}
    </NTooltip>
  </div>
</template>
