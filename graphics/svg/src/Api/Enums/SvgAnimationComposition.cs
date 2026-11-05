namespace svg;

/// <summary>Identifies the selected SMIL value-attribute combination.</summary>
public enum SvgAnimationValueSourceKind
{
    /// <summary>No valid value source was selected.</summary>
    None = 0,
    /// <summary>A semicolon-separated values list was selected.</summary>
    Values = 1,
    /// <summary>A from-to pair was selected.</summary>
    FromTo = 2,
    /// <summary>A from-by pair was selected.</summary>
    FromBy = 3,
    /// <summary>A to-only animation relative to the underlying value was selected.</summary>
    To = 4,
    /// <summary>A by-only additive animation was selected.</summary>
    By = 5
}

/// <summary>Defines stable value selection and composition failures.</summary>
public enum SvgAnimationCompositionError
{
    /// <summary>No composition failure occurred.</summary>
    None = 0,
    /// <summary>An argument, option, or mutable animation state was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>No permitted values, from/to, from/by, to, or by combination was present.</summary>
    MissingValues = 2,
    /// <summary>An authored animation value could not be parsed.</summary>
    InvalidValue = 3,
    /// <summary>keyTimes did not match the selected value count or mode.</summary>
    InvalidKeyTimes = 4,
    /// <summary>keySplines did not match spline interpolation segments.</summary>
    InvalidKeySplines = 5,
    /// <summary>keyPoints were invalid or did not match motion key times.</summary>
    InvalidKeyPoints = 6,
    /// <summary>An additive or accumulate keyword or value family was unsupported.</summary>
    InvalidComposition = 7,
    /// <summary>A to-only, by-only, or additive animation required an underlying value.</summary>
    MissingUnderlyingValue = 8,
    /// <summary>The typed interpolation or arithmetic operands were incompatible.</summary>
    IncompatibleValues = 9,
    /// <summary>A configured value or repeat-accumulation bound was exceeded.</summary>
    LimitExceeded = 10
}
