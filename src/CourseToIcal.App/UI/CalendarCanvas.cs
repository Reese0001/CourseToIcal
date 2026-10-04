using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CourseToIcal.Core.Models;
using CourseToIcal.Core.Scheduling;

namespace CourseToIcal.App.UI
{
    public sealed class CalendarCanvas : Panel
    {
        private readonly List<Course> courses;
        private readonly ScheduleConfig config;
        public int Week { get; set; }
        private static readonly string[] DayNames = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };

        public CalendarCanvas(List<Course> courses, ScheduleConfig config)
        {
            this.courses = courses;
            this.config = config;
            DoubleBuffered = true;
            BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            int left = 64;
            int top = 48;
            int rowHeight = Math.Max(42, (Height - top - 12) / 11);
            int colWidth = Math.Max(80, (Width - left - 8) / 7);
            using (var gridPen = new Pen(Color.Gainsboro))
            using (var textBrush = new SolidBrush(Color.FromArgb(55, 55, 55)))
            {
                for (int column = 0; column <= 7; column++) e.Graphics.DrawLine(gridPen, left + column * colWidth, top, left + column * colWidth, top + rowHeight * 11);
                for (int row = 0; row <= 11; row++) e.Graphics.DrawLine(gridPen, left, top + row * rowHeight, left + colWidth * 7, top + row * rowHeight);
                DateTime weekStart = config.SemesterStart.Date.AddDays(-(config.WeekStart - 1) + (Week - 1) * 7);
                for (int day = 0; day < 7; day++) e.Graphics.DrawString(DayNames[day] + "\n" + weekStart.AddDays(day).ToString("MM/dd"), Font, textBrush, left + day * colWidth + 4, 6);
                for (int row = 0; row < 11; row++) e.Graphics.DrawString((row + 1) + "\n" + config.Periods[row].StartText, Font, textBrush, 4, top + row * rowHeight + 4);
            }
            DateTime displayedWeekStart = config.SemesterStart.Date.AddDays(-(config.WeekStart - 1) + (Week - 1) * 7);
            foreach (CourseOccurrence occurrence in ScheduleEngine.Expand(courses.Where(c => c.Selected), config, Week))
            {
                int day = (occurrence.Date - displayedWeekStart).Days;
                if (day < 0 || day > 6) continue;
                int x = left + day * colWidth + 3;
                int y = top + (occurrence.FirstPeriod - 1) * rowHeight + 3;
                int height = (occurrence.LastPeriod - occurrence.FirstPeriod + 1) * rowHeight - 6;
                using (var brush = new SolidBrush(ColorFor(occurrence.Course.Name)))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillRectangle(brush, x, y, colWidth - 6, height);
                    string label = occurrence.Course.Name + "\n@" + occurrence.Course.Room;
                    e.Graphics.DrawString(label, Font, textBrush, new RectangleF(x + 5, y + 5, colWidth - 16, height - 10));
                }
            }
        }

        private static Color ColorFor(string value)
        {
            int hash = (value ?? "").GetHashCode();
            return Color.FromArgb(210, 80 + Math.Abs(hash % 130), 80 + Math.Abs((hash / 7) % 120), 120 + Math.Abs((hash / 13) % 100));
        }
    }
}
