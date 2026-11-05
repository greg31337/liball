namespace asn1;

/// <summary>
/// Contains one X.683 actual parameter and its classified payload.
/// </summary>
public class AsnActualParameter
{
    /// <summary>
    /// Contains the parameter category.
    /// </summary>
    public AsnParameterKind Kind;

    /// <summary>
    /// Contains an actual type when present.
    /// </summary>
    public AsnType? Type;

    /// <summary>
    /// Contains an actual value-set type when present.
    /// </summary>
    public AsnType? ValueSetType;

    /// <summary>
    /// Contains an actual value when present.
    /// </summary>
    public AsnValue? Value;

    /// <summary>
    /// Contains an actual object class when present.
    /// </summary>
    public AsnObjectClass? ObjectClass;

    /// <summary>
    /// Contains an actual information object when present.
    /// </summary>
    public AsnInformationObject? Object;

    /// <summary>
    /// Contains an actual information object set when present.
    /// </summary>
    public AsnInformationObjectSet? ObjectSet;
}
