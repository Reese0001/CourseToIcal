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
            BackColor = UiTheme.Surface;
            BorderStyle = BorderStyle.FixedSingle;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(UiTheme.Surface);
            int left = 68;
            int top = 54;
            int rowHeight = Math.Max(42, (Height - top - 12) / 11);
            int colWidth = Math.Max(80, (Width - left - 8) / 7);
            DateTime weekStart = config.SemesterStart.Date.AddDays(-(config.WeekStart - 1) + (Week - 1) * 7);
            using (var gridPen = new Pen(UiTheme.Border))
            using (var headerBrush = new SolidBrush(UiTheme.SurfaceMuted))
            using (var textBrush = new SolidBrush(UiTheme.Text))
            using (var mutedBrush = new SolidBrush(UiTheme.MutedText))
            using (var headerFont = new Font(UiTheme.BodyFont, FontStyle.Bold))
            using (var smallFont = UiTheme.SmallFont)
            {
                e.Graphics.FillRectangle(headerBrush, left, 0, colWidth * 7, top);
                for (int column = 0; column <= 7; column++) e.Graphics.DrawLine(gridPen, left + column * colWidth, top, left + column * colWidth, top + rowHeight * 11);
                for (int row = 0; row <= 11; row++) e.Graphics.DrawLine(gridPen, left, top + row * rowHeight, left + colWidth * 7, top + row * rowHeight);
                for (int day = 0; day < 7; day++)
                {
                    DateTime date = weekStart.AddDays(day);
                    e.Graphics.DrawString(DayNames[day], headerFont, textBrush, left + day * colWidth + 8, 7);
                    e.Graphics.DrawString(date.ToString("MM/dd"), smallFont, mutedBrush, left + day * colWidth + 8, 28);
                }
                for (int row = 0; row < 11; row++)
                {
                    e.Graphics.DrawString((row + 1).ToString(), headerFont, textBrush, 14, top + row * rowHeight + 5);
                    e.Graphics.DrawString(config.Periods[row].StartText, smallFont, mutedBrush, 8, top + row * rowHeight + 25);
                }
            }
            foreach (CourseOccurrence occurrence in ScheduleEngine.Expand(courses.Where(c => c.Selected), config, Week))
            {
                int day = (occurrence.Date - weekStart).Days;
                if (day < 0 || day > 6) continue;
                int x = left + day * colWidth + 4;
                int y = top + (occurrence.FirstPeriod - 1) * rowHeight + 4;
                int height = (occurrence.LastPeriod - occurrence.FirstPeriod + 1) * rowHeight - 8;
                using (var brush = new SolidBrush(ColorFor(occurrence.Course.Name)))
                using (var textBrush = new SolidBrush(Color.White))
                using (var boldFont = new Font(UiTheme.SmallFont, FontStyle.Bold))
                using (var smallFont = new Font("Microsoft YaHei UI", 8.5F))
                {
                    e.Graphics.FillRectangle(brush, x, y, colWidth - 8, height);
                    string title = occurrence.Course.Name ?? "未命名课程";
                    string room = string.IsNullOrWhiteSpace(occurrence.Course.Room) ? "无教室" : occurrence.Course.Room;
                    e.Graphics.DrawString(title, boldFont, textBrush, new RectangleF(x + 7, y + 6, colWidth - 20, Math.Max(20, height - 28)));
                    e.Graphics.DrawString(room, smallFont, textBrush, new RectangleF(x + 7, y + height - 22, colWidth - 20, 17));
                }
            }
        }

        private static Color ColorFor(string value)
        {
            int hash = Math.Abs((value ?? "").GetHashCode());
            Color[] palette = { Color.FromArgb(22, 119, 255), Color.FromArgb(19, 194, 194), Color.FromArgb(82, 196, 26), Color.FromArgb(250, 140, 22), Color.FromArgb(114, 46, 209) };
            return palette[hash % palette.Length];
        }
    }
}
