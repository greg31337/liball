using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a HTML tokenization operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailHtmlTokenizationResult : IEmailHtmlTokenizationResultContract
{
    List<EmailHtmlToken> tokens;
    EmailIssueList issues;
    int sourceCharacterCount;
    int malformedMarkupCount;
    int malformedEntityCount;
    int maximumObservedDepth;
    /// <summary>
    /// Creates a new HTML tokenization result.
    /// </summary>
    /// <param name = "tokens">The tokens used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "sourceCharacterCount">The number of items or bytes to process.</param>
    /// <param name = "malformedMarkupCount">The number of items or bytes to process.</param>
    /// <param name = "malformedEntityCount">The number of items or bytes to process.</param>
    /// <param name = "maximumObservedDepth">The maximum observed depth used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlTokenizationResult(List<EmailHtmlToken> tokens, EmailIssueList issues, int sourceCharacterCount, int malformedMarkupCount, int malformedEntityCount, int maximumObservedDepth)
    {
        if (tokens == null)
            throw new ArgumentNullException("tokens");
        if (issues == null)
            throw new ArgumentNullException("issues");
        if (sourceCharacterCount < 0)
            throw new ArgumentOutOfRangeException("sourceCharacterCount");
        if (malformedMarkupCount < 0)
            throw new ArgumentOutOfRangeException("malformedMarkupCount");
        if (malformedEntityCount < 0)
            throw new ArgumentOutOfRangeException("malformedEntityCount");
        if (maximumObservedDepth < 0)
            throw new ArgumentOutOfRangeException("maximumObservedDepth");
        this.tokens = new List<EmailHtmlToken>(tokens);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.sourceCharacterCount = sourceCharacterCount;
        this.malformedMarkupCount = malformedMarkupCount;
        this.malformedEntityCount = malformedEntityCount;
        this.maximumObservedDepth = maximumObservedDepth;
    }

    /// <summary>
    /// Gets the token count.
    /// </summary>
    /// <returns>The current token count.</returns>
    public int GetTokenCount()
    {
        return tokens.Count;
    }

    /// <summary>
    /// Gets the token.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current token.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlToken GetToken(int index)
    {
        if (index < 0 || index >= tokens.Count)
            throw new ArgumentOutOfRangeException("index");
        return tokens[index];
    }

    /// <summary>
    /// Gets the tokens.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current tokens.</returns>
    public List<EmailHtmlToken> GetTokens()
    {
        return new List<EmailHtmlToken>(tokens);
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
    /// Gets the source character count.
    /// </summary>
    /// <returns>The current source character count.</returns>
    public int GetSourceCharacterCount()
    {
        return sourceCharacterCount;
    }

    /// <summary>
    /// Gets the malformed markup count.
    /// </summary>
    /// <returns>The current malformed markup count.</returns>
    public int GetMalformedMarkupCount()
    {
        return malformedMarkupCount;
    }

    /// <summary>
    /// Gets the malformed entity count.
    /// </summary>
    /// <returns>The current malformed entity count.</returns>
    public int GetMalformedEntityCount()
    {
        return malformedEntityCount;
    }

    /// <summary>
    /// Gets the maximum observed depth.
    /// </summary>
    /// <returns>The current maximum observed depth.</returns>
    public int GetMaximumObservedDepth()
    {
        return maximumObservedDepth;
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
