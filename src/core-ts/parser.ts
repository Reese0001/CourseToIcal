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

export function parseOcrText(input: string, sourceFile = 'course-screenshot.png'): Course[] {
  const lines: OcrLine[] = [];
  let currentDay = 0;
  for (const rawLine of input.replaceAll('\r', '').split('\n')) {
    const line = cleanOcrLine(rawLine);
    if (!line) {
      lines.push({ text: '', day: currentDay });
      continue;
    }
    const heading = matchOcrDay(line);
    if (heading) {
      currentDay = heading.day;
      if (heading.rest) lines.push({ text: heading.rest, day: currentDay });
      continue;
    }
    lines.push({ text: line, day: currentDay });
  }

  const output: Course[] = [];
  const seen = new Set<string>();
  for (let index = 0; index < lines.length; index += 1) {
    const weekLine = lines[index];
    if (!weekLine.day || !isOcrWeekLine(weekLine.text)) continue;
    const periodIndex = findOcrPeriod(lines, index + 1);
    if (periodIndex < 0) continue;
    const teacher = previousOcrValue(lines, index - 1, weekLine.day);
    const name = previousOcrValue(lines, index - 2, weekLine.day);
    const room = previousOcrValue(lines, periodIndex - 1, weekLine.day, index + 1);
    const weeks = parseWeeks(weekLine.text);
    const period = normalizePeriod(lines[periodIndex].text);
    if (!name || !weeks.length || !period) continue;
    const key = `${weekLine.day}|${name}|${period}|${weeks.join(',')}`;
    if (seen.has(key)) continue;
    seen.add(key);
    output.push({ name, teacher, room, weeks, period, day: weekLine.day, sourceFile, selected: true });
  }
  return output;
}

export function parseOcrTsv(input: string, sourceFile = 'course-screenshot.png'): Course[] {
  const lines = parseOcrTsvLines(input);
  const headings = lines
    .map((line) => ({ ...line, day: parseOcrHeaderDay(line.text) }))
    .filter((line): line is OcrLayoutLine & { day: number } => Boolean(line.day))
    .sort((left, right) => left.centerX - right.centerX);
  if (headings.length < 2) return parseOcrText(lines.map((line) => line.text).join('\n'), sourceFile);

  const uniqueHeadings = headings.filter((heading, index) => index === 0 || heading.day !== headings[index - 1].day);
  const output: Course[] = [];
  for (let index = 0; index < uniqueHeadings.length; index += 1) {
    const heading = uniqueHeadings[index];
    const left = index === 0 ? -Infinity : (uniqueHeadings[index - 1].centerX + heading.centerX) / 2;
    const right = index === uniqueHeadings.length - 1 ? Infinity : (heading.centerX + uniqueHeadings[index + 1].centerX) / 2;
    const columnLines = lines
      .filter((line) => line.top > heading.bottom && line.centerX > left && line.centerX <= right)
      .sort((first, second) => first.top - second.top || first.left - second.left);
    output.push(...parseOcrText([`周${heading.day}`, ...columnLines.map((line) => line.text)].join('\n'), sourceFile));
  }
  return dedupeCourses(output);
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
  const normalized = value.replaceAll('周', '').replaceAll('星期', '').replaceAll('第', '').replaceAll('[', '').replaceAll(']', '').replaceAll('(', '').replaceAll(')', '').replace('，', ',').replace('、', ',').replaceAll(' ', '');
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
  const text = value.trim().replace('星期', '').replace('周', '').replace('天', '日');
  const number = Number(text);
  if (Number.isInteger(number) && number >= 1 && number <= 7) return number;
  return ['一', '二', '三', '四', '五', '六', '日'].indexOf(text) + 1;
}

interface OcrLine { text: string; day: number; }

interface OcrLayoutLine {
  text: string;
  left: number;
  top: number;
  bottom: number;
  centerX: number;
}

function cleanOcrLine(value: string): string {
  return clean(value)
    .replaceAll('：', ':')
    .replaceAll('（', '(')
    .replaceAll('）', ')')
    .replaceAll('【', '[')
    .replaceAll('】', ']')
    .replaceAll('，', ',')
    .replaceAll('、', ',')
    .replaceAll('—', '-')
    .replaceAll('–', '-')
    .replaceAll('－', '-')
    .replaceAll('~', '-')
    .replace(/([\u4e00-\u9fff])\s+(?=[\u4e00-\u9fff])/g, '$1')
    .trim();
}

