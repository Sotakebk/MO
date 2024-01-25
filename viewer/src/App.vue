<template>
  <TimeTableNew :entries="entries" :chairpersons="chairpersons" />
</template>

<script lang="ts">
import { defineComponent, onMounted, ref } from "vue";
import { Entry, Slot } from "@/types/data-types";
import { fakerPL } from "@faker-js/faker";
import TimeTableNew from "@/components/time-table-new.vue";

export default defineComponent({
  name: "App",
  components: {
    TimeTableNew,
  },
  setup() {
    const slots = ref([] as Slot[][][]);
    const entries = ref([] as Entry[]);
    const chairpersons = ref(["Bereta", "Białas", "Płażek", "Koroński"]);

    onMounted(() => {
      const days = 3;
      const rooms = 2;
      const offsets = 8;

      slots.value = setupSlots(days, rooms, offsets);
      for (let i = 0; i < days * rooms * offsets; i++)
        entries.value.push(new Entry());
    });

    const setupSlots = (days: number, rooms: number, offsets: number) => {
      const result = [];
      for (let dayNo = 0; dayNo < days; dayNo++) {
        let dayRes = [];

        for (let roomNo = 0; roomNo < rooms; roomNo++) {
          let roomRes = [];
          for (let offset = 0; offset < offsets; offset++) {
            roomRes.push(new Slot(fakerPL.person.fullName()));
          }
          dayRes.push(roomRes);
        }
        result.push(dayRes);
      }
      return result;
    };

    return {
      slots,
      entries,
      chairpersons,
    };
  },
});
</script>

<style>
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
}

html {
  font-size: 12px;
}

.v-col {
  max-width: unset !important;
}

.v-col-auto {
  max-width: unset !important;
}
</style>
