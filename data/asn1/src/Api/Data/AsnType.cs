namespace asn1;

/// <summary>
/// Describes the classification, tag, and constraints of an ASN.1 type.
/// </summary>
public class AsnType
{
    /// <summary>
    /// Contains the defined type reference name when applicable.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the type category.
    /// </summary>
    public AsnTypeKind Kind;

    /// <summary>
    /// Contains the constraints applied to the type.
    /// </summary>
    public AsnConstraint[] Constraints = [];

    /// <summary>
    /// Contains the outer tag when one is present.
    /// </summary>
    public AsnTag? Tag;

    /// <summary>
    /// Contains structured type components.
    /// </summary>
    public AsnComponent[] Components = [];

    /// <summary>
    /// Contains the element type for collection types when present.
    /// </summary>
    public AsnType? ElementType;

    /// <summary>
    /// Contains named numbers or bits.
    /// </summary>
    public AsnNamedValue[] NamedValues = [];

    /// <summary>
    /// Indicates whether the type has an extension marker.
    /// </summary>
    public bool Extensible;
}
