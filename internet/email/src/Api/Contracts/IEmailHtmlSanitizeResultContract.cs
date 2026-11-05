using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlSanitizeResult"/>.
/// </summary>
public interface IEmailHtmlSanitizeResultContract
{
    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <returns>The current HTML.</returns>
    string GetHtml();
    /// <summary>
    /// Gets the mutation count.
    /// </summary>
    /// <returns>The current mutation count.</returns>
    int GetMutationCount();
    /// <summary>
    /// Gets the mutation.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current mutation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailHtmlMutation GetMutation(int index);
    /// <summary>
    /// Gets the mutations.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current mutations.</returns>
    List<EmailHtmlMutation> GetMutations();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the tokenization result.
    /// </summary>
    /// <returns>The current tokenization result.</returns>
    EmailHtmlTokenizationResult GetTokenizationResult();
    /// <summary>
    /// Gets the blocked remote resource count.
    /// </summary>
    /// <returns>The current blocked remote resource count.</returns>
    int GetBlockedRemoteResourceCount();
    /// <summary>
    /// Gets the blocked unsafe url count.
    /// </summary>
    /// <returns>The current blocked unsafe url count.</returns>
    int GetBlockedUnsafeUrlCount();
    /// <summary>
    /// Gets the preserved cid resource count.
    /// </summary>
    /// <returns>The current preserved cid resource count.</returns>
    int GetPreservedCidResourceCount();
    /// <summary>
    /// Determines whether the operation or source value was modified.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasModified();
    /// <summary>
    /// Determines whether the source text decode result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source text decode result is available; otherwise, <c>false</c>.</returns>
    bool HasSourceTextDecodeResult();
    /// <summary>
    /// Gets the source text decode result.
    /// </summary>
    /// <returns>The current source text decode result.</returns>
    EmailTextDecodeResult? GetSourceTextDecodeResult();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
