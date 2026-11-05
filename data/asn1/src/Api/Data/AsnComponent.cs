namespace asn1;

/// <summary>
/// Contains one named component of a structured ASN.1 type.
/// </summary>
public class AsnComponent
{
    /// <summary>
    /// Contains the component name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the component type when resolved.
    /// </summary>
    public AsnType? Type;

    /// <summary>
    /// Indicates whether the component is optional.
    /// </summary>
    public bool Optional;

    /// <summary>
    /// Indicates whether a default value is present.
    /// </summary>
    public bool HasDefaultValue;

    /// <summary>
    /// Contains the default value when HasDefaultValue is true.
    /// </summary>
    public AsnValue? DefaultValue;

    /// <summary>
    /// Indicates whether the component belongs to an extension addition.
    /// </summary>
    public bool Extension;
}
