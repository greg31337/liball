namespace asn1;

/// <summary>
/// Identifies a source-to-reflection derivation step.
/// </summary>
public enum AsnDerivationKind
{
    /// <summary>
    /// Represents an alias expansion.
    /// </summary>
    Alias = 0,

    /// <summary>
    /// Represents a COMPONENTS OF expansion.
    /// </summary>
    ComponentsOf = 1,

    /// <summary>
    /// Represents a parameterized assignment instantiation.
    /// </summary>
    ParameterInstantiation = 2,

    /// <summary>
    /// Represents automatic tag synthesis.
    /// </summary>
    AutomaticTag = 3,

    /// <summary>
    /// Represents mapping from XML Schema.
    /// </summary>
    XsdMapping = 4,

    /// <summary>
    /// Represents application of an encoding-control assignment.
    /// </summary>
    EncodingControl = 5
}
