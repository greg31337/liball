using System.Threading;

namespace iso20022;

/// <summary>Controls repository artifact opening, normalization, and integrity validation.</summary>
public class RepositoryImportOptions
{
    /// <summary>The logical source name used in source locations for stream imports.</summary>
    public string SourceName = "repository.iso20022";
    /// <summary>The archive entry to select, or an empty value to require automatic unambiguous selection.</summary>
    public string ArchiveEntryName = string.Empty;
    /// <summary>The artifact coordinate recorded in repository provenance.</summary>
    public ArtifactCoordinate Coordinate = new ArtifactCoordinate();
    /// <summary>The origin URI recorded in repository provenance.</summary>
    public string OriginUri = string.Empty;
    /// <summary>The retrieval time recorded in repository provenance when known.</summary>
    public IsoDateTimeValue? RetrievedAt;
    /// <summary>The publication date recorded in repository provenance when known.</summary>
    public IsoPartialDateValue? PublicationDate;
    /// <summary>The import time recorded in repository provenance when known.</summary>
    public IsoDateTimeValue? ImportedAt;
    /// <summary>The artifact license notice recorded in repository provenance.</summary>
    public string LicenseNotice = string.Empty;
    /// <summary>Whether the imported artifact is approved rather than draft.</summary>
    public bool IsApproved;
    /// <summary>The XML and collection limits applied while reading the repository.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>The maximum encoded artifact size.</summary>
    public int MaxArtifactBytes = Iso20022Limits.MaxArtifactBytes;
    /// <summary>The maximum number of entries in an archive.</summary>
    public int MaxArchiveEntryCount = Iso20022Limits.MaxArchiveEntryCount;
    /// <summary>The maximum expanded size of one archive entry.</summary>
    public int MaxArchiveEntryBytes = Iso20022Limits.MaxArchiveEntryBytes;
    /// <summary>The maximum combined expanded size of archive entries.</summary>
    public int MaxArchiveExpandedBytes = Iso20022Limits.MaxArchiveExpandedBytes;
    /// <summary>The maximum expanded-to-compressed ratio of an archive entry.</summary>
    public int MaxArchiveCompressionRatio = Iso20022Limits.MaxArchiveCompressionRatio;
    /// <summary>The maximum normalized XMI attribute count.</summary>
    public int MaxAttributeCount = 5000000;
    /// <summary>The maximum normalized XMI relationship count.</summary>
    public int MaxRelationshipCount = 10000000;
    /// <summary>The cancellation request observed between bounded import stages.</summary>
    public CancellationToken CancellationToken;
}
