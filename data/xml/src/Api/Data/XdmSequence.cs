namespace xml;

/// <summary>
/// Carries an ordered immutable XDM sequence without a user-defined generic type.
/// </summary>
public class XdmSequence
{
    /// <summary>
    /// The kind of each item in sequence order.
    /// </summary>
    public XdmItemKind[]? ItemKinds;
    /// <summary>
    /// The category-array index of each item in sequence order.
    /// </summary>
    public int[]? ItemIndexes;
    /// <summary>
    /// The node items.
    /// </summary>
    public XmlNode[]? Nodes;
    /// <summary>
    /// The atomic items.
    /// </summary>
    public XdmAtomicValue[]? AtomicValues;
    /// <summary>
    /// The map items.
    /// </summary>
    public XdmMapValue[]? Maps;
    /// <summary>
    /// The array items.
    /// </summary>
    public XdmArrayValue[]? Arrays;
    /// <summary>
    /// The non-map and non-array function items.
    /// </summary>
    public XdmFunctionItem[]? Functions;
    /// <summary>
    /// The sequence item count.
    /// </summary>
    public int Count;
    /// <summary>
    /// Whether the sequence storage is immutable.
    /// </summary>
    public bool IsImmutable;
}

