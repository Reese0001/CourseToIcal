import { CheckOutlined, InboxOutlined } from '@ant-design/icons';
import { Button, Checkbox, Empty, List, Space, Tag, Typography } from 'antd';
import type { Course } from '../../../src/core-ts/types';

interface Props {
  courses: Course[];
  sourceFiles: string[];
  onToggle: (index: number, selected: boolean) => void;
  onSetAll: (selected: boolean) => void;
}

export default function CourseSidebar({ courses, sourceFiles, onToggle, onSetAll }: Props) {
  const selectedCount = courses.filter((course) => course.selected).length;
  return (
    <aside className="sidebar-panel">
      <div className="sidebar-heading">
        <div>
          <Typography.Title level={5}>课程筛选</Typography.Title>
          <Typography.Text type="secondary">{selectedCount} / {courses.length} 门已选</Typography.Text>
        </div>
        <Tag color={courses.length ? 'blue' : 'default'}>{courses.length} 门</Tag>
      </div>
      <Space className="selection-actions" size={8}>
        <Button size="small" icon={<CheckOutlined />} onClick={() => onSetAll(true)}>全选</Button>
        <Button size="small" onClick={() => onSetAll(false)}>清除选择</Button>
      </Space>
      <div className="course-list-wrap">
        {courses.length === 0 ? (
          <Empty image={<InboxOutlined className="empty-icon" />} imageStyle={{ height: 48 }} description="请先导入课表或下载模板" />
        ) : (
          <List
            size="small"
            dataSource={courses}
            renderItem={(course, index) => (
              <List.Item className="course-list-item">
                <Checkbox checked={course.selected} onChange={(event) => onToggle(index, event.target.checked)}>
                  <span className="course-item-name">{course.name}</span>
                  <span className="course-item-meta">周{course.day} · {course.period}节 · {course.room || '无教室'}</span>
                </Checkbox>
              </List.Item>
            )}
          />
        )}
      </div>
      <div className="source-box">
        <Typography.Text strong>导入来源 {sourceFiles.length ? `· ${sourceFiles.length}` : ''}</Typography.Text>
        {sourceFiles.length ? sourceFiles.map((file) => <Typography.Text key={file} type="secondary" ellipsis={{ tooltip: file }}>{file}</Typography.Text>) : <Typography.Text type="secondary">尚未导入文件</Typography.Text>}
      </div>
    </aside>
  );
}
