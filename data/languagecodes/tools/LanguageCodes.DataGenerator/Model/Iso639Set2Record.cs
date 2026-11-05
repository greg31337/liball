namespace LanguageCodes.DataGenerator.Model;

internal sealed class Iso639Set2Record
{
    internal Iso639Set2Record(
        string bibliographicCode,
        string? terminologyCode,
        string? alpha2Code,
        string[] englishNames,
        string[] frenchNames)
    {
        BibliographicCode = bibliographicCode;
        TerminologyCode = terminologyCode;
        Alpha2Code = alpha2Code;
        EnglishNames = englishNames;
        FrenchNames = frenchNames;
    }

    internal string BibliographicCode { get; }

    internal string? TerminologyCode { get; }

    internal string? Alpha2Code { get; }

    internal string[] EnglishNames { get; }

    internal string[] FrenchNames { get; }
}
