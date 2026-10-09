<script setup lang="ts">
/**
 * 空态："招募观察者"引导（产品文档 §10：稀疏城市展示引导页而非空地图） / Empty state: "recruit observers" onboarding (product doc §10: sparse cities show a guide page instead of an empty map)
 */
import { useI18n } from 'vue-i18n'
import { NButton } from 'naive-ui'

withDefaults(defineProps<{ title?: string; desc?: string; showJoin?: boolean }>(), { showJoin: true })
const { t } = useI18n()
</script>

<template>
  <div class="flex flex-col items-center px-6 py-10 text-center">
    <!-- 叶片插画（线稿，随主题强调色） / Leaf line-art illustration (stroke follows theme accent color) -->
    <svg viewBox="0 0 64 64" class="h-14 w-14 text-accent" fill="none" stroke="currentColor" stroke-width="2">
      <path d="M12 52C12 28 28 12 52 12c0 24-16 40-40 40Z" />
      <path d="M12 52 38 26" />
    </svg>
    <h3 class="font-display mt-3 text-base font-bold text-ink">{{ title ?? t('empty.sparseTitle') }}</h3>
    <p class="mt-1 max-w-xs text-xs leading-relaxed text-ink-3">{{ desc ?? t('empty.sparseDesc') }}</p>
    <NButton
      v-if="showJoin"
      class="mt-4"
      size="small"
      type="primary"
      tag="a"
      href="https://www.inaturalist.org/observations/upload"
      target="_blank"
    >
      {{ t('empty.join') }}
    </NButton>
  </div>
</template>
