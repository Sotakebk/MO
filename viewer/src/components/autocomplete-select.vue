<script setup lang="ts">
import { defineEmits, defineProps, PropType, ref } from "vue";

const props = defineProps({
  items: {
    type: Array as PropType<string[]>,
    required: true,
  },
  icon: {
    type: String,
    required: false,
    default: null,
  },
});

const emit = defineEmits(["selected"]);

const onChange = (val: unknown) => {
  emit("selected", val);
  searchInput.value = null;
};

const searchInput = ref(null as string | null);
</script>

<template>
  <v-autocomplete
    density="compact"
    prepend-icon="mdi-account-tie"
    :items="items"
    v-model:search-input="searchInput"
    @update:modelValue="(val) => onChange(val)"
  />
</template>

<style scoped></style>
