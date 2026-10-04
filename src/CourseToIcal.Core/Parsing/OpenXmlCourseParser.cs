using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
                        foreach (XElement cell in worksheet.Descendants(Spreadsheet + "c"))
                        {
                            string reference = (string)cell.Attribute("r");
                            int column = ColumnNumber(reference);
                            if (column < 1 || column > 7) continue;
                            string value = CellValue(cell, sharedStrings);
                            output.AddRange(CourseParser.ParseCourseCell(value, column, path));
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
    }
}
