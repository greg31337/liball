namespace svg;

/// <summary>Configures callbacks and language for <see cref="SvgDynamicSelectorState"/>.</summary>
public class SvgDynamicSelectorStateOptions
{
    /// <summary>Returns the link state for a node, or <c>null</c> when link state is unavailable.</summary>
    public System.Func<SvgNode, SvgSelectorLinkState>? LinkState;
    /// <summary>Returns whether a node is hovered, or <c>null</c> when hover state is unavailable.</summary>
    public System.Func<SvgNode, bool>? HoverState;
    /// <summary>Returns whether a node is active, or <c>null</c> when active state is unavailable.</summary>
    public System.Func<SvgNode, bool>? ActiveState;
    /// <summary>Returns whether a node is focused, or <c>null</c> when focus state is unavailable.</summary>
    public System.Func<SvgNode, bool>? FocusState;
    /// <summary>Returns whether a node is the current URL target, or <c>null</c> when target state is unavailable.</summary>
    public System.Func<SvgNode, bool>? TargetState;
    /// <summary>The normalized host content language used when no inherited <c>xml:lang</c> exists.</summary>
    public string Language;

    /// <summary>Creates options with no callbacks and an empty fallback language.</summary>
    public SvgDynamicSelectorStateOptions()
    {
        LinkState = null;
        HoverState = null;
        ActiveState = null;
        FocusState = null;
        TargetState = null;
        Language = "";
    }
}
