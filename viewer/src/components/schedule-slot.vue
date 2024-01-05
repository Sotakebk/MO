<script lang="ts">
import { PropType } from "vue";
import { Slot } from "@/types/data-types";

export default {
  props: {
    selected: {
      type: Boolean,
      required: false,
      default: false,
    },
    item: {
      type: Object as PropType<Slot>,
      required: true,
    },
  },
};
</script>

<template>
  <v-card
    :elevation="selected ? 12 : 2"
    class=""
    style="cursor: pointer"
    width="256"
    height="120"
  >
    <v-card-text class="d-flex flex-column align-start justify-center h-100">
      <p>
        <v-icon class="pb-1 mr-1">mdi-account-tie</v-icon>{{ item.chairPerson }}
      </p>
      <div v-if="item.entry" class="mt-2">
        <p class="text-center w-100">{{ item.entry.titleName }}</p>
        <p><v-icon>mdi-account-star</v-icon>{{ item.entry.supervisorId }}</p>
        <p><v-icon>mdi-account-check</v-icon>{{ item.entry.reviewerId }}</p>
      </div>
      <div v-else class="text-center w-100">
        <p><i>[ Brak ]</i></p>
      </div>
    </v-card-text>
  </v-card>
</template>

<style scoped></style>
