namespace asn1;

/// <summary>
/// Describes one step from reflected metadata back toward its source origin.
/// </summary>
public class AsnDerivationDescriptor
{
    /// <summary>
    /// Contains the stable derivation identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the derivation category.
    /// </summary>
    public AsnDerivationKind Kind;

    /// <summary>
    /// Contains the referenced schema name or mapping identity.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the source span associated with this step when available.
    /// </summary>
    public AsnSourceSpan? Span;
}
