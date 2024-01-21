import { fakerPL } from "@faker-js/faker";

export class Day {
  rooms: Room[];

  constructor() {
    this.rooms = [];
  }
}

export class Room {
  slots: Slot[];

  constructor() {
    this.slots = [];
  }
}

export class Slot {
  chairPerson: string | null;
  entry: Entry | null;

  constructor(chairPerson: string | null, entry: Entry | null = null) {
    this.chairPerson = chairPerson;
    this.entry = entry;
  }
}

export class Entry {
  reviewerId: string;
  supervisorId: string;
  titleName: string;

  constructor() {
    this.titleName = fakerPL.commerce.productName();
    this.supervisorId = fakerPL.person.fullName();
    this.reviewerId = fakerPL.person.fullName();
  }
}
