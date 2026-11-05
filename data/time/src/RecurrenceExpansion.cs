using System;
using System.Collections.Generic;

namespace time;

internal static class RecurrenceExpansion
{
    internal static LocalDateTime GetNextCandidate(LocalDateTime candidate, RecurrenceRule rule)
    {
        TimeValidation.RequireNotNull(candidate, "candidate");
        TimeValidation.RequireNotNull(rule, "rule");

        return AddFrequency(candidate, rule.GetFrequency(), rule.GetInterval());
    }

    internal static LocalDateTime AddFrequency(LocalDateTime candidate, RecurrenceFrequency frequency, int interval)
    {
        TimeValidation.RequireNotNull(candidate, "candidate");
        RequirePositiveInterval(interval);

        return candidate.AddPeriod(CreateFrequencyPeriod(frequency, interval));
    }

    internal static bool MatchesFilters(LocalDateTime candidate, RecurrenceRule rule)
    {
        TimeValidation.RequireNotNull(candidate, "candidate");
        TimeValidation.RequireNotNull(rule, "rule");

        int[] bySecond = rule.GetBySecond();
        int[] byMinute = rule.GetByMinute();
        int[] byHour = rule.GetByHour();
        RecurrenceDay[] byDay = rule.GetByDay();
        int[] byMonthDay = rule.GetByMonthDay();
        int[] byYearDay = rule.GetByYearDay();
        int[] byWeekNo = rule.GetByWeekNo();
        int[] byMonth = rule.GetByMonth();

        return MatchesIntegerFilter(candidate.GetMonth(), byMonth)
            && MatchesWeekNo(candidate, rule.GetWeekStart(), byWeekNo)
            && MatchesYearDay(candidate, byYearDay)
            && MatchesMonthDay(candidate, byMonthDay)
            && MatchesByDay(candidate, rule.GetFrequency(), byMonth, byDay)
            && MatchesIntegerFilter(candidate.GetHour(), byHour)
            && MatchesIntegerFilter(candidate.GetMinute(), byMinute)
            && MatchesIntegerFilter(candidate.GetSecond(), bySecond);
    }

    internal static LocalDateTime[] FilterCandidates(LocalDateTime[] candidates, RecurrenceRule rule)
    {
        TimeValidation.RequireNotNull(candidates, "candidates");
        TimeValidation.RequireNotNull(rule, "rule");

        LocalDateTime[] filtered = new LocalDateTime[0];
        int index = 0;
        while (index < candidates.Length)
        {
            LocalDateTime candidate = candidates[index];
            if (candidate == null)
            {
                throw new ArgumentException("Candidate list must not contain null values.", "candidates");
            }

            if (MatchesFilters(candidate, rule))
            {
                filtered = AppendDateTime(filtered, candidate);
            }

            index = index + 1;
        }

        return ApplySetPosition(filtered, rule.GetBySetPos());
    }

    internal static LocalDateTime[] ExpandBounded(LocalDateTime start, RecurrenceRule rule, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(start, "start");
        TimeValidation.RequireNotNull(rule, "rule");
        RequirePositiveMaximumCandidates(maximumCandidates);

        LocalDateTime[] result = new LocalDateTime[0];
        LocalDateTime intervalStart = GetFirstIntervalStart(start, rule);
        int scanned = 0;
        while (scanned < maximumCandidates)
        {
            LocalDateTime[] candidates = GenerateIntervalCandidates(intervalStart, start, rule);
            int candidateIndex = 0;
            while (candidateIndex < candidates.Length)
            {
                LocalDateTime candidate = candidates[candidateIndex];
                if (candidate.CompareTo(start) >= 0 && IsWithinUntil(candidate, rule))
                {
                    result = AppendDateTime(result, candidate);
                    if (IsBoundedExpansionComplete(result.Length, rule, maximumCandidates))
                    {
                        return result;
                    }
                }

                candidateIndex = candidateIndex + 1;
            }

            scanned = scanned + 1;
            if (scanned < maximumCandidates)
            {
                if (!CanAdvanceInterval(intervalStart, rule))
                {
                    break;
                }

                intervalStart = GetNextIntervalStart(intervalStart, rule);
            }
        }

        return result;
    }

