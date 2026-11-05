using System;

namespace time;

/// <summary>Provides bounded recurrence expansion as either arrays or a simple stateful local enumerator.</summary>
/// <remarks>The maximum-candidate argument is a required safety bound because a recurrence rule may otherwise be unbounded.</remarks>
public class RecurrenceEnumerator : IRecurrenceEnumerator
{
    LocalDateTime[] occurrences;
    int index;

    internal RecurrenceEnumerator(RecurrenceSet set, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(set, "set");

        occurrences = set.ExpandBounded(maximumCandidates);
        index = -1;
    }

    /// <summary>Creates a stateful enumerator over expanded local occurrences.</summary><param name="set">The recurrence set to expand.</param><param name="maximumCandidates">A positive expansion and result bound.</param><returns>An enumerator positioned before the first item.</returns>
    internal static RecurrenceEnumerator CreateLocal(RecurrenceSet set, int maximumCandidates)
    {
        return new RecurrenceEnumerator(set, maximumCandidates);
    }

    /// <summary>Expands a recurrence set as floating local values.</summary><param name="set">The set to expand.</param><param name="maximumCandidates">A positive expansion and result bound.</param><returns>Sorted local occurrences.</returns>
    internal static LocalDateTime[] EnumerateLocal(RecurrenceSet set, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(set, "set");

        return set.ExpandBounded(maximumCandidates);
    }

    /// <summary>Expands one rule from a local DTSTART value.</summary><param name="start">The recurrence anchor.</param><param name="rule">The rule to apply.</param><param name="maximumCandidates">A positive expansion and result bound.</param><returns>Matching local occurrences.</returns>
    internal static LocalDateTime[] EnumerateLocal(LocalDateTime start, RecurrenceRule rule, int maximumCandidates)
    {
        RecurrenceSet set = RecurrenceSet.Create(start);
        set.AddRule(rule);
        return set.ExpandBounded(maximumCandidates);
    }

    /// <summary>Expands a local recurrence set through an inclusive local end value.</summary><param name="set">The set to expand.</param><param name="end">The final local value that can be returned.</param><param name="maximumCandidates">A positive expansion and result bound.</param><returns>Sorted bounded local occurrences.</returns>
    internal static LocalDateTime[] EnumerateLocalThrough(RecurrenceSet set, LocalDateTime end, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(set, "set");

        return set.ExpandThrough(end, maximumCandidates);
    }

    /// <summary>Expands one local rule through an inclusive local end value.</summary><param name="start">The recurrence anchor.</param><param name="rule">The rule to apply.</param><param name="end">The final local value that can be returned.</param><param name="maximumCandidates">A positive expansion and result bound.</param><returns>Sorted bounded local occurrences.</returns>
    internal static LocalDateTime[] EnumerateLocalRuleThrough(LocalDateTime start, RecurrenceRule rule, LocalDateTime end, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(start, "start");
        TimeValidation.RequireNotNull(rule, "rule");
        TimeValidation.RequireNotNull(end, "end");

        RecurrenceSet set = RecurrenceSet.Create(start);
        set.AddRule(rule);
        return set.ExpandThrough(end, maximumCandidates);
    }

    /// <summary>Expands local occurrences and resolves each in a zone, preserving wall-clock intent.</summary><param name="set">The local recurrence set.</param><param name="zone">The zone used for every occurrence.</param><param name="resolver">The gap and overlap policy.</param><param name="maximumCandidates">A positive expansion and result bound.</param><returns>Zoned occurrences.</returns>
    internal static ZonedDateTime[] EnumerateZoned(RecurrenceSet set, TimeZoneRef zone, IZoneResolver resolver, int maximumCandidates)
    {
        return EnumerateZonedSetCore(set, zone, resolver, null, maximumCandidates);
    }

    /// <summary>Expands one local rule and resolves each result in a zone.</summary><param name="start">The local DTSTART value.</param><param name="rule">The rule to apply.</param><param name="zone">The target zone.</param><param name="resolver">The transition policy.</param><param name="maximumCandidates">A positive expansion and result bound.</param><returns>Zoned occurrences.</returns>
    internal static ZonedDateTime[] EnumerateZoned(LocalDateTime start, RecurrenceRule rule, TimeZoneRef zone, IZoneResolver resolver, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(start, "start");
        TimeValidation.RequireNotNull(rule, "rule");

        RecurrenceSet set = RecurrenceSet.Create(start);
        set.AddRule(rule);
        return EnumerateZonedSetCore(set, zone, resolver, null, maximumCandidates);
    }

