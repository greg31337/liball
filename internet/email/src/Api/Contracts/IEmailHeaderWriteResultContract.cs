using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderWriteResult"/>.
/// </summary>
public interface IEmailHeaderWriteResultContract
{
    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    EmailHeaderWriteMode GetMode();
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
    /// Gets the header count.
    /// </summary>
    /// <returns>The current header count.</returns>
    int GetHeaderCount();
    /// <summary>
    /// Gets the copied header count.
    /// </summary>
    /// <returns>The current copied header count.</returns>
    int GetCopiedHeaderCount();
    /// <summary>
    /// Gets the regenerated header count.
    /// </summary>
    /// <returns>The current regenerated header count.</returns>
    int GetRegeneratedHeaderCount();
    /// <summary>
    /// Gets the fold count.
    /// </summary>
    /// <returns>The current fold count.</returns>
    int GetFoldCount();
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
