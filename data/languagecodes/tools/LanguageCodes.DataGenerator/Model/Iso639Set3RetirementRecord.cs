using System;

namespace LanguageCodes.DataGenerator.Model;

internal sealed class Iso639Set3RetirementRecord
{
    internal Iso639Set3RetirementRecord(
        string id,
        string referenceName,
        Iso639Set3RetirementReason reason,
        string? changeToCode,
        string? remedy,
        string[] replacementCodes,
        DateOnly effectiveDate)
    {
        Id = id;
        ReferenceName = referenceName;
        Reason = reason;
        ChangeToCode = changeToCode;
        Remedy = remedy;
        ReplacementCodes = replacementCodes;
        EffectiveDate = effectiveDate;
    }

    internal string Id { get; }

    internal string ReferenceName { get; }

    internal Iso639Set3RetirementReason Reason { get; }

    internal string? ChangeToCode { get; }

    internal string? Remedy { get; }

    internal string[] ReplacementCodes { get; }

    internal DateOnly EffectiveDate { get; }
}
