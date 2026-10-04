import { useEffect, useMemo, useRef, useState } from 'react';
import { App as AntApp, Layout, message, Typography } from 'antd';
import { CalendarOutlined } from '@ant-design/icons';
import ImportToolbar from './components/ImportToolbar';
import CourseSidebar from './components/CourseSidebar';
import WeekCalendar from './components/WeekCalendar';
import ScheduleSettingsModal from './components/ScheduleSettingsModal';
import type { Course, ScheduleConfig } from '../../src/core-ts/types';
import { buildIcal } from '../../src/core-ts/ical';
import { createDefaultSchedule } from '../../src/core-ts/scheduler';
import { parseFile, parseOcrText, parseOcrTsv } from '../../src/core-ts/parser';
import { writeTemplate } from '../../src/core-ts/template';
import type { OpenedCourseFile, RecognizedImageFile } from './electron';

const settingsKey = 'coursetoical.schedule.v2';

export default function App() {
  const [courses, setCourses] = useState<Course[]>([]);
  const [sourceFiles, setSourceFiles] = useState<string[]>([]);
  const [schedule, setSchedule] = useState<ScheduleConfig>(() => loadSchedule());
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [ocrBusy, setOcrBusy] = useState(false);
  const fileInput = useRef<HTMLInputElement>(null);
  const selectedCount = useMemo(() => courses.filter((course) => course.selected).length, [courses]);

  useEffect(() => { localStorage.setItem(settingsKey, JSON.stringify(schedule)); }, [schedule]);

  async function importCourses() {
    if (window.courseToIcal) {
      const files = await window.courseToIcal.openFiles();
      await consumeFiles(files);
    } else {
      fileInput.current?.click();
    }
  }

  async function importImages() {
    if (!window.courseToIcal) {
      message.info('图片识别需要运行 Electron 桌面版，浏览器预览模式只支持表格文件导入');
      return;
    }
    setOcrBusy(true);
    try {
      const files = await window.courseToIcal.openImageFiles();
      await consumeRecognizedImages(files);
    } catch (error) {
      message.error(error instanceof Error ? error.message : '图片识别失败');
    } finally {
      setOcrBusy(false);
    }
  }

  async function consumeFiles(files: OpenedCourseFile[]) {
    for (const file of files) {
      try {
        const coursesFromFile = parseFile(decodeBase64(file.content), file.name);
        setCourses((current) => [...current, ...coursesFromFile]);
        setSourceFiles((current) => [...new Set([...current, file.name])]);
        message.success(`${file.name} 已导入 ${coursesFromFile.length} 门课程`);
      } catch (error) {
        message.error(error instanceof Error ? error.message : `${file.name} 导入失败`);
      }
    }
  }

  async function handleBrowserFiles(event: React.ChangeEvent<HTMLInputElement>) {
    const files = [...(event.target.files ?? [])];
    await consumeFiles(await Promise.all(files.map(async (file) => ({ path: file.name, name: file.name, content: encodeBase64(new Uint8Array(await file.arrayBuffer())) }))));
    event.target.value = '';
  }

  async function consumeRecognizedImages(files: RecognizedImageFile[]) {
    for (const file of files) {
      const coursesFromFile = file.tsv ? parseOcrTsv(file.tsv, file.name) : parseOcrText(file.text, file.name);
      if (!coursesFromFile.length) {
        message.warning(`${file.name} 未识别到完整课程，请使用清晰的课表截图`);
        continue;
      }
      setCourses((current) => [...current, ...coursesFromFile]);
      setSourceFiles((current) => [...new Set([...current, file.name])]);
      message.success(`${file.name} 已识别 ${coursesFromFile.length} 门课程`);
    }
  }

  async function downloadTemplate() {
    const bytes = writeTemplate();
    if (window.courseToIcal) {
      const path = await window.courseToIcal.saveBinary({ suggestedName: '课程表导入模板.xlsx', content: encodeBase64(bytes) });
      if (path) message.success('模板已保存，请编辑后重新导入');
    } else {
      downloadBlob(bytes, '课程表导入模板.xlsx', 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet');
      message.success('模板已下载，请编辑后重新导入');
    }
  }

  async function exportCalendar() {
    if (!selectedCount) { message.warning('请至少选择一门课程'); return; }
    const content = buildIcal(courses, schedule);
    if (window.courseToIcal) {
      const path = await window.courseToIcal.saveText({ suggestedName: `${schedule.name}.ics`, content });
      if (path) message.success('日历已导出');
    } else {
      downloadBlob(new TextEncoder().encode(content), `${schedule.name}.ics`, 'text/calendar;charset=utf-8');
      message.success('日历已下载');
    }
  }

  function toggleCourse(index: number, selected: boolean) { setCourses((current) => current.map((course, courseIndex) => courseIndex === index ? { ...course, selected } : course)); }
  function setAll(selected: boolean) { setCourses((current) => current.map((course) => ({ ...course, selected }))); }
  function clearCourses() { setCourses([]); setSourceFiles([]); message.info('已清空课程'); }
  function saveSettings(next: ScheduleConfig) { setSchedule(next); setSettingsOpen(false); message.success('课表设置已保存'); }

  return (
    <AntApp>
      <Layout className="app-layout">
        <header className="app-header"><div className="brand-mark"><CalendarOutlined /><span>CourseToIcal</span></div><Typography.Text className="header-title">课程表日历</Typography.Text><Typography.Text type="secondary">本地工作台</Typography.Text></header>
        <ImportToolbar schedule={schedule} onImport={importCourses} onImageImport={importImages} ocrBusy={ocrBusy} onTemplate={downloadTemplate} onSettings={() => setSettingsOpen(true)} onClear={clearCourses} onExport={exportCalendar} onWeekChange={(currentWeek) => setSchedule((current) => ({ ...current, currentWeek }))} />
        <main className="workspace"><CourseSidebar courses={courses} sourceFiles={sourceFiles} onToggle={toggleCourse} onSetAll={setAll} /><WeekCalendar courses={courses} schedule={schedule} /></main>
        <input ref={fileInput} className="visually-hidden" type="file" accept=".xls,.xlsx,.csv" multiple onChange={handleBrowserFiles} />
        <ScheduleSettingsModal open={settingsOpen} schedule={schedule} onCancel={() => setSettingsOpen(false)} onSave={saveSettings} />
      </Layout>
    </AntApp>
  );
}

function loadSchedule(): ScheduleConfig {
  try { const value = localStorage.getItem(settingsKey); if (value) return { ...createDefaultSchedule(), ...JSON.parse(value) }; } catch { /* use defaults */ }
  return createDefaultSchedule();
}

function decodeBase64(value: string): Uint8Array { const binary = atob(value); return Uint8Array.from(binary, (char) => char.charCodeAt(0)); }
function encodeBase64(value: Uint8Array): string { let binary = ''; for (let i = 0; i < value.length; i += 1) binary += String.fromCharCode(value[i]); return btoa(binary); }
function downloadBlob(bytes: Uint8Array, name: string, type: string) { const copy = new Uint8Array(bytes); const url = URL.createObjectURL(new Blob([copy.buffer], { type })); const anchor = document.createElement('a'); anchor.href = url; anchor.download = name; anchor.click(); URL.revokeObjectURL(url); }
