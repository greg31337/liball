using System;

namespace time;

/// <summary>Defines how calendar days are grouped into numbered week-years.</summary>
public class WeekRules : IWeekRules
{
    int firstDayOfWeek;
    int minimumDaysInFirstWeek;

    internal WeekRules(int firstDayOfWeek, int minimumDaysInFirstWeek)
    {
        TimeValidation.RequireIsoDayOfWeek(firstDayOfWeek, "firstDayOfWeek");
        RequireMinimumDaysInFirstWeek(minimumDaysInFirstWeek, "minimumDaysInFirstWeek");

        this.firstDayOfWeek = firstDayOfWeek;
        this.minimumDaysInFirstWeek = minimumDaysInFirstWeek;
    }

    /// <summary>Creates ISO week rules: Monday first and at least four days in the first week.</summary><returns>ISO-8601 week rules.</returns>
    internal static WeekRules Iso()
    {
        return new WeekRules(TimeConstants.IsoMonday, 4);
    }

    /// <summary>Creates custom week-year rules.</summary><param name="firstDayOfWeek">ISO weekday number 1 through 7.</param><param name="minimumDaysInFirstWeek">Required days from 1 through 7.</param><returns>The validated rules.</returns>
    internal static WeekRules Create(int firstDayOfWeek, int minimumDaysInFirstWeek)
    {
        return new WeekRules(firstDayOfWeek, minimumDaysInFirstWeek);
    }

    /// <summary>Gets the ISO weekday that begins each week.</summary><returns>A value from 1 through 7.</returns>
    public int GetFirstDayOfWeek()
    {
        return firstDayOfWeek;
    }

    /// <summary>Gets how many new-year days are required in week one.</summary><returns>A value from 1 through 7.</returns>
    public int GetMinimumDaysInFirstWeek()
    {
        return minimumDaysInFirstWeek;
    }

    /// <summary>Finds the first day of the week containing a date.</summary><param name="date">The date to locate.</param><returns>The same date or an earlier date on the configured first weekday.</returns>
    public LocalDate GetStartOfWeek(LocalDate date)
    {
        TimeValidation.RequireNotNull(date, "date");

        return date.SameOrPrevious(firstDayOfWeek);
    }

    /// <summary>Gets the week-based year containing a date.</summary><param name="date">The date to classify.</param><returns>The week-year, which can differ from the calendar year near January and December.</returns>
    public int GetWeekYear(LocalDate date)
    {
        TimeValidation.RequireNotNull(date, "date");

        int calendarYear = date.GetYear();
        LocalDate startOfThisWeekYear = GetStartOfWeekYear(calendarYear);
        if (date.CompareTo(startOfThisWeekYear) < 0)
        {
            return calendarYear - 1;
        }

        if (calendarYear < TimeConstants.MaxYear)
        {
            LocalDate startOfNextWeekYear = GetStartOfWeekYear(calendarYear + 1);
            if (date.CompareTo(startOfNextWeekYear) >= 0)
            {
                return calendarYear + 1;
            }
        }

        return calendarYear;
    }

    /// <summary>Gets the one-based week number within the date's week-year.</summary><param name="date">The date to classify.</param><returns>The week number.</returns>
    public int GetWeekOfWeekYear(LocalDate date)
    {
        TimeValidation.RequireNotNull(date, "date");

        int weekYear = GetWeekYear(date);
        LocalDate startOfWeekYear = GetStartOfWeekYear(weekYear);
        int daysSinceStart = date.GetDayNumber() - startOfWeekYear.GetDayNumber();

        return daysSinceStart / TimeConstants.DaysPerWeek + 1;
    }

    /// <summary>Finds the first configured weekday of a week-year.</summary><param name="weekYear">The requested week-based year.</param><returns>The first day of week one, which may lie in the preceding calendar year.</returns>
    public LocalDate GetStartOfWeekYear(int weekYear)
    {
        TimeValidation.RequireYear(weekYear, "weekYear");

        LocalDate firstDayOfYear = LocalDate.Create(weekYear, TimeConstants.MinMonth, TimeConstants.MinDay);
        int daysBeforeFirstDay = firstDayOfYear.GetDayOfWeek() - firstDayOfWeek;
        if (daysBeforeFirstDay < 0)
        {
            daysBeforeFirstDay = daysBeforeFirstDay + TimeConstants.DaysPerWeek;
        }

        int daysInNewYearWeek = TimeConstants.DaysPerWeek - daysBeforeFirstDay;
        int startOffset = -daysBeforeFirstDay;
        if (daysInNewYearWeek < minimumDaysInFirstWeek)
        {
            startOffset = TimeConstants.DaysPerWeek - daysBeforeFirstDay;
        }

        long startDayNumber = firstDayOfYear.GetDayNumber() + startOffset;
        if (startDayNumber < TimeConstants.MinDayNumber)
        {
            startDayNumber = TimeConstants.MinDayNumber;
        }

        return LocalDate.FromDayNumber((int)startDayNumber);
    }

    static void RequireMinimumDaysInFirstWeek(int minimumDaysInFirstWeek, string parameterName)
    {
        if (minimumDaysInFirstWeek < 1 || minimumDaysInFirstWeek > TimeConstants.DaysPerWeek)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Minimum days in first week must be in range 1 through 7.");
        }
    }
}
