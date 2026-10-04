import { describe, expect, it } from 'vitest';
import { createDefaultSchedule, expandCourses, parseCsv, parseOcrText, parseOcrTsv, writeTemplate, parseWorkbook, parseWorkbook as parseXlsx } from '../../src/core-ts';
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

  it('parses OCR text with an explicit weekday and flexible course markers', () => {
    const text = '周一\n高等数学\n王老师\n1-16周\nA101\n[01-02]节\n\n周三：大学英语\n李老师\n1-8周\nB203\n05-06节';
    const courses = parseOcrText(text, '课表截图.png');
    expect(courses).toHaveLength(2);
    expect(courses[0]).toMatchObject({ name: '高等数学', day: 1, period: '01-02' });
    expect(courses[1]).toMatchObject({ name: '大学英语', day: 3, period: '05-06' });
  });

  it('uses OCR column coordinates to assign courses to weekday headers', () => {
    const row = (text: string, left: number, top: number, line: number, word: number) => `5\t1\t1\t1\t${line}\t${word}\t${left}\t${top}\t80\t20\t96\t${text}`;
    const words = (values: string[], left: number, top: number, line: number) => values.map((value, index) => row(value, left + index * 42, top, line, index + 1).replace('\t80\t20\t', '\t20\t20\t'));
    const tsv = [
      'level\tpage_num\tblock_num\tpar_num\tline_num\tword_num\tleft\ttop\twidth\theight\tconf\ttext',
      ...words(['周', '一'], 50, 0, 1), ...words(['周', '三'], 250, 0, 1),
      ...words(['高', '等', '数', '学'], 50, 40, 2), ...words(['大', '学', '英', '语'], 250, 40, 2),
      ...words(['王', '老', '师'], 50, 65, 3), ...words(['李', '老', '师'], 250, 65, 3),
      row('1-16周', 50, 90, 4, 1), row('1-8周', 250, 90, 4, 2),
      row('A101', 50, 115, 5, 1), row('B203', 250, 115, 5, 2),
      row('[01-02]节', 50, 140, 6, 1), row('05-06节', 250, 140, 6, 2),
    ].join('\n');
    const courses = parseOcrTsv(tsv, 'grid.png');
    expect(courses).toHaveLength(2);
    expect(courses.map((course) => course.day)).toEqual([1, 3]);
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
