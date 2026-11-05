using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a HTML sanitize operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailHtmlSanitizeResult : IEmailHtmlSanitizeResultContract
{
    string html;
    List<EmailHtmlMutation> mutations;
    EmailIssueList issues;
    EmailHtmlTokenizationResult tokenization;
    int blockedRemoteResourceCount;
    int blockedUnsafeUrlCount;
    int preservedCidResourceCount;
    EmailTextDecodeResult? sourceTextDecodeResult;
    /// <summary>
    /// Creates a new HTML sanitize result.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <param name = "mutations">The mutations used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "tokenization">The tokenization used by the operation.</param>
    /// <param name = "blockedRemoteResourceCount">The number of items or bytes to process.</param>
    /// <param name = "blockedUnsafeUrlCount">The number of items or bytes to process.</param>
    /// <param name = "preservedCidResourceCount">The number of items or bytes to process.</param>
    /// <param name = "sourceTextDecodeResult">The source text decode result used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlSanitizeResult(string html, List<EmailHtmlMutation> mutations, EmailIssueList issues, EmailHtmlTokenizationResult tokenization, int blockedRemoteResourceCount, int blockedUnsafeUrlCount, int preservedCidResourceCount, EmailTextDecodeResult? sourceTextDecodeResult)
    {
        if (html == null)
            throw new ArgumentNullException("html");
        if (mutations == null)
            throw new ArgumentNullException("mutations");
        if (issues == null)
            throw new ArgumentNullException("issues");
        if (tokenization == null)
            throw new ArgumentNullException("tokenization");
        if (blockedRemoteResourceCount < 0)
            throw new ArgumentOutOfRangeException("blockedRemoteResourceCount");
        if (blockedUnsafeUrlCount < 0)
            throw new ArgumentOutOfRangeException("blockedUnsafeUrlCount");
        if (preservedCidResourceCount < 0)
            throw new ArgumentOutOfRangeException("preservedCidResourceCount");
        this.html = html;
        this.mutations = new List<EmailHtmlMutation>(mutations);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.tokenization = tokenization;
        this.blockedRemoteResourceCount = blockedRemoteResourceCount;
        this.blockedUnsafeUrlCount = blockedUnsafeUrlCount;
        this.preservedCidResourceCount = preservedCidResourceCount;
        this.sourceTextDecodeResult = sourceTextDecodeResult;
    }

    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <returns>The current HTML.</returns>
    public string GetHtml()
    {
        return html;
    }

    /// <summary>
    /// Gets the mutation count.
    /// </summary>
    /// <returns>The current mutation count.</returns>
    public int GetMutationCount()
    {
        return mutations.Count;
    }

    /// <summary>
    /// Gets the mutation.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current mutation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlMutation GetMutation(int index)
    {
        if (index < 0 || index >= mutations.Count)
            throw new ArgumentOutOfRangeException("index");
        return mutations[index];
    }

    /// <summary>
    /// Gets the mutations.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current mutations.</returns>
    public List<EmailHtmlMutation> GetMutations()
    {
        return new List<EmailHtmlMutation>(mutations);
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
    /// Gets the tokenization result.
    /// </summary>
    /// <returns>The current tokenization result.</returns>
    public EmailHtmlTokenizationResult GetTokenizationResult()
    {
        return tokenization;
    }

    /// <summary>
    /// Gets the blocked remote resource count.
    /// </summary>
    /// <returns>The current blocked remote resource count.</returns>
    public int GetBlockedRemoteResourceCount()
    {
        return blockedRemoteResourceCount;
    }

    /// <summary>
    /// Gets the blocked unsafe url count.
    /// </summary>
    /// <returns>The current blocked unsafe url count.</returns>
    public int GetBlockedUnsafeUrlCount()
    {
        return blockedUnsafeUrlCount;
    }

    /// <summary>
    /// Gets the preserved cid resource count.
    /// </summary>
    /// <returns>The current preserved cid resource count.</returns>
    public int GetPreservedCidResourceCount()
    {
        return preservedCidResourceCount;
    }

    /// <summary>
    /// Determines whether the operation or source value was modified.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasModified()
    {
        return mutations.Count > 0;
    }

    /// <summary>
    /// Determines whether the source text decode result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source text decode result is available; otherwise, <c>false</c>.</returns>
    public bool HasSourceTextDecodeResult()
    {
        return sourceTextDecodeResult != null;
    }

    /// <summary>
    /// Gets the source text decode result.
    /// </summary>
    /// <returns>The current source text decode result.</returns>
    public EmailTextDecodeResult? GetSourceTextDecodeResult()
    {
        return sourceTextDecodeResult;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