function matchOcrDay(value: string): { day: number; rest: string } | null {
  const match = value.match(/^\s*(?:星期|周)\s*([一二三四五六日天1-7])\s*(?::|,|-)?\s*(.*)$/);
  if (!match) return null;
  const day = parseDay(match[1]);
  return day ? { day, rest: clean(match[2]) } : null;
}

function parseOcrHeaderDay(value: string): number {
  const match = cleanOcrLine(value).replaceAll(' ', '').match(/^(?:星期|周)?([一二三四五六日天1-7])$/);
  return match ? parseDay(match[1]) : 0;
}

function parseOcrTsvLines(input: string): OcrLayoutLine[] {
  const grouped = new Map<string, { words: Array<{ text: string; left: number; top: number; width: number; height: number }>; }>();
  for (const rawLine of input.replaceAll('\r', '').split('\n').slice(1)) {
    const fields = rawLine.split('\t');
    if (fields.length < 12 || fields[0] !== '5') continue;
    const text = cleanOcrLine(fields.slice(11).join('\t'));
    if (!text) continue;
    const left = Number(fields[6]);
    const top = Number(fields[7]);
    const width = Number(fields[8]);
    const height = Number(fields[9]);
    if (![left, top, width, height].every(Number.isFinite)) continue;
    const key = fields.slice(1, 5).join(':');
    const line = grouped.get(key) ?? { words: [] };
    line.words.push({ text, left, top, width, height });
    grouped.set(key, line);
  }
  return [...grouped.values()].flatMap(({ words }) => {
    const sorted = words.sort((left, right) => left.left - right.left);
    const splitGap = 40;
    const chunks: typeof sorted[] = [];
    for (const word of sorted) {
      const current = chunks[chunks.length - 1];
      const previous = current?.[current.length - 1];
      if (!current || !previous || word.left - (previous.left + previous.width) > splitGap) chunks.push([word]);
      else current.push(word);
    }
    return chunks.map((chunk) => {
      const left = Math.min(...chunk.map((word) => word.left));
      const right = Math.max(...chunk.map((word) => word.left + word.width));
      const top = Math.min(...chunk.map((word) => word.top));
      const bottom = Math.max(...chunk.map((word) => word.top + word.height));
      return { text: chunk.map((word) => word.text).join(' '), left, top, bottom, centerX: (left + right) / 2 };
    });
  });
}

function isOcrWeekLine(value: string): boolean {
  return /^第?\s*\d{1,2}(?:\s*-\s*\d{1,2})?(?:\s*,\s*\d{1,2})*\s*(?:\[?\s*周(?:次)?\s*\]?|週(?:次)?)\s*$/i.test(value);
}

function isOcrPeriodLine(value: string): boolean {
  const normalized = value.replace(/[()[\]]/g, '').replaceAll(' ', '');
  return /^\d{1,2}(?:-\d{1,2})?(?:节(?:次)?|课时?)?$/i.test(normalized);
}

function findOcrPeriod(lines: OcrLine[], start: number): number {
  for (let index = start; index < Math.min(lines.length, start + 5); index += 1) {
    if (isOcrPeriodLine(lines[index].text)) return index;
  }
  return -1;
}

function previousOcrValue(lines: OcrLine[], start: number, day: number, lowerBound = 0): string {
  for (let index = start; index >= lowerBound; index -= 1) {
    if (lines[index].day === day && lines[index].text) return cleanOcrField(lines[index].text);
  }
  return '';
}

function cleanOcrField(value: string): string {
  return clean(value).replace(/^(?:课程名称|课程|教师|任课教师|教室|地点)\s*[:：]\s*/i, '');
}

function dedupeCourses(courses: Course[]): Course[] {
  const seen = new Set<string>();
  return courses.filter((course) => {
    const key = `${course.day}|${course.name}|${course.period}|${course.weeks.join(',')}`;
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}

function normalizeHeader(value: string): string {
  return value.trim().replaceAll(' ', '').replaceAll('\t', '').toLowerCase();
}
