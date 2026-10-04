import { describe, expect, it } from 'vitest';
import { createDefaultSchedule, expandCourses, parseCsv, writeTemplate, parseWorkbook, parseWorkbook as parseXlsx } from '../../src/core-ts';
import type { Course } from '../../src/core-ts/types';

describe('CourseToIcal TypeScript core', () => {
  it('parses CSV fields containing quoted commas', () => {
    const courses = parseCsv('day,name,teacher,weeks,room,period\n2,"数据库,原理",李老师,1-2,机房1,03-04\n', 'quoted.csv');
    expect(courses).toHaveLength(1);
    expect(courses[0].name).toBe('数据库,原理');
    expect(courses[0].day).toBe(2);
  });

  it('parses every course block in a timetable cell', async () => {
    const xlsx = await import('xlsx');
    const workbook = xlsx.utils.book_new();
    const sheet = xlsx.utils.aoa_to_sheet([["高等数学\n王老师\n1-16[周]\nA101\n[01-02]节\n\n大学英语\n李老师\n1-8[周]\nB203\n[05-06]节"]]);
    xlsx.utils.book_append_sheet(workbook, sheet, '课表');
    const bytes = xlsx.write(workbook, { bookType: 'xlsx', type: 'array' }) as Uint8Array;
    expect(parseXlsx(bytes, 'blocks.xlsx')).toHaveLength(2);
  });

  it('round-trips the editable xlsx template', () => {
    const bytes = writeTemplate();
    const courses = parseWorkbook(bytes, 'template.xlsx');
    expect(courses).toHaveLength(2);
    expect(courses[0]).toMatchObject({ name: '高等数学', day: 1, period: '01-02' });
  });

  it('expands a week into a dated occurrence using the configured period time', () => {
    const course: Course = { name: '晚课', teacher: '老师', room: 'A101', period: '09-11', day: 1, weeks: [1], sourceFile: 'test.csv', selected: true };
    const occurrences = expandCourses([course], createDefaultSchedule(), 1);
    expect(occurrences).toHaveLength(1);
    expect(occurrences[0].date).toBe('2026-08-31');
    expect(occurrences[0].start).toBe('18:30');
    expect(occurrences[0].end).toBe('21:15');
  });

  it('keeps generated iCalendar UIDs deterministic', async () => {
    const module = await import('../../src/core-ts/ical');
    const course: Course = { name: '英语', teacher: '李老师', room: 'B203', period: '05-06', day: 3, weeks: [1], sourceFile: 'test.csv', selected: true };
    const schedule = createDefaultSchedule();
    const first = module.buildIcal([course], schedule);
    const second = module.buildIcal([course], schedule);
    expect(first).toContain('UID:');
    expect(first).toBe(second);
  });
});
