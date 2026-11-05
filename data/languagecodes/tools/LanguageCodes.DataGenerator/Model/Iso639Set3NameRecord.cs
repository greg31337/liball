namespace LanguageCodes.DataGenerator.Model;

internal sealed class Iso639Set3NameRecord
{
    internal Iso639Set3NameRecord(string id, string printName, string invertedName)
    {
        Id = id;
        PrintName = printName;
        InvertedName = invertedName;
    }

    internal string Id { get; }

    internal string PrintName { get; }

    internal string InvertedName { get; }
}
