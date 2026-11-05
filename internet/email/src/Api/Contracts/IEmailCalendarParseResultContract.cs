using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendarParseResult"/>.
/// </summary>
public interface IEmailCalendarParseResultContract
{
    /// <summary>
    /// Determines whether the calendar is present or available.
    /// </summary>
    /// <returns><c>true</c> when the calendar is available; otherwise, <c>false</c>.</returns>
    bool HasCalendar();
    /// <summary>
    /// Gets the calendar.
    /// </summary>
    /// <returns>The current calendar.</returns>
    EmailCalendar? GetCalendar();
    /// <summary>
    /// Determines whether the MIME method is present or available.
    /// </summary>
    /// <returns><c>true</c> when the MIME method is available; otherwise, <c>false</c>.</returns>
    bool HasMimeMethod();
    /// <summary>
    /// Gets the MIME method.
    /// </summary>
    /// <returns>The current MIME method.</returns>
    string? GetMimeMethod();
    /// <summary>
    /// Determines whether the current value or state is imip.
    /// </summary>
    /// <returns><c>true</c> when the value or state is imip; otherwise, <c>false</c>.</returns>
    bool IsImip();
    /// <summary>
    /// Determines whether the current value or state is method consistent.
    /// </summary>
    /// <returns><c>true</c> when the value or state is method consistent; otherwise, <c>false</c>.</returns>
    bool IsMethodConsistent();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
