namespace languagecodes;

/// <summary>Identifies the severity of a validation issue.</summary>
public enum LanguageValidationSeverity
{
    /// <summary>The issue prevents the value from satisfying the validation policy.</summary>
    Error = 0,
    /// <summary>The issue is advisory and does not necessarily invalidate the value.</summary>
    Warning = 1
}
