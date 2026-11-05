namespace xml;

/// <summary>
/// Carries one immutable XDM map value.
/// </summary>
public class XdmMapValue
{
    /// <summary>
    /// The map entries in stable insertion order.
    /// </summary>
    public XdmMapEntry[]? Entries;
    /// <summary>
    /// The map entry count.
    /// </summary>
    public int Count;
    /// <summary>
    /// Whether the map storage is immutable.
    /// </summary>
    public bool IsImmutable;
}

