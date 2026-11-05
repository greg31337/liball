using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTextSearchResult"/>.
/// </summary>
public interface IEmailTextSearchResultContract
{
    /// <summary>
    /// Gets the query.
    /// </summary>
    /// <returns>The current query.</returns>
    string GetQuery();
    /// <summary>
    /// Gets the match count.
    /// </summary>
    /// <returns>The current match count.</returns>
    int GetMatchCount();
    /// <summary>
    /// Gets the match.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current match.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailTextSearchMatch GetMatch(int index);
    /// <summary>
    /// Gets the matches.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current matches.</returns>
    List<EmailTextSearchMatch> GetMatches();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the searched entity count.
    /// </summary>
    /// <returns>The current searched entity count.</returns>
    int GetSearchedEntityCount();
    /// <summary>
    /// Gets the searched character count.
    /// </summary>
    /// <returns>The current searched character count.</returns>
    int GetSearchedCharacterCount();
    /// <summary>
    /// Determines whether the operation or source value was truncated.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasTruncated();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
