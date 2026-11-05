namespace asn1;

/// <summary>
/// Contains reflection metadata for one encoding-rule plan.
/// </summary>
public class AsnEncodingDescriptor
{
    /// <summary>
    /// Contains the stable encoding identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the encoding rules.
    /// </summary>
    public AsnEncodingRules EncodingRules;

    /// <summary>
    /// Contains the executable plan when available.
    /// </summary>
    public AsnEncodingPlan? Plan;

    /// <summary>
    /// Contains the encoding-reference name used by source instructions.
    /// </summary>
    public string EncodingReference = "";

    /// <summary>
    /// Contains effective encoding instructions in execution order.
    /// </summary>
    public AsnEncodingInstruction[] Instructions = [];

    /// <summary>
    /// Contains the encoding source span when available.
    /// </summary>
    public AsnSourceSpan? SourceSpan;
}
