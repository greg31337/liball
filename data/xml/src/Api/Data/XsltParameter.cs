namespace xml;

/// <summary>
/// Carries one XSLT parameter binding.
/// </summary>
public class XsltParameter
{
    /// <summary>
    /// The expanded parameter name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The parameter value.
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
    /// Whether the parameter is a tunnel parameter.
    /// </summary>
    public bool IsTunnel;
    /// <summary>
    /// Whether the parameter is static.
    /// </summary>
    public bool IsStatic;
    /// <summary>
    /// Whether a supplied value is required.
    /// </summary>
    public bool IsRequired;
}

