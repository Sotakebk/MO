<template>
  <v-app>
    <v-container :fluid="true" style="max-height: 100vh">
      <v-row class="flex-nowrap" :dense="true">
        <v-col cols="auto" style="max-height: 98vh">
          <v-row :dense="true">
            <v-col>
              <v-btn-toggle v-model="state" dense mandatory color="primary">
                <v-btn small v-bind:key="State.Schedule"> Sloty </v-btn>
                <v-btn small v-bind:key="State.Absences"> Nieobecności </v-btn>
                <v-btn small v-bind:key="State.Assignments"> Obrony </v-btn>
              </v-btn-toggle>
            </v-col>
          </v-row>
          <v-row :dense="true">
            <v-col>
              <h2>Obrony:</h2>
            </v-col>
          </v-row>
          <v-container style="max-height: 100%; overflow-y: auto">
            <v-row
              v-for="(slot, index) in entriesLeft"
              :key="index"
              :dense="true"
              class="mt-1"
            >
              <v-col>
                <ScheduleEntry
                  :item="slot"
                  :selected="
                    selected &&
                    selected[0] === -1 &&
                    selected[1] === -1 &&
                    selected[2] === index
                  "
                  style="flex: 1 1 0"
                  @click="handleItemClick([-1, -1, index])"
                />
              </v-col>
            </v-row>
          </v-container>
        </v-col>
        <v-col
          style="
            max-height: 99vh;
            overflow-y: auto;
            overflow-x: auto;
            display: flex;
          "
          class="flex-nowrap"
        >
          <v-col
            v-for="(day, dayIndex) in days"
            :key="dayIndex"
            cols="auto"
            style="
              border-top: 2px solid rgba(var(--v-theme-primary), 0.2);
              border-right: 2px solid rgba(var(--v-theme-primary), 0.2);
            "
          >
            <v-row :dense="true">
              <v-col>
                <h3 style="width: fit-content; position: sticky; left: 2em">
                  Dzień {{ dayIndex + 1 }}
                </h3>
              </v-col>
            </v-row>
            <v-row class="flex-nowrap" :dense="true">
              <v-col
                v-for="(room, roomIndex) in day.rooms"
                :key="roomIndex"
                cols="auto"
              >
                <v-row :dense="true">
                  <v-col>
                    <h4>Pokój {{ roomIndex + 1 }}</h4>
                  </v-col>
                </v-row>
                <v-row
                  :key="index"
                  v-for="(slot, index) in room.slots"
                  :dense="true"
                >
                  <v-col>
                    <template v-if="index % blockLength == 0">
                      <v-row
                        style="border-top: 2px solid black"
                        :dense="true"
                        class="align-center justify-center d-flex"
                      >
                        <v-col cols="auto">
                          <span>
                            {{ getTime(index) }}
                          </span>
                        </v-col>
                        <v-col v-if="state === State.Schedule">
                          <span class="text-body-2">
                            Przypisz do obron w bloku:
                          </span>
                          <autocomplete-select
                            :items="chairpersons"
                            icon="mdi-account-tie"
                            @selected="
                              (chairperson) =>
                                setChairperson(
                                  dayIndex,
                                  roomIndex,
                                  Math.ceil(index / blockLength),
                                  chairperson
                                )
                            "
                          />
                        </v-col>
                      </v-row>
                    </template>
                    <ScheduleEntry
                      v-if="state === State.Assignments"
                      :item="slot"
                      :showChairperson="
                        index == 0 || index % blockLength == 0
                          ? true
                          : slot.chairPerson !=
                            room.slots[index - 1].chairPerson
                      "
                      :selected="
                        selected &&
                        selected[0] === dayIndex &&
                        selected[1] === roomIndex &&
                        selected[2] === index
                      "
                      @click="handleItemClick([dayIndex, roomIndex, index])"
                    />
                    <ScheduleBlock
                      v-else
                      :time="getTime(index)"
                      :item="slot"
                      :showChairperson="
                        index == 0 || index % blockLength == 0
                          ? true
                          : slot.chairPerson !=
                            room.slots[index - 1].chairPerson
                      "
                      :selected="
                        selected &&
                        selected[0] === dayIndex &&
                        selected[1] === roomIndex &&
                        selected[2] === index
                      "
                      @click="handleItemClick([dayIndex, roomIndex, index])"
                    />
                  </v-col>
                </v-row>
                <v-row v-if="state === State.Schedule" :dense="true">
                  <v-col>
                    <v-btn
                      @click="addSlot(dayIndex, roomIndex)"
                      width="100%"
                      variant="tonal"
                      color="secondary"
                    >
                      Dodaj slot
                    </v-btn>
                  </v-col>
                </v-row>
              </v-col>
              <v-col v-if="state === State.Schedule" cols="auto">
                <v-btn
                  @click="addRoom(dayIndex)"
                  height="88vh"
                  variant="tonal"
                  color="primary"
                  style="
                    font-weight: 900;
                    writing-mode: vertical-rl;
                    text-orientation: upright;
                    position: sticky;
                    top: 0;
                  "
                >
                  Dodaj pokój
                </v-btn>
              </v-col>
            </v-row>
          </v-col>
          <v-col v-if="state === State.Schedule" cols="auto">
            <v-btn
              @click="addDay()"
              height="92vh"
              variant="tonal"
              color="primary-darken-1"
              style="
                font-weight: 900;
                writing-mode: vertical-rl;
                text-orientation: upright;
                position: sticky;
                top: 0;
              "
            >
              Dodaj dzień
            </v-btn>
          </v-col>
        </v-col>
      </v-row>
    </v-container>
  </v-app>
