namespace svg;

/// <summary>Provides host callbacks for dynamic selector states without a browser event loop.</summary>
/// <remarks>Call <see cref="MarkChanged"/> after changing callback-captured state when cached style or document resolution may be reused.</remarks>
public class SvgDynamicSelectorState : ISvgDynamicSelectorState, ISvgMutableDynamicSelectorState
{
    /// <summary>The caller-managed interaction-state revision.</summary>
    internal long Version;
    /// <summary>Returns the link state for a node, or <c>null</c> when link state is unavailable.</summary>
    internal System.Func<SvgNode, SvgSelectorLinkState>? LinkState;
    /// <summary>Returns whether a node is hovered, or <c>null</c> when hover state is unavailable.</summary>
    internal System.Func<SvgNode, bool>? HoverState;
    /// <summary>Returns whether a node is active, or <c>null</c> when active state is unavailable.</summary>
    internal System.Func<SvgNode, bool>? ActiveState;
    /// <summary>Returns whether a node is focused, or <c>null</c> when focus state is unavailable.</summary>
    internal System.Func<SvgNode, bool>? FocusState;
    /// <summary>Returns whether a node is the current URL target, or <c>null</c> when target state is unavailable.</summary>
    internal System.Func<SvgNode, bool>? TargetState;
    /// <summary>The normalized host content language used when no inherited <c>xml:lang</c> exists.</summary>
    internal string Language;

    /// <summary>Creates dynamic selector state from caller-owned callback options.</summary>
    /// <param name="options">The callbacks and fallback language to retain.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>options</c> is <c>null</c>.</exception>
    public SvgDynamicSelectorState(SvgDynamicSelectorStateOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        Version = 0;
        LinkState = options.LinkState;
        HoverState = options.HoverState;
        ActiveState = options.ActiveState;
        FocusState = options.FocusState;
        TargetState = options.TargetState;
        Language = options.Language;
    }

    /// <summary>Marks caller-managed interaction state changed.</summary>
    /// <returns>The incremented interaction-state version.</returns>
    /// <exception cref="System.OverflowException">Thrown when the version cannot be incremented.</exception>
    public long MarkChanged()
    {
        Version = Version + 1;
        return Version;
    }

    /// <summary>Gets the caller-managed state revision.</summary>
    /// <returns>The current revision.</returns>
    public long GetVersion()
    {
        return Version;
    }

    /// <summary>Gets the normalized fallback host language.</summary>
    /// <returns>The host language, or an empty string.</returns>
    public string GetLanguage()
    {
        return Language;
    }

    /// <summary>Gets the link state supplied by the configured callback.</summary>
    /// <param name="node">The candidate element.</param>
    /// <returns>The current state, or <see cref="SvgSelectorLinkState.None"/> when unavailable.</returns>
    public SvgSelectorLinkState GetLinkState(SvgNode node)
    {
        return LinkState == null ? SvgSelectorLinkState.None : LinkState(node);
    }

    /// <summary>Tests hover state through the configured callback.</summary>
    /// <param name="node">The candidate element.</param>
    /// <returns>Whether the element is hovered.</returns>
    public bool IsHovered(SvgNode node)
    {
        return HoverState != null && HoverState(node);
    }

    /// <summary>Tests active state through the configured callback.</summary>
    /// <param name="node">The candidate element.</param>
    /// <returns>Whether the element is active.</returns>
    public bool IsActive(SvgNode node)
    {
        return ActiveState != null && ActiveState(node);
    }

    /// <summary>Tests focus state through the configured callback.</summary>
    /// <param name="node">The candidate element.</param>
    /// <returns>Whether the element is focused.</returns>
    public bool IsFocused(SvgNode node)
    {
        return FocusState != null && FocusState(node);
    }

    /// <summary>Tests target state through the configured callback.</summary>
    /// <param name="node">The candidate element.</param>
    /// <returns>Whether the element is the current target.</returns>
    public bool IsTarget(SvgNode node)
    {
        return TargetState != null && TargetState(node);
    }
}
