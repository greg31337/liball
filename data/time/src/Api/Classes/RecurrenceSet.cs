using System;

namespace time;

/// <summary>Combines a local start, recurrence rules, explicit inclusion dates, and exclusion dates.</summary>
/// <remarks>Add operations mutate this set and return it for chaining. Expansion merges, sorts, and removes duplicate local occurrences.</remarks>
public class RecurrenceSet : IRecurrenceSet
{
    LocalDateTime start;
    RecurrenceRule[] rules;
    LocalDateTime[] rDates;
    LocalDateTime[] exDates;

    internal RecurrenceSet(LocalDateTime start, RecurrenceRule[] rules, LocalDateTime[] rDates, LocalDateTime[] exDates)
    {
        TimeValidation.RequireNotNull(start, "start");
        TimeValidation.RequireNotNull(rules, "rules");
        TimeValidation.RequireNotNull(rDates, "rDates");
        TimeValidation.RequireNotNull(exDates, "exDates");
        RequireRuleValues(rules, "rules");
        RequireDateTimeValues(rDates, "rDates");
        RequireDateTimeValues(exDates, "exDates");

        this.start = start;
        this.rules = CopyRuleArray(rules);
        this.rDates = CopyDateTimeArray(rDates);
        this.exDates = CopyDateTimeArray(exDates);
    }

    /// <summary>Creates an empty recurrence set anchored at a local start.</summary><param name="start">The DTSTART value and default first occurrence.</param><returns>A set with no rules or exception dates.</returns>
    internal static RecurrenceSet Create(LocalDateTime start)
    {
        return new RecurrenceSet(start, new RecurrenceRule[0], new LocalDateTime[0], new LocalDateTime[0]);
    }

    /// <summary>Gets the local DTSTART value.</summary><returns>The recurrence anchor.</returns>
    public LocalDateTime GetStart()
    {
        return start;
    }

    /// <summary>Gets a copy of the rules.</summary><returns>A new array whose modification cannot change the set.</returns>
    public RecurrenceRule[] GetRules()
    {
        return CopyRuleArray(rules);
    }

    /// <summary>Gets a copy of explicit inclusion dates.</summary><returns>The RDATE values.</returns>
    public LocalDateTime[] GetRDates()
    {
        return CopyDateTimeArray(rDates);
    }

    /// <summary>Gets a copy of explicit exclusion dates.</summary><returns>The EXDATE values.</returns>
    public LocalDateTime[] GetExDates()
    {
        return CopyDateTimeArray(exDates);
    }

    /// <summary>Adds a recurrence rule.</summary><param name="rule">The rule to include.</param><returns>This set for chaining.</returns>
    public RecurrenceSet AddRule(RecurrenceRule rule)
    {
        TimeValidation.RequireNotNull(rule, "rule");

        rules = AppendRule(rules, rule);
        return this;
    }

    /// <summary>Adds an explicit local occurrence.</summary><param name="date">The RDATE value.</param><returns>This set for chaining.</returns>
    public RecurrenceSet AddRDate(LocalDateTime date)
    {
        TimeValidation.RequireNotNull(date, "date");

        rDates = AppendDateTime(rDates, date);
        return this;
    }

    /// <summary>Adds a local occurrence exclusion.</summary><param name="date">The EXDATE value removed after all inclusions are merged.</param><returns>This set for chaining.</returns>
    public RecurrenceSet AddExDate(LocalDateTime date)
    {
        TimeValidation.RequireNotNull(date, "date");

        exDates = AppendDateTime(exDates, date);
        return this;
    }

    /// <summary>Expands, merges, sorts, de-duplicates, excludes, and limits local occurrences.</summary><param name="maximumCandidates">A positive safety bound and maximum returned count.</param><returns>At most the requested number of local occurrences.</returns>
    public LocalDateTime[] ExpandBounded(int maximumCandidates)
    {
        RequirePositiveMaximumCandidates(maximumCandidates);
        RequireFloatingRules();

        return ExpandCandidates(null, maximumCandidates);
    }

    /// <summary>Expands local occurrences through an inclusive caller-specified end value.</summary><param name="end">The final local value that can be returned.</param><param name="maximumCandidates">A positive expansion safety limit and maximum returned count.</param><returns>Sorted local occurrences no later than the end value.</returns>
    public LocalDateTime[] ExpandThrough(LocalDateTime end, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(end, "end");
        RequirePositiveMaximumCandidates(maximumCandidates);
        RequireFloatingRules();

        return ExpandCandidates(end, maximumCandidates);
    }

