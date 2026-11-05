namespace svg;

/// <summary>Defines stable CFF and CFF2 outline decoding failures.</summary>
public enum SvgCffError
{
    /// <summary>No CFF decoding failure occurred.</summary>
    None = 0,
    /// <summary>The font, glyph index, or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The font does not contain CFF or CFF2 outlines.</summary>
    UnsupportedOutline = 2,
    /// <summary>The CFF header was malformed or unsupported.</summary>
    InvalidHeader = 3,
    /// <summary>An INDEX structure was malformed or outside the CFF table.</summary>
    InvalidIndex = 4,
    /// <summary>A Top, Font, or Private DICT was malformed.</summary>
    InvalidDictionary = 5,
    /// <summary>A Type 2 CharString program was malformed.</summary>
    InvalidCharString = 6,
    /// <summary>The configured CharString operand limit was exceeded.</summary>
    OperandLimitExceeded = 7,
    /// <summary>A subroutine was missing, recursive, or exceeded configured limits.</summary>
    SubroutineLimitExceeded = 8,
    /// <summary>The configured outline point limit was exceeded.</summary>
    PointLimitExceeded = 9,
    /// <summary>A decoded coordinate was not finite.</summary>
    NonFiniteResult = 10,
    /// <summary>A valid but unsupported CharString operator requires caller fallback.</summary>
    UnsupportedOperator = 11,
    /// <summary>A valid but unsupported CFF structure requires caller fallback.</summary>
    UnsupportedStructure = 12
}

/// <summary>Classifies why a CFF glyph should be delegated to a capable caller fallback.</summary>
public enum SvgCffFallbackReason
{
    /// <summary>No caller fallback is required.</summary>
    None = 0,
    /// <summary>The CharString uses a valid operator that this decoder does not evaluate.</summary>
    UnsupportedOperator = 1,
    /// <summary>The CFF2 glyph requires variation-store evaluation.</summary>
    VariationData = 2,
    /// <summary>The CFF font uses CID-keyed font selection not handled by this decoder.</summary>
    CidFont = 3,
    /// <summary>The font uses another valid CFF structure outside this decoder's profile.</summary>
    UnsupportedStructure = 4
}
