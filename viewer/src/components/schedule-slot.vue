<script lang="ts">
/* eslint-disable vue/no-mutating-props */
import { PropType } from "vue";
import { Slot } from "@/types/data-types";
import CustomField from "@/components/custom-field.vue";

export default {
  components: { CustomField },
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
    showChairperson: {
      type: Boolean,
      requireD: false,
      default: false,
    },
  },
};
</script>

<template>
  <v-card
    :variant="selected ? 'outlined' : 'elevated'"
    class=""
    style="cursor: pointer"
    width="296"
  >
    <v-card-text
      class="d-flex flex-column align-start justify-center h-100 py-1"
    >
      <custom-field
        v-if="showChairperson"
        class="text-center"
        :value="item?.chairPerson"
        icon="mdi-account-tie"
      ></custom-field>
      <!--      <custom-field-->
      <!--        class="text-center mt-2"-->
      <!--        :value="item.entry?.titleName"-->
      <!--        icon="mdi-format-title"-->
      <!--      ></custom-field>-->
      <custom-field
        class="text-center"
        :value="item.entry?.supervisorId"
        icon="mdi-account-star"
      ></custom-field>
      <custom-field
        class="text-center"
        :value="item.entry?.reviewerId"
        icon="mdi-account-check"
      ></custom-field>
    </v-card-text>
  </v-card>
</template>

<style scoped></style>
