<template>
  <v-app>
    <v-container>
      <v-row>
        <v-col cols="auto">
          <v-row>
            <h2>Obrony:</h2>
          </v-row>
          <v-row
            :key="index"
            v-for="(slot, index) in entriesLeft"
            style="height: 128px"
          >
            <ScheduleEntry
              :item="slot"
              :selected="
                selected &&
                selected[0] === -1 &&
                selected[1] === -1 &&
                selected[2] === index
              "
              class="my-1"
              style="flex: 1 1 0"
              @click="handleItemClick([-1, -1, index])"
            />
          </v-row>
        </v-col>
        <v-col cols="auto">
          <v-row>
            <h2>&nbsp;</h2>
          </v-row>
          <v-row>
            <h3>&nbsp;</h3>
          </v-row>
          <v-row
            :key="index"
            v-for="index in 8"
            style="height: 128px; font-weight: 900; font-size: 1.5rem"
            class="d-flex flex-column align-center justify-center"
          >
            {{ index }}
          </v-row>
        </v-col>
        <v-col
          v-for="(daySlots, dayIndex) in slots"
          :key="dayIndex"
          class="mx-2"
        >
          <v-row>
            <h2>Dzień {{ dayIndex }}</h2>
          </v-row>
          <v-row>
            <v-col
              v-for="(roomSlots, roomIndex) in daySlots"
              :key="roomIndex"
              class="mx-1"
            >
              <v-row>
                <h3>Pokój {{ roomIndex }}</h3>
              </v-row>
              <v-row
                :key="index"
                v-for="(slot, index) in roomSlots"
                style="height: 128px"
              >
                <ScheduleEntry
                  :item="slot"
                  :selected="
                    selected &&
                    selected[0] === dayIndex &&
                    selected[1] === roomIndex &&
                    selected[2] === index
                  "
                  class="my-1"
                  style="flex: 1 1 0"
                  @click="handleItemClick([dayIndex, roomIndex, index])"
                />
              </v-row>
            </v-col>
          </v-row>
        </v-col>
      </v-row>
    </v-container>
  </v-app>
</template>

<script setup>
import { defineProps, nextTick, onMounted, ref, watch } from "vue";
import ScheduleEntry from "@/components/schedule-slot.vue";
import { Slot } from "@/types/data-types";

const props = defineProps({
  entries: Array,
  slots: Array,
});

const selected = ref(null);
ref(0);

const entriesLeft = ref([]);

onMounted(() => {
  nextTick(() => {
    console.log("props", props.entries);
    const outsideSlots = [];
    for (const entry of props.entries) {
      console.log(entry.titleName);
      outsideSlots.push(new Slot(null, entry));
    }
    entriesLeft.value = outsideSlots;
    console.log(outsideSlots);
  });
});

watch(
  () => props.entries,
  () => {
    const outsideSlots = [];
    for (const entry of props.entries) {
      console.log(entry.titleName);
      outsideSlots.push(new Slot(null, entry));
    }
    entriesLeft.value = outsideSlots;
  }
);

const handleItemClick = (index) => {
  if (selected.value == null) {
    if (
      (index[0] === -1 && index[1] === -1) ||
      (index[0] !== -1 &&
        index[1] !== -1 &&
        props.slots[index[0]][index[1]][index[2]].entry != null)
    )
      selected.value = index;
  } else {
    const firstClickLeftOver =
      selected.value[0] === -1 && selected.value[1] === -1; // [-1, -1, N] are left overs

    const secondClickLeftOver = index[0] === -1 && index[1] === -1; // [-1, -1, N] are left overs

    let firstClicked = firstClickLeftOver
      ? entriesLeft.value[selected.value[2]]
      : props.slots[selected.value[0]][selected.value[1]][selected.value[2]];

    let secondClicked = secondClickLeftOver
      ? entriesLeft.value[index[2]]
      : props.slots[index[0]][index[1]][index[2]];
    console.log("First Clicked:", firstClicked);
    console.log("Second Clicked:", secondClicked);

    const temp = secondClicked.entry;
    secondClicked.entry = firstClicked.entry;
    firstClicked.entry = temp;

    if (firstClickLeftOver) {
      entriesLeft.value.splice(selected.value[2], 1);
    }

    if (secondClickLeftOver) {
      entriesLeft.value.splice(index[2], 1);
    }

    selected.value = null;
  }
};
</script>
