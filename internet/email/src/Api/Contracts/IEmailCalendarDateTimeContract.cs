using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendarDateTime"/>.
/// </summary>
public interface IEmailCalendarDateTimeContract
{
    /// <summary>
    /// Gets the value type.
    /// </summary>
    /// <returns>The current value type.</returns>
    EmailCalendarValueType GetValueType();
    /// <summary>
    /// Gets the year.
    /// </summary>
    /// <returns>The current year.</returns>
    int GetYear();
    /// <summary>
    /// Gets the month.
    /// </summary>
    /// <returns>The current month.</returns>
    int GetMonth();
    /// <summary>
    /// Gets the day.
    /// </summary>
    /// <returns>The current day.</returns>
    int GetDay();
    /// <summary>
    /// Gets the hour.
    /// </summary>
    /// <returns>The current hour.</returns>
    int GetHour();
    /// <summary>
    /// Gets the minute.
    /// </summary>
    /// <returns>The current minute.</returns>
    int GetMinute();
    /// <summary>
    /// Gets the second.
    /// </summary>
    /// <returns>The current second.</returns>
    int GetSecond();
    /// <summary>
    /// Determines whether the current value or state is utc.
    /// </summary>
    /// <returns><c>true</c> when the value or state is utc; otherwise, <c>false</c>.</returns>
    bool IsUtc();
    /// <summary>
    /// Determines whether the current value or state is floating.
    /// </summary>
    /// <returns><c>true</c> when the value or state is floating; otherwise, <c>false</c>.</returns>
    bool IsFloating();
    /// <summary>
    /// Determines whether the time zone ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the time zone ID is available; otherwise, <c>false</c>.</returns>
    bool HasTimeZoneId();
    /// <summary>
    /// Gets the time zone ID.
    /// </summary>
    /// <returns>The current time zone ID.</returns>
    string? GetTimeZoneId();
    /// <summary>
    /// Gets the original value.
    /// </summary>
    /// <returns>The current original value.</returns>
    string? GetOriginalValue();
}
