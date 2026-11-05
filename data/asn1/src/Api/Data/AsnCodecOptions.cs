namespace asn1;

/// <summary>
/// Configures a codec operation.
/// </summary>
public class AsnCodecOptions
{
    /// <summary>
    /// Selects the encoding rules.
    /// </summary>
    public AsnEncodingRules EncodingRules;

    /// <summary>
    /// Selects how unknown extension and open data are handled.
    /// </summary>
    public AsnUnknownDataPolicy UnknownDataPolicy;

    /// <summary>
    /// Indicates whether the abstract value is validated against its schema.
    /// </summary>
    public bool ValidateValues = true;

    /// <summary>
    /// Indicates whether constructed BER or CER values use indefinite lengths.
    /// </summary>
    public bool UseIndefiniteLengths;

    /// <summary>
    /// Contains operation-specific resource limits when supplied.
    /// </summary>
    public AsnResourceLimits? ResourceLimits;

    /// <summary>
    /// Contains a codec-specific object-set registry when catalog registrations should not be shared.
    /// </summary>
    public IAsnObjectSetRegistry? ObjectSetRegistry;

    /// <summary>
    /// Contains operation-specific PER instruction handlers when supplied.
    /// </summary>
    public IPerInstructionRegistry? PerInstructionRegistry;
}
