using System;

namespace email;
/// <summary>
/// Describes the outcome of bounded text serialization.
/// </summary>
/// <remarks>
/// The underlying byte result identifies the canonical serialization from which successful text was decoded.
/// </remarks>
public class EmailTextWriteResult : IEmailTextWriteResultContract
{
    string? text;
    string requestedCharset;
    string? resolvedCharset;
    bool newlineNormalized;
    bool transferEncodingsDecoded;
    EmailWriteResult? writeResult;
    EmailIssueList issues;
    /// <summary>
    /// Creates a text write result.
    /// </summary>
    /// <param name = "text">The produced text, or <c>null</c> when serialization failed.</param>
    /// <param name = "requestedCharset">The requested charset.</param>
    /// <param name = "resolvedCharset">The resolved charset, or <c>null</c> when none was resolved.</param>
    /// <param name = "newlineNormalized">Whether line endings changed.</param>
    /// <param name = "transferEncodingsDecoded">Whether transfer encodings were decoded for validation.</param>
    /// <param name = "writeResult">The underlying byte write result, or <c>null</c> when writing was not attempted.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTextWriteResult(string? text, string requestedCharset, string? resolvedCharset, bool newlineNormalized, bool transferEncodingsDecoded, EmailWriteResult? writeResult, EmailIssueList issues)
    {
        if (requestedCharset == null)
        {
            throw new ArgumentNullException("requestedCharset");
        }

        if (requestedCharset.Length == 0)
        {
            throw new ArgumentException("The requested charset cannot be empty.", "requestedCharset");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (text != null && resolvedCharset == null)
        {
            throw new ArgumentException("Produced text requires a resolved charset.", "resolvedCharset");
        }

        this.text = text;
        this.requestedCharset = requestedCharset;
        this.resolvedCharset = resolvedCharset;
        this.newlineNormalized = newlineNormalized;
        this.transferEncodingsDecoded = transferEncodingsDecoded;
        this.writeResult = writeResult;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>Determines whether text was produced.</summary>
    /// <returns><c>true</c> when text is available; otherwise, <c>false</c>.</returns>
    public bool HasText()
    {
        return text != null;
    }

    /// <summary>Gets the produced text.</summary>
    /// <returns>The current text.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when no text was produced.</exception>
    public string GetText()
    {
        if (text == null)
        {
            throw new InvalidOperationException("No text was produced.");
        }

        return text;
    }

    /// <summary>Gets the requested charset.</summary>
    /// <returns>The requested charset name.</returns>
    public string GetRequestedCharset()
    {
        return requestedCharset;
    }

    /// <summary>Determines whether the resolved charset is present.</summary>
    /// <returns><c>true</c> when the resolved charset is available; otherwise, <c>false</c>.</returns>
    public bool HasResolvedCharset()
    {
        return resolvedCharset != null;
    }

    /// <summary>Gets the resolved charset.</summary>
    /// <returns>The resolved charset name, or <c>null</c> when none was resolved.</returns>
    public string? GetResolvedCharset()
    {
        return resolvedCharset;
    }

    /// <summary>Determines whether line endings were normalized.</summary>
    /// <returns><c>true</c> when line endings changed; otherwise, <c>false</c>.</returns>
    public bool WasNewlineNormalized()
    {
        return newlineNormalized;
    }

    /// <summary>Determines whether transfer encodings were decoded for validation.</summary>
    /// <returns><c>true</c> when transfer encodings were decoded; otherwise, <c>false</c>.</returns>
    public bool WereTransferEncodingsDecoded()
    {
        return transferEncodingsDecoded;
    }

    /// <summary>Determines whether an underlying byte write result is present.</summary>
    /// <returns><c>true</c> when a byte write result is available; otherwise, <c>false</c>.</returns>
    public bool HasWriteResult()
    {
        return writeResult != null;
    }

    /// <summary>Gets the underlying byte write result.</summary>
    /// <returns>The byte write result.</returns>
    public EmailWriteResult? GetWriteResult()
    {
        return writeResult;
    }

    /// <summary>Gets the issues.</summary>
    /// <returns>The ordered diagnostics.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>Determines whether the operation succeeded.</summary>
    /// <returns><c>true</c> when text was produced without errors; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return text != null && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>Determines whether a fatal error occurred.</summary>
    /// <returns><c>true</c> when a fatal error is present; otherwise, <c>false</c>.</returns>
    public bool HasFatalError()
    {
        return issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal);
    }
}
