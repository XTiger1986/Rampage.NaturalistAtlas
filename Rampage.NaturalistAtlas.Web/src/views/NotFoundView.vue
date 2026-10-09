<script setup lang="ts">
/**
 * 404 页面（面板内展示，地图仍可用） / 404 page (shown inside the panel; the map stays usable)
 */
import { useI18n } from 'vue-i18n'
import { NButton } from 'naive-ui'
import EmptyState from '@/components/common/EmptyState.vue'

const { t } = useI18n()
</script>

<template>
  <div class="py-10">
    <EmptyState :title="t('notFound.title')" desc="404" :show-join="false" />
    <div class="flex justify-center">
      <NButton size="small" type="primary" @click="$router.push('/')">{{ t('notFound.back') }}</NButton>
    </div>
  </div>
</template>
