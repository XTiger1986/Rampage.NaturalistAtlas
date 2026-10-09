<script setup lang="ts">
/**
 * 物种照片墙（设计文档 §5.6） / Species photo wall (design doc §5.6)
 *
 * 自取数：/observation/list?taxonId=（每页 30，仅有图记录）无限加载； / Self-fetching: /observation/list?taxonId= (30 per page, photo records only) with infinite loading;
 * CSS columns 瀑布流（桌面 3 列 / 移动 2 列），图片按自然宽高比排列； / CSS-columns masonry (3 columns desktop / 2 mobile), images laid out at their natural aspect ratio;
 * 哨兵复用面板滚动容器（useInfiniteSentinel 内部约定）。 / The sentinel reuses the panel scroll container (useInfiniteSentinel contract).
 */
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import PhotoCard from '@/components/observation/PhotoCard.vue'
import { useInfiniteSentinel } from '@/composables/useInfiniteSentinel'
import { useSpeciesPhotos } from '@/composables/queries'

const props = defineProps<{
  taxonId: string
  cityId: number
}>()

const { t } = useI18n()

const { data, flat, isLoading, error, fetchNextPage, hasNextPage, isFetchingNextPage } = useSpeciesPhotos(
  () => props.taxonId,
  () => props.cityId
)

/** 大图键盘切换的列表上下文 / List context for lightbox keyboard navigation */
const ids = computed(() => flat.value.map((o) => o.id))

const total = computed(() => data.value?.pages[0]?.pager?.recordCount ?? null)

/** 底部哨兵（公共 composable） / Bottom sentinel (shared composable) */
const sentinel = ref<HTMLElement | null>(null)
useInfiniteSentinel(sentinel, () => {
  if (hasNextPage.value && !isFetchingNextPage.value) {
    fetchNextPage()
  }
})
</script>

<template>
  <div>
    <div v-if="total != null" class="mb-2 text-xs text-ink-3 tabular">{{ t('explore.inView', { n: total }) }}</div>
    <div v-if="error" class="text-xs text-warn">{{ t('common.networkError') }}</div>
    <div v-else-if="!isLoading && flat.length === 0" class="text-xs text-ink-3">{{ t('common.empty') }}</div>
    <!-- 面板（420px 桌面侧栏 / 移动抽屉）内固定 2 列：3 列每列仅约 120px 过挤（容器断点不再需要） / Fixed 2 columns inside the panel (420px desktop sidebar / mobile sheet): 3 columns would leave only ~120px each, too cramped (container breakpoints no longer needed) -->
    <div class="columns-2 gap-3">
      <div v-for="p in flat" :key="p.id" class="mb-3 break-inside-avoid">
        <PhotoCard :obs="p" :ids="ids" />
      </div>
    </div>
    <div v-if="isLoading" class="py-4 text-center text-xs text-ink-3">{{ t('common.loading') }}</div>
    <div v-else-if="isFetchingNextPage" class="py-2 text-center text-xs text-ink-3">{{ t('common.loading') }}</div>
    <!-- 无限滚动哨兵 / Infinite-scroll sentinel -->
    <div ref="sentinel" class="h-px w-full" />
  </div>
</template>
