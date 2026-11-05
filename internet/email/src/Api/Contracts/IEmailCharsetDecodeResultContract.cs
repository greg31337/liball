using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCharsetDecodeResult"/>.
/// </summary>
public interface IEmailCharsetDecodeResultContract
{
    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    string GetText();
    /// <summary>
    /// Determines whether the requested charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the requested charset is available; otherwise, <c>false</c>.</returns>
    bool HasRequestedCharset();
    /// <summary>
    /// Gets the requested charset.
    /// </summary>
    /// <returns>The current requested charset.</returns>
    string? GetRequestedCharset();
    /// <summary>
    /// Determines whether the resolved charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the resolved charset is available; otherwise, <c>false</c>.</returns>
    bool HasResolvedCharset();
    /// <summary>
    /// Gets the resolved charset.
    /// </summary>
    /// <returns>The current resolved charset.</returns>
    string? GetResolvedCharset();
    /// <summary>
    /// Determines whether the BOM is present or available.
    /// </summary>
    /// <returns><c>true</c> when the BOM is available; otherwise, <c>false</c>.</returns>
    bool HasBom();
    /// <summary>
    /// Gets the BOM charset.
    /// </summary>
    /// <returns>The current BOM charset.</returns>
    string? GetBomCharset();
    /// <summary>
    /// Gets the BOM byte count.
    /// </summary>
    /// <returns>The current BOM byte count.</returns>
    int GetBomByteCount();
    /// <summary>
    /// Gets the undecodable bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current undecodable bytes.</returns>
    byte[] GetUndecodableBytes();
    /// <summary>
    /// Determines whether the operation used replacement.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool UsedReplacement();
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
