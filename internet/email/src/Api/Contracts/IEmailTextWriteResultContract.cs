using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTextWriteResult"/>.
/// </summary>
public interface IEmailTextWriteResultContract
{
    /// <summary>Determines whether text was produced.</summary>
    /// <returns><c>true</c> when text is available; otherwise, <c>false</c>.</returns>
    bool HasText();
    /// <summary>Gets the produced text.</summary>
    /// <returns>The current text.</returns>
    string GetText();
    /// <summary>Gets the requested charset.</summary>
    /// <returns>The requested charset name.</returns>
    string GetRequestedCharset();
    /// <summary>Determines whether the resolved charset is present.</summary>
    /// <returns><c>true</c> when the resolved charset is available; otherwise, <c>false</c>.</returns>
    bool HasResolvedCharset();
    /// <summary>Gets the resolved charset.</summary>
    /// <returns>The resolved charset name, or <c>null</c> when none was resolved.</returns>
    string? GetResolvedCharset();
    /// <summary>Determines whether line endings were normalized.</summary>
    /// <returns><c>true</c> when line endings changed; otherwise, <c>false</c>.</returns>
    bool WasNewlineNormalized();
    /// <summary>Determines whether transfer encodings were decoded for validation.</summary>
    /// <returns><c>true</c> when transfer encodings were decoded; otherwise, <c>false</c>.</returns>
    bool WereTransferEncodingsDecoded();
    /// <summary>Determines whether an underlying byte write result is present.</summary>
    /// <returns><c>true</c> when a byte write result is available; otherwise, <c>false</c>.</returns>
    bool HasWriteResult();
    /// <summary>Gets the underlying byte write result.</summary>
    /// <returns>The byte write result.</returns>
    EmailWriteResult? GetWriteResult();
    /// <summary>Gets the issues.</summary>
    /// <returns>The ordered diagnostics.</returns>
    EmailIssueList GetIssues();
    /// <summary>Determines whether the operation succeeded.</summary>
    /// <returns><c>true</c> when text was produced without errors; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>Determines whether a fatal error occurred.</summary>
    /// <returns><c>true</c> when a fatal error is present; otherwise, <c>false</c>.</returns>
    bool HasFatalError();
}
