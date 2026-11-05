namespace uspto;

/// <summary>
/// Holds persisted state for one download, checksum, extraction, or parsing stage.
/// </summary>
public class DatasetManifestStage
{
    /// <summary>The stable stage name.</summary>
    public string? Name = null;

    /// <summary>The current stage state.</summary>
    public MirrorStageState State = MirrorStageState.NotStarted;

    /// <summary>The time at which the stage state last changed.</summary>
    public global::System.DateTimeOffset UpdatedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="UpdatedAt"/> was supplied.</summary>
    public bool HasUpdatedAt = false;

    /// <summary>A credential-free stable failure code.</summary>
    public string? FailureCode = null;

    /// <summary>A credential-free bounded failure description.</summary>
    public string? FailureMessage = null;
}

/// <summary>
/// Holds catalog, validator, digest, local-artifact, and stage state for one mirrored file.
/// </summary>
public class DatasetManifestFile
{
    /// <summary>The owning catalog product identifier.</summary>
    public string? ProductIdentifier = null;

    /// <summary>The exact catalog filename.</summary>
    public string? FileName = null;

    /// <summary>The credential-free catalog URI.</summary>
    public global::System.Uri? CatalogUri = null;

    /// <summary>The credential-free resolved download URI.</summary>
    public global::System.Uri? DownloadUri = null;

    /// <summary>The mirror-relative downloaded artifact path.</summary>
    public string? LocalPath = null;

    /// <summary>The mirror-relative resumable partial path.</summary>
    public string? PartialPath = null;

    /// <summary>The mirror-relative extracted artifact paths.</summary>
    public string[] ExtractedPaths = new string[0];

    /// <summary>The expected or observed file size.</summary>
    public long Size = 0L;

    /// <summary>Whether <see cref="Size"/> was supplied.</summary>
    public bool HasSize = false;

    /// <summary>The HTTP entity tag used to validate changes or resume state.</summary>
    public string? ETag = null;

    /// <summary>The HTTP or catalog last-modified time.</summary>
    public global::System.DateTimeOffset LastModified = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModified"/> was supplied.</summary>
    public bool HasLastModified = false;

    /// <summary>The algorithm used by the persisted content digest.</summary>
    public ChecksumAlgorithm DigestAlgorithm = ChecksumAlgorithm.None;

    /// <summary>The normalized hexadecimal content digest.</summary>
    public string? Digest = null;

    /// <summary>Whether the digest was calculated from the complete local artifact.</summary>
    public bool DigestVerified = false;

    /// <summary>The independently retryable processing stages.</summary>
    public DatasetManifestStage[] Stages = new DatasetManifestStage[0];

    /// <summary>The origin and retrieval context of this file metadata.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}

/// <summary>
/// Holds persisted catalog metadata and mirrored files for one product.
/// </summary>
public class DatasetManifestProduct
{
    /// <summary>The stable catalog product identifier.</summary>
    public string? ProductIdentifier = null;

    /// <summary>The catalog product title.</summary>
    public string? Title = null;

    /// <summary>The logical dataset family.</summary>
    public DatasetFamily Family = DatasetFamily.Unknown;

    /// <summary>The credential-free product catalog URI.</summary>
    public global::System.Uri? CatalogUri = null;

    /// <summary>The catalog product last-modified time.</summary>
    public global::System.DateTimeOffset LastModified = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModified"/> was supplied.</summary>
    public bool HasLastModified = false;

    /// <summary>The mirrored files belonging to this product.</summary>
    public DatasetManifestFile[] Files = new DatasetManifestFile[0];

    /// <summary>The origin and retrieval context of this product metadata.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}

/// <summary>
/// Holds one versioned, credential-free snapshot of persisted dataset mirror state.
/// </summary>
public class DatasetManifest
{
    /// <summary>The manifest schema version.</summary>
    public int FormatVersion = 1;

    /// <summary>A stable identifier for this mirror manifest.</summary>
    public string? ManifestIdentifier = null;

    /// <summary>The time at which the manifest was created.</summary>
    public global::System.DateTimeOffset CreatedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="CreatedAt"/> was supplied.</summary>
    public bool HasCreatedAt = false;

    /// <summary>The time at which the manifest was last updated.</summary>
    public global::System.DateTimeOffset UpdatedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="UpdatedAt"/> was supplied.</summary>
    public bool HasUpdatedAt = false;

    /// <summary>The persisted product snapshots.</summary>
    public DatasetManifestProduct[] Products = new DatasetManifestProduct[0];

    /// <summary>The provenance of the manifest snapshot.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.LocalMirror,
        IsAuthoritative = false
    };
}
