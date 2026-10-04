import type { Course, CourseOccurrence, PeriodSetting, ScheduleConfig } from './types';

const startTimes = ['08:30', '09:20', '10:25', '11:15', '14:00', '14:50', '15:55', '16:45', '18:30', '19:20', '20:30'];
const endTimes = ['09:15', '10:05', '11:10', '12:00', '14:45', '15:35', '16:40', '17:30', '19:15', '20:05', '21:15'];

export function createDefaultSchedule(): ScheduleConfig {
  const periods: PeriodSetting[] = startTimes.map((start, index) => ({ number: index + 1, start, end: endTimes[index] }));
  return { name: '默认课表', semesterStart: '2026-08-31', weekStart: 1, semesterWeeks: 20, currentWeek: 1, periods };
}

export function expandCourses(courses: Course[], schedule: ScheduleConfig, onlyWeek?: number): CourseOccurrence[] {
  const result: CourseOccurrence[] = [];
  for (const course of courses) {
    if (!course || course.day < 1 || course.day > 7) continue;
    const [firstPeriod, lastPeriod] = parsePeriod(course.period);
    if (!firstPeriod || !lastPeriod) continue;
    for (const week of [...new Set(course.weeks)].filter((week) => week >= 1 && week <= schedule.semesterWeeks)) {
      if (onlyWeek !== undefined && week !== onlyWeek) continue;
      const weekStart = addDays(parseDate(schedule.semesterStart), -(schedule.weekStart - 1) + (week - 1) * 7);
      const date = addDays(weekStart, course.day - 1);
      result.push({ course, week, date: formatDate(date), start: schedule.periods[firstPeriod - 1].start, end: schedule.periods[lastPeriod - 1].end, firstPeriod, lastPeriod });
    }
  }
  return result;
}

export function parsePeriod(value: string): [number, number] | [0, 0] {
  const matches = value.match(/\d+/g) ?? [];
  if (!matches.length) return [0, 0];
  const first = Number(matches[0]);
  const last = Number(matches[matches.length - 1]);
  if (first < 1 || last < first || last > 11) return [0, 0];
  return [first, last];
}

export function addDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

export function parseDate(value: string): Date {
  const [year, month, day] = value.slice(0, 10).split('-').map(Number);
  return new Date(year, month - 1, day);
}

export function formatDate(value: Date): string {
  return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`;
}
