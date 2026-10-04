using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CourseToIcal.Core.Export;
using CourseToIcal.Core.Models;
using CourseToIcal.Core.Parsing;

namespace CourseToIcal.App.UI
{
    /// <summary>
    /// The first screen is the calendar workspace. Import and export actions stay in the top bar
    /// so a user can edit a template in Excel and return to the same view without a wizard.
    /// </summary>
    public sealed class CalendarWorkspaceForm : Form
    {
        private readonly List<Course> courses = new List<Course>();
        private readonly ScheduleConfig config;
        private readonly HashSet<string> importedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private CheckedListBox courseList;
        private ListBox filesBox;
        private Label courseCountLabel;
        private Label sourceCountLabel;
        private Label statusLabel;
        private Label emptyCoursesLabel;
        private Label weekTitleLabel;
        private NumericUpDown weekBox;
        private readonly CalendarCanvas canvas;
        private bool suppressChecks;

        public CalendarWorkspaceForm()
        {
            Text = "CourseToIcal · 课程表日历";
            Width = 1360;
            Height = 860;
            MinimumSize = new Size(1040, 680);
            StartPosition = FormStartPosition.CenterScreen;
            UiTheme.StyleForm(this);
            config = ScheduleConfig.Load(ScheduleConfig.DefaultPath);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = UiTheme.Background, Margin = new Padding(0) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            root.Controls.Add(BuildAppBar(), 0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);

            var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiTheme.Background, Padding = new Padding(16, 0, 16, 16), Margin = new Padding(0) };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 294));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.Controls.Add(BuildSidebar(), 0, 0);
            canvas = new CalendarCanvas(courses, config) { Dock = DockStyle.Fill, Week = CurrentWeek(), Margin = new Padding(14, 0, 0, 0), BorderStyle = BorderStyle.FixedSingle };
            content.Controls.Add(BuildCalendarPanel(canvas), 1, 0);
            root.Controls.Add(content, 0, 2);

            weekBox.ValueChanged += (s, e) => ChangeWeek((int)weekBox.Value);
            UpdateUi();
        }

        private Control BuildAppBar()
        {
            var bar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = UiTheme.Surface, Padding = new Padding(20, 0, 20, 0), Margin = new Padding(0) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            bar.Controls.Add(new Label { Text = "CourseToIcal", ForeColor = UiTheme.Primary, Font = new Font(UiTheme.BodyFont, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            bar.Controls.Add(new Label { Text = "课程表日历", ForeColor = UiTheme.Text, Font = new Font(UiTheme.BodyFont, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 1, 0);
            bar.Controls.Add(new Label { Text = "日历工作台", ForeColor = UiTheme.MutedText, Font = UiTheme.SmallFont, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 2, 0);
            return bar;
        }

        private Control BuildToolbar()
        {
            var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiTheme.Surface, Padding = new Padding(20, 8, 20, 8), Margin = new Padding(0) };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 372));

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0) };
            Button import = ActionButton("导入课表", true, "导入 XLS、XLSX 或 CSV 课表");
            import.Click += (s, e) => AddFiles();
            Button template = ActionButton("下载模板", false, "生成可编辑的 XLSX 课程表模板");
            template.Click += (s, e) => DownloadTemplate();
            Button settings = ActionButton("课表设置", false, "设置学期日期和上课时间");
            settings.Click += (s, e) => EditSettings();
            Button clear = ActionButton("清空", false, "清空当前已导入的课程");
            clear.Click += (s, e) => ClearFiles();
            Button export = ActionButton("导出日历", false, "将已选课程导出为 iCalendar 文件");
            export.Click += (s, e) => ExportSelected();
            actions.Controls.Add(import);
            actions.Controls.Add(template);
            actions.Controls.Add(settings);
            actions.Controls.Add(clear);
            actions.Controls.Add(export);
            toolbar.Controls.Add(actions, 0, 0);

            var weekActions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0) };
            Button next = IconButton(">", "下一周");
            next.Click += (s, e) => MoveWeek(1);
            Button previous = IconButton("<", "上一周");
            previous.Click += (s, e) => MoveWeek(-1);
            Button current = new Button { Text = "本周", Width = 66, Height = 38, Margin = new Padding(8, 0, 0, 0) };
            UiTheme.StyleButton(current, false, false);
            current.Click += (s, e) => { weekBox.Value = CurrentWeek(); };
            weekBox = new NumericUpDown { Minimum = 1, Maximum = Math.Max(1, config.SemesterWeeks), Value = CurrentWeek(), Width = 64, Height = 38, Font = new Font(UiTheme.BodyFont, FontStyle.Bold), BackColor = UiTheme.Surface, ForeColor = UiTheme.Text, Margin = new Padding(8, 0, 0, 0) };
            weekBox.AccessibleName = "当前周";
            weekTitleLabel = new Label { Text = "第 " + CurrentWeek() + " 周", Width = 72, Height = 38, ForeColor = UiTheme.Text, Font = new Font(UiTheme.BodyFont, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(8, 0, 0, 0) };
            weekActions.Controls.Add(next);
            weekActions.Controls.Add(previous);
            weekActions.Controls.Add(current);
            weekActions.Controls.Add(weekBox);
            weekActions.Controls.Add(weekTitleLabel);
            toolbar.Controls.Add(weekActions, 1, 0);
            return toolbar;
        }

        private Control BuildSidebar()
        {
            var sidebar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = UiTheme.Surface, Margin = new Padding(0), Padding = new Padding(16) };
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
            sidebar.Controls.Add(new Label { Text = "课程筛选", ForeColor = UiTheme.Text, Font = new Font(UiTheme.BodyFont, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            courseCountLabel = new Label { Text = "0 门课程", ForeColor = UiTheme.MutedText, Font = UiTheme.SmallFont, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            sidebar.Controls.Add(courseCountLabel, 0, 1);
            var courseHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Margin = new Padding(0) };
            courseList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle, BackColor = UiTheme.Surface, ForeColor = UiTheme.Text, Font = UiTheme.SmallFont, IntegralHeight = false, HorizontalScrollbar = true };
            courseList.AccessibleName = "课程筛选列表";
            courseList.ItemCheck += CourseListItemCheck;
            emptyCoursesLabel = new Label { Text = "还没有课程\r\n请先导入课表或下载模板", Dock = DockStyle.Fill, ForeColor = UiTheme.MutedText, Font = new Font(UiTheme.SmallFont, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, BackColor = UiTheme.Surface };
            courseHost.Controls.Add(courseList);
            courseHost.Controls.Add(emptyCoursesLabel);
            sidebar.Controls.Add(courseHost, 0, 2);
            var selection = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 8, 0, 0), Padding = new Padding(0) };
            Button all = new Button { Text = "全选", Width = 70, Height = 30, Margin = new Padding(0) };
            UiTheme.StyleButton(all, false, false);
            all.Click += (s, e) => SetAll(true);
            Button none = new Button { Text = "清除选择", Width = 86, Height = 30, Margin = new Padding(8, 0, 0, 0) };
            UiTheme.StyleButton(none, false, false);
            none.Click += (s, e) => SetAll(false);
            selection.Controls.Add(all);
            selection.Controls.Add(none);
            sidebar.Controls.Add(selection, 0, 3);
            var sources = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = UiTheme.SurfaceMuted, Padding = new Padding(10, 8, 10, 6), Margin = new Padding(0, 8, 0, 0) };
            sources.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            sources.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            sourceCountLabel = new Label { Text = "导入来源", ForeColor = UiTheme.Text, Font = new Font(UiTheme.SmallFont, FontStyle.Bold), Dock = DockStyle.Fill };
            sources.Controls.Add(sourceCountLabel, 0, 0);
            filesBox = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UiTheme.SurfaceMuted, ForeColor = UiTheme.MutedText, Font = UiTheme.SmallFont, IntegralHeight = false };
            sources.Controls.Add(filesBox, 0, 1);
            sidebar.Controls.Add(sources, 0, 4);
            return sidebar;
        }

        private Control BuildCalendarPanel(CalendarCanvas calendar)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = UiTheme.Surface, Margin = new Padding(0), Padding = new Padding(16) };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiTheme.Surface, Margin = new Padding(0) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290));
            header.Controls.Add(new Label { Text = "周视图日历", ForeColor = UiTheme.Text, Font = new Font(UiTheme.BodyFont, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            statusLabel = new Label { Text = "导入课程后会显示在对应的星期和节次。", ForeColor = UiTheme.MutedText, Font = UiTheme.SmallFont, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
            header.Controls.Add(statusLabel, 1, 0);
            panel.Controls.Add(header, 0, 0);
            panel.Controls.Add(calendar, 0, 1);
            return panel;
        }

        private Button ActionButton(string text, bool primary, string tooltip)
        {
            var button = new Button { Text = text, Width = primary ? 104 : 92, Height = 38, Margin = new Padding(0, 0, 8, 0) };
            UiTheme.StyleButton(button, primary, false);
            button.AccessibleName = text;
            new ToolTip().SetToolTip(button, tooltip);
            return button;
        }

        private Button IconButton(string text, string tooltip)
        {
            var button = new Button { Text = text, Width = 38, Height = 38, Margin = new Padding(4, 0, 0, 0), Font = new Font(UiTheme.BodyFont, FontStyle.Bold) };
            UiTheme.StyleButton(button, false, false);
            button.AccessibleName = tooltip;
            new ToolTip().SetToolTip(button, tooltip);
            return button;
        }

        private void AddFiles()
        {
            using (var dialog = new OpenFileDialog { Filter = "课表文件 (*.xls;*.xlsx;*.csv)|*.xls;*.xlsx;*.csv|所有文件 (*.*)|*.*", Multiselect = true, Title = "导入课程表" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                foreach (string path in dialog.FileNames)
                {
                    string fullPath = Path.GetFullPath(path);
                    if (importedPaths.Contains(fullPath)) continue;
                    try
                    {
                        List<Course> parsed = CourseParser.Parse(path);
                        if (parsed.Count == 0) throw new InvalidDataException("未识别到课程记录，请检查模板中的必填字段。\n文件：" + path);
                        importedPaths.Add(fullPath);
                        foreach (Course course in parsed) { course.SourceFile = path; course.Selected = true; courses.Add(course); }
                        filesBox.Items.Add(Path.GetFileName(path) + "  ·  " + parsed.Count + " 门");
                    }
                    catch (Exception ex)
                    {
                        filesBox.Items.Add(Path.GetFileName(path) + "  ·  导入失败");
                        statusLabel.Text = ex.Message;
                        statusLabel.ForeColor = UiTheme.Error;
                    }
                }
                RefreshCourseList();
                UpdateUi();
            }
        }

        private void DownloadTemplate()
        {
            using (var dialog = new SaveFileDialog { Filter = "Excel 模板 (*.xlsx)|*.xlsx", DefaultExt = "xlsx", FileName = "课程表导入模板.xlsx", Title = "保存课程表模板" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    XlsxTemplate.Write(dialog.FileName);
                    MessageBox.Show(this, "模板已生成。请在“课程数据”工作表中编辑后，再从日历页点击“导入课表”。", "模板已保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "模板生成失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        private void ClearFiles()
        {
            courses.Clear();
            importedPaths.Clear();
            filesBox.Items.Clear();
            RefreshCourseList();
            UpdateUi();
        }

        private void CourseListItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (suppressChecks || IsDisposed) return;
            BeginInvoke((Action)(() =>
            {
                for (int i = 0; i < courses.Count && i < courseList.Items.Count; i++) courses[i].Selected = courseList.GetItemChecked(i);
                canvas.Invalidate();
                UpdateUi();
            }));
        }

        private void RefreshCourseList()
        {
            suppressChecks = true;
            courseList.Items.Clear();
            foreach (Course course in courses) courseList.Items.Add(DisplayCourse(course), course.Selected);
            suppressChecks = false;
            emptyCoursesLabel.Visible = courses.Count == 0;
            courseList.Visible = courses.Count > 0;
        }

        private string DisplayCourse(Course course)
        {
            string room = string.IsNullOrWhiteSpace(course.Room) ? "无教室" : course.Room;
            return course.Name + "  ·  " + course.Period + "节  ·  " + room;
        }

        private void SetAll(bool selected)
        {
            suppressChecks = true;
            for (int i = 0; i < courseList.Items.Count; i++) courseList.SetItemChecked(i, selected);
            suppressChecks = false;
            for (int i = 0; i < courses.Count; i++) courses[i].Selected = selected;
            canvas.Invalidate();
            UpdateUi();
        }

        private void MoveWeek(int delta)
        {
            int next = Math.Max(1, Math.Min(config.SemesterWeeks, (int)weekBox.Value + delta));
            weekBox.Value = next;
        }

        private int CurrentWeek()
        {
            return Math.Max(1, Math.Min(config.SemesterWeeks, config.CurrentWeek));
        }

        private void ChangeWeek(int week)
        {
            canvas.Week = week;
            weekTitleLabel.Text = "第 " + week + " 周";
            canvas.Invalidate();
            UpdateUi();
        }

        private void UpdateUi()
        {
            int selected = courses.Count(c => c.Selected);
            courseCountLabel.Text = courses.Count == 0 ? "0 门课程" : selected + " / " + courses.Count + " 门已选";
            sourceCountLabel.Text = filesBox.Items.Count == 0 ? "导入来源" : "导入来源 · " + filesBox.Items.Count;
            if (courses.Count > 0)
            {
                statusLabel.Text = "已导入 " + courses.Count + " 门课程 · 当前显示第 " + weekBox.Value + " 周";
                statusLabel.ForeColor = UiTheme.Success;
            }
            else if (statusLabel.ForeColor != UiTheme.Error)
            {
                statusLabel.Text = "导入课程后会显示在对应的星期和节次。";
                statusLabel.ForeColor = UiTheme.MutedText;
            }
        }

        private void EditSettings()
        {
            using (var dialog = new ScheduleSettingsForm(config))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                ScheduleConfig updated = dialog.Config;
                config.Name = updated.Name;
                config.SemesterStart = updated.SemesterStart;
                config.WeekStart = updated.WeekStart;
                config.SemesterWeeks = updated.SemesterWeeks;
                config.CurrentWeek = updated.CurrentWeek;
                config.Periods = updated.Periods;
                config.Save(ScheduleConfig.DefaultPath);
                weekBox.Maximum = Math.Max(1, config.SemesterWeeks);
                weekBox.Value = Math.Min(weekBox.Value, weekBox.Maximum);
                ChangeWeek((int)weekBox.Value);
            }
        }

        private void ExportSelected()
        {
            if (!courses.Any(c => c.Selected))
            {
                MessageBox.Show(this, "请至少选择一门课程。", "没有可导出的课程", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var dialog = new SaveFileDialog { Filter = "iCalendar 文件 (*.ics)|*.ics", DefaultExt = "ics", FileName = config.Name + ".ics", Title = "导出日历文件" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    IcalWriter.Write(dialog.FileName, courses.Where(c => c.Selected).ToList(), config.SemesterStart, config);
                    MessageBox.Show(this, "已导出选中的课程。", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }
}
