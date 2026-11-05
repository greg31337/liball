using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Contains the outcomes produced by authentication.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailAuthenticationResults : IEmailAuthenticationResultsContract
{
    string authenticationServiceIdentifier;
    bool versionPresent;
    int version;
    bool noResults;
    List<EmailAuthenticationMethodResult> results;
    List<EmailAuthenticationComment> comments;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new authentication results.
    /// </summary>
    /// <param name = "authenticationServiceIdentifier">The authentication service identifier used by the operation.</param>
    /// <param name = "versionPresent">Whether to enable the version present behavior.</param>
    /// <param name = "version">The version used by the operation.</param>
    /// <param name = "noResults">Whether to enable the no results behavior.</param>
    /// <param name = "results">The results used by the operation.</param>
    /// <param name = "comments">The comments used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAuthenticationResults(string authenticationServiceIdentifier, bool versionPresent, int version, bool noResults, List<EmailAuthenticationMethodResult> results, List<EmailAuthenticationComment> comments, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (authenticationServiceIdentifier == null || authenticationServiceIdentifier.Length == 0)
        {
            throw new ArgumentException("An authentication service identifier is required.", "authenticationServiceIdentifier");
        }

        if (versionPresent && version < 0)
        {
            throw new ArgumentOutOfRangeException("version");
        }

        if (results == null || comments == null || rawBytes == null || sourcePosition == null)
        {
            throw new ArgumentNullException("results");
        }

        if (noResults && results.Count != 0)
        {
            throw new ArgumentException("The no-results marker cannot accompany method results.", "noResults");
        }

        this.authenticationServiceIdentifier = authenticationServiceIdentifier;
        this.versionPresent = versionPresent;
        this.version = version;
        this.noResults = noResults;
        this.results = new List<EmailAuthenticationMethodResult>(results);
        this.comments = new List<EmailAuthenticationComment>(comments);
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the authentication service identifier.
    /// </summary>
    /// <returns>The current authentication service identifier.</returns>
    public string GetAuthenticationServiceIdentifier()
    {
        return authenticationServiceIdentifier;
    }

    /// <summary>
    /// Determines whether the version is present or available.
    /// </summary>
    /// <returns><c>true</c> when the version is available; otherwise, <c>false</c>.</returns>
    public bool HasVersion()
    {
        return versionPresent;
    }

    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    public int GetVersion()
    {
        return version;
    }

    /// <summary>
    /// Determines whether the no results is present or available.
    /// </summary>
    /// <returns><c>true</c> when the no results is available; otherwise, <c>false</c>.</returns>
    public bool HasNoResults()
    {
        return noResults;
    }

    /// <summary>
    /// Gets the result count.
    /// </summary>
    /// <returns>The current result count.</returns>
    public int GetResultCount()
    {
        return results.Count;
    }

    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current result.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAuthenticationMethodResult GetResult(int index)
    {
        if (index < 0 || index >= results.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return results[index];
    }

    /// <summary>
    /// Gets the results.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current results.</returns>
    public List<EmailAuthenticationMethodResult> GetResults()
    {
        return new List<EmailAuthenticationMethodResult>(results);
    }

    /// <summary>
    /// Gets the comment count.
    /// </summary>
    /// <returns>The current comment count.</returns>
    public int GetCommentCount()
    {
        return comments.Count;
    }

    /// <summary>
    /// Gets the comment.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current comment.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAuthenticationComment GetComment(int index)
    {
        if (index < 0 || index >= comments.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return comments[index];
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        return Copy(rawBytes);
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <returns>The authentication format result produced by the operation.</returns>
    public EmailAuthenticationFormatResult Format()
    {
        return new EmailAuthenticationFormatter().FormatWithValue(this);
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The authentication format result produced by the operation.</returns>
    public EmailAuthenticationFormatResult FormatWithOptions(EmailAuthenticationFormatOptions options)
    {
        return new EmailAuthenticationFormatter().FormatWithValueAndOptions(this, options);
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
