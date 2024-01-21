<script setup lang="ts">
import { defineProps } from "vue";

const props = defineProps({
  icon: {
    type: String,
    required: false,
    default: null,
  },
  value: {
    type: String,
    required: false,
    default: null,
  },
});
</script>

<template>
  <div
    class="w-100 d-flex flex-nowrap flex-row align-center my-1"
    style="border-radius: 4px"
  >
    <v-icon size="14" color="gray" style="width: 32px">{{ icon }}</v-icon>
    <span class="w-100 bg-grey-lighten-3 text-left px-4 py-1">
      {{ value ?? "&nbsp;" }}
    </span>
  </div>
</template>
<style scoped></style>
