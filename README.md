# CourseToIcal

CourseToIcal 是一个 React + Ant Design + Electron Windows 桌面工具。打开后直接进入周视图日历，可以导入教务系统课程表或编辑后的 Excel 模板，筛选课程并导出可导入系统日历的 iCalendar（`.ics`）文件。

## 功能

- 批量导入 `.xls`、`.xlsx` 和 CSV 课程表。
- 使用 SheetJS 解析 `.xls`、`.xlsx` 和 CSV，不需要安装 Excel。
- 日历预览支持切换学期周、勾选课程、全选和取消全选。
- 课表设置支持课表名称、第一周起始日期、每周起始日、学期周数、当前周和第 1 至第 11 节课的独立时间。
- 导出稳定 UID 的 iCalendar，重复导出不会因为 UID 变化产生重复事件。
- 首屏直接进入周视图日历，导入、模板、设置和导出操作集中在顶部工具栏。
- 可下载可编辑的 `.xlsx` 课程表模板；在 Excel/WPS 的“课程数据”工作表中编辑后，再从日历页导入。
- Electron 提供本地文件选择和保存；在浏览器开发模式下自动使用文件选择和下载 fallback。

界面使用 Ant Design 组件和企业工作台布局：顶部操作栏、侧边课程筛选、白色日历面板、清晰的主次按钮和可见焦点状态。原 WinForms 项目仍保留在仓库中，作为迁移期间的 fallback 和行为参考；默认开发与发布入口是 Electron。

## 目录结构

```text
frontend/src/                 React 页面、Ant Design 组件和样式
src/core-ts/                  TypeScript 模型、解析、排课和 iCalendar 导出
electron/                     Electron 主进程和 preload 文件桥接
frontend/src/core.test.ts     Vitest 核心测试
src/CourseToIcal.Core/        原 WinForms 版核心（迁移期间保留）
src/CourseToIcal.App/         原 WinForms 界面和程序入口
tests/CourseToIcal.Tests/     原 WinForms 功能测试
samples/                      示例 CSV
docs/superpowers/             设计文档和实施计划
```

## 运行

先安装 Node.js 18 或更高版本，然后在仓库根目录运行：

```powershell
npm install
npm run dev
```

开发窗口打开后，点击“下载模板”，在 Excel/WPS 的“课程数据”工作表中编辑，再点击“导入课表”。模板字段为：`星期`（1-7）、`课程名称`、`教师`、`周次`（例如 `1-16` 或 `1,3,5`）、`教室`、`节次`（例如 `01-02`）和 `备注`。模板中的示例行可以删除或覆盖。

浏览器 fallback 可通过 `npm run build` 生成 `dist/`，打开 `dist/index.html` 后使用浏览器文件选择和下载功能；Electron 开发模式使用本地文件对话框。

## React / Electron 构建

```powershell
npm test -- --run
npm run typecheck
npm run build
npm run dist
```

`npm run dist` 使用 electron-builder 生成 Windows 免安装目录，输出到 `publish-react/win-unpacked/`，其中包含 `CourseToIcal.exe`。首次安装如果 Electron 二进制下载因网络失败，可以先用 `npm install --ignore-scripts` 完成测试和 React 构建，网络恢复后执行 `npm rebuild electron` 再运行 `npm run dist`。

## 构建和测试

原 WinForms fallback 仍可在仓库根目录运行：

```powershell
.\build.ps1
```

有 .NET SDK 时，脚本会还原解决方案、构建 Release、运行测试并生成 `publish/` 下的 win-x64 自包含单文件程序。当前机器没有 .NET SDK 时，脚本会使用已安装的 .NET Framework C# 编译器完成本地 Core、测试和 WinForms fallback 构建。

React 测试覆盖模板回读、带引号逗号的 CSV 字段、一个单元格内多个课程块、周次日期计算和稳定 UID。原 WinForms 测试继续覆盖配置持久化、11 节课时间、跨节次时间范围和真实 `.xlsx` 解析。

## 输入格式说明

TypeScript 解析器识别教务系统常见的课程块：课程名、教师、周次（例如 `1-16[周]`）、教室和节次（例如 `[01-02]节`），同一单元格可以包含多个连续课程块。对于 Excel 文件，课程块需要位于星期一至星期日对应的前 7 列中；不同学校导出的布局可能需要单独适配。无法识别到课程时，程序会给出明确提示。

## GitHub Actions

`.github/workflows/build.yml` 在 Windows runner 上先执行 Node.js 依赖安装、Vitest、TypeScript 检查和 React 构建，再使用 .NET 8 构建原 WinForms fallback，并上传两个构建 artifact。

## 许可证

本项目使用 MIT License，详见 [LICENSE](LICENSE)。
