# CourseToIcal Project Packaging And XLSX Support

## Goal

Turn the existing CourseToIcal WinForms converter into a maintainable GitHub project with separated source folders, reproducible builds, automated checks, and verified `.xlsx` timetable import while preserving the currently working `.xls`, CSV, preview, settings, and iCalendar workflows.

## Scope

The project remains a Windows desktop application. The main workflow is:

1. Select one or more timetable files.
2. Parse course records from `.xls`, `.xlsx`, or CSV input.
3. Show imported courses in a selectable weekly calendar preview.
4. Allow the user to edit semester and 11-period timetable settings.
5. Export selected courses to one iCalendar file with deterministic event UIDs.

The existing `.xls` BIFF parser remains supported. The `.xlsx` parser will read standard Office Open XML packages directly and will not require Excel to be installed. CSV input will remain supported and will use a small quoted-field parser so commas inside fields are preserved.

## Architecture

The repository is split into three projects:

- `src/CourseToIcal.Core`: platform-neutral models, timetable calculation, file parsers, and iCalendar writer.
- `src/CourseToIcal.App`: the WinForms application, including import workspace, calendar preview, settings dialog, and application entry point.
- `tests/CourseToIcal.Tests`: a dependency-free executable test harness that exercises core behavior and generates a temporary minimal `.xlsx` fixture for parser verification.

The application references only the core project. Core has no UI dependency. The parser facade selects a parser by extension and exposes one `CourseParser.Parse(string path)` entry point. The Open XML parser uses `System.IO.Compression.ZipArchive` and XML namespaces to resolve workbook sheets, relationships, shared strings, inline strings, and cell coordinates.

## File Layout

```text
CourseToIcal/
  src/
    CourseToIcal.Core/
      CourseToIcal.Core.csproj
      Models/
      Parsing/
      Scheduling/
      Export/
    CourseToIcal.App/
      CourseToIcal.App.csproj
      Program.cs
      UI/
  tests/
    CourseToIcal.Tests/
      CourseToIcal.Tests.csproj
      Program.cs
  samples/
    sample.csv
  docs/
    superpowers/specs/
  publish/
  .github/workflows/build.yml
  CourseToIcal.sln
  build.ps1
  README.md
  LICENSE
  .gitignore
```

`publish/` is a local output directory and is ignored by Git. A Release build places the runnable application and its dependent files there; source folders remain part of the repository.

## XLSX Parsing Contract

The Open XML reader will:

- open `.xlsx` as a ZIP package with read-only file sharing;
- resolve the workbook's worksheet relationship targets rather than assuming a fixed sheet filename;
- read `sharedStrings.xml` when present;
- read `inlineStr`, shared-string, ordinary string, and numeric cell values;
- convert Excel cell references such as `C12` to a 1-based column number;
- scan string cell values for the same course block format already supported by the BIFF parser;
- use the source cell's column as the weekday, accepting columns 1 through 7;
- return the same `Course` model as the `.xls` and CSV parsers;
- throw a clear input-format error when the package is malformed or contains no usable worksheet data.

No COM automation or installed Microsoft Excel dependency is allowed.

## Build And Release

The primary build uses the .NET 8 SDK and produces a Windows Forms executable. `build.ps1` supports the repository's current machine, which has the .NET runtime and legacy C# compiler but no installed .NET SDK, by falling back to the framework compiler for local verification. GitHub Actions uses `windows-latest` with the .NET 8 SDK to restore, build, run the test harness, and publish the application artifact.

The repository will not commit generated executables, temporary ICS files, user configuration, or `bin/obj` directories. A Release package is produced by the build script and can be copied or zipped for distribution.

## Error Handling

Parser failures identify the input path and the unsupported or malformed format. Unsupported `.xlsx` behavior is removed once the Open XML reader is implemented. Empty or invalid course records are skipped; if no course is found, the UI reports that the selected timetable was not recognized. Existing configuration normalization and iCalendar escaping remain in force.

## Testing

The test harness will verify:

- default 11-period schedule and XML persistence;
- first-week/date calculation and multi-period time range mapping;
- deterministic UIDs across repeated exports;
- CSV parsing with quoted commas;
- `.xlsx` parsing from a generated minimal workbook, including shared strings and weekday cell coordinates;
- a valid iCalendar output containing the expected event count and timestamps.

The same test command runs locally through `build.ps1` and in GitHub Actions.

## Non-Goals

- Importing `.xls` or `.xlsx` files through Excel COM automation.
- Building a new WPF or web front end.
- Changing the calendar semantics, default timetable times, or the existing public workflow beyond adding `.xlsx` support and clearer packaging.
- Claiming support for arbitrary school-specific workbook layouts that do not contain the recognized course block format.
