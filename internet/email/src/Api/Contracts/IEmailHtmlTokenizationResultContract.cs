using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlTokenizationResult"/>.
/// </summary>
public interface IEmailHtmlTokenizationResultContract
{
    /// <summary>
    /// Gets the token count.
    /// </summary>
    /// <returns>The current token count.</returns>
    int GetTokenCount();
    /// <summary>
    /// Gets the token.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current token.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailHtmlToken GetToken(int index);
    /// <summary>
    /// Gets the tokens.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current tokens.</returns>
    List<EmailHtmlToken> GetTokens();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the source character count.
    /// </summary>
    /// <returns>The current source character count.</returns>
    int GetSourceCharacterCount();
    /// <summary>
    /// Gets the malformed markup count.
    /// </summary>
    /// <returns>The current malformed markup count.</returns>
    int GetMalformedMarkupCount();
    /// <summary>
    /// Gets the malformed entity count.
    /// </summary>
    /// <returns>The current malformed entity count.</returns>
    int GetMalformedEntityCount();
    /// <summary>
    /// Gets the maximum observed depth.
    /// </summary>
    /// <returns>The current maximum observed depth.</returns>
    int GetMaximumObservedDepth();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
