# CourseToIcal React 重构设计

## 目标

将 CourseToIcal 重构为真正的 React + Ant Design Windows 桌面应用。首屏进入周视图日历，用户可以导入 `.xls`、`.xlsx`、`.csv` 课程表，下载并编辑 `.xlsx` 模板，再导入模板，筛选课程并导出 `.ics`。

## 架构

- `frontend/` 使用 Vite、React、TypeScript 和 Ant Design，负责所有页面和交互。
- `electron/` 使用 Electron 主进程和 preload bridge，提供本地文件读取、保存、模板写入和窗口生命周期能力。
- `src/core-ts/` 使用 TypeScript 实现课程模型、XLS/XLSX/CSV 解析、模板生成、周次排课和 iCalendar 导出。
- 原 `src/CourseToIcal.Core` 和 WinForms App 在迁移期间保留，作为旧版可执行文件和行为参考；新的默认启动入口是 Electron。

## 数据流

1. React 请求打开文件对话框。
2. Electron 主进程返回用户选中的路径和二进制内容。
3. `src/core-ts` 用 SheetJS 读取工作簿，优先识别模板表头，否则识别教务系统课程块。
4. React 保存 `Course[]`，由周视图和侧边勾选列表共同消费。
5. 导出时 TypeScript 排课引擎生成 occurrences，iCalendar writer 写入 UTF-8 `.ics`，由 Electron 保存到用户选择的路径。

## 交互

- 首屏为日历工作台，不经过导入向导。
- 顶部主操作：导入课表、下载模板、课表设置、导出日历。
- 左侧显示课程筛选和导入来源；右侧显示 7 天 × 11 节周视图。
- 周次选择支持上一周、下一周、本周和数字输入。
- 未导入时显示空状态和明确的导入/模板入口；解析失败显示文件级错误，不影响其他文件。
- 设置保存在浏览器 localStorage，包含学期名称、第一周起始日期、周起始日、学期周数、当前周和 11 节课时间。

## 文件格式

- `.xlsx` 模板包含 `课程数据` 和 `使用说明` 两个工作表。
- `课程数据` 字段为 `星期`、`课程名称`、`教师`、`周次`、`教室`、`节次`、`备注`。
- 周次支持 `1-16`、`1,3,5`；节次支持 `01-02` 或 `5-6`；星期支持 `1-7` 或 `周一-周日`。
- 常见教务课表继续识别 `课程名 / 教师 / 周次[周] / 教室 / [节次]节` 课程块。

## 构建与发布

- `npm run dev` 启动 Vite 和 Electron 开发窗口。
- `npm run test` 执行 Vitest 核心测试。
- `npm run build` 构建 React renderer 和 Electron 主进程可运行文件。
- `npm run dist` 使用 electron-builder 生成 Windows 免安装目录和安装包。
- 旧版 `build.ps1` 继续支持原 WinForms fallback 构建，直到 React 版 Windows 发布验证完成。

## 质量要求

- 核心解析、模板回读、周次日期、11 节时间范围和稳定 UID 必须有自动化测试。
- React 构建必须无 TypeScript 错误。
- Electron 开发窗口必须能启动，生产 renderer 必须能加载。
- 不把用户课程数据发送到网络；文件只在本地读写。
