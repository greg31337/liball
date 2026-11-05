namespace asn1;

/// <summary>
/// Describes an X.683 formal or actual parameter classification.
/// </summary>
public class AsnParameter
{
    /// <summary>
    /// Contains the dummy reference name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the parameter category.
    /// </summary>
    public AsnParameterKind Kind;

    /// <summary>
    /// Contains the governing type when present.
    /// </summary>
    public AsnType? GovernorType;

    /// <summary>
    /// Contains the governing object class when present.
    /// </summary>
    public AsnObjectClass? GovernorObjectClass;
}
