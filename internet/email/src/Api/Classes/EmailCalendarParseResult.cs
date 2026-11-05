using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a calendar parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailCalendarParseResult : IEmailCalendarParseResultContract
{
    EmailCalendar? calendar;
    EmailIssueList issues;
    string? mimeMethod;
    bool imip;
    bool methodConsistent;
    /// <summary>
    /// Creates a new calendar parse result.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "mimeMethod">The mime method used by the operation.</param>
    /// <param name = "imip">Whether to enable the imip behavior.</param>
    /// <param name = "methodConsistent">Whether to enable the method consistent behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCalendarParseResult(EmailCalendar? calendar, EmailIssueList issues, string? mimeMethod, bool imip, bool methodConsistent)
    {
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.calendar = calendar;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.mimeMethod = mimeMethod;
        this.imip = imip;
        this.methodConsistent = methodConsistent;
    }

    /// <summary>
    /// Determines whether the calendar is present or available.
    /// </summary>
    /// <returns><c>true</c> when the calendar is available; otherwise, <c>false</c>.</returns>
    public bool HasCalendar()
    {
        return calendar != null;
    }

    /// <summary>
    /// Gets the calendar.
    /// </summary>
    /// <returns>The current calendar.</returns>
    public EmailCalendar? GetCalendar()
    {
        return calendar;
    }

    /// <summary>
    /// Determines whether the MIME method is present or available.
    /// </summary>
    /// <returns><c>true</c> when the MIME method is available; otherwise, <c>false</c>.</returns>
    public bool HasMimeMethod()
    {
        return mimeMethod != null;
    }

    /// <summary>
    /// Gets the MIME method.
    /// </summary>
    /// <returns>The current MIME method.</returns>
    public string? GetMimeMethod()
    {
        return mimeMethod;
    }

    /// <summary>
    /// Determines whether the current value or state is imip.
    /// </summary>
    /// <returns><c>true</c> when the value or state is imip; otherwise, <c>false</c>.</returns>
    public bool IsImip()
    {
        return imip;
    }

    /// <summary>
    /// Determines whether the current value or state is method consistent.
    /// </summary>
    /// <returns><c>true</c> when the value or state is method consistent; otherwise, <c>false</c>.</returns>
    public bool IsMethodConsistent()
    {
        return methodConsistent;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return calendar != null && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
