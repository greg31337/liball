namespace xml;

/// <summary>
/// Carries immutable XDM function metadata, its invocation binding, and captured values.
/// </summary>
public class XdmFunctionItem
{
    /// <summary>
    /// The function name, or null for an anonymous function.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The function arity.
    /// </summary>
    public int Arity;
    /// <summary>
    /// The declared atomic parameter types when known.
    /// </summary>
    public XdmAtomicType[]? ParameterTypes;
    /// <summary>
    /// The declared parameter occurrences when known.
    /// </summary>
    public XdmOccurrence[]? ParameterOccurrences;
    /// <summary>
    /// The declared atomic result type when known.
    /// </summary>
    public XdmAtomicType ResultType;
    /// <summary>
    /// The declared result occurrence.
    /// </summary>
    public XdmOccurrence ResultOccurrence;
    /// <summary>
    /// The immutable implementation binding key.
    /// </summary>
    public string? BindingKey;
    /// <summary>
    /// The interface implementation used to invoke an ordinary function item.
    /// </summary>
    public IXPathFunction? Implementation;
    /// <summary>
    /// The lexically captured variable bindings.
    /// </summary>
    public XPathVariableBinding[]? ClosureVariables;
    /// <summary>
    /// The immutable map value represented by this function item.
    /// </summary>
    public XdmMapValue? MapValue;
    /// <summary>
    /// The immutable array value represented by this function item.
    /// </summary>
    public XdmArrayValue? ArrayValue;
    /// <summary>
    /// Whether repeated calls with the same context and arguments are deterministic.
    /// </summary>
    public bool IsDeterministic;
    /// <summary>
    /// Whether the function item represents a map.
    /// </summary>
    public bool IsMap;
    /// <summary>
    /// Whether the function item represents an array.
    /// </summary>
    public bool IsArray;
    /// <summary>
    /// Whether the function metadata and closure storage are immutable snapshots.
    /// </summary>
    public bool IsImmutable;
}
