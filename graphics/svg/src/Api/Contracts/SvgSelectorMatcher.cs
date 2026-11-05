namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgSelectorMatcher"/>.</summary>
public interface ISvgSelectorMatcher
{
    /// <summary>Populates the maximum specificity fields of a selector list.</summary>
    /// <param name="selector">The parsed selector list.</param>
    void CalculateSpecificity(SvgSelector selector);
    /// <summary>Matches a parsed selector list against one element.</summary>
    /// <param name="selector">The parsed selector list.</param>
    /// <param name="node">The candidate node.</param>
    /// <param name="options">The namespace, scope, state, and work options.</param>
    /// <returns>The bounded match result.</returns>
    SvgSelectorMatchResult Match(SvgSelector? selector, SvgNode node, SvgSelectorMatchOptions options);
}
