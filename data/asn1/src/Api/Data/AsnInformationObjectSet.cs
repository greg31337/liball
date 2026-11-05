namespace asn1;

/// <summary>
/// Contains a materialized X.681 information object set.
/// </summary>
public class AsnInformationObjectSet
{
    /// <summary>
    /// Contains the stable identity of the object set or set expression.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the object-set reference name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the governing object class when resolved.
    /// </summary>
    public AsnObjectClass? ObjectClass;

    /// <summary>
    /// Contains all statically compiled objects in stable order.
    /// </summary>
    public AsnInformationObject[] Objects = [];

    /// <summary>
    /// Contains statically compiled root objects in stable order.
    /// </summary>
    public AsnInformationObject[] RootObjects = [];

    /// <summary>
    /// Contains statically compiled extension objects in stable order.
    /// </summary>
    public AsnInformationObject[] AdditionalObjects = [];

    /// <summary>
    /// Indicates whether dynamic extension objects are permitted.
    /// </summary>
    public bool Extensible;

    /// <summary>
    /// Contains compiled open-type dispatch entries.
    /// </summary>
    public AsnOpenTypeDispatchEntry[] DispatchEntries = [];
}
