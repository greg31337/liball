namespace LanguageCodes.DataGenerator.Model;

internal sealed class Iso639Set5Record
{
    internal Iso639Set5Record(string uri, string code, string[] englishNames, string[] frenchNames)
    {
        Uri = uri;
        Code = code;
        EnglishNames = englishNames;
        FrenchNames = frenchNames;
    }

    internal string Uri { get; }

    internal string Code { get; }

    internal string[] EnglishNames { get; }

    internal string[] FrenchNames { get; }
}
