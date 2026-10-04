using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CourseToIcal.Core.Models;

namespace CourseToIcal.Core.Parsing
{
    public static class CourseParser
    {
        internal static readonly Regex CourseBlock = new Regex(
            @"(?m)^\s*(?<name>[^\r\n]+)\r?\n(?<teacher>[^\r\n]+)\r?\n(?<weeks>[0-9,\-]+)\[周\]\r?\n(?<room>[^\r\n]+)\r?\n\[(?<period>[0-9\-]+)\]节",
            RegexOptions.CultureInvariant);

        public static List<Course> Parse(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("课表路径不能为空。", "path");
            if (!File.Exists(path)) throw new FileNotFoundException("找不到课表文件。", path);
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".csv") return CsvCourseParser.Parse(path);
            if (extension == ".xlsx") return OpenXmlCourseParser.Parse(path);
            if (extension == ".xls") return BiffCourseParser.Parse(path);
            throw new NotSupportedException("暂不支持该文件格式，请选择 .xls、.xlsx 或 .csv。\n文件：" + path);
        }

        internal static List<Course> ParseCourseCell(string value, int day, string sourceFile)
        {
            var output = new List<Course>();
            if (string.IsNullOrWhiteSpace(value) || day < 1 || day > 7) return output;
            foreach (Match match in CourseBlock.Matches(value.Replace("\r", "")))
            {
                var course = new Course
                {
                    Name = Clean(match.Groups["name"].Value),
                    Teacher = Clean(match.Groups["teacher"].Value),
                    Room = Clean(match.Groups["room"].Value),
                    Period = match.Groups["period"].Value,
                    Day = day,
                    SourceFile = sourceFile,
                    Selected = true
                };
                course.Weeks.AddRange(ParseWeeks(match.Groups["weeks"].Value));
                if (course.Weeks.Count > 0) output.Add(course);
            }
            return output;
        }

        internal static string Clean(string value)
        {
            return (value ?? "").Replace("\u3000", " ").Trim();
        }

        internal static IEnumerable<int> ParseWeeks(string value)
        {
            foreach (string part in (value ?? "").Split(','))
            {
                string piece = part.Trim();
                if (piece.Contains("-"))
                {
                    string[] bounds = piece.Split('-');
                    int start, end;
                    if (bounds.Length == 2 && int.TryParse(bounds[0], out start) && int.TryParse(bounds[1], out end))
                    {
                        for (int week = Math.Min(start, end); week <= Math.Max(start, end); week++) yield return week;
                    }
                }
                else
                {
                    int week;
                    if (int.TryParse(piece, out week)) yield return week;
                }
            }
        }
    }
}
