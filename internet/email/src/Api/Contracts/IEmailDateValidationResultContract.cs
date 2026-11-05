using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDateValidationResult"/>.
/// </summary>
public interface IEmailDateValidationResultContract
{
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is valid; otherwise, <c>false</c>.</returns>
    bool IsValid();
}
