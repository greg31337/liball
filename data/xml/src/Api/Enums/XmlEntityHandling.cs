namespace xml;

/// <summary>
/// Specifies how entity references are represented.
/// </summary>
public enum XmlEntityHandling
{
    /// <summary>
    /// Expands entity replacement text.
    /// </summary>
    Expand = 0,
    /// <summary>
    /// Preserves entity reference boundaries.
    /// </summary>
    PreserveReferences = 1
}