    static LocalDateTime GetFirstIntervalStart(LocalDateTime start, RecurrenceRule rule)
    {
        RecurrenceFrequency frequency = rule.GetFrequency();
        LocalDate date = start.GetDate();

        if (frequency == RecurrenceFrequency.Weekly)
        {
            int weekStartDayNumber = GetWeekStartDayNumber(date, rule.GetWeekStart());
            if (weekStartDayNumber < TimeConstants.MinDayNumber)
            {
                weekStartDayNumber = TimeConstants.MinDayNumber;
            }

            date = LocalDate.FromDayNumber(weekStartDayNumber);
        }
        else if (frequency == RecurrenceFrequency.Monthly)
        {
            date = LocalDate.Create(date.GetYear(), date.GetMonth(), TimeConstants.MinDay);
        }
        else if (frequency == RecurrenceFrequency.Yearly)
        {
            date = LocalDate.Create(date.GetYear(), TimeConstants.MinMonth, TimeConstants.MinDay);
        }

        return LocalDateTime.Create(date, start.GetTime());
    }

    static LocalDateTime GetNextIntervalStart(LocalDateTime intervalStart, RecurrenceRule rule)
    {
        if (rule.GetFrequency() == RecurrenceFrequency.Weekly)
        {
            return LocalDateTime.Create(
                intervalStart.GetDate().AddDays(GetWeeklyAdvanceDays(intervalStart.GetDate(), rule)),
                intervalStart.GetTime());
        }

        return GetNextCandidate(intervalStart, rule);
    }

    static LocalDateTime[] GenerateIntervalCandidates(LocalDateTime intervalStart, LocalDateTime start, RecurrenceRule rule)
    {
        LocalDate[] dates = GetIntervalDates(intervalStart, start, rule);
        RecurrenceFrequency frequency = rule.GetFrequency();
        int frequencyValue = (int)frequency;
        int[] hours = SelectComponentValues(intervalStart.GetHour(), rule.GetByHour(), frequencyValue >= (int)RecurrenceFrequency.Daily);
        int[] minutes = SelectComponentValues(intervalStart.GetMinute(), rule.GetByMinute(), frequencyValue >= (int)RecurrenceFrequency.Hourly);
        int[] seconds = SelectComponentValues(intervalStart.GetSecond(), rule.GetBySecond(), frequencyValue >= (int)RecurrenceFrequency.Minutely);

        List<LocalDateTime> candidateList = new List<LocalDateTime>();
        int dateIndex = 0;
        while (dateIndex < dates.Length)
        {
            int hourIndex = 0;
            while (hourIndex < hours.Length)
            {
                int minuteIndex = 0;
                while (minuteIndex < minutes.Length)
                {
                    int secondIndex = 0;
                    while (secondIndex < seconds.Length)
                    {
                        LocalDateTime candidate = LocalDateTime.Create(
                            dates[dateIndex].GetYear(),
                            dates[dateIndex].GetMonth(),
                            dates[dateIndex].GetDay(),
                            hours[hourIndex],
                            minutes[minuteIndex],
                            seconds[secondIndex],
                            start.GetMillisecond(),
                            start.GetMicrosecond(),
                            start.GetTickRemainder());

                        if (MatchesFilters(candidate, rule))
                        {
                            candidateList.Add(candidate);
                        }

                        secondIndex = secondIndex + 1;
                    }

                    minuteIndex = minuteIndex + 1;
                }

                hourIndex = hourIndex + 1;
            }

            dateIndex = dateIndex + 1;
        }

        LocalDateTime[] candidates = SortDateTimes(candidateList.ToArray());
        return ApplySetPosition(candidates, rule.GetBySetPos());
    }

