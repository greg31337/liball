using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a calendar date time.
/// </summary>
public class EmailCalendarDateTime : IEmailCalendarDateTimeContract
{
    EmailCalendarValueType valueType;
    int year;
    int month;
    int day;
    int hour;
    int minute;
    int second;
    bool utc;
    string? timeZoneId;
    string? originalValue;
    /// <summary>
    /// Creates a calendar date value.
    /// </summary>
    /// <param name = "year">The year used by the operation.</param>
    /// <param name = "month">The month used by the operation.</param>
    /// <param name = "day">The day used by the operation.</param>
    public EmailCalendarDateTime(int year, int month, int day)
    {
        Initialize(EmailCalendarValueType.Date, year, month, day, 0, 0, 0, false, null, EmailCalendarSyntax.FormatDate(year, month, day));
    }

    internal static EmailCalendarDateTime CreateImplementationWithYearAndMonthAndDay(int year, int month, int day)
    {
        return new EmailCalendarDateTime(year, month, day);
    }

    internal static EmailCalendarDateTime CreateImplementationWithYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneId(int year, int month, int day, int hour, int minute, int second, bool utc, string timeZoneId)
    {
        string original = EmailCalendarSyntax.FormatDateTime(year, month, day, hour, minute, second, utc);
        EmailCalendarDateTime value = new EmailCalendarDateTime(year, month, day);
        value.Initialize(EmailCalendarValueType.DateTime, year, month, day, hour, minute, second, utc, timeZoneId, original);
        return value;
    }

    internal static EmailCalendarDateTime CreateImplementationWithValueTypeAndYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneIdAndOriginalValue(EmailCalendarValueType valueType, int year, int month, int day, int hour, int minute, int second, bool utc, string? timeZoneId, string originalValue)
    {
        EmailCalendarDateTime value = new EmailCalendarDateTime(year, month, day);
        value.Initialize(valueType, year, month, day, hour, minute, second, utc, timeZoneId, originalValue);
        return value;
    }

    void Initialize(EmailCalendarValueType suppliedType, int suppliedYear, int suppliedMonth, int suppliedDay, int suppliedHour, int suppliedMinute, int suppliedSecond, bool suppliedUtc, string? suppliedTimeZoneId, string suppliedOriginalValue)
    {
        if (suppliedType < EmailCalendarValueType.Date || suppliedType > EmailCalendarValueType.DateTime)
            throw new ArgumentOutOfRangeException("suppliedType");
        if (!EmailCalendarSyntax.IsValidDate(suppliedYear, suppliedMonth, suppliedDay))
            throw new ArgumentOutOfRangeException("suppliedDay");
        if (suppliedType == EmailCalendarValueType.DateTime && (suppliedHour < 0 || suppliedHour > 23 || suppliedMinute < 0 || suppliedMinute > 59 || suppliedSecond < 0 || suppliedSecond > 60))
            throw new ArgumentOutOfRangeException("suppliedHour");
        if (suppliedType == EmailCalendarValueType.Date && (suppliedUtc || suppliedTimeZoneId != null))
            throw new ArgumentException("A DATE value cannot be UTC or carry a TZID.");
        if (suppliedUtc && suppliedTimeZoneId != null)
            throw new ArgumentException("A UTC DATE-TIME cannot also carry a TZID.");
        if (suppliedTimeZoneId != null && suppliedTimeZoneId.Length == 0)
            throw new ArgumentException("A TZID cannot be empty.", "suppliedTimeZoneId");
        valueType = suppliedType;
        year = suppliedYear;
        month = suppliedMonth;
        day = suppliedDay;
        hour = suppliedHour;
        minute = suppliedMinute;
        second = suppliedSecond;
        utc = suppliedUtc;
        timeZoneId = suppliedTimeZoneId;
        originalValue = suppliedOriginalValue;
    }

    /// <summary>
    /// Gets the value type.
    /// </summary>
    /// <returns>The current value type.</returns>
    public EmailCalendarValueType GetValueType()
    {
        return valueType;
    }

    /// <summary>
    /// Gets the year.
    /// </summary>
    /// <returns>The current year.</returns>
    public int GetYear()
    {
        return year;
    }

    /// <summary>
    /// Gets the month.
    /// </summary>
    /// <returns>The current month.</returns>
    public int GetMonth()
    {
        return month;
    }

    /// <summary>
    /// Gets the day.
    /// </summary>
    /// <returns>The current day.</returns>
    public int GetDay()
    {
        return day;
    }

    /// <summary>
    /// Gets the hour.
    /// </summary>
    /// <returns>The current hour.</returns>
    public int GetHour()
    {
        return hour;
    }

    /// <summary>
    /// Gets the minute.
    /// </summary>
    /// <returns>The current minute.</returns>
    public int GetMinute()
    {
        return minute;
    }

    /// <summary>
    /// Gets the second.
    /// </summary>
    /// <returns>The current second.</returns>
    public int GetSecond()
    {
        return second;
    }

    /// <summary>
    /// Determines whether the current value or state is utc.
    /// </summary>
    /// <returns><c>true</c> when the value or state is utc; otherwise, <c>false</c>.</returns>
    public bool IsUtc()
    {
        return utc;
    }

    /// <summary>
    /// Determines whether the current value or state is floating.
    /// </summary>
    /// <returns><c>true</c> when the value or state is floating; otherwise, <c>false</c>.</returns>
    public bool IsFloating()
    {
        return valueType == EmailCalendarValueType.DateTime && !utc && timeZoneId == null;
    }

    /// <summary>
    /// Determines whether the time zone ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the time zone ID is available; otherwise, <c>false</c>.</returns>
    public bool HasTimeZoneId()
    {
        return timeZoneId != null;
    }

    /// <summary>
    /// Gets the time zone ID.
    /// </summary>
    /// <returns>The current time zone ID.</returns>
    public string? GetTimeZoneId()
    {
        return timeZoneId;
    }

    /// <summary>
    /// Gets the original value.
    /// </summary>
    /// <returns>The current original value.</returns>
    public string? GetOriginalValue()
    {
        return originalValue;
    }
}
