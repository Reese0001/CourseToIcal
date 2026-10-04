# CourseToIcal

CourseToIcal 是一个 Windows 桌面工具，用来批量导入教务系统课程表、在周视图日历中预览并勾选课程，然后导出一个可导入系统日历的 iCalendar（`.ics`）文件。

## 功能

- 批量导入 `.xls`、`.xlsx` 和 CSV 课程表。
- `.xls` 使用 BIFF 单元格坐标解析，`.xlsx` 使用 Office Open XML ZIP/XML 结构解析，不需要安装 Excel。
- 日历预览支持切换学期周、勾选课程、全选和取消全选。
- 课表设置支持课表名称、第一周起始日期、每周起始日、学期周数、当前周和第 1 至第 11 节课的独立时间。
- 导出稳定 UID 的 iCalendar，重复导出不会因为 UID 变化产生重复事件。
- 支持命令行直接转换，适合拖放或脚本调用。

## 目录结构

```text
src/CourseToIcal.Core/       模型、解析器、排课计算和 iCalendar 导出
src/CourseToIcal.App/        WinForms 界面和程序入口
tests/CourseToIcal.Tests/     可执行功能测试和最小 XLSX 夹具
samples/                      示例 CSV
docs/superpowers/             设计文档和实施计划
publish/                      本地发布输出，不提交到 Git
```

## 运行

直接运行 `publish/CourseToIcal.exe`，选择一个或多个课程表文件，打开日历预览，勾选需要导出的课程，再保存 `.ics` 文件。

也可以直接使用命令行：

```powershell
CourseToIcal.exe input.xlsx output.ics 2026-08-31
```

第三个参数是第一周起始日期，可省略；省略时使用保存的课表设置。配置文件保存在 `%APPDATA%\CourseToIcal\schedule.xml`。

## 构建和测试

在仓库根目录运行：

```powershell
.\build.ps1
```

有 .NET SDK 时，脚本会还原解决方案、构建 Release、运行测试并生成 `publish/` 下的 win-x64 自包含单文件程序。当前机器没有 .NET SDK 时，脚本会使用已安装的 .NET Framework C# 编译器完成本地 Core、测试和 WinForms 回退构建。

测试覆盖配置持久化、11 节课时间、周次日期计算、跨节次时间范围、稳定 UID、带引号逗号的 CSV 字段和真实 ZIP/XML `.xlsx` 解析。

## 输入格式说明

解析器识别教务系统常见的课程块：课程名、教师、周次（例如 `1-16[周]`）、教室和节次（例如 `[01-02]节`）。对于 Excel 文件，课程块需要位于星期一至星期日对应的前 7 列中；不同学校导出的布局可能需要单独适配。无法识别到课程时，程序会给出明确提示。

## GitHub Actions

`.github/workflows/build.yml` 在 Windows runner 上使用 .NET 8，执行还原、构建、功能测试和自包含发布，并上传 `CourseToIcal-win-x64` artifact。

## 许可证

本项目使用 MIT License，详见 [LICENSE](LICENSE)。