    static LocalDate[] GetIntervalDates(LocalDateTime intervalStart, LocalDateTime start, RecurrenceRule rule)
    {
        RecurrenceFrequency frequency = rule.GetFrequency();
        if (frequency == RecurrenceFrequency.Secondly
            || frequency == RecurrenceFrequency.Minutely
            || frequency == RecurrenceFrequency.Hourly
            || frequency == RecurrenceFrequency.Daily)
        {
            return new LocalDate[] { intervalStart.GetDate() };
        }

        if (frequency == RecurrenceFrequency.Weekly)
        {
            return GetWeeklyDates(intervalStart.GetDate(), start, rule);
        }

        if (frequency == RecurrenceFrequency.Monthly)
        {
            return GetMonthlyDates(intervalStart.GetDate(), start, rule);
        }

        if (frequency == RecurrenceFrequency.Yearly)
        {
            return GetYearlyDates(intervalStart.GetYear(), start, rule);
        }

        throw new ArgumentOutOfRangeException("rule", "Recurrence frequency is not supported.");
    }

    static LocalDate[] GetWeeklyDates(LocalDate weekStart, LocalDateTime start, RecurrenceRule rule)
    {
        LocalDate[] dates = new LocalDate[0];
        RecurrenceDay[] byDay = rule.GetByDay();
        int intervalDayCount = GetWeeklyIntervalDayCount(weekStart, rule.GetWeekStart());
        int offset = 0;
        while (offset < intervalDayCount && (long)weekStart.GetDayNumber() + offset <= TimeConstants.MaxDayNumber)
        {
            LocalDate date = weekStart.AddDays(offset);
            if (byDay.Length > 0 || date.GetDayOfWeek() == start.GetDate().GetDayOfWeek())
            {
                dates = AppendDate(dates, date);
            }

            offset = offset + 1;
        }

        return dates;
    }

    static int GetWeeklyIntervalDayCount(LocalDate intervalStart, int weekStart)
    {
        if (intervalStart.GetDayOfWeek() == weekStart)
        {
            return TimeConstants.DaysPerWeek;
        }

        int daysUntilWeekStart = weekStart - intervalStart.GetDayOfWeek();
        if (daysUntilWeekStart <= 0)
        {
            daysUntilWeekStart = daysUntilWeekStart + TimeConstants.DaysPerWeek;
        }

        return daysUntilWeekStart;
    }

    static LocalDate[] GetMonthlyDates(LocalDate monthStart, LocalDateTime start, RecurrenceRule rule)
    {
        if (rule.GetByMonthDay().Length == 0 && rule.GetByDay().Length == 0)
        {
            int defaultDay = start.GetDay();
            if (defaultDay > monthStart.GetDaysInMonth())
            {
                return new LocalDate[0];
            }

            return new LocalDate[] { LocalDate.Create(monthStart.GetYear(), monthStart.GetMonth(), defaultDay) };
        }

        LocalDate[] dates = new LocalDate[0];
        int day = TimeConstants.MinDay;
        while (day <= monthStart.GetDaysInMonth())
        {
            dates = AppendDate(dates, LocalDate.Create(monthStart.GetYear(), monthStart.GetMonth(), day));
            day = day + 1;
        }

        return dates;
    }

    static LocalDate[] GetYearlyDates(int year, LocalDateTime start, RecurrenceRule rule)
    {
        int[] byMonth = rule.GetByMonth();
        int[] byWeekNo = rule.GetByWeekNo();
        int[] byYearDay = rule.GetByYearDay();
        int[] byMonthDay = rule.GetByMonthDay();
        RecurrenceDay[] byDay = rule.GetByDay();
        bool hasExplicitDay = byWeekNo.Length > 0 || byYearDay.Length > 0 || byMonthDay.Length > 0 || byDay.Length > 0;

        if (!hasExplicitDay && byMonth.Length == 0)
        {
            int month = start.GetMonth();
            int day = start.GetDay();
            if (day > CalendarMath.GetDaysInMonth(year, month))
            {
                return new LocalDate[0];
            }

            return new LocalDate[] { LocalDate.Create(year, month, day) };
        }

        if (!hasExplicitDay)
        {
            return GetYearlyMonthDefaultDates(year, start, byMonth);
        }

        LocalDate firstDate = LocalDate.Create(year, TimeConstants.MinMonth, TimeConstants.MinDay);
        LocalDate lastDate = LocalDate.Create(year, TimeConstants.MaxMonth, CalendarMath.GetDaysInMonth(year, TimeConstants.MaxMonth));
        if (byWeekNo.Length > 0)
        {
            WeekRules rules = WeekRules.Create(rule.GetWeekStart(), 4);
            firstDate = rules.GetStartOfWeekYear(year);
            if (year < TimeConstants.MaxYear)
            {
                lastDate = rules.GetStartOfWeekYear(year + 1).AddDays(-1L);
            }
        }

        LocalDate[] dates = new LocalDate[0];
        int dayNumber = firstDate.GetDayNumber();
        while (dayNumber <= lastDate.GetDayNumber())
        {
            LocalDate date = LocalDate.FromDayNumber(dayNumber);
            bool useDefaultWeekday = byWeekNo.Length > 0
                && byYearDay.Length == 0
                && byMonthDay.Length == 0
                && byDay.Length == 0;
            if (!useDefaultWeekday || date.GetDayOfWeek() == start.GetDate().GetDayOfWeek())
            {
                dates = AppendDate(dates, date);
            }

            dayNumber = dayNumber + 1;
        }

        return dates;
    }

