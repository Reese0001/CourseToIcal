using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CourseToIcal.Core.Export;
using CourseToIcal.Core.Models;
using CourseToIcal.Core.Parsing;
using CourseToIcal.Core.Scheduling;

namespace CourseToIcal.Tests
{
    internal static class Program
    {
        private static void Main()
        {
            try
            {
                Run();
                Console.WriteLine("PASS");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.GetType().FullName);
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }

        private static void Run()
        {
            ScheduleConfig config = ScheduleConfig.CreateDefault();
            Assert(config.Periods.Count == 11, "默认时间表应包含 11 节课");
            Assert(config.Periods[0].Start == new TimeSpan(8, 30, 0), "第 1 节开始时间");
            Assert(config.Periods[10].End == new TimeSpan(21, 15, 0), "第 11 节结束时间");
            config.Name = "测试课表";
            string configPath = TempPath("schedule", ".xml");
            config.Save(configPath);
            ScheduleConfig loaded = ScheduleConfig.Load(configPath);
            Assert(loaded.Name == "测试课表" && loaded.SemesterWeeks == 20, "配置保存和读取");
            File.Delete(configPath);

            var course = new Course { Name = "晚课", Teacher = "老师", Room = "A101", Period = "09-10-11", Day = 1, Selected = true };
            course.Weeks.Add(1);
            string ics1 = TempPath("calendar-one", ".ics");
            string ics2 = TempPath("calendar-two", ".ics");
            IcalWriter.Write(ics1, new List<Course> { course }, config.SemesterStart, config);
            IcalWriter.Write(ics2, new List<Course> { course }, config.SemesterStart, config);
            string first = File.ReadAllText(ics1);
            string second = File.ReadAllText(ics2);
            Assert(first.Contains("DTSTART;TZID=Asia/Shanghai:20260831T183000") && first.Contains("DTEND;TZID=Asia/Shanghai:20260831T211500"), "跨 9-11 节时间范围");
            Assert(ReadLine(first, "UID:") == ReadLine(second, "UID:"), "重复导出 UID 稳定");
            File.Delete(ics1); File.Delete(ics2);
            List<CourseOccurrence> occurrences = ScheduleEngine.Expand(new List<Course> { course }, config, 1);
            Assert(occurrences.Count == 1 && occurrences[0].Date == new DateTime(2026, 8, 31), "首周日期映射");

            string csvPath = TempPath("quoted", ".csv");
            File.WriteAllText(csvPath, "day,name,teacher,weeks,room,period\n2,\"数据库,原理\",李老师,1-2,机房1,03-04\n", new UTF8Encoding(false));
            List<Course> csvCourses = CourseParser.Parse(csvPath);
            Assert(csvCourses.Count == 1 && csvCourses[0].Name == "数据库,原理" && csvCourses[0].Day == 2, "CSV 引号和逗号字段");
            File.Delete(csvPath);

            string xlsxPath = TempPath("openxml", ".xlsx");
            WriteMinimalWorkbook(xlsxPath);
            List<Course> xlsxCourses = CourseParser.Parse(xlsxPath);
            Assert(xlsxCourses.Count == 1, ".xlsx 课程记录数量");
            Assert(xlsxCourses[0].Name == "线性代数" && xlsxCourses[0].Teacher == "张老师" && xlsxCourses[0].Room == "教学楼101" && xlsxCourses[0].Period == "01-02" && xlsxCourses[0].Day == 2, ".xlsx 课程字段和星期列");
            Assert(xlsxCourses[0].Weeks.SequenceEqual(new[] { 1, 2, 3 }), ".xlsx 周次解析");
            File.Delete(xlsxPath);
        }

        private static void WriteMinimalWorkbook(string path)
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                AddEntry(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/></Types>");
                AddEntry(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"课表\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                AddEntry(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
                AddEntry(archive, "xl/sharedStrings.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"1\" uniqueCount=\"1\"><si><t>线性代数\n张老师\n1-3[周]\n教学楼101\n[01-02]节</t></si></sst>");
                AddEntry(archive, "xl/worksheets/sheet1.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"4\"><c r=\"B4\" t=\"s\"><v>0</v></c></row></sheetData></worksheet>");
            }
        }

        private static void AddEntry(ZipArchive archive, string name, string content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name);
            using (StreamWriter writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

        private static string TempPath(string stem, string extension)
        {
            return Path.Combine(Path.GetTempPath(), "coursetoical-" + stem + "-" + Guid.NewGuid().ToString("N") + extension);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception("FAIL: " + message);
        }

        private static string ReadLine(string text, string prefix)
        {
            foreach (string line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith(prefix, StringComparison.Ordinal)) return line;
            }
            return "";
        }
    }
}
