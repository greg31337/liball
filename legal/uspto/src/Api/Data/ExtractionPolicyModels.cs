namespace uspto;

/// <summary>
/// Defines safety limits applied while listing or extracting an archive.
/// </summary>
public class ExtractionLimits
{
    /// <summary>The maximum number of entries accepted from one archive.</summary>
    public int MaximumEntries = UsptoDefaults.ArchiveEntryCount;

    /// <summary>The maximum uncompressed size of one entry in bytes.</summary>
    public long MaximumEntryBytes = UsptoDefaults.ArchiveEntryBytes;

    /// <summary>The maximum total uncompressed archive size in bytes.</summary>
    public long MaximumTotalBytes = UsptoDefaults.ArchiveTotalBytes;

    /// <summary>The maximum ratio of uncompressed bytes to compressed bytes.</summary>
    public int MaximumCompressionRatio = UsptoDefaults.ArchiveCompressionRatio;

    /// <summary>The maximum permitted archive nesting depth.</summary>
    public int MaximumNestingDepth = 1;

    /// <summary>Whether duplicate canonical entry names are rejected.</summary>
    public bool RejectDuplicateNames = true;
}
