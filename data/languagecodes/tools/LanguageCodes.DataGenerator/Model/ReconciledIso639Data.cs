namespace LanguageCodes.DataGenerator.Model;

internal sealed class ReconciledIso639Data
{
    internal ReconciledIso639Data(
        ReconciledIso639Entity[] entities,
        Iso639Set2ReservedRange[] reservedRanges)
    {
        Entities = entities;
        ReservedRanges = reservedRanges;
    }

    internal ReconciledIso639Entity[] Entities { get; }

    internal Iso639Set2ReservedRange[] ReservedRanges { get; }
}
