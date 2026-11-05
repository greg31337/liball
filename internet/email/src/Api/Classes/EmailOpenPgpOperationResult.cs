using System;
using System.IO.Compression;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Describes the outcome of a openPGP operation operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailOpenPgpOperationResult : IDisposable, IEmailOpenPgpOperationResultContract
{
    EmailOpenPgpOperationStatus status;
    byte[] output;
    byte[] preservedInput;
    bool verified;
    EmailIssueList issues;
    bool disposed;
    /// <summary>
    /// Creates a new openPGP operation result.
    /// </summary>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "preservedInput">The preserved input used by the operation.</param>
    /// <param name = "verified">Whether to enable the verified behavior.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailOpenPgpOperationResult(EmailOpenPgpOperationStatus status, byte[]? output, byte[] preservedInput, bool verified, EmailIssueList issues)
    {
        if (status < EmailOpenPgpOperationStatus.Success || status > EmailOpenPgpOperationStatus.AuthenticationFailed)
            throw new ArgumentOutOfRangeException("status");
        if (output == null || preservedInput == null || issues == null)
            throw new ArgumentNullException("output");
        this.status = status;
        this.output = new EmailOpenPgpArmorResultOperations().Copy(output);
        this.preservedInput = new EmailOpenPgpArmorResultOperations().Copy(preservedInput);
        this.verified = verified;
        this.issues = new EmailOpenPgpArmorResultOperations().CopyIssues(issues);
        disposed = false;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !disposed && status == EmailOpenPgpOperationStatus.Success && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Determines whether the current value or state is unsupported.
    /// </summary>
    /// <returns><c>true</c> when the value or state is unsupported; otherwise, <c>false</c>.</returns>
    public bool IsUnsupported()
    {
        return status == EmailOpenPgpOperationStatus.UnsupportedAlgorithm;
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailOpenPgpOperationStatus GetStatus()
    {
        return status;
    }

    /// <summary>
    /// Determines whether the output is present or available.
    /// </summary>
    /// <returns><c>true</c> when the output is available; otherwise, <c>false</c>.</returns>
    public bool HasOutput()
    {
        return !disposed && output.Length > 0;
    }

    /// <summary>
    /// Gets the output.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current output.</returns>
    public byte[] GetOutput()
    {
        ThrowIfDisposed();
        return new EmailOpenPgpArmorResultOperations().Copy(output);
    }

    /// <summary>
    /// Gets the preserved input.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current preserved input.</returns>
    public byte[] GetPreservedInput()
    {
        ThrowIfDisposed();
        return new EmailOpenPgpArmorResultOperations().Copy(preservedInput);
    }

    /// <summary>
    /// Determines whether the current value or state is verified.
    /// </summary>
    /// <returns><c>true</c> when the value or state is verified; otherwise, <c>false</c>.</returns>
    public bool IsVerified()
    {
        return !disposed && status == EmailOpenPgpOperationStatus.Success && verified;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        return new EmailOpenPgpArmorResultOperations().CopyIssues(issues);
    }

    /// <summary>
    /// Determines whether the current value or state is disposed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is disposed; otherwise, <c>false</c>.</returns>
    public bool IsDisposed()
    {
        return disposed;
    }

    /// <summary>
    /// Releases streams, temporary storage, and other resources retained by this instance.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;
        CryptographicOperations.ZeroMemory(output);
        CryptographicOperations.ZeroMemory(preservedInput);
        output = new byte[0];
        preservedInput = new byte[0];
        disposed = true;
    }

    void ThrowIfDisposed()
    {
        if (disposed)
            throw new ObjectDisposedException("EmailOpenPgpOperationResult");
    }
}
