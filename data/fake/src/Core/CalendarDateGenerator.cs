using System;

namespace fake;

internal static class CalendarDateGenerator
{
    internal static DateOnly ReferenceDate(DateTimeOffset reference)
    {
        return DateOnly.FromDateTime(reference.DateTime);
    }

    internal static int Age(DateOnly birthDate, DateOnly referenceDate)
    {
        if (birthDate > referenceDate)
        {
            throw new ArgumentOutOfRangeException(
                "birthDate",
                birthDate,
                "birthDate cannot be after the captured reference date.");
        }
        return AgeUnchecked(birthDate, referenceDate);
    }

    internal static DateOnly BirthDate(
        FakeRandom random,
        DateOnly referenceDate,
        int minimumAge,
        int maximumAge)
    {
        Guard.NonNegative(minimumAge, "minimumAge");
        if (maximumAge < minimumAge)
        {
            throw new ArgumentOutOfRangeException(
                "maximumAge",
                maximumAge,
                "maximumAge must be greater than or equal to minimumAge.");
        }
        int oldestRepresentableAge = AgeUnchecked(DateOnly.MinValue, referenceDate);
        if (minimumAge > oldestRepresentableAge)
        {
            throw new ArgumentOutOfRangeException(
                "minimumAge",
                minimumAge,
                "minimumAge cannot be represented at the captured reference date.");
        }
        int effectiveMaximumAge = maximumAge;
        if (effectiveMaximumAge > oldestRepresentableAge)
        {
            effectiveMaximumAge = oldestRepresentableAge;
        }
        int firstDay = FirstDayAtMostAge(referenceDate, effectiveMaximumAge);
        int lastDay = LastDayAtLeastAge(referenceDate, minimumAge);
        int dayNumber = random.IntFromMinimumAndMaximumAndMaximumInclusive(firstDay, lastDay, true);
        return DateOnly.FromDayNumber(dayNumber);
    }

    internal static DateOnly Anniversary(DateOnly originalDate, DateOnly referenceDate)
    {
        if (originalDate > referenceDate)
        {
            throw new ArgumentOutOfRangeException(
                "originalDate",
                originalDate,
                "originalDate cannot be after the captured reference date.");
        }
        int years = referenceDate.Year - originalDate.Year;
        DateOnly candidate = originalDate.AddYears(years);
        if (candidate < referenceDate)
        {
            if (originalDate.Year + years == DateOnly.MaxValue.Year)
            {
                throw FakeException.Create("No representable anniversary exists on or after the captured reference date.");
            }
            candidate = originalDate.AddYears(years + 1);
        }
        return candidate;
    }

    internal static DateOnly Anniversary(DateOnly originalDate, int years)
    {
        Guard.Positive(years, "years");
        if (years > DateOnly.MaxValue.Year - originalDate.Year)
        {
            throw new ArgumentOutOfRangeException(
                "years",
                years,
                "years would place the anniversary outside the representable calendar.");
        }
        return originalDate.AddYears(years);
    }

    internal static DateOnly Anniversary(
        FakeRandom random,
        DateOnly originalDate,
        int minimumYears,
        int maximumYears)
    {
        Guard.Positive(minimumYears, "minimumYears");
        if (maximumYears < minimumYears)
        {
            throw new ArgumentOutOfRangeException(
                "maximumYears",
                maximumYears,
                "maximumYears must be greater than or equal to minimumYears.");
        }
        int maximumRepresentableYears = DateOnly.MaxValue.Year - originalDate.Year;
        if (minimumYears > maximumRepresentableYears)
        {
            throw new ArgumentOutOfRangeException(
                "minimumYears",
                minimumYears,
                "minimumYears would place the anniversary outside the representable calendar.");
        }
        int effectiveMaximumYears = maximumYears;
        if (effectiveMaximumYears > maximumRepresentableYears)
        {
            effectiveMaximumYears = maximumRepresentableYears;
        }
        int years = random.IntFromMinimumAndMaximumAndMaximumInclusive(minimumYears, effectiveMaximumYears, true);
        return originalDate.AddYears(years);
    }

    internal static DateTimeOffset ExpiryDate(
        FakeRandom random,
        DateTimeOffset reference,
        TimeSpan minimumLead,
        TimeSpan maximumLead)
    {
        Positive(minimumLead, "minimumLead");
        Positive(maximumLead, "maximumLead");
        if (maximumLead < minimumLead)
        {
            throw new ArgumentOutOfRangeException(
                "maximumLead",
                maximumLead,
                "maximumLead must be greater than or equal to minimumLead.");
        }
        long available = DateTimeOffset.MaxValue.UtcTicks - reference.UtcTicks;
        if (minimumLead.Ticks > available)
        {
            throw FakeException.Create("No expiry instant in the requested lead range is representable after the captured reference time.");
        }
        long maximumTicks = maximumLead.Ticks;
        if (maximumTicks > available)
        {
            maximumTicks = available;
        }
        long leadTicks = random.LongFromMinimumAndMaximumAndMaximumInclusive(minimumLead.Ticks, maximumTicks, true);
        return new DateTimeOffset(reference.UtcTicks + leadTicks, TimeSpan.Zero);
    }

