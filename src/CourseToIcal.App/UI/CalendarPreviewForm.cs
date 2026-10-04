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

        public CalendarPreviewForm(List<Course> courses, ScheduleConfig config)
        {
            this.courses = courses;
            this.config = config;
            Text = "课表数据 · 日历预览";
            Width = 1180;
            Height = 760;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei UI", 10F);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 3 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            Controls.Add(root);
            var header = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            header.Controls.Add(new Label { Text = "第", AutoSize = true, Padding = new Padding(0, 9, 0, 0) });
            weekBox = new NumericUpDown { Minimum = 1, Maximum = Math.Max(1, config.SemesterWeeks), Value = Math.Max(1, Math.Min(config.CurrentWeek, config.SemesterWeeks)), Width = 60 };
            header.Controls.Add(weekBox);
            header.Controls.Add(new Label { Text = "周  ·  " + config.Name, AutoSize = true, Padding = new Padding(0, 9, 0, 0), Font = new Font(Font, FontStyle.Bold) });
            summary = new Label { Text = SummaryText(), AutoSize = true, ForeColor = Color.DimGray, Padding = new Padding(16, 9, 0, 0) };
            header.Controls.Add(summary);
            root.Controls.Add(header, 0, 0); root.SetColumnSpan(header, 2);
            courseList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, HorizontalScrollbar = true };
            foreach (Course course in courses) courseList.Items.Add(DisplayCourse(course), course.Selected);
            root.Controls.Add(courseList, 0, 1);
            canvas = new CalendarCanvas(courses, config) { Dock = DockStyle.Fill, Week = (int)weekBox.Value };
            root.Controls.Add(canvas, 1, 1);
            weekBox.ValueChanged += (s, e) => { canvas.Week = (int)weekBox.Value; summary.Text = SummaryText(); canvas.Invalidate(); };
            courseList.ItemCheck += (s, e) => BeginInvoke((Action)(() => { for (int i = 0; i < courses.Count; i++) courses[i].Selected = courseList.GetItemChecked(i); canvas.Invalidate(); summary.Text = SummaryText(); }));
            var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var export = new Button { Text = "导出选中课程为 iCalendar", Width = 210, Height = 36 };
            export.Click += (s, e) => ExportSelected();
            var selectAll = new Button { Text = "全选", Width = 70, Height = 36 };
            selectAll.Click += (s, e) => SetAll(true);
            var selectNone = new Button { Text = "取消全选", Width = 90, Height = 36 };
            selectNone.Click += (s, e) => SetAll(false);
            bottom.Controls.Add(export); bottom.Controls.Add(selectNone); bottom.Controls.Add(selectAll);
            root.Controls.Add(bottom, 0, 2); root.SetColumnSpan(bottom, 2);
        }

        private string DisplayCourse(Course course)
        {
            return course.Name + "  ·  " + course.Period + "节  ·  " + (string.IsNullOrWhiteSpace(course.Room) ? "无教室" : course.Room);
        }

        private string SummaryText()
        {
            DateTime weekStart = config.SemesterStart.Date.AddDays(-(config.WeekStart - 1) + ((int)weekBox.Value - 1) * 7);
            return "日期：" + weekStart.ToString("yyyy/MM/dd") + " 起  ·  已选 " + courses.Count(c => c.Selected) + " 门";
        }

        private void SetAll(bool selected)
        {
            for (int i = 0; i < courseList.Items.Count; i++) courseList.SetItemChecked(i, selected);
            for (int i = 0; i < courses.Count; i++) courses[i].Selected = selected;
            summary.Text = SummaryText();
            canvas.Invalidate();
        }

        private void ExportSelected()
        {
            if (!courses.Any(c => c.Selected))
            {
                MessageBox.Show(this, "请至少选择一门课程。", "没有可导出的课程", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var dialog = new SaveFileDialog { Filter = "iCalendar 文件 (*.ics)|*.ics", DefaultExt = "ics", FileName = config.Name + ".ics" })
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
