namespace languagecodes;

/// <summary>Identifies a language-code or language-tag validation issue.</summary>
public enum LanguageValidationError
{
    /// <summary>No validation issue.</summary>
    None = 0,
    /// <summary>The input value is null.</summary>
    NullInput = 1,
    /// <summary>The input value is empty.</summary>
    EmptyInput = 2,
    /// <summary>The input has an invalid length.</summary>
    InvalidLength = 3,
    /// <summary>The input contains a non-ASCII character where ASCII is required.</summary>
    NonAscii = 4,
    /// <summary>The input contains an invalid character.</summary>
    InvalidCharacter = 5,
    /// <summary>The input contains an invalid separator.</summary>
    InvalidSeparator = 6,
    /// <summary>The code or subtag is unknown.</summary>
    UnknownCode = 7,
    /// <summary>The code is well formed but unassigned.</summary>
    UnassignedCode = 8,
    /// <summary>The code belongs to a reserved range.</summary>
    ReservedCode = 9,
    /// <summary>The code has been retired.</summary>
    RetiredCode = 10,
    /// <summary>The code or subtag is deprecated.</summary>
    DeprecatedCode = 11,
    /// <summary>The code does not belong to the required ISO set.</summary>
    WrongSet = 12,
    /// <summary>The code does not have the required form.</summary>
    WrongCodeForm = 13,
    /// <summary>The entity scope is disallowed by the validation policy.</summary>
    DisallowedScope = 14,
    /// <summary>The language type is disallowed by the validation policy.</summary>
    DisallowedType = 15,
    /// <summary>A special-purpose identifier is disallowed by the validation policy.</summary>
    SpecialCodeNotAllowed = 16,
    /// <summary>A language group or family is disallowed by the validation policy.</summary>
    GroupCodeNotAllowed = 17,
    /// <summary>A private-use identifier is disallowed by the validation policy.</summary>
    PrivateUseNotAllowed = 18,
    /// <summary>The value is valid but not canonical under the selected policy.</summary>
    NonCanonical = 19,
    /// <summary>The language tag is not well formed.</summary>
    InvalidTag = 20,
    /// <summary>The language tag contains a duplicate subtag.</summary>
    DuplicateSubtag = 21,
    /// <summary>A registry prefix requirement is not satisfied.</summary>
    PrefixMismatch = 22
}
