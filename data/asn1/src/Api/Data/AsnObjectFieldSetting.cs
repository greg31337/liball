namespace asn1;

/// <summary>
/// Contains one classified field setting in an information object.
/// </summary>
public class AsnObjectFieldSetting
{
    /// <summary>
    /// Contains the object-class field name.
    /// </summary>
    public string FieldName = "";

    /// <summary>
    /// Contains a type setting when present.
    /// </summary>
    public AsnType? Type;

    /// <summary>
    /// Contains a value-set type when present.
    /// </summary>
    public AsnType? ValueSetType;

    /// <summary>
    /// Contains a value setting when present.
    /// </summary>
    public AsnValue? Value;

    /// <summary>
    /// Contains an object setting when present.
    /// </summary>
    public AsnInformationObject? Object;

    /// <summary>
    /// Contains an object-set setting when present.
    /// </summary>
    public AsnInformationObjectSet? ObjectSet;
}
