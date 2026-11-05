namespace languagecodes;

/// <summary>Contains the authoritative retirement record for an ISO 639 Set 3 code.</summary>
public class LanguageRetirement
{
    /// <summary>The retired ISO 639 Set 3 code.</summary>
    public string? RetiredCode;

    /// <summary>The reference name associated with the code before retirement.</summary>
    public string? FormerReferenceName;

    /// <summary>The authoritative reason for retirement.</summary>
    public LanguageRetirementReason Reason;

    /// <summary>The authoritative replacement candidates, empty when no replacement exists.</summary>
    public string[] ReplacementCodes = new string[0];

    /// <summary>The authoritative free-text retirement remedy.</summary>
    public string? Remedy;

    /// <summary>The effective date in ISO <c>yyyy-MM-dd</c> form.</summary>
    public string? EffectiveDate;
}
