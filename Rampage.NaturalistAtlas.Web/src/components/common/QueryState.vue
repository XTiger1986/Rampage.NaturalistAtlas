<script setup lang="ts">
/**
 * 查询状态包装：加载骨架 / 错误重试 / 空态 / 离线提示，内容就绪后渲染默认插槽 / Query state wrapper: loading skeleton / error retry / empty state / offline hint; renders the default slot once content is ready
 * 所有依赖接口的面板区块都应包一层，保证体验一致。 / Every API-backed panel section should be wrapped for a consistent experience.
 */
import { useI18n } from 'vue-i18n'
import { NButton, NSkeleton } from 'naive-ui'
import EmptyState from './EmptyState.vue'
import { useCityStore } from '@/stores/city'

withDefaults(
  defineProps<{
    /** 加载中（首次） / Loading (first fetch) */
    loading?: boolean
    /** 错误对象 / Error object */
    error?: unknown
    /** 数据为空 / Data is empty */
    empty?: boolean
    /** 骨架行数 / Skeleton row count */
    rows?: number
  }>(),
  { loading: false, error: undefined, empty: false, rows: 4 }
)

const emit = defineEmits<{ retry: [] }>()
const { t } = useI18n()
const city = useCityStore()
</script>

<template>
  <!-- 离线：后端未连接 / Offline: backend not reachable -->
  <div v-if="city.loaded && city.offline" class="px-5 py-8 text-center text-sm text-ink-3">
    <p class="text-ink-2">{{ t('common.networkError') }}</p>
    <p class="mt-1 text-xs">{{ t('common.backendHint') }}</p>
  </div>
  <div v-else-if="loading" class="space-y-3 px-5 py-4">
    <NSkeleton v-for="i in rows" :key="i" text :repeat="1" height="48px" :sharp="false" />
  </div>
  <div v-else-if="error" class="px-5 py-8 text-center text-sm text-ink-3">
    <p class="text-danger">{{ (error as Error)?.message ?? t('common.networkError') }}</p>
    <NButton class="mt-3" size="small" @click="emit('retry')">{{ t('common.retry') }}</NButton>
  </div>
  <slot v-else-if="empty" name="empty">
    <EmptyState />
  </slot>
  <slot v-else />
</template>
