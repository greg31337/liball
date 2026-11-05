namespace asn1;

/// <summary>
/// Contains reflection metadata for one effective tag.
/// </summary>
public class AsnTagDescriptor
{
    /// <summary>
    /// Contains the stable tag identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the tag class.
    /// </summary>
    public AsnTagClass Class;

    /// <summary>
    /// Contains the tagging mode.
    /// </summary>
    public AsnTaggingMode Mode;

    /// <summary>
    /// Contains the arbitrary-size tag number.
    /// </summary>
    public System.Numerics.BigInteger Number;

    /// <summary>
    /// Indicates whether the tag is the universal tag for the effective type.
    /// </summary>
    public bool Universal;

    /// <summary>
    /// Indicates whether automatic tagging synthesized the tag.
    /// </summary>
    public bool AutomaticallyGenerated;

    /// <summary>
    /// Contains the tag source span when available.
    /// </summary>
    public AsnSourceSpan? SourceSpan;

    /// <summary>
    /// Contains ordered derivation steps for the tag.
    /// </summary>
    public AsnDerivationDescriptor[] Derivations = [];
}
