namespace asn1;

/// <summary>
/// Associates a normalized UNIQUE-field value with an open type.
/// </summary>
public class AsnOpenTypeDispatchEntry
{
    /// <summary>
    /// Contains the normalized dispatch key when resolved.
    /// </summary>
    public AsnValue? Key;

    /// <summary>
    /// Contains the dispatched type when resolved.
    /// </summary>
    public AsnType? Type;

    /// <summary>
    /// Contains the stable identity of the dispatch entry.
    /// </summary>
    public string StableId = "";
}
