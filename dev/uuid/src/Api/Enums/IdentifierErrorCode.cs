namespace uuid;

/// <summary>Provides stable machine-readable codes for identifier operation failures.</summary>
public enum IdentifierErrorCode
{
    /// <summary>The operation succeeded and no failure occurred.</summary>
    None = 0,

    /// <summary>A required input value was <c>null</c>.</summary>
    NullInput = 1,

    /// <summary>An input had an invalid byte or character length.</summary>
    InvalidLength = 2,

    /// <summary>Text contained a character that is not valid for the requested identifier format.</summary>
    InvalidCharacter = 3,

    /// <summary>A UUID did not use the variant required by the operation.</summary>
    InvalidUuidVariant = 4,

    /// <summary>A UUID version was unknown, unexpected, or unsupported by the operation.</summary>
    InvalidUuidVersion = 5,

    /// <summary>A timestamp was outside the range supported by the identifier or date representation.</summary>
    TimestampOverflow = 6,

    /// <summary>A random or payload field could not be incremented or represented.</summary>
    RandomFieldOverflow = 7,

    /// <summary>A monotonic generator observed a clock value earlier than its previous value.</summary>
    ClockRollback = 8,

    /// <summary>A generator used every sequence value available for one timestamp.</summary>
    SequenceExhaustion = 9,

    /// <summary>An alphabet had an invalid size or contained duplicate characters.</summary>
    AlphabetError = 10,

    /// <summary>A numeric or encoded identifier value exceeded its supported range.</summary>
    ValueOverflow = 11,

    /// <summary>An input or configuration value was otherwise invalid.</summary>
    InvalidValue = 12,

    /// <summary>The requested or detected identifier kind is unknown or unsupported.</summary>
    UnsupportedKind = 13
}
