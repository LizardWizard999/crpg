<script setup lang="ts">
import type { ItemFlat } from '~/models/item'

import { humanizeBucket } from '~/services/item-service'

const { aggregation, bucketValue, docCount } = defineProps<{
  aggregation: keyof ItemFlat
  bucketValue: any
  docCount: number
}>()

const modelValue = defineModel<string[] | number[]>()

const bucket = computed(() => humanizeBucket(aggregation, bucketValue))
</script>

<template>
  <Tooltip
    v-bind="{
      placement: 'top',
      disabled: bucket.tooltip?.description === null,
      title: bucket.tooltip?.title,
      description: bucket.tooltip?.description,
    }"
  >
    <OCheckbox
      v-model="modelValue"
      :native-value="bucketValue"
      class="items-center"
    >
      <div class="flex items-center gap-2">
        <ItemFieldIcon
          v-if="bucket.icon !== null"
          :icon="bucket.icon"
          :label="bucket.label"
        />
        {{ bucket.label }}
        <span class="inline text-content-400">({{ docCount }})</span>
      </div>
    </OCheckbox>
  </Tooltip>
</template>