    /// <summary>Expands a zoned recurrence set through an inclusive instant end value.</summary><param name="set">The local recurrence set.</param><param name="zone">The zone used for every occurrence.</param><param name="resolver">The gap and overlap policy.</param><param name="end">The final instant that can be returned.</param><param name="maximumCandidates">A positive expansion and result bound.</param><returns>Sorted bounded zoned occurrences.</returns>
    internal static ZonedDateTime[] EnumerateZonedThrough(RecurrenceSet set, TimeZoneRef zone, IZoneResolver resolver, Instant end, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(end, "end");

        return EnumerateZonedSetCore(set, zone, resolver, end, maximumCandidates);
    }

    /// <summary>Expands one zoned recurrence rule through an inclusive instant end value.</summary><param name="start">The local recurrence anchor.</param><param name="rule">The rule to apply.</param><param name="zone">The target zone.</param><param name="resolver">The transition policy.</param><param name="end">The final instant that can be returned.</param><param name="maximumCandidates">A positive expansion and result bound.</param><returns>Sorted bounded zoned occurrences.</returns>
    internal static ZonedDateTime[] EnumerateZonedRuleThrough(LocalDateTime start, RecurrenceRule rule, TimeZoneRef zone, IZoneResolver resolver, Instant end, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(start, "start");
        TimeValidation.RequireNotNull(rule, "rule");
        TimeValidation.RequireNotNull(end, "end");

        RecurrenceSet set = RecurrenceSet.Create(start);
        set.AddRule(rule);
        return EnumerateZonedSetCore(set, zone, resolver, end, maximumCandidates);
    }

    /// <summary>Resolves an existing local occurrence array in one zone.</summary><param name="localOccurrences">Non-null floating local values.</param><param name="zone">The target zone.</param><param name="resolver">The gap and overlap policy.</param><returns>A same-length zoned array in input order.</returns>
    internal static ZonedDateTime[] MapLocalToZoned(LocalDateTime[] localOccurrences, TimeZoneRef zone, IZoneResolver resolver)
    {
        TimeValidation.RequireNotNull(localOccurrences, "localOccurrences");
        TimeValidation.RequireNotNull(zone, "zone");
        TimeValidation.RequireNotNull(resolver, "resolver");
        RequireLocalOccurrences(localOccurrences);

        ZonedDateTime[] result = new ZonedDateTime[localOccurrences.Length];
        int index = 0;
        while (index < localOccurrences.Length)
        {
            result[index] = ZonedDateTime.CreateFromLocal(localOccurrences[index], zone, resolver);
            index = index + 1;
        }

        return result;
    }

    /// <summary>Expands secondly through weekly recurrence directly on the UTC timeline.</summary><remarks>Daily and weekly intervals use fixed 24-hour and seven-day UTC steps. Monthly and yearly frequencies are rejected because their lengths depend on a calendar.</remarks><param name="start">The first timeline candidate.</param><param name="rule">The exact-frequency rule.</param><param name="maximumCandidates">A positive expansion and result bound.</param><returns>Matching instants.</returns>
    internal static Instant[] EnumerateInstants(Instant start, RecurrenceRule rule, int maximumCandidates)
    {
        return EnumerateInstantCore(start, rule, null, maximumCandidates);
    }

    /// <summary>Expands an exact recurrence through an inclusive instant end value.</summary><param name="start">The DTSTART instant.</param><param name="rule">The exact-frequency rule.</param><param name="end">The final instant that can be returned.</param><param name="maximumCandidates">A positive expansion and result bound.</param><returns>Bounded exact occurrences.</returns>
    internal static Instant[] EnumerateInstantsThrough(Instant start, RecurrenceRule rule, Instant end, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(end, "end");

        return EnumerateInstantCore(start, rule, end, maximumCandidates);
    }

