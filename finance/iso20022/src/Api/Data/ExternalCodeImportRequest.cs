using System.Threading;

namespace iso20022;

/// <summary>Controls bounded normalization of one external-code artifact.</summary>
public class ExternalCodeImportRequest
{
    /// <summary>The logical source name used in import diagnostics.</summary>
    public string SourceName = "external-code-sets";
    /// <summary>The archive entry to select, or an empty value to require unambiguous selection.</summary>
    public string ArchiveEntryName = string.Empty;
    /// <summary>The source artifact and integrity provenance.</summary>
    public ArtifactManifest Artifact = new ArtifactManifest();
    /// <summary>The publication edition assigned when the source does not declare one.</summary>
    public string Edition = string.Empty;
    /// <summary>The XML and collection limits applied while importing.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>The maximum encoded artifact size.</summary>
    public int MaxArtifactBytes = Iso20022Limits.MaxArtifactBytes;
    /// <summary>The maximum expanded archive-entry size.</summary>
    public int MaxArchiveEntryBytes = Iso20022Limits.MaxArchiveEntryBytes;
    /// <summary>The maximum number of entries in an archive.</summary>
    public int MaxArchiveEntryCount = Iso20022Limits.MaxArchiveEntryCount;
    /// <summary>The maximum combined expanded size of archive entries.</summary>
    public int MaxArchiveExpandedBytes = Iso20022Limits.MaxArchiveExpandedBytes;
    /// <summary>The maximum expanded-to-compressed ratio of an archive entry.</summary>
    public int MaxArchiveCompressionRatio = Iso20022Limits.MaxArchiveCompressionRatio;
    /// <summary>The cancellation request observed between bounded import stages.</summary>
    public CancellationToken CancellationToken;
}
