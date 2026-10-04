export interface Course {
  name: string;
  teacher: string;
  room: string;
  period: string;
  day: number;
  weeks: number[];
  sourceFile: string;
  selected: boolean;
}

export interface PeriodSetting {
  number: number;
  start: string;
  end: string;
}

export interface ScheduleConfig {
  name: string;
  semesterStart: string;
  weekStart: number;
  semesterWeeks: number;
  currentWeek: number;
  periods: PeriodSetting[];
}

export interface CourseOccurrence {
  course: Course;
  week: number;
  date: string;
  start: string;
  end: string;
  firstPeriod: number;
  lastPeriod: number;
}
