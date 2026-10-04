using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CourseToIcal.Core.Export;
using CourseToIcal.Core.Models;

namespace CourseToIcal.App.UI
{
    public sealed class CalendarPreviewForm : Form
    {
        private readonly List<Course> courses;
        private readonly ScheduleConfig config;
        private readonly CheckedListBox courseList;
        private readonly CalendarCanvas canvas;
        private readonly NumericUpDown weekBox;
        private readonly Label summary;
        private readonly Label selectionCount;

        public CalendarPreviewForm(List<Course> courses, ScheduleConfig config)
        {
            this.courses = courses;
            this.config = config;
            Text = "CourseToIcal · 日历预览";
            Width = 1260;
            Height = 820;
            MinimumSize = new Size(960, 620);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.StyleForm(this);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 2, RowCount = 3, BackColor = UiTheme.Background };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            Controls.Add(root);

            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Margin = new Padding(0), BackColor = UiTheme.Background };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            header.Controls.Add(new Label { Text = "CALENDAR PREVIEW", AutoSize = true, ForeColor = UiTheme.Primary, Font = new Font(UiTheme.SmallFont, FontStyle.Bold), Dock = DockStyle.Fill }, 0, 0);
            header.SetColumnSpan(header.GetControlFromPosition(0, 0), 4);
            header.Controls.Add(new Label { Text = "第", AutoSize = true, ForeColor = UiTheme.Text, Font = new Font(UiTheme.BodyFont, FontStyle.Bold), Dock = DockStyle.Fill, Padding = new Padding(0, 5, 0, 0) }, 0, 1);
            weekBox = new NumericUpDown { Minimum = 1, Maximum = Math.Max(1, config.SemesterWeeks), Value = Math.Max(1, Math.Min(config.CurrentWeek, config.SemesterWeeks)), Width = 70, Height = 34, Font = new Font(UiTheme.BodyFont, FontStyle.Bold), BackColor = UiTheme.Surface, ForeColor = UiTheme.Text, Margin = new Padding(0) };
            header.Controls.Add(weekBox, 1, 1);
            header.Controls.Add(new Label { Text = "周  ·  " + config.Name, AutoSize = true, ForeColor = UiTheme.Text, Font = new Font(UiTheme.BodyFont, FontStyle.Bold), Dock = DockStyle.Fill, Padding = new Padding(10, 5, 0, 0) }, 2, 1);
            summary = new Label { Text = SummaryText(), AutoSize = true, ForeColor = UiTheme.MutedText, Font = UiTheme.SmallFont, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0) };
            header.Controls.Add(summary, 3, 1);
            root.Controls.Add(header, 0, 0); root.SetColumnSpan(header, 2);

            var selectionPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(0, 0, 16, 0), Margin = new Padding(0), BackColor = UiTheme.Background };
            selectionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            selectionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            selectionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            selectionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            selectionPanel.Controls.Add(new Label { Text = "选择课程", AutoSize = true, ForeColor = UiTheme.Text, Font = new Font(UiTheme.BodyFont, FontStyle.Bold), Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) }, 0, 0);
            selectionPanel.Controls.Add(new Label { Text = "勾选后会同步显示在右侧日历", AutoSize = true, ForeColor = UiTheme.MutedText, Font = UiTheme.SmallFont, Dock = DockStyle.Fill }, 0, 1);
            courseList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, HorizontalScrollbar = true, BorderStyle = BorderStyle.FixedSingle, BackColor = UiTheme.Surface, ForeColor = UiTheme.Text, Font = UiTheme.SmallFont, IntegralHeight = false };
            foreach (Course course in courses) courseList.Items.Add(DisplayCourse(course), course.Selected);
            selectionPanel.Controls.Add(courseList, 0, 2);
            selectionCount = new Label { Text = SelectionText(), AutoSize = true, ForeColor = UiTheme.Success, Font = new Font(UiTheme.SmallFont, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            selectionPanel.Controls.Add(selectionCount, 0, 3);
            root.Controls.Add(selectionPanel, 0, 1);

            canvas = new CalendarCanvas(courses, config) { Dock = DockStyle.Fill, Week = (int)weekBox.Value, Margin = new Padding(0), BorderStyle = BorderStyle.FixedSingle };
            root.Controls.Add(canvas, 1, 1);
            weekBox.ValueChanged += (s, e) => { canvas.Week = (int)weekBox.Value; summary.Text = SummaryText(); canvas.Invalidate(); };
            courseList.ItemCheck += (s, e) => BeginInvoke((Action)(() => { for (int i = 0; i < courses.Count; i++) courses[i].Selected = courseList.GetItemChecked(i); canvas.Invalidate(); summary.Text = SummaryText(); selectionCount.Text = SelectionText(); }));

            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 10, 0, 0) };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390));
            bottom.Controls.Add(UiTheme.HintLabel("导出后可将 .ics 文件导入 Outlook、Apple 日历或其他日历应用。"), 0, 0);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            var export = new Button { Text = "导出选中课程", Width = 160, Height = 44 };
            UiTheme.StyleButton(export, true, true);
            export.Click += (s, e) => ExportSelected();
            var selectNone = new Button { Text = "取消全选", Width = 100, Height = 44, Margin = new Padding(8, 0, 0, 0) };
            UiTheme.StyleButton(selectNone, false, false);
            selectNone.Click += (s, e) => SetAll(false);
            var selectAll = new Button { Text = "全选", Width = 80, Height = 44, Margin = new Padding(8, 0, 0, 0) };
            UiTheme.StyleButton(selectAll, false, false);
            selectAll.Click += (s, e) => SetAll(true);
            actions.Controls.Add(export); actions.Controls.Add(selectNone); actions.Controls.Add(selectAll);
            bottom.Controls.Add(actions, 1, 0);
            root.Controls.Add(bottom, 0, 2); root.SetColumnSpan(bottom, 2);
        }

        private string DisplayCourse(Course course)
        {
            string room = string.IsNullOrWhiteSpace(course.Room) ? "无教室" : course.Room;
            return course.Name + "  ·  " + course.Period + "节  ·  " + room;
        }

        private string SummaryText()
        {
            DateTime weekStart = config.SemesterStart.Date.AddDays(-(config.WeekStart - 1) + ((int)weekBox.Value - 1) * 7);
            return "本周从 " + weekStart.ToString("yyyy/MM/dd") + " 开始  ·  " + courses.Count(c => c.Selected) + " 门已选";
        }

        private string SelectionText()
        {
            return courses.Count(c => c.Selected) + " / " + courses.Count + " 门课程已选";
        }

        private void SetAll(bool selected)
        {
            for (int i = 0; i < courseList.Items.Count; i++) courseList.SetItemChecked(i, selected);
            for (int i = 0; i < courses.Count; i++) courses[i].Selected = selected;
            summary.Text = SummaryText();
            selectionCount.Text = SelectionText();
            canvas.Invalidate();
        }

        private void ExportSelected()
        {
            if (!courses.Any(c => c.Selected))
            {
                MessageBox.Show(this, "请至少选择一门课程。", "没有可导出的课程", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var dialog = new SaveFileDialog { Filter = "iCalendar 文件 (*.ics)|*.ics", DefaultExt = "ics", FileName = config.Name + ".ics", Title = "保存 iCalendar 文件" })
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
