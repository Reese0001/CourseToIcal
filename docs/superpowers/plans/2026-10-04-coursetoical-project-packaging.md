# CourseToIcal Project Packaging And XLSX Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Convert the existing single-file timetable converter into a folder-structured GitHub project with verified `.xlsx` support, reproducible builds, tests, documentation, and a runnable Windows release output.

**Architecture:** Keep timetable models, parsing, scheduling, and iCalendar export in a platform-neutral Core project. Keep WinForms windows and command-line startup in an App project. Keep a small executable test harness in a Tests project so the local legacy compiler can validate the core even when the .NET SDK is unavailable.

**Tech Stack:** C#/.NET 8, WinForms, `System.IO.Compression.ZipArchive`, `System.Xml.Linq`, PowerShell, GitHub Actions on Windows.

**Spec:** `docs/superpowers/specs/2026-10-04-coursetoical-project-design.md`

## Global Constraints

- Support `.xls`, `.xlsx`, and CSV input without Excel COM automation.
- Preserve the current preview, settings, scheduling, and deterministic iCalendar behavior.
- `.xlsx` parsing must resolve worksheet relationships and shared strings from the Open XML package.
- Generated executables, temporary ICS files, user configuration, `bin/`, and `obj/` are ignored by Git.
- The primary target is Windows desktop; local fallback compilation uses the installed .NET Framework C# compiler when no SDK is installed.

---

### Task 1: Create the repository structure and Core project

**Files:**
- Create: `CourseToIcal.sln`
- Create: `src/CourseToIcal.Core/CourseToIcal.Core.csproj`
- Create: `src/CourseToIcal.Core/Models/Course.cs`
- Create: `src/CourseToIcal.Core/Models/ScheduleConfig.cs`
- Create: `src/CourseToIcal.Core/Scheduling/ScheduleEngine.cs`
- Create: `src/CourseToIcal.Core/Export/IcalWriter.cs`
- Create: `src/CourseToIcal.Core/Parsing/CourseParser.cs`
- Create: `src/CourseToIcal.Core/Parsing/CsvCourseParser.cs`
- Create: `src/CourseToIcal.Core/Parsing/BiffCourseParser.cs`
- Create: `src/CourseToIcal.Core/Parsing/OpenXmlCourseParser.cs`
- Delete: `CourseToIcal.cs`

**Interfaces:**
- `CourseParser.Parse(string path)` returns `List<Course>` and dispatches by extension.
- `ScheduleEngine.Expand(IEnumerable<Course>, ScheduleConfig, int? onlyWeek = null)` returns `List<CourseOccurrence>`.
- `IcalWriter.Write(string path, List<Course> courses, DateTime firstWeekStart, ScheduleConfig schedule = null)` writes one UTF-8 iCalendar file.

- [ ] **Step 1: Add Core project metadata and folders.**
  Target `net8.0`, disable nullable and implicit usings for compatibility, and reference no UI assemblies.
- [ ] **Step 2: Move the existing models and scheduling logic into focused files.**
  Preserve the 11 default periods, XML persistence path, week-start semantics, period-range validation, and stable SHA-1 UID inputs.
- [ ] **Step 3: Move the BIFF parser and add a quoted CSV parser.**
  Keep weekday assignment from BIFF cell coordinates and parse CSV fields with quotes and escaped quotes instead of `Split(',')`.
- [ ] **Step 4: Implement the Open XML parser.**
  Read `xl/workbook.xml`, `xl/_rels/workbook.xml.rels`, worksheet XML, and `xl/sharedStrings.xml` through `ZipArchive`; map cell references such as `C12` to column 3; parse course blocks from columns 1 through 7.
- [ ] **Step 5: Build the Core project or compile it with the legacy compiler.**
  Confirm the new files have no UI references and that `.xls`, CSV, and `.xlsx` dispatch paths compile.

### Task 2: Split the WinForms application into App files

**Files:**
- Create: `src/CourseToIcal.App/CourseToIcal.App.csproj`
- Create: `src/CourseToIcal.App/Program.cs`
- Create: `src/CourseToIcal.App/UI/ScheduleSettingsForm.cs`
- Create: `src/CourseToIcal.App/UI/CalendarWorkspaceForm.cs`
- Create: `src/CourseToIcal.App/UI/CalendarPreviewForm.cs`
- Create: `src/CourseToIcal.App/UI/CalendarCanvas.cs`

**Interfaces:**
- `Program.Main()` opens `CalendarWorkspaceForm` with no arguments and performs direct conversion when the first argument is a timetable path.
- `CalendarWorkspaceForm` imports one or more `.xls`, `.xlsx`, or `.csv` paths and passes courses to `CalendarPreviewForm`.
- `CalendarPreviewForm` updates `Course.Selected` and calls `IcalWriter.Write` for checked courses.

