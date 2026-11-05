using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTransferValidationResult"/>.
/// </summary>
public interface IEmailTransferValidationResultContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailTransferEncodingKind GetKind();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    long GetSourceByteCount();
    /// <summary>
    /// Gets the copied byte count.
    /// </summary>
    /// <returns>The current copied byte count.</returns>
    long GetCopiedByteCount();
    /// <summary>
    /// Determines whether the current value or state is cancelled.
    /// </summary>
    /// <returns><c>true</c> when the value or state is cancelled; otherwise, <c>false</c>.</returns>
    bool IsCancelled();
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
    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    bool HasFatalError();
}
