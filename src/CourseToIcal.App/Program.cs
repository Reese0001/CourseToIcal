using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CourseToIcal.App.UI;
using CourseToIcal.Core.Export;
using CourseToIcal.Core.Models;
using CourseToIcal.Core.Parsing;

namespace CourseToIcal.App
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && File.Exists(args[1]))
            {
                RunCommandLine(args);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CalendarWorkspaceForm());
        }

        private static void RunCommandLine(string[] args)
        {
            try
            {
                string output = args.Length > 2 ? args[2] : Path.ChangeExtension(args[1], ".ics");
                ScheduleConfig config = ScheduleConfig.Load(ScheduleConfig.DefaultPath);
                DateTime firstWeekStart = args.Length > 3 ? DateTime.Parse(args[3]).Date : config.SemesterStart.Date;
                List<Course> courses = CourseParser.Parse(args[1]);
                if (courses.Count == 0) throw new InvalidDataException("没有识别到课程记录。请确认这是教务系统导出的个人课表。");
                IcalWriter.Write(output, courses, firstWeekStart, config);
            }
            catch (Exception ex)
            {
                try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coursetoical-error.txt"), ex.ToString(), Encoding.UTF8); } catch { }
                MessageBox.Show(ex.Message, "转换失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
