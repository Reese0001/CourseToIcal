import { Badge, Empty, Typography } from 'antd';
import type { Course, ScheduleConfig } from '../../../src/core-ts/types';
import { expandCourses } from '../../../src/core-ts/scheduler';

const dayNames = ['周一', '周二', '周三', '周四', '周五', '周六', '周日'];
const colors = ['#1677ff', '#13c2c2', '#52c41a', '#fa8c16', '#722ed1', '#eb2f96'];

interface Props { courses: Course[]; schedule: ScheduleConfig; }

export default function WeekCalendar({ courses, schedule }: Props) {
  const occurrences = expandCourses(courses, schedule, schedule.currentWeek);
  const byCell = new Map<string, typeof occurrences>();
  for (const occurrence of occurrences) {
    const key = `${occurrence.course.day}-${occurrence.firstPeriod}`;
    const items = byCell.get(key) ?? [];
    items.push(occurrence);
    byCell.set(key, items);
  }
  return (
    <div className="calendar-panel">
      <div className="calendar-title-row">
        <div>
          <Typography.Title level={4}>周视图日历</Typography.Title>
          <Typography.Text type="secondary">{schedule.name} · 当前第 {schedule.currentWeek} 周</Typography.Text>
        </div>
        <Badge status={courses.length ? 'success' : 'default'} text={courses.length ? `${occurrences.length} 个日程` : '等待导入'} />
      </div>
      <div className="calendar-grid" role="grid" aria-label="课程周视图">
        <div className="calendar-corner" />
        {dayNames.map((day, index) => <div className="calendar-day-header" key={day}><strong>{day}</strong><span>{formatHeaderDate(schedule, index)}</span></div>)}
        {schedule.periods.map((period) => (
          <div className="calendar-row" key={period.number}>
            <div className="period-label"><strong>{period.number}</strong><span>{period.start}</span></div>
            {dayNames.map((_, dayIndex) => {
              const items = byCell.get(`${dayIndex + 1}-${period.number}`) ?? [];
              return <div className="calendar-cell" key={dayIndex} role="gridcell">{items.map((item) => <div className="course-block" style={{ backgroundColor: colors[Math.abs(hash(item.course.name)) % colors.length] }} key={`${item.course.name}-${item.week}`}><strong>{item.course.name}</strong><span>{item.course.room || '无教室'}</span></div>)}</div>;
            })}
          </div>
        ))}
      </div>
      {courses.length === 0 && <div className="calendar-empty"><Empty description="导入课程表后，课程会显示在这里" /></div>}
    </div>
  );
}

function formatHeaderDate(schedule: ScheduleConfig, day: number): string {
  const start = new Date(`${schedule.semesterStart}T00:00:00`);
  start.setDate(start.getDate() - (schedule.weekStart - 1) + (schedule.currentWeek - 1) * 7 + day);
  return `${start.getMonth() + 1}/${start.getDate()}`;
}

function hash(value: string): number {
  let result = 0;
  for (let i = 0; i < value.length; i += 1) result = (result * 31 + value.charCodeAt(i)) | 0;
  return result;
}