    static LocalDate[] GetYearlyMonthDefaultDates(int year, LocalDateTime start, int[] byMonth)
    {
        int[] months = SortIntegers(byMonth);
        LocalDate[] dates = new LocalDate[0];
        int index = 0;
        while (index < months.Length)
        {
            int month = months[index];
            int day = start.GetDay();
            if (day <= CalendarMath.GetDaysInMonth(year, month))
            {
                LocalDate date = LocalDate.Create(year, month, day);
                if (!ContainsDate(dates, date))
                {
                    dates = AppendDate(dates, date);
                }
            }

            index = index + 1;
        }

        return dates;
    }

    static int[] SelectComponentValues(int defaultValue, int[] values, bool expands)
    {
        if (!expands || values.Length == 0)
        {
            return new int[] { defaultValue };
        }

        return SortUniqueIntegers(values);
    }

    static bool CanAdvanceInterval(LocalDateTime intervalStart, RecurrenceRule rule)
    {
        RecurrenceFrequency frequency = rule.GetFrequency();
        long interval = rule.GetInterval();
        if (frequency == RecurrenceFrequency.Secondly)
        {
            return CanAdvanceTicks(intervalStart, interval * TimeConstants.TicksPerSecond);
        }

        if (frequency == RecurrenceFrequency.Minutely)
        {
            return CanAdvanceTicks(intervalStart, interval * TimeConstants.TicksPerMinute);
        }

        if (frequency == RecurrenceFrequency.Hourly)
        {
            return CanAdvanceTicks(intervalStart, interval * TimeConstants.TicksPerHour);
        }

        if (frequency == RecurrenceFrequency.Daily)
        {
            return (long)intervalStart.GetDate().GetDayNumber() + interval <= TimeConstants.MaxDayNumber;
        }

        if (frequency == RecurrenceFrequency.Weekly)
        {
            return (long)intervalStart.GetDate().GetDayNumber() + GetWeeklyAdvanceDays(intervalStart.GetDate(), rule) <= TimeConstants.MaxDayNumber;
        }

        if (frequency == RecurrenceFrequency.Monthly)
        {
            long monthIndex = (long)(intervalStart.GetYear() - 1) * TimeConstants.MonthsPerYear + intervalStart.GetMonth() - 1L;
            long maximumMonthIndex = (long)TimeConstants.MaxYear * TimeConstants.MonthsPerYear - 1L;
            return monthIndex + interval <= maximumMonthIndex;
        }

        if (frequency == RecurrenceFrequency.Yearly)
        {
            return (long)intervalStart.GetYear() + interval <= TimeConstants.MaxYear;
        }

        throw new ArgumentOutOfRangeException("rule", "Recurrence frequency is not supported.");
    }

    static bool CanAdvanceTicks(LocalDateTime intervalStart, long tickDelta)
    {
        long maximumTicks = (long)TimeConstants.MaxDayNumber * TimeConstants.TicksPerDay + TimeConstants.MaxTicksSinceMidnight;
        return GetLocalTicks(intervalStart) <= maximumTicks - tickDelta;
    }

