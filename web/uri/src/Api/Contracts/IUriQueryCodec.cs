namespace uri;

/// <summary>
/// Parses, composes, and explicitly transforms ordered URI query parameters.
/// </summary>
public interface IUriQueryCodec
{
    /// <summary>Parses encoded query text into ordered decoded parameters.</summary>
    /// <param name="query">The query text without a leading question mark.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query parsing outcome.</returns>
    UriQueryResult ParseQuery(string query, UriQueryOptions options);

    /// <summary>Composes ordered decoded parameters as encoded query text.</summary>
    /// <param name="parameters">The ordered decoded parameters.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query composition outcome.</returns>
    UriQueryResult ComposeQuery(
        UriQueryParameter[] parameters,
        UriQueryOptions options);

    /// <summary>Appends one parameter and composes the resulting query.</summary>
    /// <param name="parameters">The existing ordered parameters.</param>
    /// <param name="parameter">The parameter to append.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query operation outcome.</returns>
    UriQueryResult AddQueryParameter(
        UriQueryParameter[] parameters,
        UriQueryParameter parameter,
        UriQueryOptions options);

    /// <summary>Replaces all ordinal name matches and composes the resulting query.</summary>
    /// <param name="parameters">The existing ordered parameters.</param>
    /// <param name="name">The decoded name to match ordinally.</param>
    /// <param name="replacements">The parameters inserted at the first match, or appended when absent.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query operation outcome.</returns>
    UriQueryResult ReplaceQueryParameters(
        UriQueryParameter[] parameters,
        string name,
        UriQueryParameter[] replacements,
        UriQueryOptions options);

    /// <summary>Removes every ordinal name match and composes the resulting query.</summary>
    /// <param name="parameters">The existing ordered parameters.</param>
    /// <param name="name">The decoded name to remove ordinally.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query operation outcome.</returns>
    UriQueryResult RemoveQueryParameters(
        UriQueryParameter[] parameters,
        string name,
        UriQueryOptions options);

    /// <summary>Stably sorts parameters by ordinal name, value state, and value.</summary>
    /// <param name="parameters">The existing ordered parameters.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query operation outcome.</returns>
    UriQueryResult SortQueryParameters(
        UriQueryParameter[] parameters,
        UriQueryOptions options);

    /// <summary>Removes exact semantic pair duplicates while keeping each first occurrence.</summary>
    /// <param name="parameters">The existing ordered parameters.</param>
    /// <param name="options">The query codec options.</param>
    /// <returns>The query operation outcome.</returns>
    UriQueryResult DeduplicateQueryParameters(
        UriQueryParameter[] parameters,
        UriQueryOptions options);
}
