import type { Course, ScheduleConfig } from './types';
import { expandCourses } from './scheduler';

export function buildIcal(courses: Course[], schedule: ScheduleConfig): string {
  const lines = ['BEGIN:VCALENDAR', 'VERSION:2.0', 'PRODID:-//CourseToIcal//CN', 'CALSCALE:GREGORIAN', 'METHOD:PUBLISH', `X-WR-CALNAME:${escapeIcal(schedule.name)}`, 'X-WR-TIMEZONE:Asia/Shanghai'];
  for (const occurrence of expandCourses(courses.filter((course) => course.selected), schedule)) {
    const { course } = occurrence;
    const start = `${occurrence.date.replaceAll('-', '')}T${occurrence.start.replace(':', '')}00`;
    const end = `${occurrence.date.replaceAll('-', '')}T${occurrence.end.replace(':', '')}00`;
    const uid = stableUid(`${course.name}|${course.teacher}|${course.room}|${start}|${course.period}|${occurrence.week}`);
    lines.push('BEGIN:VEVENT', `UID:${uid}`, 'DTSTAMP:19700101T000000Z', `DTSTART;TZID=Asia/Shanghai:${start}`, `DTEND;TZID=Asia/Shanghai:${end}`, `SUMMARY:${escapeIcal(course.name)}`, `LOCATION:${escapeIcal(course.room)}`, `DESCRIPTION:${escapeIcal(`教师：${course.teacher}\n周次：第${occurrence.week}周\n节次：${course.period}节`)}`, 'END:VEVENT');
  }
  lines.push('END:VCALENDAR');
  return `${lines.join('\r\n')}\r\n`;
}

export function stableUid(value: string): string {
  let hashA = 0x811c9dc5;
  let hashB = 0x01000193;
  for (const char of value) {
    hashA ^= char.charCodeAt(0);
    hashA = Math.imul(hashA, 0x01000193);
    hashB ^= char.charCodeAt(0) + 0x9e3779b9;
    hashB = Math.imul(hashB, 0x85ebca6b);
  }
  return `${(hashA >>> 0).toString(16).padStart(8, '0')}${(hashB >>> 0).toString(16).padStart(8, '0')}@coursetoical`;
}

function escapeIcal(value: string): string {
  return (value ?? '').replaceAll('\\', '\\\\').replaceAll(';', '\\;').replaceAll(',', '\\,').replaceAll('\r', '').replaceAll('\n', '\\n');
}
