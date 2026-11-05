namespace LanguageCodes.DataGenerator.Model;

internal sealed class Iso639Set2ReservedRange
{
    internal Iso639Set2ReservedRange(
        string firstCode,
        string lastCode,
        string[] englishNames,
        string[] frenchNames)
    {
        FirstCode = firstCode;
        LastCode = lastCode;
        EnglishNames = englishNames;
        FrenchNames = frenchNames;
    }

    internal string FirstCode { get; }

    internal string LastCode { get; }

    internal string[] EnglishNames { get; }

    internal string[] FrenchNames { get; }
}
