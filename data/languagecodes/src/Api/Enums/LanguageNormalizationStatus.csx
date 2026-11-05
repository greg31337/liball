namespace languagecodes;

/// <summary>Identifies the outcome of language-code or language-tag normalization.</summary>
public enum LanguageNormalizationStatus
{
    /// <summary>The value was normalized successfully.</summary>
    Success = 0,
    /// <summary>The input was already canonical.</summary>
    AlreadyCanonical = 1,
    /// <summary>No assigned value matched the input.</summary>
    NotFound = 2,
    /// <summary>The input was not well formed.</summary>
    Invalid = 3,
    /// <summary>The input resolved to more than one candidate.</summary>
    Ambiguous = 4,
    /// <summary>The requested target code form is unavailable.</summary>
    TargetUnavailable = 5,
    /// <summary>A retired code has multiple split replacements.</summary>
    Split = 6,
    /// <summary>No authoritative replacement exists.</summary>
    NoReplacement = 7,
    /// <summary>A replacement chain contains a cycle.</summary>
    ReplacementCycle = 8,
    /// <summary>The resolved entity is incompatible with the requested policy.</summary>
    IncompatibleEntity = 9
}