    static long GetWeeklyAdvanceDays(LocalDate intervalStart, RecurrenceRule rule)
    {
        long interval = rule.GetInterval();
        int weekStart = rule.GetWeekStart();
        if (intervalStart.GetDayOfWeek() == weekStart)
        {
            return interval * TimeConstants.DaysPerWeek;
        }

        return GetWeeklyIntervalDayCount(intervalStart, weekStart)
            + (interval - 1L) * TimeConstants.DaysPerWeek;
    }

    static int GetWeekStartDayNumber(LocalDate date, int weekStart)
    {
        int delta = date.GetDayOfWeek() - weekStart;
        if (delta < 0)
        {
            delta = delta + TimeConstants.DaysPerWeek;
        }

        return date.GetDayNumber() - delta;
    }

    internal static bool MatchesOccurrence(LocalDateTime start, LocalDateTime candidate, RecurrenceRule rule)
    {
        TimeValidation.RequireNotNull(start, "start");
        TimeValidation.RequireNotNull(candidate, "candidate");
        TimeValidation.RequireNotNull(rule, "rule");

        if (candidate.CompareTo(start) < 0)
        {
            return false;
        }

        return IsWithinUntil(candidate, rule)
            && IsOnInterval(start, candidate, rule)
            && MatchesFilters(candidate, rule);
    }

    internal static bool IsOnInterval(LocalDateTime start, LocalDateTime candidate, RecurrenceRule rule)
    {
        TimeValidation.RequireNotNull(start, "start");
        TimeValidation.RequireNotNull(candidate, "candidate");
        TimeValidation.RequireNotNull(rule, "rule");

        if (candidate.CompareTo(start) < 0)
        {
            return false;
        }

        RecurrenceFrequency frequency = rule.GetFrequency();
        int interval = rule.GetInterval();

        if (frequency == RecurrenceFrequency.Secondly)
        {
            return IsOnTickInterval(start, candidate, TimeConstants.TicksPerSecond, interval);
        }

        if (frequency == RecurrenceFrequency.Minutely)
        {
            return IsOnTickInterval(start, candidate, TimeConstants.TicksPerMinute, interval);
        }

        if (frequency == RecurrenceFrequency.Hourly)
        {
            return IsOnTickInterval(start, candidate, TimeConstants.TicksPerHour, interval);
        }

        if (frequency == RecurrenceFrequency.Daily)
        {
            return IsOnDayInterval(start, candidate, interval);
        }

        if (frequency == RecurrenceFrequency.Weekly)
        {
            return IsOnWeekInterval(start, candidate, rule.GetWeekStart(), interval);
        }

        if (frequency == RecurrenceFrequency.Monthly)
        {
            return IsOnMonthInterval(start, candidate, interval);
        }

        if (frequency == RecurrenceFrequency.Yearly)
        {
            return IsOnYearInterval(start, candidate, interval);
        }

        throw new ArgumentOutOfRangeException("rule", "Recurrence frequency is not supported.");
    }

    static Period CreateFrequencyPeriod(RecurrenceFrequency frequency, int interval)
    {
        long amount = interval;

        if (frequency == RecurrenceFrequency.Secondly)
        {
            return Period.FromSeconds(amount);
        }

        if (frequency == RecurrenceFrequency.Minutely)
        {
            return Period.FromMinutes(amount);
        }

        if (frequency == RecurrenceFrequency.Hourly)
        {
            return Period.FromHours(amount);
        }

        if (frequency == RecurrenceFrequency.Daily)
        {
            return Period.FromDays(amount);
        }

        if (frequency == RecurrenceFrequency.Weekly)
        {
            return Period.FromWeeks(amount);
        }

        if (frequency == RecurrenceFrequency.Monthly)
        {
            return Period.FromMonths(amount);
        }

        if (frequency == RecurrenceFrequency.Yearly)
        {
            return Period.FromYears(amount);
        }

        throw new ArgumentOutOfRangeException("frequency", "Recurrence frequency is not supported.");
    }

    static bool IsWithinUntil(LocalDateTime candidate, RecurrenceRule rule)
    {
        LocalDateTime? until = rule.GetLocalUntil();
        if (until == null)
        {
            return true;
        }

        return candidate.CompareTo(until) <= 0;
    }

