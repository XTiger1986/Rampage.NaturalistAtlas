<script setup lang="ts">
/**
 * 面板页头：标题 + 副标题 + 右侧操作插槽 / Panel page header: title + subtitle + right-side actions slot
 * 纸本主题下标题为宋体并带下划墨线（由 .font-display 与主题令牌控制） / Under the paper theme the title uses a serif face with an ink underline (controlled by .font-display and theme tokens)
 */
defineProps<{
  /** 标题 / Title */
  title: string
  /** 副标题（如城市名、数据口径） / Subtitle (e.g. city name, data caliber) */
  subtitle?: string
}>()
</script>

<template>
  <header class="flex items-end justify-between gap-3 border-b border-line px-5 pt-5 pb-3 pr-12">
    <div class="min-w-0">
      <h1 class="font-display truncate text-xl font-bold text-ink">{{ title }}</h1>
      <p v-if="subtitle" class="mt-0.5 truncate text-xs text-ink-3">{{ subtitle }}</p>
    </div>
    <div class="flex shrink-0 items-center gap-2">
      <slot name="actions" />
    </div>
  </header>
</template>
