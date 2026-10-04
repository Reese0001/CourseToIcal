using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace CourseToIcal.Core.Models
{
    public sealed class PeriodSetting
    {
        public int Number { get; set; }
        public string StartText { get; set; }
        public string EndText { get; set; }

        [XmlIgnore]
        public TimeSpan Start
        {
            get { return TimeSpan.Parse(StartText); }
            set { StartText = value.ToString(@"hh\:mm"); }
        }

        [XmlIgnore]
        public TimeSpan End
        {
            get { return TimeSpan.Parse(EndText); }
            set { EndText = value.ToString(@"hh\:mm"); }
        }
    }

    public sealed class ScheduleConfig
    {
        public string Name { get; set; }
        public DateTime SemesterStart { get; set; }
        public int WeekStart { get; set; }
        public int SemesterWeeks { get; set; }
        public int CurrentWeek { get; set; }

        [XmlArray]
        public List<PeriodSetting> Periods { get; set; }

        public ScheduleConfig()
        {
            Name = "默认";
            SemesterStart = new DateTime(2026, 8, 31);
            WeekStart = 1;
            SemesterWeeks = 20;
            CurrentWeek = 1;
            Periods = new List<PeriodSetting>();
        }

        public static ScheduleConfig CreateDefault()
        {
            var config = new ScheduleConfig();
            string[] starts = { "08:30", "09:20", "10:25", "11:15", "14:00", "14:50", "15:55", "16:45", "18:30", "19:20", "20:30" };
            string[] ends = { "09:15", "10:05", "11:10", "12:00", "14:45", "15:35", "16:40", "17:30", "19:15", "20:05", "21:15" };
            for (int i = 0; i < starts.Length; i++)
            {
                config.Periods.Add(new PeriodSetting { Number = i + 1, StartText = starts[i], EndText = ends[i] });
            }
            return config;
        }

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(Name)) Name = "默认";
            if (WeekStart < 1 || WeekStart > 7) WeekStart = 1;
            if (Periods == null) Periods = new List<PeriodSetting>();
            ScheduleConfig defaults = CreateDefault();
            for (int i = Periods.Count; i < defaults.Periods.Count; i++) Periods.Add(defaults.Periods[i]);
            if (Periods.Count > 11) Periods = Periods.Take(11).ToList();
            for (int i = 0; i < Periods.Count; i++)
            {
                if (Periods[i] == null) Periods[i] = new PeriodSetting();
                Periods[i].Number = i + 1;
                if (string.IsNullOrWhiteSpace(Periods[i].StartText)) Periods[i].StartText = defaults.Periods[i].StartText;
                if (string.IsNullOrWhiteSpace(Periods[i].EndText)) Periods[i].EndText = defaults.Periods[i].EndText;
            }
            if (SemesterWeeks < 1) SemesterWeeks = 20;
            if (CurrentWeek < 1) CurrentWeek = 1;
            if (CurrentWeek > SemesterWeeks) CurrentWeek = SemesterWeeks;
        }

        public void Save(string path)
        {
            Normalize();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                new XmlSerializer(typeof(ScheduleConfig)).Serialize(writer, this);
            }
        }

        public static ScheduleConfig Load(string path)
        {
            if (!File.Exists(path)) return CreateDefault();
            try
            {
                using (var reader = new StreamReader(path, Encoding.UTF8))
                {
                    var config = (ScheduleConfig)new XmlSerializer(typeof(ScheduleConfig)).Deserialize(reader);
                    config.Normalize();
                    return config;
                }
            }
            catch
            {
                return CreateDefault();
            }
        }

        public static string DefaultPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CourseToIcal", "schedule.xml"); }
        }
    }
}
