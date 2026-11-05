namespace asn1;

/// <summary>
/// Identifies the phase or subsystem that produced a diagnostic.
/// </summary>
public enum AsnDiagnosticCategory
{
    /// <summary>
    /// Represents general.
    /// </summary>
    General = 0,

    /// <summary>
    /// Represents source.
    /// </summary>
    Source = 1,

    /// <summary>
    /// Represents lexical.
    /// </summary>
    Lexical = 2,

    /// <summary>
    /// Represents syntax.
    /// </summary>
    Syntax = 3,

    /// <summary>
    /// Represents binding.
    /// </summary>
    Binding = 4,

    /// <summary>
    /// Represents semantic.
    /// </summary>
    Semantic = 5,

    /// <summary>
    /// Represents constraint.
    /// </summary>
    Constraint = 6,

    /// <summary>
    /// Represents generation.
    /// </summary>
    Generation = 7,

    /// <summary>
    /// Represents encoding.
    /// </summary>
    Encoding = 8,

    /// <summary>
    /// Represents decoding.
    /// </summary>
    Decoding = 9,

    /// <summary>
    /// Represents canonicality.
    /// </summary>
    Canonicality = 10,

    /// <summary>
    /// Represents resource.
    /// </summary>
    Resource = 11,

    /// <summary>
    /// Represents ecn.
    /// </summary>
    Ecn = 12,

    /// <summary>
    /// Represents xsd.
    /// </summary>
    Xsd = 13,

    /// <summary>
    /// Represents input output.
    /// </summary>
    InputOutput = 14
}
