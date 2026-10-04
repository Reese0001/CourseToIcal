import { useEffect, useState } from 'react';
import { Form, Input, InputNumber, Modal, Select, Space, Table, Typography } from 'antd';
import type { PeriodSetting, ScheduleConfig } from '../../../src/core-ts/types';

interface Props { open: boolean; schedule: ScheduleConfig; onCancel: () => void; onSave: (schedule: ScheduleConfig) => void; }

export default function ScheduleSettingsModal({ open, schedule, onCancel, onSave }: Props) {
  const [draft, setDraft] = useState<ScheduleConfig>(schedule);
  useEffect(() => { if (open) setDraft(structuredClone(schedule)); }, [open, schedule]);
  const update = (patch: Partial<ScheduleConfig>) => setDraft((current) => ({ ...current, ...patch }));
  const updatePeriod = (index: number, patch: Partial<PeriodSetting>) => setDraft((current) => ({ ...current, periods: current.periods.map((period, periodIndex) => periodIndex === index ? { ...period, ...patch } : period) }));
  return (
    <Modal title="课表设置" open={open} width={760} okText="保存设置" cancelText="取消" onCancel={onCancel} onOk={() => onSave({ ...draft, currentWeek: Math.min(draft.currentWeek, draft.semesterWeeks) })}>
      <Form layout="vertical" className="settings-form">
        <Space size={16} align="start" wrap>
          <Form.Item label="课表名称"><Input value={draft.name} onChange={(event) => update({ name: event.target.value })} /></Form.Item>
          <Form.Item label="第一周起始日期"><Input type="date" value={draft.semesterStart} onChange={(event) => update({ semesterStart: event.target.value })} /></Form.Item>
          <Form.Item label="每周起始日"><Select value={draft.weekStart} onChange={(value) => update({ weekStart: value })} options={[1, 2, 3, 4, 5, 6, 7].map((value) => ({ value, label: `周${['一', '二', '三', '四', '五', '六', '日'][value - 1]}` }))} /></Form.Item>
          <Form.Item label="学期周数"><InputNumber min={1} max={60} value={draft.semesterWeeks} onChange={(value) => value && update({ semesterWeeks: value })} /></Form.Item>
          <Form.Item label="当前周"><InputNumber min={1} max={60} value={draft.currentWeek} onChange={(value) => value && update({ currentWeek: value })} /></Form.Item>
        </Space>
        <Typography.Title level={5}>上课时间</Typography.Title>
        <Table<PeriodSetting> size="small" pagination={false} rowKey="number" dataSource={draft.periods} columns={[{ title: '节次', dataIndex: 'number', width: 80 }, { title: '开始', render: (_, period, index) => <Input aria-label={`第${period.number}节开始`} value={period.start} onChange={(event) => updatePeriod(index, { start: event.target.value })} /> }, { title: '结束', render: (_, period, index) => <Input aria-label={`第${period.number}节结束`} value={period.end} onChange={(event) => updatePeriod(index, { end: event.target.value })} /> }]} />
      </Form>
    </Modal>
  );
}
