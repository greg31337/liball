using System;

namespace email;
/// <summary>
/// Describes the outcome of a header write operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailHeaderWriteResult : IEmailHeaderWriteResultContract
{
    EmailHeaderWriteMode mode;
    byte[] bytes;
    EmailIssueList issues;
    int headerCount;
    int copiedHeaderCount;
    int regeneratedHeaderCount;
    int foldCount;
    /// <summary>
    /// Creates a new header write result.
    /// </summary>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "bytes">The source bytes or data to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "headerCount">The number of items or bytes to process.</param>
    /// <param name = "copiedHeaderCount">The number of items or bytes to process.</param>
    /// <param name = "regeneratedHeaderCount">The number of items or bytes to process.</param>
    /// <param name = "foldCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHeaderWriteResult(EmailHeaderWriteMode mode, byte[] bytes, EmailIssueList issues, int headerCount, int copiedHeaderCount, int regeneratedHeaderCount, int foldCount)
    {
        if (mode < EmailHeaderWriteMode.Raw || mode > EmailHeaderWriteMode.Canonical)
        {
            throw new ArgumentOutOfRangeException("mode");
        }

        if (bytes == null || issues == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (headerCount < 0 || copiedHeaderCount < 0 || regeneratedHeaderCount < 0 || copiedHeaderCount + regeneratedHeaderCount > headerCount || foldCount < 0)
        {
            throw new ArgumentOutOfRangeException("headerCount");
        }

        this.mode = mode;
        this.bytes = Copy(bytes);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.headerCount = headerCount;
        this.copiedHeaderCount = copiedHeaderCount;
        this.regeneratedHeaderCount = regeneratedHeaderCount;
        this.foldCount = foldCount;
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailHeaderWriteMode GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current bytes.</returns>
    public byte[] GetBytes()
    {
        return Copy(bytes);
    }

    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    public int GetByteCount()
    {
        return bytes.Length;
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
    /// Gets the header count.
    /// </summary>
    /// <returns>The current header count.</returns>
    public int GetHeaderCount()
    {
        return headerCount;
    }

    /// <summary>
    /// Gets the copied header count.
    /// </summary>
    /// <returns>The current copied header count.</returns>
    public int GetCopiedHeaderCount()
    {
        return copiedHeaderCount;
    }

    /// <summary>
    /// Gets the regenerated header count.
    /// </summary>
    /// <returns>The current regenerated header count.</returns>
    public int GetRegeneratedHeaderCount()
    {
        return regeneratedHeaderCount;
    }

    /// <summary>
    /// Gets the fold count.
    /// </summary>
    /// <returns>The current fold count.</returns>
    public int GetFoldCount()
    {
        return foldCount;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
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

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
