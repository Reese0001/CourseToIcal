using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CourseToIcal.Core.Models;
using CourseToIcal.Core.Parsing;

namespace CourseToIcal.App.UI
{
    public sealed class CalendarWorkspaceForm : Form
    {
        private readonly List<Course> courses = new List<Course>();
        private readonly ScheduleConfig config;
        private readonly ListBox filesBox;
        private readonly Label statusLabel;
        private readonly Label filesCountLabel;
        private readonly Button previewButton;
        private readonly Label emptyLabel;

        public CalendarWorkspaceForm()
        {
            Text = "CourseToIcal · 课程表日历导入器";
            Width = 980;
            Height = 680;
            MinimumSize = new Size(760, 560);
            StartPosition = FormStartPosition.CenterScreen;
            UiTheme.StyleForm(this);
            config = ScheduleConfig.Load(ScheduleConfig.DefaultPath);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 5, BackColor = UiTheme.Background };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            Controls.Add(root);

            var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
            heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            heading.Controls.Add(new Label { Text = "COURSETOICAL  /  IMPORT WORKSPACE", AutoSize = true, ForeColor = UiTheme.Primary, Font = new Font(UiTheme.SmallFont, FontStyle.Bold), Dock = DockStyle.Fill }, 0, 0);
            heading.Controls.Add(new Label { Text = "批量导入课程表", AutoSize = true, ForeColor = UiTheme.Text, Font = UiTheme.HeadingFont, Dock = DockStyle.Fill }, 0, 1);
            root.Controls.Add(heading, 0, 0);

            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 4, 0, 4), Margin = new Padding(0) };
            var addButton = new Button { Text = "添加课表文件", Width = 150, Height = 42 };
            UiTheme.StyleButton(addButton, true, false);
            addButton.Click += (s, e) => AddFiles();
            var settingButton = new Button { Text = "课表设置", Width = 120, Height = 42, Margin = new Padding(12, 0, 0, 0) };
            UiTheme.StyleButton(settingButton, false, false);
            settingButton.Click += (s, e) => EditSettings();
            var clearButton = new Button { Text = "清空列表", Width = 110, Height = 42, Margin = new Padding(8, 0, 0, 0) };
            UiTheme.StyleButton(clearButton, false, false);
            clearButton.Click += (s, e) => ClearFiles();
            toolbar.Controls.Add(addButton); toolbar.Controls.Add(settingButton); toolbar.Controls.Add(clearButton);
            root.Controls.Add(toolbar, 0, 1);

            var filesGroup = new GroupBox { Text = "已添加的课表", Dock = DockStyle.Fill, Padding = new Padding(14), ForeColor = UiTheme.Text, Font = new Font(UiTheme.BodyFont, FontStyle.Bold), BackColor = UiTheme.Surface, Margin = new Padding(0, 6, 0, 6) };
            var filesContent = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0), BackColor = UiTheme.Surface };
            filesContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            filesContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            filesCountLabel = new Label { Text = "尚未添加文件", Dock = DockStyle.Fill, ForeColor = UiTheme.MutedText, Font = UiTheme.SmallFont, TextAlign = ContentAlignment.MiddleLeft };
            filesContent.Controls.Add(filesCountLabel, 0, 0);
            var listHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
            filesBox = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true, BorderStyle = BorderStyle.FixedSingle, BackColor = UiTheme.Surface, ForeColor = UiTheme.Text, Font = UiTheme.BodyFont, IntegralHeight = false };
            emptyLabel = new Label { Text = "还没有课程表\r\n点击“添加课表文件”开始导入", Dock = DockStyle.Fill, ForeColor = UiTheme.MutedText, Font = new Font(UiTheme.BodyFont, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, BackColor = UiTheme.Surface };
            listHost.Controls.Add(filesBox);
            listHost.Controls.Add(emptyLabel);
            filesContent.Controls.Add(listHost, 0, 1);
            filesGroup.Controls.Add(filesContent);
            root.Controls.Add(filesGroup, 0, 2);

            var statusPanel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.SurfaceMuted, Padding = new Padding(14, 8, 14, 8), Margin = new Padding(0, 6, 0, 0) };
            statusLabel = new Label { Text = "请选择一个或多个课表文件。", Dock = DockStyle.Fill, ForeColor = UiTheme.MutedText, Font = UiTheme.SmallFont, TextAlign = ContentAlignment.MiddleLeft };
            statusPanel.Controls.Add(statusLabel);
            root.Controls.Add(statusPanel, 0, 3);

            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 4, 0, 0) };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            bottom.Controls.Add(UiTheme.HintLabel("支持 XLS、XLSX 和 CSV。导入后可在日历预览中勾选课程。"), 0, 0);
            previewButton = new Button { Text = "打开日历预览", Width = 170, Height = 44, Anchor = AnchorStyles.Right, Enabled = false };
            UiTheme.StyleButton(previewButton, true, true);
            previewButton.Click += (s, e) => OpenPreview();
            bottom.Controls.Add(previewButton, 1, 0);
            root.Controls.Add(bottom, 0, 4);
            UpdateStatus();
        }

        private void AddFiles()
        {
            using (var dialog = new OpenFileDialog { Filter = "课表文件 (*.xls;*.xlsx;*.csv)|*.xls;*.xlsx;*.csv|所有文件 (*.*)|*.*", Multiselect = true, Title = "选择要导入的课程表" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                foreach (string path in dialog.FileNames)
                {
                    if (filesBox.Items.Cast<string>().Any(item => item.StartsWith(path + "\t", StringComparison.OrdinalIgnoreCase))) continue;
                    try
                    {
                        List<Course> parsed = CourseParser.Parse(path);
                        foreach (Course course in parsed) { course.SourceFile = path; course.Selected = true; courses.Add(course); }
                        filesBox.Items.Add(path + "\t（识别 " + parsed.Count + " 门课程）");
                    }
                    catch (Exception ex) { filesBox.Items.Add(path + "\t（导入失败：" + ex.Message + "）"); }
                }
                UpdateStatus();
            }
        }

        private void ClearFiles()
        {
            if (courses.Count == 0 && filesBox.Items.Count == 0) return;
            courses.Clear();
            filesBox.Items.Clear();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            previewButton.Enabled = courses.Count > 0;
            emptyLabel.Visible = filesBox.Items.Count == 0;
            filesCountLabel.Text = filesBox.Items.Count == 0 ? "尚未添加文件" : filesBox.Items.Count + " 个文件 · " + courses.Count + " 门课程";
            statusLabel.Text = courses.Count == 0 ? "请选择一个或多个课表文件。" : "已识别 " + courses.Count + " 门课程。打开日历预览后，可以按课程勾选需要导出的内容。";
            statusLabel.ForeColor = courses.Count == 0 ? UiTheme.MutedText : UiTheme.Success;
        }

        private void EditSettings()
        {
            using (var dialog = new ScheduleSettingsForm(config))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                ScheduleConfig updated = dialog.Config;
                config.Name = updated.Name; config.SemesterStart = updated.SemesterStart; config.WeekStart = updated.WeekStart;
                config.SemesterWeeks = updated.SemesterWeeks; config.CurrentWeek = updated.CurrentWeek; config.Periods = updated.Periods;
                config.Save(ScheduleConfig.DefaultPath);
            }
        }

        private void OpenPreview()
        {
            using (var preview = new CalendarPreviewForm(courses, config)) preview.ShowDialog(this);
        }
    }
}
