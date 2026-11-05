using System;

namespace currencycodes;

internal static class PartialDateRules
{
    internal static bool HasKnownPrecision(PartialDate date)
    {
        if (!date.IsKnown)
        {
            return false;
        }

        return date.Precision == PartialDatePrecision.Year
            || date.Precision == PartialDatePrecision.Month
            || date.Precision == PartialDatePrecision.Day;
    }

    internal static DateOnly NormalizeInclusiveStartDate(PartialDate date)
    {
        if (!HasKnownPrecision(date))
        {
            throw new InvalidOperationException("An unknown partial date has no inclusive start date.");
        }

        switch (date.Precision)
        {
            case PartialDatePrecision.Year:
                return new DateOnly(date.Year, 1, 1);
            case PartialDatePrecision.Month:
                return new DateOnly(date.Year, date.Month, 1);
            case PartialDatePrecision.Day:
                return new DateOnly(date.Year, date.Month, date.Day);
            default:
                throw new InvalidOperationException("An unknown partial date has no inclusive start date.");
        }
    }

    internal static DateOnly NormalizeInclusiveEndDate(PartialDate date)
    {
        if (!HasKnownPrecision(date))
        {
            throw new InvalidOperationException("An unknown partial date has no inclusive end date.");
        }

        switch (date.Precision)
        {
            case PartialDatePrecision.Year:
                return new DateOnly(date.Year, 12, 31);
            case PartialDatePrecision.Month:
                return new DateOnly(
                    date.Year,
                    date.Month,
                    DateTime.DaysInMonth(date.Year, date.Month));
            case PartialDatePrecision.Day:
                return new DateOnly(date.Year, date.Month, date.Day);
            default:
                throw new InvalidOperationException("An unknown partial date has no inclusive end date.");
        }
    }

    internal static int CompareDateToInclusiveStart(DateOnly date, PartialDate startDate)
    {
        DateOnly normalizedStart = NormalizeInclusiveStartDate(startDate);
        return date.CompareTo(normalizedStart);
    }

    internal static int CompareDateToInclusiveEnd(DateOnly date, PartialDate endDate)
    {
        DateOnly normalizedEnd = NormalizeInclusiveEndDate(endDate);
        return date.CompareTo(normalizedEnd);
    }

    internal static bool IsWithinInclusiveBounds(
        DateOnly date,
        bool hasStartDate,
        DateOnly startDate,
        bool hasEndDate,
        DateOnly endDate)
    {
        if (hasStartDate && date < startDate)
        {
            return false;
        }

        if (hasEndDate && date > endDate)
        {
            return false;
        }

        return true;
    }
}
