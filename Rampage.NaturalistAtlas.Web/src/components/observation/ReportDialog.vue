<script setup lang="ts">
/**
 * 举报对话框（产品文档 §3.2 内容安全：用户可举报；运营隐藏后前端 5 分钟内不可见） / Report dialog (product doc §3.2 content safety: users can report; operator-hidden items become invisible to the frontend within 5 minutes)
 *
 * 提交 POST /api/report，携带幂等键（同一次打开对话框内重复点击只生效一次）。 / Submits POST /api/report with an idempotency key (repeat clicks within one dialog session take effect only once).
 */
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { NButton, NForm, NFormItem, NInput, NModal, NRadio, NRadioGroup, useMessage } from 'naive-ui'
import { useUiStore } from '@/stores/ui'
import { useSubmitReport } from '@/composables/queries'
import type { ReportInput } from '@/api/types'
import { ApiError } from '@/api/http'
import { addReported } from '@/utils/reported'

const { t, tm } = useI18n()
const ui = useUiStore()
const message = useMessage()
const { mutateAsync, isPending } = useSubmitReport()

/** 表单 / Form fields */
const reason = ref<ReportInput['reason']>(1)
const content = ref('')
const contact = ref('')
/** 本次对话框的幂等键 / Idempotency key for this dialog session */
let idemKey = ''

/** 选择"其他"（0）时必须填写补充说明（与后端 AtlasObservationService.SubmitReport 校验一致） / Choosing "other" (0) requires a supplemental note (mirrors the backend AtlasObservationService.SubmitReport validation) */
const canSubmit = computed(() => reason.value !== 0 || content.value.trim() !== '')

// 每次打开重置表单并生成新的幂等键 / Reset the form and generate a fresh idempotency key on each open
watch(
  () => ui.reportId,
  (id) => {
    if (id) {
      reason.value = 1
      content.value = ''
      contact.value = ''
      idemKey = crypto.randomUUID()
    }
  }
)

/** 提交 / Submit */
async function submit(): Promise<void> {
  if (!ui.reportId) {
    return
  }
  try {
    await mutateAsync({
      input: { observationId: ui.reportId, reason: reason.value, content: content.value || undefined, contact: contact.value || undefined },
      key: idemKey
    })
    message.success(t('report.success'))
    addReported(ui.reportId)
    ui.reportId = null
  } catch (e) {
    // 业务失败（Rampage ResultCode ≥ 1000）展示后端文案；网络（-1）/ HTTP（< 600）错误展示通用提示 / Business failure (Rampage ResultCode ≥ 1000) shows the backend message; network (-1) / HTTP (< 600) errors show a generic hint
    message.error(e instanceof ApiError && e.code >= 1000 && e.message ? e.message : t('report.fail'))
  }
}
</script>

<template>
  <NModal
    :show="ui.reportId !== null"
    preset="card"
    :title="t('report.title')"
    class="!w-[min(420px,94vw)]"
    :mask-closable="!isPending"
    @update:show="(v: boolean) => !v && (ui.reportId = null)"
  >
    <NForm label-placement="top" size="small">
      <NFormItem :label="t('report.reason')">
        <NRadioGroup v-model:value="reason">
          <div class="grid grid-cols-2 gap-1.5">
            <NRadio v-for="(label, i) in (tm('report.reasons') as string[])" :key="i" :value="i">{{ label }}</NRadio>
          </div>
        </NRadioGroup>
      </NFormItem>
      <NFormItem :label="t('report.content')">
        <NInput v-model:value="content" type="textarea" :maxlength="500" show-count :autosize="{ minRows: 3 }" />
      </NFormItem>
      <NFormItem :label="t('report.contact')">
        <NInput v-model:value="contact" :maxlength="100" />
      </NFormItem>
    </NForm>
    <template #footer>
      <div class="flex justify-end gap-2">
        <NButton size="small" @click="ui.reportId = null">{{ t('common.cancel') }}</NButton>
        <NButton size="small" type="primary" :loading="isPending" :disabled="!canSubmit" @click="submit">{{ t('common.submit') }}</NButton>
      </div>
    </template>
  </NModal>
</template>
