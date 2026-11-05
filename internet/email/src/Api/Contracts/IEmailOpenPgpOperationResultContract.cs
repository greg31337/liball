using System;
using System.IO.Compression;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpOperationResult"/>.
/// </summary>
public interface IEmailOpenPgpOperationResultContract
{
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Determines whether the current value or state is unsupported.
    /// </summary>
    /// <returns><c>true</c> when the value or state is unsupported; otherwise, <c>false</c>.</returns>
    bool IsUnsupported();
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailOpenPgpOperationStatus GetStatus();
    /// <summary>
    /// Determines whether the output is present or available.
    /// </summary>
    /// <returns><c>true</c> when the output is available; otherwise, <c>false</c>.</returns>
    bool HasOutput();
    /// <summary>
    /// Gets the output.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current output.</returns>
    byte[] GetOutput();
    /// <summary>
    /// Gets the preserved input.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current preserved input.</returns>
    byte[] GetPreservedInput();
    /// <summary>
    /// Determines whether the current value or state is verified.
    /// </summary>
    /// <returns><c>true</c> when the value or state is verified; otherwise, <c>false</c>.</returns>
    bool IsVerified();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is disposed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is disposed; otherwise, <c>false</c>.</returns>
    bool IsDisposed();
}