    /// <summary>Gets the total number of precomputed local occurrences.</summary><returns>The array length, independent of the current position.</returns>
    public int GetCount()
    {
        return occurrences.Length;
    }

    /// <summary>Advances to the next local occurrence.</summary><returns><c>true</c> when a current item is available; <c>false</c> after the final item.</returns>
    public bool MoveNext()
    {
        if (index + 1 >= occurrences.Length)
        {
            index = occurrences.Length;
            return false;
        }

        index = index + 1;
        return true;
    }

    /// <summary>Gets the current local occurrence.</summary><returns>The item selected by the latest successful move.</returns><exception cref="InvalidOperationException">The enumerator is before the first item or after the final item.</exception>
    public LocalDateTime GetCurrent()
    {
        if (index < 0 || index >= occurrences.Length)
        {
            throw new InvalidOperationException("Enumerator is not positioned on an occurrence.");
        }

        return occurrences[index];
    }

    /// <summary>Moves the enumerator back before the first occurrence.</summary>
    public void Reset()
    {
        index = -1;
    }

    /// <summary>Gets a copy of every precomputed local occurrence.</summary><returns>A new array independent of enumerator position.</returns>
    public LocalDateTime[] ToArray()
    {
        return CopyDateTimeArray(occurrences);
    }

    static ZonedDateTime[] EnumerateZonedSetCore(RecurrenceSet set, TimeZoneRef zone, IZoneResolver resolver, Instant? end, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(set, "set");
        TimeValidation.RequireNotNull(zone, "zone");
        TimeValidation.RequireNotNull(resolver, "resolver");
        RequirePositiveMaximumCandidates(maximumCandidates);

        RecurrenceRule[] rules = set.GetRules();
        int validationIndex = 0;
        while (validationIndex < rules.Length)
        {
            rules[validationIndex].RequireUtcStart();
            validationIndex = validationIndex + 1;
        }

        LocalDateTime[] exclusions = set.GetExDates();
        ZonedDateTime[] result = new ZonedDateTime[0];
        result = AddZonedCandidate(result, set.GetStart(), exclusions, zone, resolver, null, end);

        int ruleIndex = 0;
        while (ruleIndex < rules.Length)
        {
            LocalDateTime[] ruleDates = set.ExpandRuleCandidates(rules[ruleIndex], maximumCandidates);
            int dateIndex = 0;
            while (dateIndex < ruleDates.Length)
            {
                result = AddZonedCandidate(result, ruleDates[dateIndex], exclusions, zone, resolver, rules[ruleIndex].GetUntil(), end);
                dateIndex = dateIndex + 1;
            }

            ruleIndex = ruleIndex + 1;
        }

        LocalDateTime[] rDates = set.GetRDates();
        int rDateIndex = 0;
        while (rDateIndex < rDates.Length)
        {
            result = AddZonedCandidate(result, rDates[rDateIndex], exclusions, zone, resolver, null, end);
            rDateIndex = rDateIndex + 1;
        }

        result = SortZonedDateTimes(result);
        return LimitZonedDateTimes(result, maximumCandidates);
    }

    static ZonedDateTime[] AddZonedCandidate(
        ZonedDateTime[] values,
        LocalDateTime candidate,
        LocalDateTime[] exclusions,
        TimeZoneRef zone,
        IZoneResolver resolver,
        Instant? ruleEnd,
        Instant? callerEnd)
    {
        if (ContainsLocalDateTime(exclusions, candidate))
        {
            return values;
        }

        ZonedDateTime zoned = ZonedDateTime.CreateFromLocal(candidate, zone, resolver);
        Instant instant = zoned.GetInstant();
        if (ruleEnd != null && instant.CompareTo(ruleEnd) > 0)
        {
            return values;
        }

        if (callerEnd != null && instant.CompareTo(callerEnd) > 0)
        {
            return values;
        }

        if (ContainsZonedInstant(values, instant))
        {
            return values;
        }

        return AppendZonedDateTime(values, zoned);
    }

