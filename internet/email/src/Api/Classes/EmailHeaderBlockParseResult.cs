using System;

namespace email;
/// <summary>
/// Describes the outcome of a header block parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailHeaderBlockParseResult : IEmailHeaderBlockParseResultContract
{
    EmailHeaderBlockParseStatus status;
    EmailHeaders headers;
    EmailIssueList issues;
    int consumedByteCount;
    int sourceByteCount;
    EmailLineEnding separatorLineEnding;
    /// <summary>
    /// Creates a new header block parse result.
    /// </summary>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "consumedByteCount">The number of items or bytes to process.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <param name = "separatorLineEnding">The separator line ending used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHeaderBlockParseResult(EmailHeaderBlockParseStatus status, EmailHeaders headers, EmailIssueList issues, int consumedByteCount, int sourceByteCount, EmailLineEnding separatorLineEnding)
    {
        if (status < EmailHeaderBlockParseStatus.Complete || status > EmailHeaderBlockParseStatus.LimitReached)
        {
            throw new ArgumentOutOfRangeException("status");
        }

        if (headers == null || issues == null)
        {
            throw new ArgumentNullException("headers");
        }

        if (consumedByteCount < 0 || sourceByteCount < 0 || consumedByteCount > sourceByteCount)
        {
            throw new ArgumentOutOfRangeException("consumedByteCount");
        }

        if (separatorLineEnding < EmailLineEnding.None || separatorLineEnding > EmailLineEnding.Cr)
        {
            throw new ArgumentOutOfRangeException("separatorLineEnding");
        }

        if (status == EmailHeaderBlockParseStatus.Complete && separatorLineEnding == EmailLineEnding.None)
        {
            throw new ArgumentException("A complete header block must retain its separator line ending.", "separatorLineEnding");
        }

        if (status != EmailHeaderBlockParseStatus.Complete && separatorLineEnding != EmailLineEnding.None)
        {
            throw new ArgumentException("An incomplete header block cannot have a separator line ending.", "separatorLineEnding");
        }

        this.status = status;
        this.headers = headers;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.consumedByteCount = consumedByteCount;
        this.sourceByteCount = sourceByteCount;
        this.separatorLineEnding = separatorLineEnding;
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailHeaderBlockParseStatus GetStatus()
    {
        return status;
    }

    /// <summary>
    /// Gets the headers.
    /// </summary>
    /// <returns>The current headers.</returns>
    public EmailHeaders GetHeaders()
    {
        return headers;
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
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    public int GetConsumedByteCount()
    {
        return consumedByteCount;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public int GetSourceByteCount()
    {
        return sourceByteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return status == EmailHeaderBlockParseStatus.Complete || status == EmailHeaderBlockParseStatus.Recovered;
    }

    /// <summary>
    /// Determines whether the current state needs more data.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool NeedsMoreData()
    {
        return status == EmailHeaderBlockParseStatus.NeedMoreData;
    }

    /// <summary>
    /// Determines whether processing reached end of input.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool ReachedEndOfInput()
    {
        return status == EmailHeaderBlockParseStatus.EndOfInput;
    }

    /// <summary>
    /// Determines whether the operation or source value was recovered.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasRecovered()
    {
        return status == EmailHeaderBlockParseStatus.Recovered;
    }

    /// <summary>
    /// Determines whether the operation or source value was limit reached.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasLimitReached()
    {
        return status == EmailHeaderBlockParseStatus.LimitReached;
    }

    /// <summary>
    /// Determines whether the separator is present or available.
    /// </summary>
    /// <returns><c>true</c> when the separator is available; otherwise, <c>false</c>.</returns>
    public bool HasSeparator()
    {
        return separatorLineEnding != EmailLineEnding.None;
    }

    /// <summary>
    /// Gets the separator line ending.
    /// </summary>
    /// <returns>The current separator line ending.</returns>
    public EmailLineEnding GetSeparatorLineEnding()
    {
        return separatorLineEnding;
    }

    /// <summary>
    /// Determines whether the body start offset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body start offset is available; otherwise, <c>false</c>.</returns>
    public bool HasBodyStartOffset()
    {
        return IsComplete();
    }

    /// <summary>
    /// Gets the body start offset.
    /// </summary>
    /// <returns>The current body start offset.</returns>
    public int GetBodyStartOffset()
    {
        return IsComplete() ? consumedByteCount : -1;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return IsComplete() && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    public bool HasFatalError()
    {
        return issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal);
    }
}
