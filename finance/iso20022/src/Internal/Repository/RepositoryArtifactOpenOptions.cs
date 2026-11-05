namespace iso20022;

internal class RepositoryArtifactOpenOptions
{
    internal int MaxArtifactBytes = Iso20022Limits.MaxArtifactBytes;
    internal int MaxArchiveEntryCount = Iso20022Limits.MaxArchiveEntryCount;
    internal int MaxArchiveEntryBytes = Iso20022Limits.MaxArchiveEntryBytes;
    internal int MaxArchiveExpandedBytes = Iso20022Limits.MaxArchiveExpandedBytes;
    internal int MaxArchiveCompressionRatio = Iso20022Limits.MaxArchiveCompressionRatio;
    internal string EntryName = string.Empty;
    internal ArtifactCoordinate Coordinate = new ArtifactCoordinate();
    internal string OriginUri = string.Empty;
    internal IsoDateTimeValue? RetrievedAt;
    internal IsoPartialDateValue? PublicationDate;
    internal string LicenseNotice = string.Empty;
    internal bool IsApproved;
}
