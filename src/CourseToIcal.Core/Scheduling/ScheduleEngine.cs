using System;
using System.Collections.Generic;
using System.Linq;
using CourseToIcal.Core.Models;

namespace CourseToIcal.Core.Scheduling
{
    public static class ScheduleEngine
    {
        public static List<CourseOccurrence> Expand(IEnumerable<Course> courses, ScheduleConfig schedule, int? onlyWeek = null)
        {
            schedule = schedule ?? ScheduleConfig.CreateDefault();
            schedule.Normalize();
            var result = new List<CourseOccurrence>();
            foreach (Course course in courses ?? Enumerable.Empty<Course>())
            {
                if (course == null || course.Day < 1 || course.Day > 7) continue;
                int first, last;
                if (!TryParsePeriod(course.Period, out first, out last)) continue;
                foreach (int week in course.Weeks.Distinct().Where(w => w >= 1 && w <= schedule.SemesterWeeks))
                {
                    if (onlyWeek.HasValue && onlyWeek.Value != week) continue;
                    DateTime weekBase = schedule.SemesterStart.Date.AddDays(-(schedule.WeekStart - 1) + (week - 1) * 7);
                    DateTime date = weekBase.AddDays(course.Day - 1);
                    result.Add(new CourseOccurrence
                    {
                        Course = course,
                        Week = week,
                        Date = date.Date,
                        Start = schedule.Periods[first - 1].Start,
                        End = schedule.Periods[last - 1].End,
                        FirstPeriod = first,
                        LastPeriod = last
                    });
                }
            }
            return result;
        }

        public static bool TryParsePeriod(string period, out int first, out int last)
        {
            first = last = 0;
            if (string.IsNullOrWhiteSpace(period)) return false;
            string[] parts = period.Split('-');
            if (!int.TryParse(parts[0], out first)) return false;
            last = first;
            if (parts.Length > 1 && !int.TryParse(parts[parts.Length - 1], out last)) return false;
            if (first < 1 || last < first || last > 11) return false;
            return true;
        }
    }
}
