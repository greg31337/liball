namespace LanguageCodes.DataGenerator.Model;

internal sealed class ReconciledCodeAliasProvenance
{
    internal ReconciledCodeAliasProvenance(
        ReconciledCodeAliasSource source,
        string recordIdentifier,
        Iso639Set3RetirementRecord? retirementRecord,
        IanaRegistryRecord? ianaRecord)
    {
        Source = source;
        RecordIdentifier = recordIdentifier;
        RetirementRecord = retirementRecord;
        IanaRecord = ianaRecord;
    }

    internal ReconciledCodeAliasSource Source { get; }

    internal string RecordIdentifier { get; }

    internal Iso639Set3RetirementRecord? RetirementRecord { get; }

    internal IanaRegistryRecord? IanaRecord { get; }
}
