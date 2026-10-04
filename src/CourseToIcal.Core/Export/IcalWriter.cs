using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CourseToIcal.Core.Models;
using CourseToIcal.Core.Scheduling;

namespace CourseToIcal.Core.Export
{
    public static class IcalWriter
    {
        public static void Write(string path, List<Course> courses, DateTime firstWeekStart, ScheduleConfig schedule = null)
        {
            ScheduleConfig activeSchedule = schedule ?? ScheduleConfig.CreateDefault();
            activeSchedule.SemesterStart = firstWeekStart.Date;
            List<Course> selected = (courses ?? new List<Course>()).Where(c => c != null && c.Selected).ToList();
            List<CourseOccurrence> occurrences = ScheduleEngine.Expand(selected, activeSchedule);
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//CourseToIcal//CN");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:PUBLISH");
            sb.AppendLine("X-WR-CALNAME:" + Escape(activeSchedule.Name));
            sb.AppendLine("X-WR-TIMEZONE:Asia/Shanghai");
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
            foreach (CourseOccurrence occurrence in occurrences)
            {
                Course course = occurrence.Course;
                DateTime start = occurrence.Date + occurrence.Start;
                DateTime end = occurrence.Date + occurrence.End;
                string uid = StableUid(course.Name + "|" + course.Teacher + "|" + course.Room + "|" + start.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture) + "|" + course.Period + "|" + occurrence.Week);
                sb.AppendLine("BEGIN:VEVENT");
                sb.AppendLine("UID:" + uid);
                sb.AppendLine("DTSTAMP:" + stamp);
                sb.AppendLine("DTSTART;TZID=Asia/Shanghai:" + start.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture));
                sb.AppendLine("DTEND;TZID=Asia/Shanghai:" + end.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture));
                sb.AppendLine("SUMMARY:" + Escape(course.Name));
                sb.AppendLine("LOCATION:" + Escape(course.Room));
                sb.AppendLine("DESCRIPTION:" + Escape("教师：" + course.Teacher + "\n周次：第" + occurrence.Week + "周\n节次：" + course.Period + "节"));
                sb.AppendLine("END:VEVENT");
            }
            sb.AppendLine("END:VCALENDAR");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        private static string Escape(string value)
        {
            return (value ?? "").Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r", "").Replace("\n", "\\n");
        }

        private static string StableUid(string value)
        {
            using (var sha = SHA1.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant() + "@coursetoical";
            }
        }
    }
}
