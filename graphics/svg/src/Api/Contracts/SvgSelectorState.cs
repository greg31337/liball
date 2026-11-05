namespace svg;

/// <summary>Supplies a cohesive snapshot of host-managed dynamic selector state.</summary>
/// <remarks>The version must change whenever any returned state or the host language changes. This lets style resolvers invalidate cached selector results without inspecting provider implementation details.</remarks>
public interface ISvgDynamicSelectorState
{
    /// <summary>Gets the caller-managed state revision.</summary>
    /// <returns>The current nonnegative revision.</returns>
    long GetVersion();
    /// <summary>Gets the normalized host content language used when no inherited <c>xml:lang</c> exists.</summary>
    /// <returns>The host language, or an empty string.</returns>
    string GetLanguage();
    /// <summary>Gets the link state for one element.</summary>
    /// <param name="node">The candidate element.</param>
    /// <returns>The current link state.</returns>
    SvgSelectorLinkState GetLinkState(SvgNode node);
    /// <summary>Tests whether one element is hovered.</summary>
    /// <param name="node">The candidate element.</param>
    /// <returns>Whether the element is hovered.</returns>
    bool IsHovered(SvgNode node);
    /// <summary>Tests whether one element is active.</summary>
    /// <param name="node">The candidate element.</param>
    /// <returns>Whether the element is active.</returns>
    bool IsActive(SvgNode node);
    /// <summary>Tests whether one element is focused.</summary>
    /// <param name="node">The candidate element.</param>
    /// <returns>Whether the element is focused.</returns>
    bool IsFocused(SvgNode node);
    /// <summary>Tests whether one element is the current URL target.</summary>
    /// <param name="node">The candidate element.</param>
    /// <returns>Whether the element is the current target.</returns>
    bool IsTarget(SvgNode node);
}

/// <summary>Advances the revision of caller-managed dynamic selector state.</summary>
public interface ISvgMutableDynamicSelectorState
{
    /// <summary>Marks caller-managed interaction state changed.</summary>
    /// <returns>The incremented interaction-state version.</returns>
    /// <exception cref="System.OverflowException">Thrown when the version cannot be incremented.</exception>
    long MarkChanged();
}

/// <summary>Defines the public operations exposed by <see cref="SvgDynamicSelectorMatcher"/>.</summary>
public interface ISvgDynamicSelectorMatcher
{
    /// <summary>Matches one parsed dynamic pseudo-class against a node and explicit host state.</summary>
    /// <param name="selector">The parsed dynamic pseudo-class.</param>
    /// <param name="node">The candidate element node.</param>
    /// <param name="state">The host-provided state service.</param>
    /// <returns>A safe match result with diagnostics for invalid or unsupported input.</returns>
    SvgDynamicSelectorMatchResult Match(SvgSimpleSelector? selector, SvgNode node, ISvgDynamicSelectorState state);
}
