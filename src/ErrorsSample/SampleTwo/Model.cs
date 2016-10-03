using System;
using ExpressiveAnnotations.Attributes;

namespace SampleTwo
{
    public enum Score
    {
        Low,
        High
    }

    public class Model
    {
        public bool GoAbroad { get; set; }

        [AssertThat("Level == Score.High")]
        public Score Level { get; set; }

        [RequiredIf("GoAbroad == true")]
        [AssertThat("IsDigitChain(passportNumber)")]
        public string PassportNumber { get; set; }

        [RequiredIf("GoAbroad = true")]
        [AssertThat("ReturnDate >= Today('asd')")]
        [AssertThat("ReturnDate >= Today() + WeekPeriod")]
        [AssertThat("ReturnDate < AddYears(Today(), 1)")]
        public DateTime? ReturnDate { get; set; }

        public DateTime AddYears(DateTime from, int years)
        {
            return from.AddYears(years);
        }

        public TimeSpan WeekPeriod => new TimeSpan(7, 0, 0, 0);
    }
}
