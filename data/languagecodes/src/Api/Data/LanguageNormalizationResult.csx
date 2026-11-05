namespace languagecodes;

/// <summary>Contains the complete outcome and audit trail of a normalization operation.</summary>
public class LanguageNormalizationResult
{
    /// <summary>The normalization outcome.</summary>
    public LanguageNormalizationStatus Status;

    /// <summary>The input supplied to the normalization operation.</summary>
    public string? OriginalInput;

    /// <summary>The normalized code or language tag when normalization succeeds.</summary>
    public string? Output;

    /// <summary>The recognized code form of the input.</summary>
    public LanguageCodeForm SourceForm;

    /// <summary>The code form produced or requested by the operation.</summary>
    public LanguageCodeForm TargetForm;

    /// <summary>The resolved language entity when one was identified.</summary>
    public LanguageCodeEntry? Entry;

    /// <summary>The macrolanguage relationship used when folding was requested.</summary>
    public LanguageRelationship? MacrolanguageRelationship;

    /// <summary>The ordered changes applied to produce <see cref="Output"/>.</summary>
    public LanguageNormalizationStep[] AppliedSteps = new LanguageNormalizationStep[0];

    /// <summary>Non-fatal diagnostic messages produced during normalization.</summary>
    public string[] Warnings = new string[0];

    /// <summary>Authoritative replacement candidates when no single replacement can be selected.</summary>
    public string[] ReplacementCandidates = new string[0];
}
