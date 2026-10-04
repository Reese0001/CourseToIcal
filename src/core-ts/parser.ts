import * as XLSX from 'xlsx';
import type { Course } from './types';

const courseBlock = /^\s*(?<name>[^\r\n]+)\r?\n(?<teacher>[^\r\n]+)\r?\n(?<weeks>[0-9,\-]+)\[周\]\r?\n(?<room>[^\r\n]+)\r?\n\[(?<period>[0-9\-]+)\]节/mg;

export function parseCsv(input: string, sourceFile = 'courses.csv'): Course[] {
  const rows = splitCsvRows(input).slice(1);
  return rows.flatMap((fields) => {
    if (fields.length < 6) return [];
    const day = parseDay(fields[0]);
    const period = normalizePeriod(fields[5]);
    const weeks = parseWeeks(fields[3]);
    if (!day || !period || !fields[1].trim() || !weeks.length) return [];
    return [{
      day,
      name: clean(fields[1]),
      teacher: clean(fields[2]),
      weeks,
      room: clean(fields[4]),
      period,
      sourceFile,
      selected: true,
    }];
  });
}

export function parseWorkbook(bytes: Uint8Array | ArrayBuffer, sourceFile = 'courses.xlsx'): Course[] {
  const workbook = XLSX.read(bytes, { type: 'array', cellText: true, cellDates: false });
  const output: Course[] = [];
  for (const sheetName of workbook.SheetNames) {
    const sheet = workbook.Sheets[sheetName];
    const rows = XLSX.utils.sheet_to_json<unknown[]>(sheet, { header: 1, raw: false, defval: '' });
    const templateCourses = parseTemplateRows(rows, sourceFile);
    if (templateCourses !== null) {
      output.push(...templateCourses);
      continue;
    }
    for (let row = 0; row < rows.length; row += 1) {
      const values = rows[row] ?? [];
      for (let column = 0; column < 7; column += 1) {
        const value = String(values[column] ?? '');
        output.push(...parseCourseCell(value, column + 1, sourceFile));
      }
    }
  }
  if (!output.length) throw new Error(`未在 Excel 工作表中识别到课程记录。\n文件：${sourceFile}`);
  return output;
}

export function parseFile(bytes: Uint8Array, fileName: string): Course[] {
  const extension = fileName.split('.').pop()?.toLowerCase();
  if (extension === 'csv') return parseCsv(new TextDecoder('utf-8').decode(bytes), fileName);
  if (extension === 'xls' || extension === 'xlsx') return parseWorkbook(bytes, fileName);
  throw new Error(`暂不支持该文件格式，请选择 .xls、.xlsx 或 .csv。\n文件：${fileName}`);
}

function parseTemplateRows(rows: unknown[][], sourceFile: string): Course[] | null {
  const header = rows.findIndex((row) => {
    const names = row.map((value) => normalizeHeader(String(value ?? '')));
    return names.includes('星期') && names.includes('课程名称') && names.includes('周次') && names.includes('节次');
  });
  if (header < 0) return null;
  const headers = rows[header].map((value) => normalizeHeader(String(value ?? '')));
  const col = (names: string[]) => headers.findIndex((headerName) => names.includes(headerName));
  const dayColumn = col(['星期', '周几', 'day']);
  const nameColumn = col(['课程名称', '课程', 'name']);
  const teacherColumn = col(['教师', '任课教师', 'teacher']);
  const weeksColumn = col(['周次', 'weeks', 'week']);
  const roomColumn = col(['教室', '地点', 'room']);
  const periodColumn = col(['节次', '上课节次', 'period']);
  return rows.slice(header + 1).flatMap((row) => {
    const name = clean(String(row[nameColumn] ?? ''));
    if (!name) return [];
    const day = parseDay(String(row[dayColumn] ?? ''));
    const period = normalizePeriod(String(row[periodColumn] ?? ''));
    const weeks = parseWeeks(String(row[weeksColumn] ?? ''));
    if (!day || !period || !weeks.length) return [];
    return [{
      day,
      name,
      teacher: clean(String(row[teacherColumn] ?? '')),
      weeks,
      room: clean(String(row[roomColumn] ?? '')),
      period,
      sourceFile,
      selected: true,
    }];
  });
}

function parseCourseCell(value: string, day: number, sourceFile: string): Course[] {
  if (!value.trim()) return [];
  return [...value.replaceAll('\r', '').matchAll(courseBlock)].flatMap((match) => {
    if (!match.groups) return [];
    const weeks = parseWeeks(match.groups.weeks);
    return weeks.length ? [{
      name: clean(match.groups.name),
      teacher: clean(match.groups.teacher),
      weeks,
      room: clean(match.groups.room),
      period: normalizePeriod(match.groups.period),
      day,
      sourceFile,
      selected: true,
    }] : [];
  });
}

function splitCsvRows(input: string): string[][] {
  const rows: string[][] = [];
  let row: string[] = [];
  let value = '';
  let quoted = false;
  for (let i = 0; i < input.length; i += 1) {
    const char = input[i];
    if (char === '"') {
      if (quoted && input[i + 1] === '"') {
        value += '"';
        i += 1;
      } else {
        quoted = !quoted;
      }
    } else if (char === ',' && !quoted) {
      row.push(value);
      value = '';
    } else if ((char === '\n' || char === '\r') && !quoted) {
      if (char === '\r' && input[i + 1] === '\n') i += 1;
      row.push(value);
      if (row.some((field) => field.length > 0)) rows.push(row);
      row = [];
      value = '';
    } else {
      value += char;
    }
  }
  if (value.length || row.length) {
    row.push(value);
    rows.push(row);
  }
  return rows;
}

export function clean(value: string): string {
  return value.replaceAll('\u3000', ' ').trim();
}

export function parseWeeks(value: string): number[] {
  const normalized = value.replaceAll('周', '').replaceAll('星期', '').replace('，', ',').replace('、', ',').replaceAll(' ', '');
  const weeks = new Set<number>();
  for (const piece of normalized.split(',')) {
    if (piece.includes('-')) {
      const [left, right] = piece.split('-').map(Number);
      if (Number.isFinite(left) && Number.isFinite(right)) {
        for (let week = Math.min(left, right); week <= Math.max(left, right); week += 1) weeks.add(week);
      }
    } else if (/^\d+$/.test(piece)) {
      weeks.add(Number(piece));
    }
  }
  return [...weeks].sort((a, b) => a - b);
}

export function normalizePeriod(value: string): string {
  const matches = value.match(/\d+/g) ?? [];
  if (!matches.length) return '';
  const first = Number(matches[0]);
  const last = Number(matches[matches.length - 1]);
  return matches.length === 1 ? String(first).padStart(2, '0') : `${String(first).padStart(2, '0')}-${String(last).padStart(2, '0')}`;
}

export function parseDay(value: string): number {
  const text = value.trim().replace('星期', '').replace('周', '');
  const number = Number(text);
  if (Number.isInteger(number) && number >= 1 && number <= 7) return number;
  return ['一', '二', '三', '四', '五', '六', '日'].indexOf(text) + 1;
}

function normalizeHeader(value: string): string {
  return value.trim().replaceAll(' ', '').replaceAll('\t', '').toLowerCase();
}