</template>

<script lang="ts" setup>
import { defineProps, nextTick, onMounted, PropType, ref, watch } from "vue";
import ScheduleEntry from "@/components/schedule-slot.vue";
import { Day, Entry, Room, Slot } from "@/types/data-types";
import { fakerPL } from "@faker-js/faker";
import AutocompleteSelect from "@/components/autocomplete-select.vue";
import ScheduleBlock from "@/components/schedule-block.vue";

const enum State {
  Schedule,
  Absences,
  Assignments,
}

const props = defineProps({
  entries: {
    type: Array as PropType<Entry[]>,
    required: true,
  },
  chairpersons: {
    type: Array as PropType<string[]>,
    required: true,
  },
});

const selected = ref(null as number[] | null);

const blockLength = ref(8);

const entriesLeft = ref([] as Slot[]);

const days = ref([] as Day[]);

const state = ref(State.Assignments);

onMounted(() => {
  for (let dayNo = 0; dayNo < 3; dayNo++) {
    addDay();
    for (let roomNo = 0; roomNo < 2; roomNo++) {
      if (roomNo != 0) addRoom(dayNo);
      for (let slotNo = 0; slotNo < 16; slotNo++) {
        if (slotNo != 0) addSlot(dayNo, roomNo);
      }
    }
  }

  nextTick(() => {
    const outsideSlots = [] as Slot[];
    for (const entry of props.entries) {
      outsideSlots.push(new Slot(null, entry));
    }
    entriesLeft.value = outsideSlots;
  });
});

const getTime = (index: number) => {
  const date = new Date("2024-01-01T08:00:00+01:00");
  date.setMinutes(date.getMinutes() + index * 30);
  return date.toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h24",
  });
};

const addSlot = (day: number, room: number) => {
  const roomLen = days.value[day].rooms[room].slots.length;
  let previousChairperson: null | string = null;

  if (roomLen > 0)
    previousChairperson =
      days.value[day].rooms[room].slots[roomLen - 1].chairPerson;
  days.value[day].rooms[room].slots.push(new Slot(previousChairperson));
};

const addRoom = (day: number) => {
  let newRoom = new Room();
  newRoom.slots.push(new Slot(fakerPL.person.fullName()));

  days.value[day].rooms.push(newRoom);
};

const addDay = () => {
  let room = new Room();
  room.slots.push(new Slot(fakerPL.person.fullName()));
  let day = new Day();
  day.rooms.push(room);

  days.value.push(day);
};

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

const setChairperson = (
  dayIndex: number,
  roomIndex: number,
  blockIndex: number,
  chairperson: string
) => {
  const roomSlots = days.value[dayIndex].rooms[roomIndex].slots.length;
  for (
    let i = blockIndex * blockLength.value;
    i < (blockIndex + 1) * blockLength.value && i < roomSlots;
    i++
  ) {
    days.value[dayIndex].rooms[roomIndex].slots[i].chairPerson = chairperson;
  }
};

const handleItemClick = (index: number[]) => {
  if (selected.value == null) {
    if (
      (index[0] === -1 && index[1] === -1) ||
      (index[0] !== -1 &&
        index[1] !== -1 &&
        days.value[index[0]].rooms[index[1]].slots[index[2]].entry != null)
    )
      selected.value = index;
  } else {
    const firstClickLeftOver =
      selected.value[0] === -1 && selected.value[1] === -1; // [-1, -1, N] are left overs

    const secondClickLeftOver = index[0] === -1 && index[1] === -1; // [-1, -1, N] are left overs

    if (
      selected.value[0] == index[0] &&
      selected.value[1] == index[1] &&
      selected.value[2] == index[2]
    ) {
      selected.value = null;
      return;
    }
    let firstClicked = firstClickLeftOver
      ? entriesLeft.value[selected.value[2]]
      : days.value[selected.value[0]].rooms[selected.value[1]].slots[
          selected.value[2]
        ];

    let secondClicked = secondClickLeftOver
      ? entriesLeft.value[index[2]]
      : days.value[index[0]].rooms[index[1]].slots[index[2]];

    const temp = secondClicked.entry;
    secondClicked.entry = firstClicked.entry;
    firstClicked.entry = temp;

    if (firstClickLeftOver && !secondClickLeftOver && temp == null) {
      entriesLeft.value.splice(selected.value[2], 1);
    }

    if (secondClickLeftOver && !firstClickLeftOver && temp == null) {
      entriesLeft.value.splice(index[2], 1);
    }

    selected.value = null;
  }
};
</script>
