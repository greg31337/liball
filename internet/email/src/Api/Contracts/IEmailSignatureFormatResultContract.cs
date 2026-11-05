using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSignatureFormatResult"/>.
/// </summary>
public interface IEmailSignatureFormatResultContract
{
    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current bytes.</returns>
    byte[] GetBytes();
    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    int GetByteCount();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the fold count.
    /// </summary>
    /// <returns>The current fold count.</returns>
    int GetFoldCount();
    /// <summary>
    /// Gets the final line column.
    /// </summary>
    /// <returns>The current final line column.</returns>
    int GetFinalLineColumn();
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
