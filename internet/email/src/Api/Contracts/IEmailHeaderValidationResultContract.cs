using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderValidationResult"/>.
/// </summary>
public interface IEmailHeaderValidationResultContract
{
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the validated header count.
    /// </summary>
    /// <returns>The current validated header count.</returns>
    int GetValidatedHeaderCount();
    /// <summary>
    /// Gets the resent block count.
    /// </summary>
    /// <returns>The current resent block count.</returns>
    int GetResentBlockCount();
    /// <summary>
    /// Determines whether the current policy requires smtp UTF-8.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool RequiresSmtpUtf8();
    /// <summary>
    /// Determines whether the current value or state is valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is valid; otherwise, <c>false</c>.</returns>
    bool IsValid();
    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    bool HasFatalError();
}
