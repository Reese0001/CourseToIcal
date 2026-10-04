import { CalendarOutlined, ClearOutlined, DownloadOutlined, ExportOutlined, FileExcelOutlined, FileImageOutlined, SettingOutlined } from '@ant-design/icons';
import { Button, InputNumber, Space, Tooltip, Typography } from 'antd';
import type { ScheduleConfig } from '../../../src/core-ts/types';

interface Props {
  schedule: ScheduleConfig;
  onImport: () => void;
  onImageImport: () => void;
  ocrBusy: boolean;
  onTemplate: () => void;
  onSettings: () => void;
  onClear: () => void;
  onExport: () => void;
  onWeekChange: (week: number) => void;
}

export default function ImportToolbar({ schedule, onImport, onImageImport, ocrBusy, onTemplate, onSettings, onClear, onExport, onWeekChange }: Props) {
  return (
    <div className="toolbar-shell">
      <Space size={8} wrap>
        <Button type="primary" icon={<FileExcelOutlined />} onClick={onImport}>导入课表</Button>
        <Button icon={<FileImageOutlined />} loading={ocrBusy} onClick={onImageImport}>识别图片</Button>
        <Button icon={<DownloadOutlined />} onClick={onTemplate}>下载模板</Button>
        <Button icon={<SettingOutlined />} onClick={onSettings}>课表设置</Button>
        <Button icon={<ClearOutlined />} onClick={onClear}>清空</Button>
        <Button icon={<ExportOutlined />} onClick={onExport}>导出日历</Button>
      </Space>
      <Space className="week-controls" size={8}>
        <Tooltip title="上一周"><Button aria-label="上一周" onClick={() => onWeekChange(Math.max(1, schedule.currentWeek - 1))}>‹</Button></Tooltip>
        <Button onClick={() => onWeekChange(schedule.currentWeek)}>本周</Button>
        <InputNumber aria-label="当前周" min={1} max={schedule.semesterWeeks} value={schedule.currentWeek} onChange={(value) => value && onWeekChange(value)} />
        <Typography.Text strong>第 {schedule.currentWeek} 周</Typography.Text>
        <Tooltip title="下一周"><Button aria-label="下一周" onClick={() => onWeekChange(Math.min(schedule.semesterWeeks, schedule.currentWeek + 1))}>›</Button></Tooltip>
        <CalendarOutlined className="toolbar-calendar-icon" />
      </Space>
    </div>
  );
}