    internal LocalDateTime[] ExpandRuleCandidates(RecurrenceRule rule, int maximumCandidates)
    {
        TimeValidation.RequireNotNull(rule, "rule");
        RequirePositiveMaximumCandidates(maximumCandidates);

        LocalDateTime[] ruleDates = RecurrenceExpansion.ExpandBounded(start, rule, maximumCandidates);
        int ruleCount = rule.GetCount();
        if (ruleCount > 0 && !ContainsDateTime(ruleDates, start))
        {
            return LimitDateTimes(ruleDates, ruleCount - 1);
        }

        return ruleDates;
    }

    LocalDateTime[] ExpandCandidates(LocalDateTime? end, int maximumCandidates)
    {
        LocalDateTime[] result = new LocalDateTime[] { start };
        int ruleIndex = 0;
        while (ruleIndex < rules.Length)
        {
            LocalDateTime[] ruleDates = ExpandRuleCandidates(rules[ruleIndex], maximumCandidates);
            result = AddUniqueDates(result, ruleDates);
            ruleIndex = ruleIndex + 1;
        }

        result = AddUniqueDates(result, rDates);
        result = FilterThrough(result, end);
        result = RemoveExcludedDates(result, exDates);
        result = SortDateTimes(result);
        return LimitDateTimes(result, maximumCandidates);
    }

    void RequireFloatingRules()
    {
        int validationIndex = 0;
        while (validationIndex < rules.Length)
        {
            rules[validationIndex].RequireFloatingStart();
            validationIndex = validationIndex + 1;
        }
    }

    static LocalDateTime[] AddUniqueDates(LocalDateTime[] result, LocalDateTime[] dates)
    {
        int index = 0;
        while (index < dates.Length)
        {
            if (!ContainsDateTime(result, dates[index]))
            {
                result = AppendDateTime(result, dates[index]);
            }

            index = index + 1;
        }

        return result;
    }

    static LocalDateTime[] RemoveExcludedDates(LocalDateTime[] dates, LocalDateTime[] exclusions)
    {
        LocalDateTime[] result = new LocalDateTime[0];
        int index = 0;
        while (index < dates.Length)
        {
            if (!ContainsDateTime(exclusions, dates[index]))
            {
                result = AppendDateTime(result, dates[index]);
            }

            index = index + 1;
        }

        return result;
    }

    static LocalDateTime[] FilterThrough(LocalDateTime[] dates, LocalDateTime? end)
    {
        if (end == null)
        {
            return dates;
        }

        LocalDateTime[] result = new LocalDateTime[0];
        int index = 0;
        while (index < dates.Length)
        {
            if (dates[index].CompareTo(end) <= 0)
            {
                result = AppendDateTime(result, dates[index]);
            }

            index = index + 1;
        }

        return result;
    }

    static LocalDateTime[] SortDateTimes(LocalDateTime[] dates)
    {
        LocalDateTime[] result = CopyDateTimeArray(dates);
        int index = 1;
        while (index < result.Length)
        {
            LocalDateTime value = result[index];
            int insert = index - 1;
            while (insert >= 0 && result[insert].CompareTo(value) > 0)
            {
                result[insert + 1] = result[insert];
                insert = insert - 1;
            }

            result[insert + 1] = value;
            index = index + 1;
        }

        return result;
    }

    static LocalDateTime[] LimitDateTimes(LocalDateTime[] dates, int maximumCandidates)
    {
        if (dates.Length <= maximumCandidates)
        {
            return dates;
        }

        LocalDateTime[] result = new LocalDateTime[maximumCandidates];
        Array.Copy(dates, result, maximumCandidates);
        return result;
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

    static RecurrenceRule[] AppendRule(RecurrenceRule[] values, RecurrenceRule value)
    {
        RecurrenceRule[] result = new RecurrenceRule[values.Length + 1];
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

    static RecurrenceRule[] CopyRuleArray(RecurrenceRule[] values)
    {
        RecurrenceRule[] result = new RecurrenceRule[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    static LocalDateTime[] CopyDateTimeArray(LocalDateTime[] values)
    {
        LocalDateTime[] result = new LocalDateTime[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    static void RequireRuleValues(RecurrenceRule[] values, string parameterName)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Recurrence rule list must not contain null values.", parameterName);
            }

            index = index + 1;
        }
    }

    static void RequireDateTimeValues(LocalDateTime[] values, string parameterName)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Date-time list must not contain null values.", parameterName);
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