    internal static DateOnly CalendarDay(
        FakeRandom random,
        DateOnly minimum,
        DateOnly maximum,
        DateBoundary boundary,
        bool businessDay)
    {
        BoundedTemporalGenerator.ValidateDateOnlyRange(minimum, maximum, boundary);
        int firstDay = minimum.DayNumber;
        int lastDay = maximum.DayNumber;
        if (boundary == DateBoundary.Exclusive)
        {
            lastDay--;
        }
        int count = CountCalendarDays(firstDay, lastDay, businessDay);
        if (count == 0)
        {
            string kind = businessDay ? "business day" : "weekend day";
            throw FakeException.Create($"The requested interval contains no {kind}.");
        }
        int ordinal = random.IntFromMaximumExclusive(count);
        return DateOnly.FromDayNumber(CalendarDayAt(firstDay, ordinal, businessDay));
    }

    internal static FiscalPeriod CurrentFiscalPeriod(
        DateOnly referenceDate,
        int fiscalYearStartMonth)
    {
        ValidateMonth(fiscalYearStartMonth);
        int fiscalYear = referenceDate.Year;
        if (referenceDate.Month < fiscalYearStartMonth)
        {
            fiscalYear--;
        }
        if (fiscalYear < DateOnly.MinValue.Year)
        {
            throw FakeException.Create("The fiscal period begins before the representable calendar.");
        }
        int monthOffset = referenceDate.Month - fiscalYearStartMonth;
        if (monthOffset < 0)
        {
            monthOffset += 12;
        }
        int quarter = monthOffset / 3 + 1;
        return Fiscal(fiscalYear, quarter, fiscalYearStartMonth);
    }

    internal static FiscalPeriod Fiscal(
        int fiscalYear,
        int quarter,
        int fiscalYearStartMonth)
    {
        ValidateMonth(fiscalYearStartMonth);
        if (fiscalYear < DateOnly.MinValue.Year || fiscalYear > DateOnly.MaxValue.Year)
        {
            throw new ArgumentOutOfRangeException(
                "fiscalYear",
                fiscalYear,
                "fiscalYear must be within the representable calendar.");
        }
        if (quarter < 1 || quarter > 4)
        {
            throw new ArgumentOutOfRangeException(
                "quarter",
                quarter,
                "quarter must be between 1 and 4, inclusive.");
        }
        int monthIndex = fiscalYearStartMonth - 1 + (quarter - 1) * 3;
        int startYear = fiscalYear + monthIndex / 12;
        int startMonth = monthIndex % 12 + 1;
        if (startYear > DateOnly.MaxValue.Year)
        {
            throw new ArgumentOutOfRangeException(
                "quarter",
                quarter,
                "quarter begins outside the representable calendar.");
        }
        DateOnly start = new DateOnly(startYear, startMonth, 1);
        DateOnly end;
        if (start.Year == DateOnly.MaxValue.Year && start.Month >= 10)
        {
            end = DateOnly.MaxValue;
        }
        else
        {
            end = start.AddMonths(3).AddDays(-1);
        }
        return new FiscalPeriod(fiscalYear, quarter, start, end);
    }

    internal static DateInterval OrderedInterval(
        FakeRandom random,
        DateTimeOffset minimum,
        DateTimeOffset maximum)
    {
        BoundedTemporalGenerator.ValidateDateTimeOffsetRange(
            minimum,
            maximum,
            DateBoundary.Inclusive);
        if (minimum.UtcTicks == maximum.UtcTicks)
        {
            return new DateInterval(minimum, minimum);
        }
        DateTimeOffset first = BoundedTemporalGenerator.DateTimeOffset(
            random,
            minimum,
            maximum,
            DateBoundary.Inclusive);
        DateTimeOffset second = BoundedTemporalGenerator.DateTimeOffset(
            random,
            minimum,
            maximum,
            DateBoundary.Inclusive);
        if (first <= second)
        {
            return new DateInterval(first, second);
        }
        return new DateInterval(second, first);
    }

