namespace LanguageCodes.DataGenerator.Model;

internal sealed class Iso639Set2Data
{
    internal Iso639Set2Data(Iso639Set2Record[] records, Iso639Set2ReservedRange[] reservedRanges)
    {
        Records = records;
        ReservedRanges = reservedRanges;
    }

    internal Iso639Set2Record[] Records { get; }

    internal Iso639Set2ReservedRange[] ReservedRanges { get; }
}
