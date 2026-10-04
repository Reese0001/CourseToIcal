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
        private readonly Button previewButton;

        public CalendarWorkspaceForm()
        {
            Text = "课程表日历导入器";
            Width = 920;
            Height = 620;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 10F);
            config = ScheduleConfig.Load(ScheduleConfig.DefaultPath);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 5 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            Controls.Add(root);
            root.Controls.Add(new Label { Text = "批量导入课程表", Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            var addButton = new Button { Text = "添加课表文件...", Width = 140, Height = 34 };
            addButton.Click += (s, e) => AddFiles();
            var clearButton = new Button { Text = "清空", Width = 80, Height = 34 };
            clearButton.Click += (s, e) => { courses.Clear(); filesBox.Items.Clear(); UpdateStatus(); };
            var settingButton = new Button { Text = "课表设置...", Width = 110, Height = 34 };
            settingButton.Click += (s, e) => EditSettings();
            toolbar.Controls.Add(addButton); toolbar.Controls.Add(clearButton); toolbar.Controls.Add(settingButton);
            root.Controls.Add(toolbar, 0, 1);
            filesBox = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true };
            root.Controls.Add(filesBox, 0, 2);
            statusLabel = new Label { Text = "请选择一个或多个课表文件。", Dock = DockStyle.Fill, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft };
            root.Controls.Add(statusLabel, 0, 3);
            var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            previewButton = new Button { Text = "打开日历预览", Width = 150, Height = 36, Enabled = false };
            previewButton.Click += (s, e) => OpenPreview();
            bottom.Controls.Add(previewButton);
            root.Controls.Add(bottom, 0, 4);
        }

        private void AddFiles()
        {
            using (var dialog = new OpenFileDialog { Filter = "课表文件 (*.xls;*.xlsx;*.csv)|*.xls;*.xlsx;*.csv|所有文件 (*.*)|*.*", Multiselect = true })
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

        private void UpdateStatus()
        {
            previewButton.Enabled = courses.Count > 0;
            statusLabel.Text = courses.Count == 0 ? "请选择一个或多个课表文件。" : "已导入 " + filesBox.Items.Count + " 个文件，识别 " + courses.Count + " 门课程。点击“打开日历预览”查看并勾选导出内容。";
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
