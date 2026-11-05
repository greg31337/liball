using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines typed operations for JMAP search snippets.
/// </summary>
public interface ISearchSnippetClient
{
    /// <summary>Gets highlighted search snippets for selected Emails.</summary>
    /// <param name="request">The SearchSnippet/get arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the SearchSnippet/get result.</returns>
    Task<SearchSnippetGetResponse> GetSearchSnippetsAsync(SearchSnippetGetRequest request, CancellationToken cancellationToken);
}
