using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CourseToIcal.Core.Models;

namespace CourseToIcal.Core.Parsing
{
    internal static class OpenXmlCourseParser
    {
        static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
        static readonly XNamespace DocumentRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        public static List<Course> Parse(string path)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(path))
                {
                    Dictionary<int, string> sharedStrings = ReadSharedStrings(archive);
                    XDocument workbook = ReadXml(archive, "xl/workbook.xml");
                    XDocument relationships = ReadXml(archive, "xl/_rels/workbook.xml.rels");
                    var relationshipTargets = relationships.Root.Elements(PackageRelationships + "Relationship")
                        .ToDictionary(e => (string)e.Attribute("Id"), e => NormalizeTarget((string)e.Attribute("Target")));
                    var output = new List<Course>();
                    foreach (XElement sheet in workbook.Root.Element(Spreadsheet + "sheets").Elements(Spreadsheet + "sheet"))
                    {
                        string relationshipId = (string)sheet.Attribute(DocumentRelationships + "id");
                        string target;
                        if (relationshipId == null || !relationshipTargets.TryGetValue(relationshipId, out target)) continue;
                        XDocument worksheet = ReadXml(archive, target);
                        Dictionary<int, Dictionary<int, string>> rows = ReadRows(worksheet, sharedStrings);
                        List<Course> templateCourses;
                        if (TryParseTemplateRows(rows, path, out templateCourses))
                        {
                            output.AddRange(templateCourses);
                            continue;
                        }
                        foreach (KeyValuePair<int, Dictionary<int, string>> row in rows)
                        {
                            foreach (KeyValuePair<int, string> cell in row.Value)
                            {
                                int column = cell.Key;
                                if (column < 1 || column > 7) continue;
                                output.AddRange(CourseParser.ParseCourseCell(cell.Value, column, path));
                            }
                        }
                    }
                    if (output.Count == 0) throw new InvalidDataException("未在 .xlsx 工作表中识别到课程记录。\n文件：" + path);
                    return output;
                }
            }
            catch (InvalidDataException) { throw; }
            catch (Exception ex)
            {
                throw new InvalidDataException("无法读取 .xlsx 文件，请确认它是有效的 Excel Open XML 工作簿。\n文件：" + path, ex);
            }
        }

        private static Dictionary<int, string> ReadSharedStrings(ZipArchive archive)
        {
            var result = new Dictionary<int, string>();
            ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return result;
            XDocument document;
            using (Stream stream = entry.Open()) document = XDocument.Load(stream);
            int index = 0;
            foreach (XElement item in document.Root.Elements(Spreadsheet + "si"))
            {
                result[index++] = string.Concat(item.Descendants(Spreadsheet + "t").Select(t => (string)t));
            }
            return result;
        }

        private static string CellValue(XElement cell, Dictionary<int, string> sharedStrings)
        {
            string type = (string)cell.Attribute("t");
            if (type == "inlineStr") return string.Concat(cell.Descendants(Spreadsheet + "t").Select(t => (string)t));
            string raw = (string)cell.Element(Spreadsheet + "v") ?? "";
            if (type == "s")
            {
                int index;
                return int.TryParse(raw, out index) && sharedStrings.ContainsKey(index) ? sharedStrings[index] : "";
            }
            return raw;
        }

        private static Dictionary<int, Dictionary<int, string>> ReadRows(XDocument worksheet, Dictionary<int, string> sharedStrings)
        {
            var rows = new Dictionary<int, Dictionary<int, string>>();
            foreach (XElement cell in worksheet.Descendants(Spreadsheet + "c"))
            {
                string reference = (string)cell.Attribute("r");
                int column = ColumnNumber(reference);
                int row = RowNumber(reference);
                if (column < 1 || row < 1) continue;
                Dictionary<int, string> values;
                if (!rows.TryGetValue(row, out values)) { values = new Dictionary<int, string>(); rows[row] = values; }
                values[column] = CellValue(cell, sharedStrings);
            }
            return rows;
        }

        private static bool TryParseTemplateRows(Dictionary<int, Dictionary<int, string>> rows, string path, out List<Course> output)
        {
            output = new List<Course>();
            int headerRow = 0;
            int dayColumn = 0;
            int nameColumn = 0;
            int teacherColumn = 0;
            int weeksColumn = 0;
            int roomColumn = 0;
            int periodColumn = 0;
            foreach (KeyValuePair<int, Dictionary<int, string>> row in rows.OrderBy(r => r.Key))
            {
                foreach (KeyValuePair<int, string> cell in row.Value)
                {
                    string header = NormalizeHeader(cell.Value);
                    if (IsHeader(header, "星期", "周几", "day")) dayColumn = cell.Key;
                    else if (IsHeader(header, "课程名称", "课程", "name")) nameColumn = cell.Key;
                    else if (IsHeader(header, "教师", "任课教师", "teacher")) teacherColumn = cell.Key;
                    else if (IsHeader(header, "周次", "weeks", "week")) weeksColumn = cell.Key;
                    else if (IsHeader(header, "教室", "地点", "room")) roomColumn = cell.Key;
                    else if (IsHeader(header, "节次", "上课节次", "period")) periodColumn = cell.Key;
                }
                if (dayColumn > 0 && nameColumn > 0 && weeksColumn > 0 && periodColumn > 0)
                {
                    headerRow = row.Key;
                    break;
                }
                dayColumn = nameColumn = teacherColumn = weeksColumn = roomColumn = periodColumn = 0;
            }
            if (headerRow == 0) return false;
            foreach (KeyValuePair<int, Dictionary<int, string>> row in rows.Where(r => r.Key > headerRow).OrderBy(r => r.Key))
            {
                string name = Value(row.Value, nameColumn);
                if (string.IsNullOrWhiteSpace(name)) continue;
                int day = ParseDay(Value(row.Value, dayColumn));
                string weeksText = NormalizeWeeks(Value(row.Value, weeksColumn));
                string period = NormalizePeriod(Value(row.Value, periodColumn));
                if (day < 1 || day > 7 || string.IsNullOrWhiteSpace(period)) continue;
                var course = new Course
                {
                    Day = day,
                    Name = CourseParser.Clean(name),
                    Teacher = CourseParser.Clean(Value(row.Value, teacherColumn)),
                    Room = CourseParser.Clean(Value(row.Value, roomColumn)),
                    Period = period,
                    SourceFile = path,
                    Selected = true
                };
                course.Weeks.AddRange(CourseParser.ParseWeeks(weeksText));
                if (course.Weeks.Count > 0) output.Add(course);
            }
            return true;
        }

        private static string Value(Dictionary<int, string> row, int column)
        {
            string value;
            return column > 0 && row.TryGetValue(column, out value) ? value : "";
        }

        private static string NormalizeHeader(string value)
        {
            return (value ?? "").Trim().Replace(" ", "").Replace("\t", "").ToLowerInvariant();
        }

        private static bool IsHeader(string value, params string[] candidates)
        {
            foreach (string candidate in candidates) if (value == NormalizeHeader(candidate)) return true;
            return false;
        }

        private static string NormalizeWeeks(string value)
        {
            return (value ?? "").Replace("周", "").Replace("星期", "").Replace("，", ",").Replace("、", ",").Replace(" ", "").Trim();
        }

        private static string NormalizePeriod(string value)
        {
            MatchCollection matches = Regex.Matches(value ?? "", "\\d+");
            if (matches.Count == 0) return "";
            if (matches.Count == 1) return int.Parse(matches[0].Value).ToString("00");
            return int.Parse(matches[0].Value).ToString("00") + "-" + int.Parse(matches[matches.Count - 1].Value).ToString("00");
        }

        private static int ParseDay(string value)
        {
            string text = (value ?? "").Trim().Replace("星期", "").Replace("周", "");
            int number;
            if (int.TryParse(text, out number)) return number;
            string[] names = { "一", "二", "三", "四", "五", "六", "日" };
            for (int i = 0; i < names.Length; i++) if (text == names[i]) return i + 1;
            return 0;
        }

        private static XDocument ReadXml(ZipArchive archive, string path)
        {
            ZipArchiveEntry entry = archive.GetEntry(path);
            if (entry == null) throw new InvalidDataException(".xlsx 缺少内部文件：" + path);
            using (Stream stream = entry.Open()) return XDocument.Load(stream);
        }

        private static string NormalizeTarget(string target)
        {
            string normalized = (target ?? "").Replace('\\', '/').TrimStart('/');
            if (!normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) normalized = "xl/" + normalized;
            var segments = new List<string>();
            foreach (string segment in normalized.Split('/'))
            {
                if (segment.Length == 0 || segment == ".") continue;
                if (segment == ".." && segments.Count > 0) segments.RemoveAt(segments.Count - 1);
                else if (segment != "..") segments.Add(segment);
            }
            return string.Join("/", segments);
        }

        private static int ColumnNumber(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference)) return 0;
            int value = 0;
            foreach (char ch in reference)
            {
                if (ch < 'A' || ch > 'Z') break;
                value = value * 26 + ch - 'A' + 1;
            }
            return value;
        }

        private static int RowNumber(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference)) return 0;
            int index = 0;
            while (index < reference.Length && reference[index] >= 'A' && reference[index] <= 'Z') index++;
            int value;
            return index < reference.Length && int.TryParse(reference.Substring(index), out value) ? value : 0;
        }
    }
}
