namespace svg;

/// <summary>Defines the host-provided link state used by dynamic selector matching.</summary>
public enum SvgSelectorLinkState
{
    /// <summary>The node is not a link.</summary>
    None = 0,
    /// <summary>The node is an unvisited link.</summary>
    Unvisited = 1,
    /// <summary>The node is a visited link.</summary>
    Visited = 2
}
