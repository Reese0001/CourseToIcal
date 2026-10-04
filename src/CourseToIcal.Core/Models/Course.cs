using System;
using System.Collections.Generic;

namespace CourseToIcal.Core.Models
{
    public sealed class Course
    {
        public string Name;
        public string Teacher;
        public string Room;
        public string Period;
        public int Day;
        public string SourceFile;
        public bool Selected = true;
        public List<int> Weeks = new List<int>();
    }

    public sealed class CourseOccurrence
    {
        public Course Course;
        public int Week;
        public DateTime Date;
        public TimeSpan Start;
        public TimeSpan End;
        public int FirstPeriod;
        public int LastPeriod;
    }
}
