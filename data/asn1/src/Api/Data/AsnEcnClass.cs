namespace asn1;

/// <summary>
/// Contains a classified X.692 encoding-class definition.
/// </summary>
public class AsnEcnClass
{
    /// <summary>
    /// Contains the encoding-class reference name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the built-in or user-defined category name.
    /// </summary>
    public string Category = "";

    /// <summary>
    /// Contains the class plan steps.
    /// </summary>
    public AsnEncodingPlanStep[] Steps = [];
}
