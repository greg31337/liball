namespace fake;

/// <summary>
/// Identifies a broad family of explicit edge-case fixtures.
/// </summary>
public enum EdgeCaseCategory
{
    /// <summary>Null, empty, and whitespace-only values.</summary>
    EmptyNullWhitespace = 0,

    /// <summary>Numeric limits, non-finite values, and precision boundaries.</summary>
    BoundaryNumber = 1,

    /// <summary>Long, repeated, and all-equal text.</summary>
    LongRepeatedText = 2,

    /// <summary>Unicode directionality, composition, scalar, and malformed-code-unit cases.</summary>
    Unicode = 3,

    /// <summary>Calendar, clock, offset, epoch, and runtime temporal boundaries.</summary>
    Temporal = 4,

    /// <summary>Identifier structure, checksum, prefix, version, and range cases.</summary>
    Identifier = 5,

    /// <summary>Opt-in strings shaped like input-injection probes.</summary>
    Injection = 6,

    /// <summary>Pairs of values that express contradictory input constraints.</summary>
    ContradictoryInput = 7
}
