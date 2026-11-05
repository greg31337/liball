using System;

namespace email;
/// <summary>
/// Describes the outcome of a header validation operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailHeaderValidationResult : IEmailHeaderValidationResultContract
{
    EmailIssueList issues;
    int validatedHeaderCount;
    int resentBlockCount;
    bool requiresSmtpUtf8;
    /// <summary>
    /// Creates a new header validation result.
    /// </summary>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "validatedHeaderCount">The number of items or bytes to process.</param>
    /// <param name = "resentBlockCount">The number of items or bytes to process.</param>
    /// <param name = "requiresSmtpUtf8">Whether to enable the requires smtp UTF-8 behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHeaderValidationResult(EmailIssueList issues, int validatedHeaderCount, int resentBlockCount, bool requiresSmtpUtf8)
    {
        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (validatedHeaderCount < 0 || resentBlockCount < 0)
        {
            throw new ArgumentOutOfRangeException("validatedHeaderCount");
        }

        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.validatedHeaderCount = validatedHeaderCount;
        this.resentBlockCount = resentBlockCount;
        this.requiresSmtpUtf8 = requiresSmtpUtf8;
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
    /// Gets the validated header count.
    /// </summary>
    /// <returns>The current validated header count.</returns>
    public int GetValidatedHeaderCount()
    {
        return validatedHeaderCount;
    }

    /// <summary>
    /// Gets the resent block count.
    /// </summary>
    /// <returns>The current resent block count.</returns>
    public int GetResentBlockCount()
    {
        return resentBlockCount;
    }

    /// <summary>
    /// Determines whether the current policy requires smtp UTF-8.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool RequiresSmtpUtf8()
    {
        return requiresSmtpUtf8;
    }

    /// <summary>
    /// Determines whether the current value or state is valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is valid; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
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
