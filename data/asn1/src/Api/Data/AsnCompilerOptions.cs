namespace asn1;

/// <summary>
/// Configures compiler-wide encoding and unknown-data behavior.
/// </summary>
public class AsnCompilerOptions
{
    /// <summary>
    /// Indicates whether retired ANY syntax is accepted and mapped to open types.
    /// </summary>
    public bool EnableLegacyAny;

    /// <summary>
    /// Indicates whether warnings are treated as compilation errors.
    /// </summary>
    public bool TreatWarningsAsErrors;

    /// <summary>
    /// Selects the primary encoding rules used when lowering codec plans.
    /// </summary>
    public AsnEncodingRules EncodingRules;

    /// <summary>
    /// Selects how unknown data is handled by generated codec plans.
    /// </summary>
    public AsnUnknownDataPolicy UnknownDataPolicy;
}
