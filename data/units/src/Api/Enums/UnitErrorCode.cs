namespace units;

/// <summary>
/// Identifies failures returned by parsing, lookup, arithmetic, registry, and conversion operations.
/// </summary>
public enum UnitErrorCode
{
    /// <summary>
    /// Indicates that no error occurred.
    /// </summary>
    None = 0,
    /// <summary>
    /// Indicates empty or malformed input that cannot be parsed.
    /// </summary>
    ParseInvalidInput = 100,
    /// <summary>
    /// Indicates that no registered unit matches the parsed text.
    /// </summary>
    ParseUnknownUnit = 101,
    /// <summary>
    /// Indicates that several registered units match the parsed text.
    /// </summary>
    ParseAmbiguousUnit = 102,
    /// <summary>
    /// Indicates that lookup found no matching definition.
    /// </summary>
    LookupNotFound = 200,
    /// <summary>
    /// Indicates that lookup found more than one matching definition.
    /// </summary>
    LookupAmbiguous = 201,
    /// <summary>
    /// Indicates arithmetic between quantities with incompatible dimensions.
    /// </summary>
    ArithmeticIncompatibleDimensions = 300,
    /// <summary>
    /// Indicates arithmetic that is not meaningful for the supplied units or values.
    /// </summary>
    ArithmeticInvalidOperation = 301,
    /// <summary>
    /// Indicates an attempted registration with an identifier already in use.
    /// </summary>
    RegistryDuplicateId = 400,
    /// <summary>
    /// Indicates an attempted registration that would make a unit symbol ambiguous.
    /// </summary>
    RegistryAmbiguousSymbol = 401,
    /// <summary>
    /// Indicates an attempted change to a frozen registry.
    /// </summary>
    RegistryFrozen = 402,
    /// <summary>
    /// Indicates metadata that references a missing or incompatible catalog definition.
    /// </summary>
    RegistryInvalidMetadata = 403,
    /// <summary>
    /// Indicates conversion between different dimensions.
    /// </summary>
    ConversionIncompatibleDimensions = 500,
    /// <summary>
    /// Indicates conversion mechanics that the ordinary converter does not support.
    /// </summary>
    ConversionUnsupported = 501,
    /// <summary>
    /// Indicates an invalid mix of affine points and scale-only intervals.
    /// </summary>
    ConversionInvalidAffineOperation = 502
}