    static bool IsBoundedExpansionComplete(int count, RecurrenceRule rule, int maximumCandidates)
    {
        if (count >= maximumCandidates)
        {
            return true;
        }

        int ruleCount = rule.GetCount();
        return ruleCount > 0 && count >= ruleCount;
    }

    static bool IsOnTickInterval(LocalDateTime start, LocalDateTime candidate, long unitTicks, int interval)
    {
        long tickDelta = GetLocalTicks(candidate) - GetLocalTicks(start);
        if (tickDelta % unitTicks != 0L)
        {
            return false;
        }

        long unitDelta = tickDelta / unitTicks;
        return unitDelta % interval == 0L;
    }

    static bool IsOnDayInterval(LocalDateTime start, LocalDateTime candidate, int interval)
    {
        int dayDelta = candidate.GetDate().GetDayNumber() - start.GetDate().GetDayNumber();
        return dayDelta % interval == 0;
    }

    static bool IsOnWeekInterval(LocalDateTime start, LocalDateTime candidate, int weekStart, int interval)
    {
        int startWeek = GetWeekStartDayNumber(start.GetDate(), weekStart);
        int candidateWeek = GetWeekStartDayNumber(candidate.GetDate(), weekStart);
        int dayDelta = candidateWeek - startWeek;
        int weekDelta = dayDelta / TimeConstants.DaysPerWeek;
        return weekDelta % interval == 0;
    }

    static bool IsOnMonthInterval(LocalDateTime start, LocalDateTime candidate, int interval)
    {
        int monthDelta = (candidate.GetYear() - start.GetYear()) * TimeConstants.MonthsPerYear + candidate.GetMonth() - start.GetMonth();
        return monthDelta % interval == 0;
    }

    static bool IsOnYearInterval(LocalDateTime start, LocalDateTime candidate, int interval)
    {
        int yearDelta = candidate.GetYear() - start.GetYear();
        return yearDelta % interval == 0;
    }

    static long GetLocalTicks(LocalDateTime candidate)
    {
        return (long)candidate.GetDate().GetDayNumber() * TimeConstants.TicksPerDay + candidate.GetTime().GetTicksSinceMidnight();
    }

    static bool MatchesIntegerFilter(int value, int[] allowedValues)
    {
        if (allowedValues.Length == 0)
        {
            return true;
        }

        return ContainsInteger(allowedValues, value);
    }

