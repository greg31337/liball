namespace xml;

/// <summary>
/// Carries one XPath variable binding.
/// </summary>
public class XPathVariableBinding
{
    /// <summary>
    /// The expanded variable name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The bound sequence.
    /// </summary>
    public XdmSequence? Value;
    /// <summary>
    /// The declared atomic item type when known.
    /// </summary>
    public XdmAtomicType DeclaredType;
    /// <summary>
    /// The declared occurrence indicator.
    /// </summary>
    public XdmOccurrence DeclaredOccurrence;
    /// <summary>
    /// Whether the value is supplied externally.
    /// </summary>
    public bool IsExternal;
    /// <summary>
    /// Whether an external value is required.
    /// </summary>
    public bool IsRequired;
    internal XPathTypeSyntax? StaticTypeSyntax;
}
