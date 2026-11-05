namespace asn1;

/// <summary>
/// Associates one reflected dispatch key with its effective type.
/// </summary>
public class AsnObjectTableEntryDescriptor
{
    /// <summary>
    /// Contains the stable table-entry identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the dispatch key when present.
    /// </summary>
    public AsnValueDescriptor? Key;

    /// <summary>
    /// Contains the dispatched type when present.
    /// </summary>
    public AsnTypeDescriptor? Type;

    /// <summary>
    /// Contains the entry source span when available.
    /// </summary>
    public AsnSourceSpan? SourceSpan;
}
