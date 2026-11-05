namespace asn1;

/// <summary>
/// Contains an X.681 information object and its field settings.
/// </summary>
public class AsnInformationObject
{
    /// <summary>
    /// Contains the stable identity used by object-set operations.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the object reference name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the governing object class when resolved.
    /// </summary>
    public AsnObjectClass? ObjectClass;

    /// <summary>
    /// Contains field settings in class-field order.
    /// </summary>
    public AsnObjectFieldSetting[] FieldSettings = [];
}
