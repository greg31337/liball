using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDateTime"/>.
/// </summary>
public interface IEmailDateTimeContract
{
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
    /// Determines whether the second is present or available.
    /// </summary>
    /// <returns><c>true</c> when the second is available; otherwise, <c>false</c>.</returns>
    bool HasSecond();
    /// <summary>
    /// Gets the second.
    /// </summary>
    /// <returns>The current second.</returns>
    int GetSecond();
    /// <summary>
    /// Determines whether the current value or state is leap second.
    /// </summary>
    /// <returns><c>true</c> when the value or state is leap second; otherwise, <c>false</c>.</returns>
    bool IsLeapSecond();
    /// <summary>
    /// Determines whether the day of week is present or available.
    /// </summary>
    /// <returns><c>true</c> when the day of week is available; otherwise, <c>false</c>.</returns>
    bool HasDayOfWeek();
    /// <summary>
    /// Gets the day of week.
    /// </summary>
    /// <returns>The current day of week.</returns>
    EmailDateDayOfWeek GetDayOfWeek();
    /// <summary>
    /// Determines whether day of week match.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool DoesDayOfWeekMatch();
    /// <summary>
    /// Gets the zone kind.
    /// </summary>
    /// <returns>The current zone kind.</returns>
    EmailDateZoneKind GetZoneKind();
    /// <summary>
    /// Gets the original zone.
    /// </summary>
    /// <returns>The current original zone.</returns>
    string GetOriginalZone();
    /// <summary>
    /// Determines whether the known zone offset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the known zone offset is available; otherwise, <c>false</c>.</returns>
    bool HasKnownZoneOffset();
    /// <summary>
    /// Gets the zone offset minutes.
    /// </summary>
    /// <returns>The current zone offset minutes.</returns>
    int GetZoneOffsetMinutes();
    /// <summary>
    /// Determines whether the current value or state is unknown zone.
    /// </summary>
    /// <returns><c>true</c> when the value or state is unknown zone; otherwise, <c>false</c>.</returns>
    bool IsUnknownZone();
    /// <summary>
    /// Determines whether the current value or state uses obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses obsolete syntax; otherwise, <c>false</c>.</returns>
    bool UsesObsoleteSyntax();
    /// <summary>
    /// Gets the original year digit count.
    /// </summary>
    /// <returns>The current original year digit count.</returns>
    int GetOriginalYearDigitCount();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDateValidationResult Validate();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <returns>The date format result produced by the operation.</returns>
    EmailDateFormatResult Format();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The date format result produced by the operation.</returns>
    EmailDateFormatResult FormatWithOptions(EmailDateFormatOptions options);
}
