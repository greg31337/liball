using System;

namespace email;
/// <summary>
/// Represents a date time.
/// </summary>
public class EmailDateTime : IEmailDateTimeContract
{
    int year;
    int month;
    int day;
    int hour;
    int minute;
    int second;
    bool secondPresent;
    bool leapSecond;
    bool dayOfWeekPresent;
    EmailDateDayOfWeek dayOfWeek;
    bool dayOfWeekMatches;
    EmailDateZoneKind zoneKind;
    string originalZone;
    int zoneOffsetMinutes;
    bool zoneOffsetKnown;
    bool obsoleteSyntax;
    int originalYearDigitCount;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new date time.
    /// </summary>
    /// <param name = "year">The year used by the operation.</param>
    /// <param name = "month">The month used by the operation.</param>
    /// <param name = "day">The day used by the operation.</param>
    /// <param name = "hour">The hour used by the operation.</param>
    /// <param name = "minute">The minute used by the operation.</param>
    /// <param name = "second">The second used by the operation.</param>
    /// <param name = "secondPresent">Whether to enable the second present behavior.</param>
    /// <param name = "leapSecond">Whether to enable the leap second behavior.</param>
    /// <param name = "dayOfWeekPresent">Whether to enable the day of week present behavior.</param>
    /// <param name = "dayOfWeek">The day of week used by the operation.</param>
    /// <param name = "dayOfWeekMatches">Whether to enable the day of week matches behavior.</param>
    /// <param name = "zoneKind">The zone kind used by the operation.</param>
    /// <param name = "originalZone">The original zone used by the operation.</param>
    /// <param name = "zoneOffsetMinutes">The zone offset minutes used by the operation.</param>
    /// <param name = "zoneOffsetKnown">Whether to enable the zone offset known behavior.</param>
    /// <param name = "obsoleteSyntax">Whether to enable the obsolete syntax behavior.</param>
    /// <param name = "originalYearDigitCount">The number of items or bytes to process.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDateTime(int year, int month, int day, int hour, int minute, int second, bool secondPresent, bool leapSecond, bool dayOfWeekPresent, EmailDateDayOfWeek dayOfWeek, bool dayOfWeekMatches, EmailDateZoneKind zoneKind, string? originalZone, int zoneOffsetMinutes, bool zoneOffsetKnown, bool obsoleteSyntax, int originalYearDigitCount, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (year < 1)
        {
            throw new ArgumentOutOfRangeException("year");
        }

        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException("month");
        }

        if (day < 1 || day > 31)
        {
            throw new ArgumentOutOfRangeException("day");
        }

        if (hour < 0 || hour > 23)
        {
            throw new ArgumentOutOfRangeException("hour");
        }

        if (minute < 0 || minute > 59)
        {
            throw new ArgumentOutOfRangeException("minute");
        }

        if (second < 0 || second > 60)
        {
            throw new ArgumentOutOfRangeException("second");
        }

        if (dayOfWeek < EmailDateDayOfWeek.Sunday || dayOfWeek > EmailDateDayOfWeek.Saturday)
        {
            throw new ArgumentOutOfRangeException("dayOfWeek");
        }

        if (zoneKind < EmailDateZoneKind.Numeric || zoneKind > EmailDateZoneKind.UnknownNamed)
        {
            throw new ArgumentOutOfRangeException("zoneKind");
        }

        if (originalZone == null || originalZone.Length == 0)
        {
            throw new ArgumentException("An original zone token is required.", "originalZone");
        }

        if (originalYearDigitCount < 2)
        {
            throw new ArgumentOutOfRangeException("originalYearDigitCount");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.year = year;
        this.month = month;
        this.day = day;
        this.hour = hour;
        this.minute = minute;
        this.second = second;
        this.secondPresent = secondPresent;
        this.leapSecond = leapSecond;
        this.dayOfWeekPresent = dayOfWeekPresent;
        this.dayOfWeek = dayOfWeek;
        this.dayOfWeekMatches = dayOfWeekMatches;
        this.zoneKind = zoneKind;
        this.originalZone = originalZone;
        this.zoneOffsetMinutes = zoneOffsetMinutes;
        this.zoneOffsetKnown = zoneOffsetKnown;
        this.obsoleteSyntax = obsoleteSyntax;
        this.originalYearDigitCount = originalYearDigitCount;
        this.rawBytes = new byte[rawBytes.Length];
        rawBytes.CopyTo(this.rawBytes, 0);
        this.sourcePosition = sourcePosition;
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
    /// Determines whether the second is present or available.
    /// </summary>
    /// <returns><c>true</c> when the second is available; otherwise, <c>false</c>.</returns>
    public bool HasSecond()
    {
        return secondPresent;
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
    /// Determines whether the current value or state is leap second.
    /// </summary>
    /// <returns><c>true</c> when the value or state is leap second; otherwise, <c>false</c>.</returns>
    public bool IsLeapSecond()
    {
        return leapSecond;
    }

    /// <summary>
    /// Determines whether the day of week is present or available.
    /// </summary>
    /// <returns><c>true</c> when the day of week is available; otherwise, <c>false</c>.</returns>
    public bool HasDayOfWeek()
    {
        return dayOfWeekPresent;
    }

    /// <summary>
    /// Gets the day of week.
    /// </summary>
    /// <returns>The current day of week.</returns>
    public EmailDateDayOfWeek GetDayOfWeek()
    {
        return dayOfWeek;
    }

    /// <summary>
    /// Determines whether day of week match.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool DoesDayOfWeekMatch()
    {
        return dayOfWeekMatches;
    }

    /// <summary>
    /// Gets the zone kind.
    /// </summary>
    /// <returns>The current zone kind.</returns>
    public EmailDateZoneKind GetZoneKind()
    {
        return zoneKind;
    }

    /// <summary>
    /// Gets the original zone.
    /// </summary>
    /// <returns>The current original zone.</returns>
    public string GetOriginalZone()
    {
        return originalZone;
    }

    /// <summary>
    /// Determines whether the known zone offset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the known zone offset is available; otherwise, <c>false</c>.</returns>
    public bool HasKnownZoneOffset()
    {
        return zoneOffsetKnown;
    }

    /// <summary>
    /// Gets the zone offset minutes.
    /// </summary>
    /// <returns>The current zone offset minutes.</returns>
    public int GetZoneOffsetMinutes()
    {
        return zoneOffsetMinutes;
    }

    /// <summary>
    /// Determines whether the current value or state is unknown zone.
    /// </summary>
    /// <returns><c>true</c> when the value or state is unknown zone; otherwise, <c>false</c>.</returns>
    public bool IsUnknownZone()
    {
        return !zoneOffsetKnown;
    }

    /// <summary>
    /// Determines whether the current value or state uses obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses obsolete syntax; otherwise, <c>false</c>.</returns>
    public bool UsesObsoleteSyntax()
    {
        return obsoleteSyntax;
    }

    /// <summary>
    /// Gets the original year digit count.
    /// </summary>
    /// <returns>The current original year digit count.</returns>
    public int GetOriginalYearDigitCount()
    {
        return originalYearDigitCount;
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        byte[] copy = new byte[rawBytes.Length];
        rawBytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDateValidationResult Validate()
    {
        return new EmailDateValidator().Validate(this);
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <returns>The date format result produced by the operation.</returns>
    public EmailDateFormatResult Format()
    {
        return new EmailDateFormatter().FormatWithValue(this);
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The date format result produced by the operation.</returns>
    public EmailDateFormatResult FormatWithOptions(EmailDateFormatOptions options)
    {
        return new EmailDateFormatter().FormatWithValueAndOptions(this, options);
    }
}
