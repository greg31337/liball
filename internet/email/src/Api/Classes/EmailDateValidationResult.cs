using System;

namespace email;
/// <summary>
/// Describes the outcome of a date validation operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailDateValidationResult : IEmailDateValidationResultContract
{
    EmailIssueList issues;
    /// <summary>
    /// Creates a new date validation result.
    /// </summary>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDateValidationResult(EmailIssueList issues)
    {
        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
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
    /// Determines whether the current value or state is valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is valid; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
