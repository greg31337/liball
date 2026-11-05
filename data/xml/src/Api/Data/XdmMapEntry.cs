namespace xml;

/// <summary>
/// Carries one immutable XDM map entry.
/// </summary>
public class XdmMapEntry
{
    /// <summary>
    /// The atomic map key.
    /// </summary>
    public XdmAtomicValue? Key;
    /// <summary>
    /// The sequence associated with the key.
    /// </summary>
    public XdmSequence? Value;
}

