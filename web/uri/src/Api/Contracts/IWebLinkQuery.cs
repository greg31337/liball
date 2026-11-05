namespace uri;

/// <summary>
/// Selects Web Links using relation-aware and URI-aware criteria.
/// </summary>
public interface IWebLinkQuery
{
    /// <summary>Filters links using every active criterion.</summary>
    /// <param name="linkSet">The links to inspect.</param>
    /// <param name="filter">The criteria to combine.</param>
    /// <returns>The matching links in source order.</returns>
    WebLinkSet FilterWebLinks(
        WebLinkSet linkSet,
        WebLinkFilter filter);

    /// <summary>Finds links with the requested relation type.</summary>
    /// <param name="linkSet">The links to inspect.</param>
    /// <param name="relation">The relation type to match.</param>
    /// <returns>The matching links in source order.</returns>
    WebLinkSet FindLinksByRelation(
        WebLinkSet linkSet,
        string relation);

    /// <summary>Finds links with the requested target.</summary>
    /// <param name="linkSet">The links to inspect.</param>
    /// <param name="target">The target to match.</param>
    /// <returns>The matching links in source order.</returns>
    WebLinkSet FindLinksByTarget(
        WebLinkSet linkSet,
        string target);

    /// <summary>Finds links with the requested effective context.</summary>
    /// <param name="linkSet">The links to inspect.</param>
    /// <param name="context">The context to match.</param>
    /// <returns>The matching links in source order.</returns>
    WebLinkSet FindLinksByContext(
        WebLinkSet linkSet,
        string context);
}
