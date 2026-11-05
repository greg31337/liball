namespace xml;

/// <summary>
/// Carries one XPath extension or registered function binding.
/// </summary>
public class XPathFunctionBinding
{
    internal bool IsExtensionBinding;
    internal XmlExtensionPolicy? ExtensionPolicy;
    /// <summary>
    /// The expanded function name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The function arity.
    /// </summary>
    public int Arity;
    /// <summary>
    /// The declared atomic parameter types.
    /// </summary>
    public XdmAtomicType[]? ParameterTypes;
    /// <summary>
    /// The parameter occurrence indicators.
    /// </summary>
    public XdmOccurrence[]? ParameterOccurrences;
    /// <summary>
    /// The declared atomic result type.
    /// </summary>
    public XdmAtomicType ResultType;
    /// <summary>
    /// The result occurrence indicator.
    /// </summary>
    public XdmOccurrence ResultOccurrence;
    /// <summary>
    /// The immutable implementation binding key.
    /// </summary>
    public string? BindingKey;
    /// <summary>
    /// The function implementation captured when the binding is compiled.
    /// </summary>
    public IXPathFunction? Implementation;
    /// <summary>
    /// Whether repeated calls with the same context and arguments are deterministic.
    /// </summary>
    public bool IsDeterministic;
    internal XPathTypeSyntax?[]? ParameterTypeSyntax;
    internal XPathTypeSyntax? ResultTypeSyntax;
}
