using System;

namespace time;

/// <summary>Represents an ISO weekday, optionally qualified by an ordinal for recurrence filters.</summary>
/// <remarks>An ordinal such as <c>1</c> means the first matching weekday and <c>-1</c> means the last.</remarks>
public class RecurrenceDay : IRecurrenceDay
{
    internal const int NoOrdinal = 0;
    internal const int MinOrdinal = -53;
    internal const int MaxOrdinal = 53;

    int dayOfWeek;
    int ordinal;

    internal RecurrenceDay(int dayOfWeek, int ordinal)
    {
        TimeValidation.RequireIsoDayOfWeek(dayOfWeek, "dayOfWeek");
        RequireOrdinal(ordinal, "ordinal");

        this.dayOfWeek = dayOfWeek;
        this.ordinal = ordinal;
    }

    /// <summary>Creates a recurrence weekday without an ordinal.</summary>
    /// <param name="dayOfWeek">The ISO weekday number, Monday as 1 through Sunday as 7.</param>
    /// <returns>A weekday filter that matches every occurrence of that weekday.</returns>
    internal static RecurrenceDay Create(int dayOfWeek)
    {
        return new RecurrenceDay(dayOfWeek, NoOrdinal);
    }

    /// <summary>Creates an ordinal recurrence weekday.</summary>
    /// <param name="dayOfWeek">The ISO weekday number, Monday as 1 through Sunday as 7.</param>
    /// <param name="ordinal">A nonzero position from -53 through 53.</param>
    /// <returns>An ordinal weekday filter.</returns>
    internal static RecurrenceDay Create(int dayOfWeek, int ordinal)
    {
        return new RecurrenceDay(dayOfWeek, ordinal);
    }

    /// <summary>Gets the ISO weekday number.</summary>
    /// <returns>Monday as 1 through Sunday as 7.</returns>
    public int GetDayOfWeek()
    {
        return dayOfWeek;
    }

    /// <summary>Gets the optional weekday position.</summary>
    /// <returns>Zero when no ordinal was supplied; otherwise a signed position.</returns>
    public int GetOrdinal()
    {
        return ordinal;
    }

    /// <summary>Reports whether this weekday has an ordinal position.</summary>
    /// <returns><c>true</c> when an ordinal was supplied; otherwise <c>false</c>.</returns>
    public bool HasOrdinal()
    {
        return ordinal != NoOrdinal;
    }

    /// <summary>Compares the weekday and ordinal fields for equality.</summary>
    /// <param name="other">The value to compare, or null.</param>
    /// <returns><c>true</c> when both fields match; otherwise <c>false</c>.</returns>
    public bool EqualsValue(RecurrenceDay? other)
    {
        if (other == null)
        {
            return false;
        }

        return dayOfWeek == other.GetDayOfWeek() && ordinal == other.GetOrdinal();
    }

    static void RequireOrdinal(int ordinal, string parameterName)
    {
        if (ordinal < MinOrdinal || ordinal > MaxOrdinal)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Recurrence day ordinal must be in range -53 through 53.");
        }
    }
}
