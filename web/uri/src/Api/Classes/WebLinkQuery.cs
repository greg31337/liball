namespace uri;

/// <summary>
/// Provides relation-aware and URI-aware Web Link selection.
/// </summary>
public class WebLinkQuery : IWebLinkQuery
{
    /// <summary>Initializes a stateless Web Link query service.</summary>
    public WebLinkQuery()
    {
    }

    /// <summary>Filters links using every active criterion.</summary>
    /// <param name="linkSet">The links to inspect.</param>
    /// <param name="filter">The criteria to combine.</param>
    /// <returns>The matching links in source order.</returns>
    public WebLinkSet FilterWebLinks(
        WebLinkSet linkSet,
        WebLinkFilter filter)
    {
        return WebLinkQueryCore.FilterWebLinks(linkSet, filter);
    }

    /// <summary>Finds links with the requested relation type.</summary>
    /// <param name="linkSet">The links to inspect.</param>
    /// <param name="relation">The relation type to match.</param>
    /// <returns>The matching links in source order.</returns>
    public WebLinkSet FindLinksByRelation(
        WebLinkSet linkSet,
        string relation)
    {
        return WebLinkQueryCore.FindLinksByRelation(linkSet, relation);
    }

    /// <summary>Finds links with the requested target.</summary>
    /// <param name="linkSet">The links to inspect.</param>
    /// <param name="target">The target to match.</param>
    /// <returns>The matching links in source order.</returns>
    public WebLinkSet FindLinksByTarget(
        WebLinkSet linkSet,
        string target)
    {
        return WebLinkQueryCore.FindLinksByTarget(linkSet, target);
    }

    /// <summary>Finds links with the requested effective context.</summary>
    /// <param name="linkSet">The links to inspect.</param>
    /// <param name="context">The context to match.</param>
    /// <returns>The matching links in source order.</returns>
    public WebLinkSet FindLinksByContext(
        WebLinkSet linkSet,
        string context)
    {
        return WebLinkQueryCore.FindLinksByContext(linkSet, context);
    }
}
