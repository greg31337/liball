namespace asn1;

/// <summary>
/// Describes an X.681 object-class field classification.
/// </summary>
public class AsnObjectClassField
{
    /// <summary>
    /// Contains the field reference name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the object-class field category.
    /// </summary>
    public AsnObjectFieldKind Kind;

    /// <summary>
    /// Contains the governing fixed type when present.
    /// </summary>
    public AsnType? Type;

    /// <summary>
    /// Contains the governing object class for object and object-set fields when resolved.
    /// </summary>
    public AsnObjectClass? ObjectClass;

    /// <summary>
    /// Contains the resolved governor field path for variable-type fields.
    /// </summary>
    public string[] GovernorFieldPath = [];

    /// <summary>
    /// Indicates whether the field is optional.
    /// </summary>
    public bool Optional;

    /// <summary>
    /// Indicates whether the field is the UNIQUE field of its class.
    /// </summary>
    public bool Unique;

    /// <summary>
    /// Indicates whether the field has a default setting.
    /// </summary>
    public bool HasDefaultSetting;

    /// <summary>
    /// Contains the default setting when HasDefaultSetting is true.
    /// </summary>
    public AsnObjectFieldSetting? DefaultSetting;
}
