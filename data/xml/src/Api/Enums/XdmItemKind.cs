namespace xml;

/// <summary>
/// Identifies an XDM item kind.
/// </summary>
public enum XdmItemKind
{
    /// <summary>
    /// Identifies a node.
    /// </summary>
    Node = 0,
    /// <summary>
    /// Identifies an atomic value.
    /// </summary>
    Atomic = 1,
    /// <summary>
    /// Identifies a function item.
    /// </summary>
    Function = 2,
    /// <summary>
    /// Identifies a map.
    /// </summary>
    Map = 3,
    /// <summary>
    /// Identifies an array.
    /// </summary>
    Array = 4
}
