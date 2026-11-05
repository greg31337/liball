namespace LanguageCodes.DataGenerator.Model;

internal sealed class Iso639Set3Record
{
    internal Iso639Set3Record(
        string id,
        string? part2BibliographicCode,
        string? part2TerminologyCode,
        string? alpha2Code,
        Iso639Set3Scope scope,
        Iso639Set3LanguageType languageType,
        string referenceName,
        string? comment)
    {
        Id = id;
        Part2BibliographicCode = part2BibliographicCode;
        Part2TerminologyCode = part2TerminologyCode;
        Alpha2Code = alpha2Code;
        Scope = scope;
        LanguageType = languageType;
        ReferenceName = referenceName;
        Comment = comment;
    }

    internal string Id { get; }

    internal string? Part2BibliographicCode { get; }

    internal string? Part2TerminologyCode { get; }

    internal string? Alpha2Code { get; }

    internal Iso639Set3Scope Scope { get; }

    internal Iso639Set3LanguageType LanguageType { get; }

    internal string ReferenceName { get; }

    internal string? Comment { get; }
}