- [ ] **Step 1: Add the Windows Forms project reference to Core.**
  Target `net8.0-windows`, enable WinForms and Windows targeting, and set assembly name `CourseToIcal`.
- [ ] **Step 2: Extract the settings dialog and workspace form.**
  Update imports to `CourseToIcal.Core` and include `.xlsx` in all file filters and import status text.
- [ ] **Step 3: Extract the preview form and canvas.**
  Use `ScheduleEngine.Expand` for the preview and calculate the displayed week base using `WeekStart` so non-Monday configurations remain consistent.
- [ ] **Step 4: Add the application entry point.**
  Preserve direct CLI usage as `CourseToIcal.exe input.xlsx [output.ics] [first-week-date]`, loading the persisted schedule configuration.
- [ ] **Step 5: Compile the App project with Core.**
  Launch the produced executable in a smoke check and confirm the workspace window is responsive.

### Task 3: Add tests, fixtures, and reproducible build commands

**Files:**
- Create: `tests/CourseToIcal.Tests/CourseToIcal.Tests.csproj`
- Create: `tests/CourseToIcal.Tests/Program.cs`
- Create: `samples/sample.csv`
- Modify: `build.ps1`

**Interfaces:**
- The test executable exits 0 and prints `PASS` when all assertions pass.
- `build.ps1` builds Core, App, and Tests with the .NET SDK when available, and falls back to `csc.exe` on this machine.

- [ ] **Step 1: Add a generated minimal `.xlsx` fixture helper to the test program.**
  Create a ZIP with workbook XML, relationships, shared strings, and one worksheet whose column B contains a recognized course block; parse it through `CourseParser.Parse` and assert day 2, course name, teacher, room, period, and weeks.
- [ ] **Step 2: Port the existing schedule/config/iCalendar assertions.**
  Assert 11 periods, XML round-trip, first-week date mapping, 9-11 time range, stable UID, and expected iCalendar timestamps.
- [ ] **Step 3: Add quoted CSV coverage.**
  Write a temporary CSV row containing a quoted course name with a comma and assert the parser retains the comma.
- [ ] **Step 4: Implement the SDK/fallback build script.**
  SDK mode runs restore, Release build, tests, and self-contained single-file publish to `publish/`; fallback mode compiles the Core sources, App sources, and test sources with required .NET Framework references and runs the resulting test executable.
- [ ] **Step 5: Run the complete script and inspect generated outputs.**
  Confirm `PASS`, a runnable `publish/CourseToIcal.exe`, and no generated files are required in the source tree.

### Task 4: Add repository documentation and CI

**Files:**
- Create: `.gitignore`
- Create: `LICENSE`
- Create: `.github/workflows/build.yml`
- Modify: `README.md`

- [ ] **Step 1: Document folder layout and user workflows.**
  Explain source versus release output, GUI import/preview/export, direct CLI usage, supported formats, `.xlsx` implementation, configuration location, and known timetable-layout limitations.
- [ ] **Step 2: Add MIT licensing and Git ignore rules.**
  Ignore `bin/`, `obj/`, `publish/`, generated executables, generated ICS files, `coursetoical-error.txt`, and user settings.
- [ ] **Step 3: Add Windows GitHub Actions.**
  Check out code, set up .NET 8, restore/build, run the executable tests, publish a self-contained win-x64 single-file application, and upload the publish directory as an artifact.
- [ ] **Step 4: Run a documentation and placeholder scan.**
  Confirm README claims match parser behavior and there are no stale statements that `.xlsx` is rejected.

### Task 5: Initialize Git and publish the public GitHub repository

**Files:**
- Modify: Git metadata only (`.git/`)

- [ ] **Step 1: Review status and generated-file exclusions.**
  Ensure only source, tests, docs, samples, scripts, and workflow files are staged.
- [ ] **Step 2: Initialize the repository and create the first commit.**
  Use the configured Git identity and commit with `feat: package CourseToIcal with xlsx support`.
- [ ] **Step 3: Create `Reese0001/CourseToIcal` as a public GitHub repository and push `master`.**
  Use the authenticated GitHub CLI and set `origin` to the created repository.
- [ ] **Step 4: Verify the remote repository.**
  Check the remote URL, pushed branch, latest commit, and GitHub Actions workflow file.

## Verification Commands

```powershell
Set-Location D:\DevProject\CourseToIcal
.\build.ps1
git status --short
git log --oneline -1
gh repo view Reese0001/CourseToIcal
```

## Self-Review

- The spec's folder layout is covered by Tasks 1 through 4.
- `.xlsx` relationship, shared-string, inline-string, and cell-coordinate requirements are covered by Task 1 and the generated fixture in Task 3.
- Existing `.xls`, CSV, preview, configuration, scheduling, and iCalendar requirements are covered by Tasks 1 through 3.
- Release packaging, CI, Git metadata, and public repository creation are covered by Tasks 3 through 5.
- No step relies on an unspecified parser library or an unverified Excel installation.
