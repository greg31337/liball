using System;

namespace email;
/// <summary>
/// Describes the outcome of a charset decode operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailCharsetDecodeResult : IEmailCharsetDecodeResultContract
{
    string text;
    string? requestedCharset;
    string? resolvedCharset;
    string? bomCharset;
    int bomByteCount;
    byte[] undecodableBytes;
    bool usedReplacement;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new charset decode result.
    /// </summary>
    /// <param name = "text">The text used by the operation.</param>
    /// <param name = "requestedCharset">The requested charset used by the operation.</param>
    /// <param name = "resolvedCharset">The resolved charset used by the operation.</param>
    /// <param name = "bomCharset">The bom charset used by the operation.</param>
    /// <param name = "bomByteCount">The number of items or bytes to process.</param>
    /// <param name = "undecodableBytes">The source bytes or data to process.</param>
    /// <param name = "usedReplacement">Whether to enable the used replacement behavior.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailCharsetDecodeResult(string text, string? requestedCharset, string? resolvedCharset, string? bomCharset, int bomByteCount, byte[] undecodableBytes, bool usedReplacement, EmailIssueList issues)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (undecodableBytes == null)
        {
            throw new ArgumentNullException("undecodableBytes");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (bomByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("bomByteCount");
        }

        this.text = text;
        this.requestedCharset = requestedCharset;
        this.resolvedCharset = resolvedCharset;
        this.bomCharset = bomCharset;
        this.bomByteCount = bomByteCount;
        this.undecodableBytes = new byte[undecodableBytes.Length];
        undecodableBytes.CopyTo(this.undecodableBytes, 0);
        this.usedReplacement = usedReplacement;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    public string GetText()
    {
        return text;
    }

    /// <summary>
    /// Determines whether the requested charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the requested charset is available; otherwise, <c>false</c>.</returns>
    public bool HasRequestedCharset()
    {
        return requestedCharset != null;
    }

    /// <summary>
    /// Gets the requested charset.
    /// </summary>
    /// <returns>The current requested charset.</returns>
    public string? GetRequestedCharset()
    {
        return requestedCharset;
    }

    /// <summary>
    /// Determines whether the resolved charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the resolved charset is available; otherwise, <c>false</c>.</returns>
    public bool HasResolvedCharset()
    {
        return resolvedCharset != null;
    }

    /// <summary>
    /// Gets the resolved charset.
    /// </summary>
    /// <returns>The current resolved charset.</returns>
    public string? GetResolvedCharset()
    {
        return resolvedCharset;
    }

    /// <summary>
    /// Determines whether the BOM is present or available.
    /// </summary>
    /// <returns><c>true</c> when the BOM is available; otherwise, <c>false</c>.</returns>
    public bool HasBom()
    {
        return bomCharset != null;
    }

    /// <summary>
    /// Gets the BOM charset.
    /// </summary>
    /// <returns>The current BOM charset.</returns>
    public string? GetBomCharset()
    {
        return bomCharset;
    }

    /// <summary>
    /// Gets the BOM byte count.
    /// </summary>
    /// <returns>The current BOM byte count.</returns>
    public int GetBomByteCount()
    {
        return bomByteCount;
    }

    /// <summary>
    /// Gets the undecodable bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current undecodable bytes.</returns>
    public byte[] GetUndecodableBytes()
    {
        byte[] copy = new byte[undecodableBytes.Length];
        undecodableBytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Determines whether the operation used replacement.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool UsedReplacement()
    {
        return usedReplacement;
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
}
