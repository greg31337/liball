namespace languagecodes;

internal class RetirementRow
{
    internal string RetiredCode;
    internal string FormerReferenceName;
    internal LanguageRetirementReason Reason;
    internal string[] ReplacementCodes;
    internal string? Remedy;
    internal string EffectiveDate;

    internal RetirementRow(
        string retiredCode,
        string formerReferenceName,
        LanguageRetirementReason reason,
        string[] replacementCodes,
        string? remedy,
        string effectiveDate)
    {
        RetiredCode = retiredCode;
        FormerReferenceName = formerReferenceName;
        Reason = reason;
        ReplacementCodes = replacementCodes;
        Remedy = remedy;
        EffectiveDate = effectiveDate;
    }
}
