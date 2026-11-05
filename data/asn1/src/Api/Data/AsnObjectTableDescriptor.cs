namespace asn1;

/// <summary>
/// Contains reflection metadata for one information-object dispatch table.
/// </summary>
public class AsnObjectTableDescriptor
{
    /// <summary>
    /// Contains the stable table identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the table-constraint category.
    /// </summary>
    public AsnConstraintKind Kind;

    /// <summary>
    /// Contains the governing object-class name.
    /// </summary>
    public string ObjectClassName = "";

    /// <summary>
    /// Contains the UNIQUE field name used as a dispatch key.
    /// </summary>
    public string UniqueFieldName = "";

    /// <summary>
    /// Contains dispatch entries in deterministic key order.
    /// </summary>
    public AsnObjectTableEntryDescriptor[] Entries = [];

    /// <summary>
    /// Contains the table source span when available.
    /// </summary>
    public AsnSourceSpan? SourceSpan;

    /// <summary>
    /// Contains ordered derivation steps for the table.
    /// </summary>
    public AsnDerivationDescriptor[] Derivations = [];
}
