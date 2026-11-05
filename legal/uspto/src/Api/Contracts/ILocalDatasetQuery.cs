namespace uspto;

/// <summary>
/// Provides bounded structured queries over mirrored local datasets.
/// </summary>
public interface ILocalDatasetQuery
{
    /// <summary>Lists queryable local dataset tables.</summary>
    /// <param name="mirrorDirectory">The local mirror directory.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The discovered table names.</returns>
    string[] ListLocalDatasetTables(string mirrorDirectory, RequestContext context);

    /// <summary>Queries generic local rows.</summary>
    /// <param name="tableName">The local table name.</param>
    /// <param name="request">The bounded structured search request.</param>
    /// <returns>Metadata for the returned rows.</returns>
    SearchResponseMetadata QueryLocalRows(string tableName, SearchRequest request);

    /// <summary>Queries local patent records.</summary>
    /// <param name="request">The bounded structured search request.</param>
    /// <returns>Metadata for the returned records.</returns>
    SearchResponseMetadata QueryLocalPatents(SearchRequest request);

    /// <summary>Queries local pre-grant publication records.</summary>
    /// <param name="request">The bounded structured search request.</param>
    /// <returns>Metadata for the returned records.</returns>
    SearchResponseMetadata QueryLocalPublications(SearchRequest request);

    /// <summary>Queries local inventor records.</summary>
    /// <param name="request">The bounded structured search request.</param>
    /// <returns>Metadata for the returned records.</returns>
    SearchResponseMetadata QueryLocalInventors(SearchRequest request);

    /// <summary>Queries local assignee records.</summary>
    /// <param name="request">The bounded structured search request.</param>
    /// <returns>Metadata for the returned records.</returns>
    SearchResponseMetadata QueryLocalAssignees(SearchRequest request);

    /// <summary>Queries local classification records.</summary>
    /// <param name="request">The bounded structured search request.</param>
    /// <returns>Metadata for the returned records.</returns>
    SearchResponseMetadata QueryLocalClassifications(SearchRequest request);

    /// <summary>Queries local citation records.</summary>
    /// <param name="request">The bounded structured search request.</param>
    /// <returns>Metadata for the returned records.</returns>
    SearchResponseMetadata QueryLocalCitations(SearchRequest request);
}
