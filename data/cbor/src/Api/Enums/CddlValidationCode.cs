namespace cbor;

/// <summary>Identifies a category of CDDL validation outcome.</summary>
public enum CddlValidationCode
{
    /// <summary>No validation failure has been assigned.</summary>
    None = 0,
    /// <summary>The instance has the wrong data type.</summary>
    TypeMismatch = 1,
    /// <summary>The instance does not equal a required literal.</summary>
    LiteralMismatch = 2,
    /// <summary>The instance lies outside a required range.</summary>
    RangeMismatch = 3,
    /// <summary>The instance has an invalid size.</summary>
    SizeMismatch = 4,
    /// <summary>The encoded representation has the wrong width or major type.</summary>
    RepresentationMismatch = 5,
    /// <summary>A required member is absent.</summary>
    MissingMember = 6,
    /// <summary>An unrecognized member is present.</summary>
    UnknownMember = 7,
    /// <summary>A member occurs more often than allowed.</summary>
    DuplicateMember = 8,
    /// <summary>An occurrence constraint is not satisfied.</summary>
    OccurrenceMismatch = 9,
    /// <summary>No available choice matches the instance.</summary>
    ChoiceMismatch = 10,
    /// <summary>A control operator does not match the instance.</summary>
    ControlMismatch = 11,
    /// <summary>A referenced rule could not be evaluated.</summary>
    ReferenceFailure = 12,
    /// <summary>A recursion limit was exceeded.</summary>
    RecursionLimit = 13,
    /// <summary>A configured validation resource limit was exceeded.</summary>
    ResourceLimit = 14,
    /// <summary>The compiled schema is invalid for validation.</summary>
    InvalidSchema = 15,
    /// <summary>A required feature or control is unsupported.</summary>
    UnsupportedFeature = 16,
    /// <summary>The encoded CBOR input could not be decoded as one complete data item.</summary>
    InvalidEncoding = 17
}
