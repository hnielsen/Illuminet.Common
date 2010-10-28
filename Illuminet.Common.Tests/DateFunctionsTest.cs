using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Illuminet.Common.Tests
{
    [TestFixture]
    class DateFunctionsTest
    {
        [TestCase]
        public void Easter_in_2010_should_be_the_4th_april()
        {
            var easter = DateFunctions.GetEaster(2010);
            Assert.IsTrue(easter == new DateTime(2010, 4, 4));
        }

        [TestCase]
        public void Should_match_list_of_Danish_holidays_for_2010()
        {
            var holidays = DateFunctions.GetDanishHolidays(2010);
            foreach (var holiday in holidays)
            {
                var result = string.Format("Helligdag: {0}. Falder på datoen {1}", holiday.Key,
                                              holiday.Value.ToShortDateString());
                Console.WriteLine(result);
            }
            Assert.IsTrue(true);
        }

        [TestCase]
        public void Should_return_week_43_for_27102010()
        {
            var weeknr = DateFunctions.GetISOWeekInYear(new DateTime(2010, 10, 27));
            Assert.AreEqual(43, weeknr, "Ach! Mein Leben!");
        }

        [TestCase]
        public void Week_31_in_2010_should_start_02082010()
        {
            var weekStart = DateFunctions.GetStartOfISOWeekInYear(31, 2010);
            Assert.AreEqual(new DateTime(2010, 8, 2), weekStart);
        }

        [TestCase]
        public void AddWorkDays_variant_1()
        {
            var start = new DateTime(2010, 10, 4);
            var resultNoHolidays = DateFunctions.AddWorkDays(start, 3);
            Assert.AreEqual(new DateTime(2010, 10, 7), resultNoHolidays);
        }

        [TestCase]
        public void AddWorkDays_variant_2()
        {
            var start = new DateTime(2010, 10, 7);
            var resultNoHolidays = DateFunctions.AddWorkDays(start, 3);
            Assert.AreEqual(new DateTime(2010, 10, 12), resultNoHolidays);
        }

        [TestCase]
        public void AddWorkDays_variant_3()
        {
            // Testing Christmas 2010 with and without taking holidays into consideration

            var start1 = new DateTime(2010, 12, 20);
            var resultNoHolidays1 = DateFunctions.AddWorkDays(start1, 6, false);
            Assert.AreEqual(new DateTime(2010, 12, 28), resultNoHolidays1);

            var start2 = new DateTime(2010, 12, 20);
            var resultNoHolidays2 = DateFunctions.AddWorkDays(start2, 6, true);
            Assert.AreEqual(new DateTime(2010, 12, 29), resultNoHolidays2);
        }

        [TestCase]
        public void AddWorkDays_variant_4()
        {
            // Testing Easter 2011 with and without taking holidays into consideration

            var start1 = new DateTime(2011, 4, 20);
            var resultNoHolidays1 = DateFunctions.AddWorkDays(start1, 1, false);
            Assert.AreEqual(new DateTime(2011, 4, 21), resultNoHolidays1);

            var start2 = new DateTime(2011, 4, 20);
            var resultNoHolidays2 = DateFunctions.AddWorkDays(start2, 1, true);
            Assert.AreEqual(new DateTime(2011, 4, 26), resultNoHolidays2);
        }
    }
}
