namespace languagecodes;

/// <summary>Contains lexical, assignment, acceptance, and canonicality results for a validated value.</summary>
public class LanguageValidationResult
{
    /// <summary>Whether the input satisfies the applicable lexical grammar.</summary>
    public bool IsWellFormed;

    /// <summary>Whether the input resolves to an assigned code or registered subtag.</summary>
    public bool IsAssigned;

    /// <summary>Whether the input satisfies the complete validation policy.</summary>
    public bool IsValid;

    /// <summary>Whether the input is canonical under the selected policy.</summary>
    public bool IsCanonical;

    /// <summary>The resolved language entity when assignment resolution succeeds.</summary>
    public LanguageCodeEntry? Entry;

    /// <summary>The canonical alternative when one is available.</summary>
    public string? CanonicalValue;

    /// <summary>All errors and warnings in deterministic validation order.</summary>
    public LanguageValidationIssue[] Issues = new LanguageValidationIssue[0];
}
