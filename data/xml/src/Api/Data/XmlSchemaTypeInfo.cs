namespace xml;

/// <summary>
/// Carries XML Schema type annotation and assessed value information.
/// </summary>
public class XmlSchemaTypeInfo
{
    /// <summary>
    /// The declared or selected type name.
    /// </summary>
    public XmlName? TypeName;
    /// <summary>
    /// The immutable compiled type-definition handle used for schema-aware matching.
    /// </summary>
    public object? TypeDefinition;
    /// <summary>
    /// The immutable compiled element or attribute declaration handle used for schema-aware matching.
    /// </summary>
    public object? Declaration;
    /// <summary>
    /// The selected union member type name.
    /// </summary>
    public XmlName? MemberTypeName;
    /// <summary>
    /// The corresponding built-in atomic type when applicable.
    /// </summary>
    public XdmAtomicType AtomicType;
    /// <summary>
    /// The assessed validity.
    /// </summary>
    public XmlSchemaValidity Validity;
    /// <summary>
    /// Whether the element is nilled.
    /// </summary>
    public bool IsNilled;
    /// <summary>
    /// Whether the element or attribute received its value from a schema value constraint.
    /// </summary>
    public bool IsDefaulted;
    /// <summary>
    /// Whether the value contributes an ID.
    /// </summary>
    public bool IsId;
    /// <summary>
    /// Whether the value contributes an ID reference.
    /// </summary>
    public bool IsIdReference;
    /// <summary>
    /// The schema-normalized lexical value.
    /// </summary>
    public string? NormalizedValue;
    /// <summary>
    /// The assessed atomic value.
    /// </summary>
    public XdmAtomicValue? ActualValue;
    /// <summary>
    /// The complete assessed atomic value sequence, including list values.
    /// </summary>
    public XdmAtomicValue[]? ActualValues;
}
