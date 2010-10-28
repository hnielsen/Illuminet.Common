/*
 * ------------------------------------------------------------------------------------------------
 * DateFunctions.cs
 * 
 * Author: Henrik Nielsen (nielsen.henrik@gmail.com)
 * Date: 06-03-2008 11:07
 * 
 * Purpose:
 * - Static class containing many date and business date functions not found in the DateTime class.
 * - Code lifted partially from http://channel9.msdn.com/ShowPost.aspx?PostID=147390
 * ------------------------------------------------------------------------------------------------
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CuttingEdge.Conditions;

namespace Illuminet.Common
{
    /// <summary>
    /// Static class containing many date and business date functions not found in the DateTime class.
    /// </summary>
    /// <exclude/>
    public static class DateFunctions
    {
        #region Constants

        private static Dictionary<string, int> _holidaysRelativeToEaster = new Dictionary<string, int>
                                                                       {
                                                                           {"Fastelavn", -49},
                                                                           {"Palmesøndag", -7},
                                                                           {"Skærtorsdag", -3},
                                                                           {"Langfredag", -2},
                                                                           {"Påskedag", 0},
                                                                           {"2. påskedag", 1},
                                                                           {"Store Bededag", 26},
                                                                           {"Kristi Himmelfart", 39},
                                                                           {"Pinsedag", 49},
                                                                           {"2. pinsedag", 50}
                                                                       };

        private static Dictionary<string, DateTime> _fixedHolidays = new Dictionary<string, DateTime>
                                                                         {
                                                                             {"Juleaften", new DateTime(1, 12, 24)},
                                                                             {"1. juledag", new DateTime(1, 12, 25)},
                                                                             {"2. juledag", new DateTime(1, 12, 26)},
                                                                             {"Nytårsaftensdag", new DateTime(1, 1, 1)},
                                                                             {"Grundlovsdag", new DateTime(1, 6, 5)},
                                                                         };
        #endregion

        /// <summary>
        /// Adds the number of specified work days to the value of the startDate instance.
        /// This will skip any holidays in the DateTime[] array.
        /// </summary>
        /// <param name="startDate">The date to add work days to.</param>
        /// <param name="workDays">Number of work days to add (positive or negative).</param>
        /// <returns>
        /// The first work day after skipping weekends and holidays.
        /// </returns>
        public static DateTime AddWorkDays(DateTime startDate, int workDays)
        {
            return AddWorkDays(startDate, workDays, null);
        }

        public static DateTime AddWorkDays(DateTime startDate, int workDays, bool useDanishHolidays)
        {
            DateTime[] holidays = useDanishHolidays
                                      ? GetDanishHolidays(startDate.Year).Select(h => h.Value).ToArray()
                                      : null;
            return AddWorkDays(startDate, workDays, holidays);
        }

        /// <summary>
        /// Adds the number of specified work days to the value of the startDate instance.
        /// This will skip any holidays in the DateTime[] array.
        /// </summary>
        /// <param name="startDate">The date to add work days to.</param>
        /// <param name="workDays">Number of work days to add (positive or negative).</param>
        /// <param name="holidays">Array of DateTimes to skip.</param>
        /// <returns>The first work day after skipping weekends and holidays.</returns>
        public static DateTime AddWorkDays(DateTime startDate, int workDays, DateTime[] holidays)
        {
            Condition.Requires(workDays, "workdays").IsGreaterThan(0);

            if (holidays == null)
                holidays = new DateTime[0];

            startDate = GetStartOfDay(startDate);
            DateTime endDate = startDate.AddDays(workDays + (2*(int) workDays/5) + holidays.Length + 5); // Make sure we have enough days.
            DateTime[] days = GetWorkDaysInRange(startDate, endDate, holidays);
            
            if (days[0] == startDate)
                return days[workDays];
            else
                return days[workDays - 1];
        }

        /// <summary>
        /// Returns a DateTime[] containing all the work days between two dates, excluding holidays.
        /// </summary>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <param name="holidays">Array of DateTimes to skip.</param>
        /// <returns>DateTime[] of all work days.</returns>
        private static DateTime[] GetWorkDaysInRange(DateTime startDate, DateTime endDate, DateTime[] holidays)
        {
            DayOfWeek[] dow = new DayOfWeek[] {DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday};
            DateTime[] days = GetDayOfWeekInRange(startDate, endDate, dow);

            //return days;

            if (holidays == null)
                holidays = new DateTime[0];

            //Remove any Holidays in that date range.
            List<DateTime> holidayList = new List<DateTime>(holidays);
            List<DateTime> list = new List<DateTime>();
            foreach (DateTime d in days)
            {
                // Add any date that is not a Holiday to new list.
                if (!holidayList.Exists(delegate(DateTime h) { return GetStartOfDay(h.Date) == d; }))
                    list.Add(d);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Returns all days matching DayOfWeek between two dates.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="dayOfWeek"></param>
        /// <returns>DateTime[] with all days matching DayOfWeek.</returns>
        private static DateTime[] GetDayOfWeekInRange(DateTime startDate, DateTime endDate, DayOfWeek dayOfWeek)
        {
            return GetDayOfWeekInRange(startDate, endDate, new DayOfWeek[] { dayOfWeek });
        }

        /// <summary>
        /// Returns all days matching any DayOfWeek in daysOfWeek array.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="daysOfWeek"></param>
        /// <returns>DateTime[] containing all matching days.</returns>
        private static DateTime[] GetDayOfWeekInRange(DateTime startDate, DateTime endDate, DayOfWeek[] daysOfWeek)
        {
            Condition.Requires(daysOfWeek, "week days").IsNotNull();
            Condition.Requires(startDate, "startdate").IsLessThan(endDate);

            ArrayList list = new ArrayList();
            DateTime curr = GetStartOfDay(startDate);
            DateTime end = GetStartOfDay(endDate);

            while (curr <= end)
            {
                foreach (DayOfWeek dow in daysOfWeek)
                {
                    if (curr.DayOfWeek == dow)
                        list.Add(curr);
                }
                curr = curr.AddDays(1);
            }
            return (DateTime[])list.ToArray(typeof(DateTime));
        }

        #region Helper methods

        /// <summary>
        /// Returns the start (or floor) of the minute in the date. This zeros seconds and milliseconds
        /// of the datetime instance.
        /// </summary>
        /// <param name="date">DateTime to operate on.</param>
        /// <returns>New DateTime at start of the minute.</returns>
        public static DateTime GetStartOfMinute(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, 0);
        }

        /// <summary>
        /// Returns the end (or ceiling) of the minute in the date. This makes seconds 59 and milliseconds 999
        /// of the datetime instance.  Adding one more millisecond or second would represent the next day. 
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetEndOfMinute(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 59, 999);
        }

        /// <summary>
        /// Returns the start (or floor) of the day represented by date.
        /// This zeros out the hours, minutes, seconds, and milliseconds of the date instance.
        /// </summary>
        /// <param name="date">DateTime to operate on.</param>
        /// <returns>DateTime representing the start of the day.</returns>
        public static DateTime GetStartOfDay(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0);
        }

        /// <summary>
        /// Returns the start of the day represented by year, month, day.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <returns>DateTime representing the start of the day.</returns>
        public static DateTime GetStartOfDay(int year, int month, int day)
        {
            Condition.Requires(year, "year").IsInRange(1, 9999);    // DateTime restriction
            Condition.Requires(month, "month").IsInRange(1, 12);
            Condition.Requires(day, "day").IsInRange(1, 366);      // Leap year
            
            return new DateTime(year, month, day);
        }

        /// <summary>
        /// Returns the end of the day represented by date.
        /// This maximizes the hours, minutes, seconds, and milliseconds of the date instance.
        /// </summary>
        /// <param name="date">DateTime to operate on.</param>
        /// <returns>DateTime representing the end of the day.</returns>
        public static DateTime GetEndOfDay(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, 999);
        }

        /// <summary>
        /// Returns the end of the day represented by year, month, and day.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <returns>DateTime representing the end of the day.</returns>
        public static DateTime GetEndOfDay(int year, int month, int day)
        {
            Condition.Requires(year, "year").IsInRange(1, 9999);    // DateTime restriction
            Condition.Requires(month, "month").IsInRange(1, 12);
            Condition.Requires(day, "day").IsInRange(1, 366);      // Leap year

            return new DateTime(year, month, day, 23, 59, 59, 999);
        }

        /// <summary>
        /// Returns the first day of the week which is the Sunday in the week represented by date.
        /// </summary>
        /// <param name="date">DateTime representing a day in the week.</param>
        /// <returns>DateTime of the Sunday in the week.</returns>
        public static DateTime GetStartOfWeek(DateTime date)
        {
            date = date.Date;
            // Get the Sunday of this week.
            DateTime day = date.AddDays(-((int) date.DayOfWeek));
            return day;
        }

        /// <summary>
        /// Returns the first day of the week which is the Monday in the week represented by date.
        /// </summary>
        /// <param name="date">DateTime representing a day in the week.</param>
        /// <returns>DateTime of the Monday in the week.</returns>
        public static DateTime GetStartOfWeekISO(DateTime date)
        {
            date = date.Date;
            int dow = (int) date.DayOfWeek;
            if (dow == 0)
                dow = 7;

            // Get the Monday of this week.
            DateTime day = date.AddDays(-(dow - 1));
            return day;
        }

        /// <summary>
        /// Returns the last day of the week which is the Saturday in the week represented by date.
        /// </summary>
        /// <param name="date">DateTime representing a day in the week.</param>
        /// <returns>DateTime of the Saturday in the week.</returns>
        public static DateTime GetEndOfWeek(DateTime date)
        {
            DateTime dt = GetStartOfWeek(date).AddDays(6);
            return GetEndOfDay(dt);
        }

        /// <summary>
        /// Returns the last day of the ISO week which is the Sunday in the week represented by date.
        /// </summary>
        /// <param name="date">DateTime representing a day in the week.</param>
        /// <returns>DateTime of the Sunday in the week.</returns>
        public static DateTime GetEndOfWeekISO(DateTime date)
        {
            DateTime dt = GetStartOfWeekISO(date).AddDays(6);
            return GetEndOfDay(dt);
        }

        /// <summary>
        /// Returns the week number of the year that date is within.
        /// </summary>
        /// <param name="date">DateTime representing a day in a week.</param>
        /// <returns>Week number.</returns>
        public static int GetWeekInYear(DateTime date)
        {
            int weekNum = 1;
            DateTime startOfWeek = GetStartOfWeek(GetStartOfYear(date));
            DateTime endOfWeek = GetEndOfWeek(startOfWeek);
            while (true)
            {
                if (date <= endOfWeek)
                    return weekNum;
                endOfWeek = endOfWeek.AddDays(7);
                weekNum++;
            }
        }

        /// <summary>
        /// Returns the ISO week number of the year that date is within. 
        /// </summary>
        /// <param name="date">DateTime representing a day in a week.</param>
        /// <returns>Week number.</returns>
        public static int GetISOWeekInYear(DateTime date)
        {
            int weekNum = 1;
            // Get the ISO week containing the 4th day of Jan.  This will always be the first ISO week of the year.
            DateTime startOfWeek = GetStartOfWeekISO(new DateTime(date.Year, 1, 4));

            if (date.Date < startOfWeek)
            {
                // Date is before the first Monday of the year.  Jan 1, 2005 and Jan 2 2005 are examples as they are in last week of 2004.
                return GetISOWeekInYear(new DateTime(date.Year - 1, 12, 31));
            }

            DateTime endOfWeek = GetEndOfWeekISO(startOfWeek);
            while (true)
            {
                if (date <= endOfWeek)
                    return weekNum;
                endOfWeek = endOfWeek.AddDays(7);
                weekNum++;
            }
        }

        public static DateTime GetStartOfISOWeekInYear(int week, int year)
        {
            Condition.Requires(week, "week").IsInRange(1, 53);
            Condition.Requires(year, "year").IsInRange(1, 9999);    // DateTime restriction

            DateTime startFirstWeek = GetStartOfWeekISO(new DateTime(year, 1, 4));
            return startFirstWeek.AddDays((week - 1)*7);
        }

        /// <summary>
        /// Returns a DateTime representing the first day of the month.
        /// </summary>
        /// <param name="date">DateTime representing a day in a month.</param>
        /// <returns>DateTime representing the first day of the month.</returns>
        public static DateTime GetStartOfMonth(DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1, 0, 0, 0, 0);
        }

        /// <summary>
        /// Returns a DateTime representing the first day of the month.
        /// </summary>
        /// <param name="year">Year number.</param>
        /// <param name="month">Month number.</param>
        /// <returns>DateTime representing the first day of the month.</returns>
        public static DateTime GetStartOfMonth(int year, int month)
        {
            Condition.Requires(year, "year").IsInRange(1, 9999);    // DateTime restriction
            Condition.Requires(month, "month").IsInRange(1, 12);

            return new DateTime(year, month, 1, 0, 0, 0, 0);
        }

        /// <summary>
        /// Returns a DateTime representing the last millisecond (i.e. 999) of the month.
        /// </summary>
        /// <param name="date"></param>
        /// <returns>DateTime representing the last millisecond of the month.</returns>
        public static DateTime GetEndOfMonth(DateTime date)
        {
            int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            return new DateTime(date.Year, date.Month, daysInMonth, 23, 59, 59, 999);
        }

        /// <summary>
        /// Gets the end of month.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <returns></returns>
        public static DateTime GetEndOfMonth(int year, int month)
        {
            int daysInMonth = DateTime.DaysInMonth(year, month);
            return new DateTime(year, month, daysInMonth, 23, 59, 59, 999);
        }

        public static DateTime GetStartOfYear(DateTime date)
        {
            return new DateTime(date.Year, 1, 1, 0, 0, 0, 0);
        }

        public static DateTime GetStartOfYear(int year)
        {
            return new DateTime(year, 1, 1, 0, 0, 0, 0);
        }

        public static DateTime GetEndOfYear(int year)
        {
            return new DateTime(year, 12, 31, 23, 59, 59, 999);
        }

        public static DateTime GetEndOfYear(DateTime date)
        {
            return GetEndOfYear(date.Year);
        }


        // Helper methods for calculating danish holidays

        /// <summary>
        /// Gets the date that Easter occurs on in a given year
        /// </summary>
        /// <remarks>
        /// See http://www.dayweekyear.com/faq_holidays_calculation.jsp
        /// See http://www.wordiq.com/definition/Computus#Meeus.2FJones.2FButcher_Gregorian_Algorithm
        /// </remarks>
        /// <param name="year"></param>
        /// <returns></returns>
        public static DateTime GetEaster(int year)
        {

            int Y = year;
            int a = Y % 19;
            int b = Y / 100;
            int c = Y % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int L = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * L) / 451;
            int month = (h + L - 7 * m + 114) / 31;
            int day = ((h + L - 7 * m + 114) % 31) + 1;

            return new DateTime(year, month, day);
        }

        /// <summary>
        /// Based on the dictionary of constants defined, returns the dates of the danish holidays
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        public static IDictionary<string, DateTime> GetDanishHolidays(int year)
        {
            var easterDependentHolidays = GetEasterDependentHolidays(year);

            var fixedHolidays = GetFixedHolidays(year);

            IDictionary<string, DateTime> allHolidays = easterDependentHolidays.Union(fixedHolidays).ToDictionary(pair => pair.Key, pair => pair.Value);
            
            return allHolidays;
        }

        public static IEnumerable<KeyValuePair<string, DateTime>> GetEasterDependentHolidays(int year)
        {
            var easter = GetEaster(year);
            return _holidaysRelativeToEaster.ToDictionary(h => h.Key, h => easter.AddDays(h.Value));
        }

        public static IEnumerable<KeyValuePair<string, DateTime>> GetFixedHolidays(int year)
        {
            return _fixedHolidays.ToDictionary(f => f.Key, f => f.Value.AddYears(year - 1));
        }

        #endregion
    }
}