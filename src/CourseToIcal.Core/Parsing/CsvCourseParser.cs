using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CourseToIcal.Core.Models;

namespace CourseToIcal.Core.Parsing
{
    internal static class CsvCourseParser
    {
        public static List<Course> Parse(string path)
        {
            var output = new List<Course>();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            foreach (string line in lines.Skip(1))
            {
                List<string> fields = ParseFields(line);
                if (fields.Count < 6) continue;
                int day;
                if (!int.TryParse(fields[0].Trim(), out day) || day < 1 || day > 7) continue;
                var course = new Course
                {
                    Day = day,
                    Name = fields[1].Trim(),
                    Teacher = fields[2].Trim(),
                    Room = fields[4].Trim(),
                    Period = fields[5].Trim(),
                    SourceFile = path,
                    Selected = true
                };
                course.Weeks.AddRange(CourseParser.ParseWeeks(fields[3]));
                if (course.Weeks.Count > 0) output.Add(course);
            }
            return output;
        }

        private static List<string> ParseFields(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < (line ?? "").Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else quoted = !quoted;
                }
                else if (ch == ',' && !quoted) { fields.Add(current.ToString()); current.Clear(); }
                else current.Append(ch);
            }
            fields.Add(current.ToString());
            return fields;
        }
    }
}
