<script setup lang="ts">
/**
 * 版权署名徽标：©观察者（· 数据发布机构）· 协议 / Attribution badge: ©observer (· dataset publisher) · license
 * 产品文档 §9 版权要求：所有照片展示保留作者与协议署名——照片出现的任何位置都必须带上此组件。 / Product doc §9 copyright requirement: every photo display keeps author and license attribution — this component must accompany a photo wherever one appears.
 * compact 形态只显示协议短名（缩略图/榜单行等空间极小处；hover title 展示完整署名）。 / The compact variant shows only the short license name (thumbnails, rank rows and other tight spots; hover title shows full attribution).
 */
import { computed } from 'vue'
import { attribution, licenseLabel, licenseUrl } from '@/utils/format'

const props = withDefaults(
  defineProps<{
    userLogin?: string | null
    license: number
    datasetTitle?: string | null
    /** 紧凑形态：仅协议短名（title 保留完整署名） / Compact variant: short license name only (title keeps full attribution) */
    compact?: boolean
  }>(),
  { userLogin: null, datasetTitle: null, compact: false }
)

const fullText = computed(() => attribution(props.userLogin, props.license, props.datasetTitle))
const text = computed(() => (props.compact ? licenseLabel(props.license) : fullText.value))
const href = computed(() => licenseUrl(props.license))
</script>

<template>
  <a
    v-if="href"
    :href="href"
    target="_blank"
    rel="noopener"
    class="inline-block max-w-full truncate text-[10px] text-ink-3 hover:text-ink-2"
    :title="fullText"
  >
    {{ text }}
  </a>
  <span v-else class="inline-block max-w-full truncate text-[10px] text-ink-3" :title="fullText">{{ text }}</span>
</template>
