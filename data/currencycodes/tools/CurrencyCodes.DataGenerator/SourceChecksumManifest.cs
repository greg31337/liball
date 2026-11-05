using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class SourceChecksumManifest
{
    internal SourceChecksumManifest(
        int manifestVersion,
        string isoPublicationDate,
        string cldrRelease,
        string retrievalDate,
        IReadOnlyList<SourceChecksumEntry> entries)
    {
        ManifestVersion = manifestVersion;
        IsoPublicationDate = isoPublicationDate;
        CldrRelease = cldrRelease;
        RetrievalDate = retrievalDate;
        Entries = entries;
    }

    internal int ManifestVersion { get; }

    internal string IsoPublicationDate { get; }

    internal string CldrRelease { get; }

    internal string RetrievalDate { get; }

    internal IReadOnlyList<SourceChecksumEntry> Entries { get; }
}