    static bool MatchesByDay(LocalDateTime candidate, RecurrenceFrequency frequency, int[] byMonth, RecurrenceDay[] byDay)
    {
        if (byDay.Length == 0)
        {
            return true;
        }

        int dayOfWeek = candidate.GetDate().GetDayOfWeek();
        int index = 0;
        while (index < byDay.Length)
        {
            RecurrenceDay day = byDay[index];
            if (day.GetDayOfWeek() == dayOfWeek && MatchesDayOrdinal(candidate, frequency, byMonth, day.GetOrdinal()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool MatchesDayOrdinal(LocalDateTime candidate, RecurrenceFrequency frequency, int[] byMonth, int ordinal)
    {
        if (ordinal == RecurrenceDay.NoOrdinal)
        {
            return true;
        }

        if (UsesYearDayOrdinal(frequency, byMonth))
        {
            return GetYearDayOrdinal(candidate) == ordinal;
        }

        return GetMonthDayOrdinal(candidate) == ordinal;
    }

    static bool UsesYearDayOrdinal(RecurrenceFrequency frequency, int[] byMonth)
    {
        return frequency == RecurrenceFrequency.Yearly && byMonth.Length == 0;
    }

    static int GetMonthDayOrdinal(LocalDateTime candidate)
    {
        LocalDate date = candidate.GetDate();
        int dayOfWeek = date.GetDayOfWeek();

        if (date.GetDay() <= date.GetDaysInMonth() / 2)
        {
            return CountWeekdaysInMonthRange(date.GetYear(), date.GetMonth(), dayOfWeek, 1, date.GetDay(), 1);
        }

        return -CountWeekdaysInMonthRange(date.GetYear(), date.GetMonth(), dayOfWeek, date.GetDaysInMonth(), date.GetDay(), -1);
    }

    static int GetYearDayOrdinal(LocalDateTime candidate)
    {
        LocalDate date = candidate.GetDate();
        int dayOfWeek = date.GetDayOfWeek();

        if (date.GetDayOfYear() <= date.GetDaysInYear() / 2)
        {
            return CountWeekdaysInYearRange(date.GetYear(), dayOfWeek, 1, date.GetDayOfYear(), 1);
        }

        return -CountWeekdaysInYearRange(date.GetYear(), dayOfWeek, date.GetDaysInYear(), date.GetDayOfYear(), -1);
    }

    static int CountWeekdaysInMonthRange(int year, int month, int dayOfWeek, int startDay, int endDay, int step)
    {
        int count = 0;
        int day = startDay;
        while (IsInRange(day, endDay, step))
        {
            LocalDate date = LocalDate.Create(year, month, day);
            if (date.GetDayOfWeek() == dayOfWeek)
            {
                count = count + 1;
            }

            day = day + step;
        }

        return count;
    }

    static int CountWeekdaysInYearRange(int year, int dayOfWeek, int startDayOfYear, int endDayOfYear, int step)
    {
        LocalDate yearStart = LocalDate.Create(year, TimeConstants.MinMonth, TimeConstants.MinDay);
        int count = 0;
        int dayOfYear = startDayOfYear;
        while (IsInRange(dayOfYear, endDayOfYear, step))
        {
            LocalDate date = yearStart.AddDays(dayOfYear - 1);
            if (date.GetDayOfWeek() == dayOfWeek)
            {
                count = count + 1;
            }

            dayOfYear = dayOfYear + step;
        }

        return count;
    }

    static bool IsInRange(int value, int end, int step)
    {
        if (step > 0)
        {
            return value <= end;
        }

        return value >= end;
    }

    static bool MatchesMonthDay(LocalDateTime candidate, int[] byMonthDay)
    {
        if (byMonthDay.Length == 0)
        {
            return true;
        }

        int day = candidate.GetDay();
        int daysInMonth = candidate.GetDate().GetDaysInMonth();
        int index = 0;
        while (index < byMonthDay.Length)
        {
            int normalizedDay = NormalizeNegativeOrdinal(byMonthDay[index], daysInMonth);
            if (day == normalizedDay)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool MatchesYearDay(LocalDateTime candidate, int[] byYearDay)
    {
        if (byYearDay.Length == 0)
        {
            return true;
        }

        int dayOfYear = candidate.GetDate().GetDayOfYear();
        int daysInYear = candidate.GetDate().GetDaysInYear();
        int index = 0;
        while (index < byYearDay.Length)
        {
            int normalizedDay = NormalizeNegativeOrdinal(byYearDay[index], daysInYear);
            if (dayOfYear == normalizedDay)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool MatchesWeekNo(LocalDateTime candidate, int weekStart, int[] byWeekNo)
    {
        if (byWeekNo.Length == 0)
        {
            return true;
        }

        WeekRules rules = WeekRules.Create(weekStart, 4);
        LocalDate date = candidate.GetDate();
        int weekYear = rules.GetWeekYear(date);
        int week = rules.GetWeekOfWeekYear(date);
        int weeksInWeekYear = GetWeeksInWeekYear(rules, weekYear);

        int index = 0;
        while (index < byWeekNo.Length)
        {
            int normalizedWeek = NormalizeNegativeOrdinal(byWeekNo[index], weeksInWeekYear);
            if (week == normalizedWeek)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static int GetWeeksInWeekYear(WeekRules rules, int weekYear)
    {
        if (weekYear < TimeConstants.MaxYear)
        {
            LocalDate start = rules.GetStartOfWeekYear(weekYear);
            LocalDate nextStart = rules.GetStartOfWeekYear(weekYear + 1);
            return (nextStart.GetDayNumber() - start.GetDayNumber()) / TimeConstants.DaysPerWeek;
        }

        LocalDate lastDay = LocalDate.Create(TimeConstants.MaxYear, TimeConstants.MaxMonth, CalendarMath.GetDaysInMonth(TimeConstants.MaxYear, TimeConstants.MaxMonth));
        return rules.GetWeekOfWeekYear(lastDay);
    }

    static int NormalizeNegativeOrdinal(int value, int length)
    {
        if (value < 0)
        {
            return length + value + 1;
        }

        return value;
    }

    static LocalDateTime[] ApplySetPosition(LocalDateTime[] candidates, int[] bySetPos)
    {
        if (bySetPos.Length == 0)
        {
            return candidates;
        }

        LocalDateTime[] orderedCandidates = SortDateTimes(candidates);
        LocalDateTime[] result = new LocalDateTime[0];
        int index = 0;
        while (index < bySetPos.Length)
        {
            int candidateIndex = GetSetPositionIndex(bySetPos[index], orderedCandidates.Length);
            if (candidateIndex >= 0 && candidateIndex < orderedCandidates.Length && !ContainsDateTime(result, orderedCandidates[candidateIndex]))
            {
                result = AppendDateTime(result, orderedCandidates[candidateIndex]);
            }

            index = index + 1;
        }

        return SortDateTimes(result);
    }

    static int GetSetPositionIndex(int setPosition, int count)
    {
        if (setPosition < 0)
        {
            return count + setPosition;
        }

        return setPosition - 1;
    }

    static bool ContainsInteger(int[] values, int value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool ContainsDateTime(LocalDateTime[] values, LocalDateTime value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].EqualsValue(value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool ContainsDate(LocalDate[] values, LocalDate value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].EqualsValue(value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static int[] SortIntegers(int[] values)
    {
        int[] result = new int[values.Length];
        Array.Copy(values, result, values.Length);

        int index = 1;
        while (index < result.Length)
        {
            int value = result[index];
            int insertionIndex = index;
            while (insertionIndex > 0 && result[insertionIndex - 1] > value)
            {
                result[insertionIndex] = result[insertionIndex - 1];
                insertionIndex = insertionIndex - 1;
            }

            result[insertionIndex] = value;
            index = index + 1;
        }

        return result;
    }

    static int[] SortUniqueIntegers(int[] values)
    {
        int[] sorted = SortIntegers(values);
        if (sorted.Length < 2)
        {
            return sorted;
        }

        int uniqueCount = 1;
        int index = 1;
        while (index < sorted.Length)
        {
            if (sorted[index] != sorted[index - 1])
            {
                uniqueCount = uniqueCount + 1;
            }

            index = index + 1;
        }

        int[] result = new int[uniqueCount];
        result[0] = sorted[0];
        int resultIndex = 1;
        index = 1;
        while (index < sorted.Length)
        {
            if (sorted[index] != sorted[index - 1])
            {
                result[resultIndex] = sorted[index];
                resultIndex = resultIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    static LocalDateTime[] SortDateTimes(LocalDateTime[] values)
    {
        LocalDateTime[] result = new LocalDateTime[values.Length];
        Array.Copy(values, result, values.Length);

        int index = 1;
        while (index < result.Length)
        {
            LocalDateTime value = result[index];
            int insertionIndex = index;
            while (insertionIndex > 0 && result[insertionIndex - 1].CompareTo(value) > 0)
            {
                result[insertionIndex] = result[insertionIndex - 1];
                insertionIndex = insertionIndex - 1;
            }

            result[insertionIndex] = value;
            index = index + 1;
        }

        return result;
    }

    static LocalDate[] AppendDate(LocalDate[] values, LocalDate value)
    {
        LocalDate[] result = new LocalDate[values.Length + 1];
        Array.Copy(values, result, values.Length);
        result[values.Length] = value;
        return result;
    }

    static LocalDateTime[] AppendDateTime(LocalDateTime[] values, LocalDateTime value)
    {
        LocalDateTime[] result = new LocalDateTime[values.Length + 1];
        Array.Copy(values, result, values.Length);
        result[values.Length] = value;
        return result;
    }

    static void RequirePositiveInterval(int interval)
    {
        if (interval <= 0)
        {
            throw new ArgumentOutOfRangeException("interval", "Recurrence interval must be positive.");
        }
    }

    static void RequirePositiveMaximumCandidates(int maximumCandidates)
    {
        if (maximumCandidates <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumCandidates", "Maximum candidate count must be positive.");
        }
    }
}
