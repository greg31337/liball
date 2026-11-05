namespace xml;

/// <summary>
/// Carries one immutable XDM array whose members are sequences.
/// </summary>
public class XdmArrayValue
{
    /// <summary>
    /// The array members in one-based lookup order.
    /// </summary>
    public XdmSequence[]? Members;
    /// <summary>
    /// The array member count.
    /// </summary>
    public int Count;
    /// <summary>
    /// Whether the array storage is immutable.
    /// </summary>
    public bool IsImmutable;
}

