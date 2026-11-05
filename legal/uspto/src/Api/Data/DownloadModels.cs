namespace uspto;

/// <summary>
/// Describes one checksum expected for downloaded content.
/// </summary>
public class ChecksumExpectation
{
    /// <summary>The digest algorithm used by the expected value.</summary>
    public ChecksumAlgorithm Algorithm = ChecksumAlgorithm.None;

    /// <summary>The expected digest in catalog-provided hexadecimal or base64 text.</summary>
    public string? Value = null;
}

/// <summary>
/// Describes a streamed or file-backed download operation.
/// </summary>
public class DownloadRequest
{
    /// <summary>The catalog-provided filename, when available.</summary>
    public string? CatalogFileName = null;

    /// <summary>The representation requested for a search-result export.</summary>
    public ExportFormat ExportFormat = ExportFormat.Json;

    /// <summary>The credential-free source URI.</summary>
    public global::System.Uri? SourceUri = null;

    /// <summary>A caller-owned output stream, or <c>null</c> for a file-backed download.</summary>
    public global::System.IO.Stream? Output = null;

    /// <summary>The requested destination path, or <c>null</c> for a stream-backed download.</summary>
    public string? DestinationPath = null;

    /// <summary>Whether an existing completed destination may be replaced.</summary>
    public bool OverwriteExisting = false;

    /// <summary>Whether compatible persisted partial state may be resumed.</summary>
    public bool ResumePartial = true;

    /// <summary>The expected response size in bytes, when known.</summary>
    public long ExpectedSize = 0L;

    /// <summary>Whether <see cref="ExpectedSize"/> contains a value.</summary>
    public bool HasExpectedSize = false;

    /// <summary>The catalog-provided checksum expectations.</summary>
    public ChecksumExpectation[] ExpectedChecksums = new ChecksumExpectation[0];

    /// <summary>An optional caller progress receiver.</summary>
    public global::System.IProgress<DownloadProgress>? Progress = null;

    /// <summary>The shared request cancellation, raw-retention, and deadline context.</summary>
    public RequestContext Context = new RequestContext();
}

/// <summary>
/// Reports a point-in-time snapshot of download progress.
/// </summary>
public class DownloadProgress
{
    /// <summary>The current transfer lifecycle state.</summary>
    public DownloadState State = DownloadState.NotStarted;

    /// <summary>The total verified bytes available at the destination.</summary>
    public long BytesTransferred = 0L;

    /// <summary>The number of bytes reused from validated partial state.</summary>
    public long ResumedBytes = 0L;

    /// <summary>The expected final byte count, when known.</summary>
    public long TotalBytes = 0L;

    /// <summary>Whether <see cref="TotalBytes"/> contains a value.</summary>
    public bool HasTotalBytes = false;

    /// <summary>The catalog or response filename, when known.</summary>
    public string? FileName = null;

    /// <summary>The time at which this snapshot was produced, when assigned.</summary>
    public global::System.DateTimeOffset Timestamp = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="Timestamp"/> contains a value.</summary>
    public bool HasTimestamp = false;
}

/// <summary>
/// Holds validated response metadata for downloaded content.
/// </summary>
public class DownloadContentMetadata
{
    /// <summary>The final credential-free URI after approved redirects.</summary>
    public global::System.Uri? FinalUri = null;

    /// <summary>The sanitized response or catalog filename.</summary>
    public string? FileName = null;

    /// <summary>The normalized response media type without parameters.</summary>
    public string? MediaType = null;

    /// <summary>The recognized document representation.</summary>
    public DocumentFormat Format = DocumentFormat.Unknown;

    /// <summary>The expected content length in bytes, when supplied.</summary>
    public long ContentLength = 0L;

    /// <summary>Whether <see cref="ContentLength"/> contains a value.</summary>
    public bool HasContentLength = false;

    /// <summary>The response entity tag, when supplied.</summary>
    public string? ETag = null;

    /// <summary>The response last-modified time, when supplied.</summary>
    public global::System.DateTimeOffset LastModified = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModified"/> contains a value.</summary>
    public bool HasLastModified = false;

    /// <summary>Whether the server advertises byte-range support.</summary>
    public bool AcceptsByteRanges = false;
}

/// <summary>
/// Holds persisted, credential-free state required to validate a range resume.
/// </summary>
public class DownloadResumeState
{
    /// <summary>The credential-free source URI associated with the partial bytes.</summary>
    public global::System.Uri? SourceUri = null;

    /// <summary>The local partial-file path.</summary>
    public string? PartialPath = null;

    /// <summary>The number of complete bytes currently persisted.</summary>
    public long CompletedBytes = 0L;

    /// <summary>The expected final byte count, when known.</summary>
    public long ExpectedTotalBytes = 0L;

    /// <summary>Whether <see cref="ExpectedTotalBytes"/> contains a value.</summary>
    public bool HasExpectedTotalBytes = false;

    /// <summary>The entity tag used as an <c>If-Range</c> validator.</summary>
    public string? ETag = null;

    /// <summary>The last-modified time used as an <c>If-Range</c> validator.</summary>
    public global::System.DateTimeOffset LastModified = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModified"/> contains a value.</summary>
    public bool HasLastModified = false;

    /// <summary>The time at which this state was last updated, when assigned.</summary>
    public global::System.DateTimeOffset UpdatedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="UpdatedAt"/> contains a value.</summary>
    public bool HasUpdatedAt = false;
}

/// <summary>
/// Holds the outcome of calculating and comparing one content checksum.
/// </summary>
public class ChecksumResult
{
    /// <summary>The expected checksum and algorithm.</summary>
    public ChecksumExpectation Expectation = new ChecksumExpectation();

    /// <summary>The normalized checksum calculated from local bytes.</summary>
    public string? ActualValue = null;

    /// <summary>Whether an expected checksum was available for comparison.</summary>
    public bool IsAvailable = false;

    /// <summary>Whether checksum calculation read the complete content.</summary>
    public bool IsComplete = false;

    /// <summary>Whether the complete calculated checksum matched the expectation.</summary>
    public bool IsMatch = false;
}

/// <summary>
/// Holds the final state and validated artifacts of a download operation.
/// </summary>
public class DownloadResult
{
    /// <summary>The request associated with this result.</summary>
    public DownloadRequest Request = new DownloadRequest();

    /// <summary>The terminal transfer state.</summary>
    public DownloadState State = DownloadState.NotStarted;

    /// <summary>The final progress snapshot.</summary>
    public DownloadProgress Progress = new DownloadProgress();

    /// <summary>The validated response content metadata.</summary>
    public DownloadContentMetadata Content = new DownloadContentMetadata();

    /// <summary>The final persisted resume state, when a partial file remains.</summary>
    public DownloadResumeState ResumeState = new DownloadResumeState();

    /// <summary>Whether <see cref="ResumeState"/> represents persisted partial content.</summary>
    public bool HasResumeState = false;

    /// <summary>The checksum calculations requested or discovered for the content.</summary>
    public ChecksumResult[] Checksums = new ChecksumResult[0];

    /// <summary>The completed local destination path, when applicable.</summary>
    public string? FinalPath = null;

    /// <summary>A stable, credential-free failure code, or <c>null</c> on success.</summary>
    public string? FailureCode = null;

    /// <summary>A bounded, credential-free failure message, or <c>null</c> on success.</summary>
    public string? FailureMessage = null;
}
