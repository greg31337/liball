using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailCalendarDateTime"/>.
/// </summary>
public class EmailCalendarDateTimeOperations : IEmailCalendarDateTimeOperationsContract
{
    /// <summary>Creates a calendar date value.</summary>
    /// <param name = "year">The year.</param>
    /// <param name = "month">The month.</param>
    /// <param name = "day">The day.</param>
    /// <returns>The configured calendar value.</returns>
    public EmailCalendarDateTime CreateWithYearAndMonthAndDay(int year, int month, int day)
    {
        return EmailCalendarDateTime.CreateImplementationWithYearAndMonthAndDay(year, month, day);
    }

    /// <summary>
    /// Creates a new calendar date time.
    /// </summary>
    /// <param name = "year">The year used by the operation.</param>
    /// <param name = "month">The month used by the operation.</param>
    /// <param name = "day">The day used by the operation.</param>
    /// <param name = "hour">The hour used by the operation.</param>
    /// <param name = "minute">The minute used by the operation.</param>
    /// <param name = "second">The second used by the operation.</param>
    /// <param name = "utc">Whether to enable the utc behavior.</param>
    /// <param name = "timeZoneId">The time zone ID used by the operation.</param>
    /// <returns>The configured calendar value.</returns>
    public EmailCalendarDateTime CreateWithYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneId(int year, int month, int day, int hour, int minute, int second, bool utc, string timeZoneId)
    {
        return EmailCalendarDateTime.CreateImplementationWithYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneId(year, month, day, hour, minute, second, utc, timeZoneId);
    }

    /// <summary>
    /// Creates a calendar value while retaining its original representation.
    /// </summary>
    /// <param name = "valueType">The value type used by the operation.</param>
    /// <param name = "year">The year used by the operation.</param>
    /// <param name = "month">The month used by the operation.</param>
    /// <param name = "day">The day used by the operation.</param>
    /// <param name = "hour">The hour used by the operation.</param>
    /// <param name = "minute">The minute used by the operation.</param>
    /// <param name = "second">The second used by the operation.</param>
    /// <param name = "utc">Whether to enable the utc behavior.</param>
    /// <param name = "timeZoneId">The time zone ID used by the operation.</param>
    /// <param name = "originalValue">The original value used by the operation.</param>
    /// <returns>The configured calendar value.</returns>
    public EmailCalendarDateTime CreateWithValueTypeAndYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneIdAndOriginalValue(EmailCalendarValueType valueType, int year, int month, int day, int hour, int minute, int second, bool utc, string? timeZoneId, string originalValue)
    {
        return EmailCalendarDateTime.CreateImplementationWithValueTypeAndYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneIdAndOriginalValue(valueType, year, month, day, hour, minute, second, utc, timeZoneId, originalValue);
    }
}
