# CourseToIcal React Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the WinForms-first workflow with a real React + Ant Design + Electron desktop application while preserving local timetable import, template editing, weekly calendar preview, and iCalendar export.

**Architecture:** Keep the existing C# project as a legacy fallback and create a TypeScript core consumed by a Vite React renderer. Electron owns local file dialogs and a narrow preload bridge; renderer code never receives Node filesystem access directly.

**Tech Stack:** React 18, TypeScript, Vite, Ant Design, SheetJS (`xlsx`), Electron, electron-builder, Vitest.

**Spec:** `docs/superpowers/specs/2026-10-04-coursetoical-react-design.md`

## Global Constraints

- Support `.xls`, `.xlsx`, and `.csv` without Microsoft Excel.
- The first screen is the weekly calendar workspace.
- The editable template must round-trip through `.xlsx`.
- All course files remain local; no network API is needed at runtime.
- The existing WinForms build remains available while the React build is being verified.

---

### Task 1: Add the React/Electron workspace and failing core tests

**Files:**
- Create: `package.json`, `tsconfig.json`, `vite.config.ts`, `vitest.config.ts`, `index.html`
- Create: `frontend/src/core.test.ts`
- Create: `src/core-ts/types.ts`, `src/core-ts/index.ts`

- [ ] **Step 1: Add npm scripts and dependencies.**

Use scripts `dev`, `test`, `build`, `dist`, and `typecheck`; dependencies include `@ant-design/icons`, `antd`, `dayjs`, `electron`, `electron-builder`, `jsdom`, `react`, `react-dom`, `typescript`, `vite`, `vitest`, and `xlsx`.

- [ ] **Step 2: Write failing tests for the TypeScript core.**

Cover template round-trip, CSV parsing with a quoted comma, schedule expansion for week 1, and deterministic iCalendar UID output.

- [ ] **Step 3: Run `npm test -- --run` and verify the tests fail because core functions are not implemented.**

The expected state is a module-resolution or missing-export failure, not a passing test.

### Task 2: Implement the TypeScript core

**Files:**
- Create: `src/core-ts/parser.ts`
- Create: `src/core-ts/template.ts`
- Create: `src/core-ts/scheduler.ts`
- Create: `src/core-ts/ical.ts`
- Modify: `src/core-ts/types.ts`, `src/core-ts/index.ts`
- Test: `frontend/src/core.test.ts`

- [ ] **Step 1: Implement typed models and default schedule configuration.**
- [ ] **Step 2: Implement SheetJS parsing for `.xls`, `.xlsx`, and `.csv`.**
- [ ] **Step 3: Implement the editable two-sheet `.xlsx` template and header-based template parsing.**
- [ ] **Step 4: Implement week expansion and period-range validation.**
- [ ] **Step 5: Implement UTF-8 iCalendar output with deterministic UIDs.**
- [ ] **Step 6: Run `npm test -- --run` and verify all core tests pass.**

### Task 3: Add Electron local file bridge

**Files:**
- Create: `electron/main.cjs`
- Create: `electron/preload.cjs`
- Create: `frontend/src/electron.d.ts`
- Modify: `package.json`

- [ ] **Step 1: Implement `openFiles`, `saveText`, `saveBinary`, and `getAppVersion` IPC handlers.**
- [ ] **Step 2: Expose only typed bridge functions through `contextBridge`.**
- [ ] **Step 3: Configure Vite dev URL and production `dist/index.html` loading.**
- [ ] **Step 4: Run `npm run typecheck` and launch `npm run dev` for a smoke check.**

### Task 4: Build the Ant Design calendar workspace

**Files:**
- Create: `frontend/src/main.tsx`
- Create: `frontend/src/App.tsx`
- Create: `frontend/src/styles.css`
- Create: `frontend/src/components/WeekCalendar.tsx`
- Create: `frontend/src/components/CourseSidebar.tsx`
- Create: `frontend/src/components/ImportToolbar.tsx`
- Create: `frontend/src/components/ScheduleSettingsModal.tsx`
- Modify: `frontend/src/electron.d.ts`

- [ ] **Step 1: Render the app bar, toolbar, sidebar, empty state, and weekly calendar.**
- [ ] **Step 2: Connect import and template actions to the Electron bridge and TypeScript parser.**
- [ ] **Step 3: Connect course selection, week navigation, settings persistence, and ICS export.**
- [ ] **Step 4: Apply Ant Design blue tokens, neutral surfaces, visible focus states, and responsive calendar sizing.**
- [ ] **Step 5: Run `npm run typecheck`, `npm test -- --run`, and `npm run build`.**

### Task 5: Update release workflow and documentation

**Files:**
- Modify: `.gitignore`, `README.md`, `.github/workflows/build.yml`
- Modify: `build.ps1` or add `build-react.ps1`

- [ ] **Step 1: Ignore frontend dependencies and Electron output.**
- [ ] **Step 2: Document React development, template workflow, and Windows packaging.**
- [ ] **Step 3: Add CI steps for npm install, test, typecheck, and build.**
- [ ] **Step 4: Run the full local verification sequence and inspect git diff.**
- [ ] **Step 5: Commit and push the React refactor after all checks pass.**
