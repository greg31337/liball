namespace uspto;

/// <summary>
/// Holds the result of detecting an archive or compression format.
/// </summary>
public class CompressionDetectionResult
{
    /// <summary>The detected format.</summary>
    public CompressionFormat Format = CompressionFormat.Unknown;

    /// <summary>Whether the format was identified from a file signature.</summary>
    public bool DetectedBySignature = false;

    /// <summary>Whether the filename extension agreed with the detected signature.</summary>
    public bool ExtensionMatches = false;

    /// <summary>Whether the detected format can be processed by the library.</summary>
    public bool IsSupported = false;
}

/// <summary>
/// Describes one normalized archive entry without materializing it.
/// </summary>
public class ArchiveEntry
{
    /// <summary>The normalized forward-slash-separated entry name.</summary>
    public string? Name = null;

    /// <summary>The compressed entry size in bytes, when available.</summary>
    public long CompressedSize = 0L;

    /// <summary>Whether <see cref="CompressedSize"/> contains a value.</summary>
    public bool HasCompressedSize = false;

    /// <summary>The uncompressed entry size in bytes, when available.</summary>
    public long UncompressedSize = 0L;

    /// <summary>Whether <see cref="UncompressedSize"/> contains a value.</summary>
    public bool HasUncompressedSize = false;

    /// <summary>Whether the entry represents a directory.</summary>
    public bool IsDirectory = false;

    /// <summary>Whether the entry represents a symbolic or hard link.</summary>
    public bool IsLink = false;

    /// <summary>The entry modification time, when available.</summary>
    public global::System.DateTimeOffset LastModified = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModified"/> contains a value.</summary>
    public bool HasLastModified = false;
}

/// <summary>
/// Holds archive format detection and a bounded entry listing.
/// </summary>
public class ArchiveListingResult
{
    /// <summary>The local archive path or credential-free source name.</summary>
    public string? SourceName = null;

    /// <summary>The detected archive or compression format.</summary>
    public CompressionDetectionResult Detection = new CompressionDetectionResult();

    /// <summary>The normalized entries discovered in the archive.</summary>
    public ArchiveEntry[] Entries = new ArchiveEntry[0];

    /// <summary>Whether the complete entry listing was read successfully.</summary>
    public bool IsComplete = false;

    /// <summary>A stable failure code, or <c>null</c> when listing succeeded.</summary>
    public string? FailureCode = null;

    /// <summary>A bounded failure message, or <c>null</c> when listing succeeded.</summary>
    public string? FailureMessage = null;
}

/// <summary>
/// Describes a bounded archive extraction operation.
/// </summary>
public class ExtractionOptions
{
    /// <summary>The local archive path.</summary>
    public string? ArchivePath = null;

    /// <summary>The destination directory beneath which entries are materialized.</summary>
    public string? DestinationDirectory = null;

    /// <summary>The normalized entry names selected for extraction.</summary>
    public string[] SelectedEntries = new string[0];

    /// <summary>Whether every safe archive entry is selected.</summary>
    public bool ExtractAll = true;

    /// <summary>Whether an existing completed destination file may be replaced.</summary>
    public bool OverwriteExisting = false;

    /// <summary>The archive bomb and entry safety limits.</summary>
    public ExtractionLimits Limits = new ExtractionLimits();

    /// <summary>The shared request cancellation and deadline context.</summary>
    public RequestContext Context = new RequestContext();
}

/// <summary>
/// Holds the outcome of applying one extraction safety limit.
/// </summary>
public class ArchiveSafetyLimitResult
{
    /// <summary>Whether the observed value was accepted by the configured limit.</summary>
    public bool IsAccepted = true;

    /// <summary>The stable name of the evaluated limit.</summary>
    public string? LimitName = null;

    /// <summary>The value observed while reading the archive.</summary>
    public long ObservedValue = 0L;

    /// <summary>The configured maximum value.</summary>
    public long LimitValue = 0L;

    /// <summary>The entry associated with the limit, when applicable.</summary>
    public ArchiveEntry? Entry = null;
}

/// <summary>
/// Holds the outcome of extracting one archive entry.
/// </summary>
public class ExtractionEntryResult
{
    /// <summary>The archive entry considered for extraction.</summary>
    public ArchiveEntry Entry = new ArchiveEntry();

    /// <summary>The canonical destination path, when the entry was materialized.</summary>
    public string? DestinationPath = null;

    /// <summary>Whether the entry was extracted successfully.</summary>
    public bool IsExtracted = false;

    /// <summary>Whether the entry was intentionally skipped.</summary>
    public bool IsSkipped = false;

    /// <summary>A stable failure code, or <c>null</c> when no entry failure occurred.</summary>
    public string? FailureCode = null;

    /// <summary>A bounded failure message, or <c>null</c> when no entry failure occurred.</summary>
    public string? FailureMessage = null;
}

/// <summary>
/// Holds the final state and per-entry outcomes of an extraction operation.
/// </summary>
public class ExtractionResult
{
    /// <summary>The extraction options associated with this result.</summary>
    public ExtractionOptions Options = new ExtractionOptions();

    /// <summary>The detected archive or compression format.</summary>
    public CompressionDetectionResult Detection = new CompressionDetectionResult();

    /// <summary>The evaluated archive safety limit outcomes.</summary>
    public ArchiveSafetyLimitResult[] SafetyLimits = new ArchiveSafetyLimitResult[0];

    /// <summary>The per-entry extraction outcomes.</summary>
    public ExtractionEntryResult[] Entries = new ExtractionEntryResult[0];

    /// <summary>The total uncompressed bytes materialized.</summary>
    public long BytesWritten = 0L;

    /// <summary>Whether all selected entries completed without failure.</summary>
    public bool IsComplete = false;

    /// <summary>Whether extraction stopped because caller cancellation was requested.</summary>
    public bool IsCancelled = false;

    /// <summary>A stable operation failure code, or <c>null</c> on success.</summary>
    public string? FailureCode = null;

    /// <summary>A bounded operation failure message, or <c>null</c> on success.</summary>
    public string? FailureMessage = null;
}
