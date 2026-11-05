using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class GeneratedVersionData
{
    internal GeneratedVersionData(
        CanonicalCSharpFile file,
        string isoPublicationDate,
        string cldrVersion,
        string generatorSchemaVersion,
        string snapshotIdentifier,
        IReadOnlyList<SourceChecksumEntry> sourceChecksums)
    {
        File = file;
        IsoPublicationDate = isoPublicationDate;
        CldrVersion = cldrVersion;
        GeneratorSchemaVersion = generatorSchemaVersion;
        SnapshotIdentifier = snapshotIdentifier;
        SourceChecksums = sourceChecksums;
    }

    internal CanonicalCSharpFile File { get; }

    internal string IsoPublicationDate { get; }

    internal string CldrVersion { get; }

    internal string GeneratorSchemaVersion { get; }

    internal string SnapshotIdentifier { get; }

    internal IReadOnlyList<SourceChecksumEntry> SourceChecksums { get; }
}