    static Instant[] EnumerateInstantCore(Instant start, RecurrenceRule rule, Instant? end, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(start, "start");
        TimeValidation.RequireNotNull(rule, "rule");
        RequirePositiveMaximumCandidates(maximumCandidates);
        rule.RequireUtcStart();
        RequireFixedInstantFrequency(rule);

        Instant[] result = new Instant[0];
        if (end != null && start.CompareTo(end) > 0)
        {
            return result;
        }

        result = AppendInstant(result, start);
        LocalDateTime utcStart = LocalDateTime.FromDateTime(start.ToUtcDateTime());
        RecurrenceSet set = RecurrenceSet.Create(utcStart);
        LocalDateTime[] localCandidates = set.ExpandRuleCandidates(rule, maximumCandidates);
        int index = 0;
        while (index < localCandidates.Length)
        {
            Instant candidate = Instant.FromUtcTicks(localCandidates[index].ToDateTimeUnspecified().Ticks);
            Instant? until = rule.GetUntil();

            if ((until == null || candidate.CompareTo(until) <= 0)
                && (end == null || candidate.CompareTo(end) <= 0)
                && !ContainsInstant(result, candidate))
            {
                result = AppendInstant(result, candidate);
            }

            index = index + 1;
        }

        return LimitInstants(result, maximumCandidates);
    }

    static void RequireFixedInstantFrequency(RecurrenceRule rule)
    {
        RecurrenceFrequency frequency = rule.GetFrequency();
        if (frequency != RecurrenceFrequency.Secondly
            && frequency != RecurrenceFrequency.Minutely
            && frequency != RecurrenceFrequency.Hourly
            && frequency != RecurrenceFrequency.Daily
            && frequency != RecurrenceFrequency.Weekly)
        {
            throw new ArgumentOutOfRangeException("rule", "Instant recurrence frequency must have a fixed elapsed duration.");
        }
    }

    static LocalDateTime[] CopyDateTimeArray(LocalDateTime[] values)
    {
        LocalDateTime[] result = new LocalDateTime[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    static Instant[] AppendInstant(Instant[] values, Instant value)
    {
        Instant[] result = new Instant[values.Length + 1];
        Array.Copy(values, result, values.Length);
        result[values.Length] = value;
        return result;
    }

    static bool ContainsInstant(Instant[] values, Instant value)
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

    static Instant[] LimitInstants(Instant[] values, int maximumCandidates)
    {
        if (values.Length <= maximumCandidates)
        {
            return values;
        }

        Instant[] result = new Instant[maximumCandidates];
        Array.Copy(values, result, maximumCandidates);
        return result;
    }

    static ZonedDateTime[] AppendZonedDateTime(ZonedDateTime[] values, ZonedDateTime value)
    {
        ZonedDateTime[] result = new ZonedDateTime[values.Length + 1];
        Array.Copy(values, result, values.Length);
        result[values.Length] = value;
        return result;
    }

    static bool ContainsLocalDateTime(LocalDateTime[] values, LocalDateTime value)
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

    static bool ContainsZonedInstant(ZonedDateTime[] values, Instant instant)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].GetInstant().EqualsValue(instant))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static ZonedDateTime[] SortZonedDateTimes(ZonedDateTime[] values)
    {
        ZonedDateTime[] result = new ZonedDateTime[values.Length];
        Array.Copy(values, result, values.Length);

        int index = 1;
        while (index < result.Length)
        {
            ZonedDateTime value = result[index];
            int insertionIndex = index;
            while (insertionIndex > 0 && result[insertionIndex - 1].GetInstant().CompareTo(value.GetInstant()) > 0)
            {
                result[insertionIndex] = result[insertionIndex - 1];
                insertionIndex = insertionIndex - 1;
            }

            result[insertionIndex] = value;
            index = index + 1;
        }

        return result;
    }

    static ZonedDateTime[] LimitZonedDateTimes(ZonedDateTime[] values, int maximumCandidates)
    {
        if (values.Length <= maximumCandidates)
        {
            return values;
        }

        ZonedDateTime[] result = new ZonedDateTime[maximumCandidates];
        Array.Copy(values, result, maximumCandidates);
        return result;
    }

    static void RequireLocalOccurrences(LocalDateTime[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Local occurrence list must not contain null values.", "localOccurrences");
            }

            index = index + 1;
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
