using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CourseToIcal.Core.Parsing
{
    /// <summary>
    /// Creates the editable workbook used by the desktop import workflow.
    /// The package intentionally uses only inline strings so it can be generated without Excel.
    /// </summary>
    public static class XlsxTemplate
    {
        public static void Write(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("模板路径不能为空。", "path");
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            if (File.Exists(path)) File.Delete(path);

            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                AddEntry(archive, "[Content_Types].xml", ContentTypes());
                AddEntry(archive, "_rels/.rels", RootRelationships());
                AddEntry(archive, "xl/workbook.xml", Workbook());
                AddEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
                AddEntry(archive, "xl/worksheets/sheet1.xml", CourseSheet());
                AddEntry(archive, "xl/worksheets/sheet2.xml", HelpSheet());
            }
        }

        private static void AddEntry(ZipArchive archive, string name, string content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

        private static string ContentTypes()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "</Types>";
        }

        private static string RootRelationships()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>";
        }

        private static string Workbook()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"课程数据\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"使用说明\" sheetId=\"2\" r:id=\"rId2\"/></sheets>" +
                "</workbook>";
        }

        private static string WorkbookRelationships()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/>" +
                "</Relationships>";
        }

        private static string CourseSheet()
        {
            var xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            xml.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane xSplit=\"0\" ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            xml.Append("<cols><col min=\"1\" max=\"1\" width=\"10\" customWidth=\"1\"/><col min=\"2\" max=\"2\" width=\"24\" customWidth=\"1\"/><col min=\"3\" max=\"7\" width=\"18\" customWidth=\"1\"/></cols><sheetData>");
            string[] headers = { "星期", "课程名称", "教师", "周次", "教室", "节次", "备注" };
            xml.Append(Row(1, headers, false));
            xml.Append(Row(2, new[] { "1", "高等数学", "王老师", "1-16", "教学楼A101", "01-02", "示例行，可删除" }, true));
            xml.Append(Row(3, new[] { "3", "大学英语", "李老师", "1-8", "教学楼B203", "05-06", "示例行，可删除" }, true));
            xml.Append(Row(4, new[] { "", "", "", "", "", "", "" }, true));
            xml.Append(Row(5, new[] { "", "", "", "", "", "", "" }, true));
            xml.Append("</sheetData><autoFilter ref=\"A1:G5\"/></worksheet>");
            return xml.ToString();
        }

        private static string HelpSheet()
        {
            var xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            xml.Append(Row(1, new[] { "课程表模板使用说明" }, false));
            xml.Append(Row(2, new[] { "请在“课程数据”工作表中编辑，每一行代表一门课程。" }, false));
            xml.Append(Row(3, new[] { "星期：填写 1-7，分别代表周一至周日。" }, false));
            xml.Append(Row(4, new[] { "周次：支持 1-16、1,3,5 等写法。" }, false));
            xml.Append(Row(5, new[] { "节次：支持 01-02、5-6 等写法。" }, false));
            xml.Append(Row(6, new[] { "导入时会忽略空行和示例行之外无法识别的记录。" }, false));
            xml.Append("</sheetData></worksheet>");
            return xml.ToString();
        }

        private static string Row(int number, string[] values, bool allowNumbers)
        {
            var xml = new StringBuilder();
            xml.Append("<row r=\"").Append(number).Append("\">");
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i] ?? "";
                string reference = ColumnName(i + 1) + number;
                if (allowNumbers && i == 0 && value.Length > 0)
                {
                    xml.Append("<c r=\"").Append(reference).Append("\"><v>").Append(Escape(value)).Append("</v></c>");
                }
                else
                {
                    xml.Append("<c r=\"").Append(reference).Append("\" t=\"inlineStr\"><is><t>").Append(Escape(value)).Append("</t></is></c>");
                }
            }
            xml.Append("</row>");
            return xml.ToString();
        }

        private static string ColumnName(int number)
        {
            var result = new StringBuilder();
            int value = number;
            while (value > 0)
            {
                value--;
                result.Insert(0, (char)('A' + value % 26));
                value /= 26;
            }
            return result.ToString();
        }

        private static string Escape(string value)
        {
            return (value ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }
    }
}
