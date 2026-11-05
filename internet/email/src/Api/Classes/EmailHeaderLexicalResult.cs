using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a header lexical operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailHeaderLexicalResult : IEmailHeaderLexicalResultContract
{
    List<EmailHeaderToken> tokens;
    EmailIssueList issues;
    int sourceByteCount;
    int consumedByteCount;
    byte[] remainingBytes;
    /// <summary>
    /// Creates a new header lexical result.
    /// </summary>
    /// <param name = "tokens">The tokens used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <param name = "consumedByteCount">The number of items or bytes to process.</param>
    /// <param name = "remainingBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHeaderLexicalResult(List<EmailHeaderToken> tokens, EmailIssueList issues, int sourceByteCount, int consumedByteCount, byte[] remainingBytes)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (sourceByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("sourceByteCount");
        }

        if (consumedByteCount < 0 || consumedByteCount > sourceByteCount)
        {
            throw new ArgumentOutOfRangeException("consumedByteCount");
        }

        if (remainingBytes == null)
        {
            throw new ArgumentNullException("remainingBytes");
        }

        this.tokens = new List<EmailHeaderToken>(tokens);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.sourceByteCount = sourceByteCount;
        this.consumedByteCount = consumedByteCount;
        this.remainingBytes = new byte[remainingBytes.Length];
        remainingBytes.CopyTo(this.remainingBytes, 0);
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
    public EmailHeaderToken GetToken(int index)
    {
        if (index < 0 || index >= tokens.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return tokens[index];
    }

    /// <summary>
    /// Gets the tokens.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current tokens.</returns>
    public List<EmailHeaderToken> GetTokens()
    {
        return new List<EmailHeaderToken>(tokens);
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
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public int GetSourceByteCount()
    {
        return sourceByteCount;
    }

    /// <summary>
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    public int GetConsumedByteCount()
    {
        return consumedByteCount;
    }

    /// <summary>
    /// Gets the remaining bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current remaining bytes.</returns>
    public byte[] GetRemainingBytes()
    {
        byte[] copy = new byte[remainingBytes.Length];
        remainingBytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return consumedByteCount == sourceByteCount;
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
