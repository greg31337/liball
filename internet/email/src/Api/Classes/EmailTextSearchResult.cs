using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a text search operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailTextSearchResult : IEmailTextSearchResultContract
{
    string query;
    List<EmailTextSearchMatch> matches;
    EmailIssueList issues;
    int searchedEntityCount;
    int searchedCharacterCount;
    bool truncated;
    /// <summary>
    /// Creates a new text search result.
    /// </summary>
    /// <param name = "query">The query used by the operation.</param>
    /// <param name = "matches">The matches used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "searchedEntityCount">The number of items or bytes to process.</param>
    /// <param name = "searchedCharacterCount">The number of items or bytes to process.</param>
    /// <param name = "truncated">Whether to enable the truncated behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTextSearchResult(string query, List<EmailTextSearchMatch> matches, EmailIssueList issues, int searchedEntityCount, int searchedCharacterCount, bool truncated)
    {
        if (query == null || query.Length == 0)
            throw new ArgumentException("A search query is required.", "query");
        if (matches == null)
            throw new ArgumentNullException("matches");
        if (issues == null)
            throw new ArgumentNullException("issues");
        if (searchedEntityCount < 0)
            throw new ArgumentOutOfRangeException("searchedEntityCount");
        if (searchedCharacterCount < 0)
            throw new ArgumentOutOfRangeException("searchedCharacterCount");
        this.query = query;
        this.matches = new List<EmailTextSearchMatch>(matches);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.searchedEntityCount = searchedEntityCount;
        this.searchedCharacterCount = searchedCharacterCount;
        this.truncated = truncated;
    }

    /// <summary>
    /// Gets the query.
    /// </summary>
    /// <returns>The current query.</returns>
    public string GetQuery()
    {
        return query;
    }

    /// <summary>
    /// Gets the match count.
    /// </summary>
    /// <returns>The current match count.</returns>
    public int GetMatchCount()
    {
        return matches.Count;
    }

    /// <summary>
    /// Gets the match.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current match.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTextSearchMatch GetMatch(int index)
    {
        if (index < 0 || index >= matches.Count)
            throw new ArgumentOutOfRangeException("index");
        return matches[index];
    }

    /// <summary>
    /// Gets the matches.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current matches.</returns>
    public List<EmailTextSearchMatch> GetMatches()
    {
        return new List<EmailTextSearchMatch>(matches);
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
    /// Gets the searched entity count.
    /// </summary>
    /// <returns>The current searched entity count.</returns>
    public int GetSearchedEntityCount()
    {
        return searchedEntityCount;
    }

    /// <summary>
    /// Gets the searched character count.
    /// </summary>
    /// <returns>The current searched character count.</returns>
    public int GetSearchedCharacterCount()
    {
        return searchedCharacterCount;
    }

    /// <summary>
    /// Determines whether the operation or source value was truncated.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasTruncated()
    {
        return truncated;
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