    internal static DateInterval OrderedInterval(
        FakeRandom random,
        DateTimeOffset minimum,
        DateTimeOffset maximum,
        TimeSpan minimumDuration,
        TimeSpan maximumDuration)
    {
        NonNegative(minimumDuration, "minimumDuration");
        NonNegative(maximumDuration, "maximumDuration");
        if (maximumDuration < minimumDuration)
        {
            throw new ArgumentOutOfRangeException(
                "maximumDuration",
                maximumDuration,
                "maximumDuration must be greater than or equal to minimumDuration.");
        }
        BoundedTemporalGenerator.ValidateDateTimeOffsetRange(
            minimum,
            maximum,
            DateBoundary.Inclusive);
        long available = maximum.UtcTicks - minimum.UtcTicks;
        if (minimumDuration.Ticks > available)
        {
            throw new ArgumentOutOfRangeException(
                "minimumDuration",
                minimumDuration,
                "minimumDuration exceeds the requested instant interval.");
        }
        long effectiveMaximumTicks = maximumDuration.Ticks;
        if (effectiveMaximumTicks > available)
        {
            effectiveMaximumTicks = available;
        }
        if (available == 0L)
        {
            return new DateInterval(minimum, minimum);
        }
        long durationTicks = random.LongFromMinimumAndMaximumAndMaximumInclusive(
            minimumDuration.Ticks,
            effectiveMaximumTicks,
            true);
        long latestStartTicks = maximum.UtcTicks - durationTicks;
        long startTicks = random.LongFromMinimumAndMaximumAndMaximumInclusive(
            minimum.UtcTicks,
            latestStartTicks,
            true);
        DateTimeOffset start = new DateTimeOffset(startTicks, TimeSpan.Zero);
        DateTimeOffset end = new DateTimeOffset(startTicks + durationTicks, TimeSpan.Zero);
        return new DateInterval(start, end);
    }

    static int AgeUnchecked(DateOnly birthDate, DateOnly referenceDate)
    {
        int age = referenceDate.Year - birthDate.Year;
        if (referenceDate.Month < birthDate.Month ||
            referenceDate.Month == birthDate.Month && referenceDate.Day < birthDate.Day)
        {
            age--;
        }
        return age;
    }

    static int FirstDayAtMostAge(DateOnly referenceDate, int maximumAge)
    {
        int low = DateOnly.MinValue.DayNumber;
        int high = referenceDate.DayNumber;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            int age = AgeUnchecked(DateOnly.FromDayNumber(middle), referenceDate);
            if (age > maximumAge)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }
        return low;
    }

    static int LastDayAtLeastAge(DateOnly referenceDate, int minimumAge)
    {
        int low = DateOnly.MinValue.DayNumber;
        int high = referenceDate.DayNumber;
        while (low < high)
        {
            int middle = low + (high - low + 1) / 2;
            int age = AgeUnchecked(DateOnly.FromDayNumber(middle), referenceDate);
            if (age >= minimumAge)
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }
        return low;
    }

    static int CountCalendarDays(int firstDay, int lastDay, bool businessDay)
    {
        int total = lastDay - firstDay + 1;
        int perWeek = businessDay ? 5 : 2;
        int fullWeeks = total / 7;
        int count = fullWeeks * perWeek;
        int index = fullWeeks * 7;
        while (index < total)
        {
            DateOnly date = DateOnly.FromDayNumber(firstDay + index);
            if (MatchesCalendarKind(date, businessDay))
            {
                count++;
            }
            index++;
        }
        return count;
    }

    static int CalendarDayAt(int firstDay, int ordinal, bool businessDay)
    {
        int perWeek = businessDay ? 5 : 2;
        int day = firstDay + ordinal / perWeek * 7;
        int remaining = ordinal % perWeek;
        while (true)
        {
            DateOnly candidate = DateOnly.FromDayNumber(day);
            if (MatchesCalendarKind(candidate, businessDay))
            {
                if (remaining == 0)
                {
                    return day;
                }
                remaining--;
            }
            day++;
        }
    }

    static bool MatchesCalendarKind(DateOnly value, bool businessDay)
    {
        bool weekend = value.DayOfWeek == DayOfWeek.Saturday ||
            value.DayOfWeek == DayOfWeek.Sunday;
        return businessDay ? !weekend : weekend;
    }

    static void ValidateMonth(int fiscalYearStartMonth)
    {
        if (fiscalYearStartMonth < 1 || fiscalYearStartMonth > 12)
        {
            throw new ArgumentOutOfRangeException(
                "fiscalYearStartMonth",
                fiscalYearStartMonth,
                "fiscalYearStartMonth must be between 1 and 12, inclusive.");
        }
    }

    static void Positive(TimeSpan value, string parameterName)
    {
        if (value.Ticks <= 0L)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{parameterName} must be greater than zero.");
        }
    }

    static void NonNegative(TimeSpan value, string parameterName)
    {
        if (value.Ticks < 0L)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{parameterName} must be zero or greater.");
        }
    }
}
