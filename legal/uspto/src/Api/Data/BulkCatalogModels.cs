namespace uspto;

/// <summary>
/// Holds a parsed bulk-data date window while preserving the source date text.
/// </summary>
public class BulkDateRange
{
    /// <summary>The parsed beginning of the data window.</summary>
    public global::System.DateTimeOffset FromDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FromDate"/> was parsed successfully.</summary>
    public bool HasFromDate = false;

    /// <summary>The beginning of the data window exactly as supplied.</summary>
    public string? FromDateText = null;

    /// <summary>The parsed end of the data window.</summary>
    public global::System.DateTimeOffset ToDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ToDate"/> was parsed successfully.</summary>
    public bool HasToDate = false;

    /// <summary>The end of the data window exactly as supplied.</summary>
    public string? ToDateText = null;
}

/// <summary>
/// Holds catalog metadata for one downloadable USPTO bulk product file.
/// </summary>
public class BulkProductFile
{
    /// <summary>The catalog product identifier that owns the file.</summary>
    public string? ProductIdentifier = null;

    /// <summary>The exact catalog filename.</summary>
    public string? FileName = null;

    /// <summary>The file size in bytes.</summary>
    public long Size = 0L;

    /// <summary>Whether <see cref="Size"/> was supplied.</summary>
    public bool HasSize = false;

    /// <summary>The source data dates covered by the file.</summary>
    public BulkDateRange DataDateRange = new BulkDateRange();

    /// <summary>The source file-type label.</summary>
    public string? FileType = null;

    /// <summary>The catalog MIME type or format identifier.</summary>
    public string? MimeTypeIdentifier = null;

    /// <summary>The normalized media type, when known.</summary>
    public string? MediaType = null;

    /// <summary>The recognized document representation.</summary>
    public DocumentFormat Format = DocumentFormat.Unknown;

    /// <summary>The recognized compression or archive format.</summary>
    public CompressionFormat Compression = CompressionFormat.Unknown;

    /// <summary>The credential-free catalog endpoint used to resolve the file.</summary>
    public global::System.Uri? CatalogUri = null;

    /// <summary>The credential-free catalog or resolved download URI.</summary>
    public global::System.Uri? DownloadUri = null;

    /// <summary>The HTTP status received while resolving the exact file.</summary>
    public int ResponseStatusCode = 0;

    /// <summary>Whether <see cref="ResponseStatusCode"/> was supplied.</summary>
    public bool HasResponseStatusCode = false;

    /// <summary>The parsed file release time.</summary>
    public global::System.DateTimeOffset ReleaseDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ReleaseDate"/> was parsed successfully.</summary>
    public bool HasReleaseDate = false;

    /// <summary>The file release time exactly as supplied.</summary>
    public string? ReleaseDateText = null;

    /// <summary>The parsed catalog file date.</summary>
    public global::System.DateTimeOffset FileDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FileDate"/> was parsed successfully.</summary>
    public bool HasFileDate = false;

    /// <summary>The catalog file date exactly as supplied.</summary>
    public string? FileDateText = null;

    /// <summary>The parsed last-modified time.</summary>
    public global::System.DateTimeOffset LastModified = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModified"/> was parsed successfully.</summary>
    public bool HasLastModified = false;

    /// <summary>The last-modified time exactly as supplied.</summary>
    public string? LastModifiedText = null;

    /// <summary>The catalog-provided checksum expectations.</summary>
    public ChecksumExpectation[] Checksums = new ChecksumExpectation[0];

    /// <summary>The origin and retrieval context of the file metadata.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>A bounded retained raw exact-file response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Unmodeled file fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one product published by the USPTO bulk-data catalog.
/// </summary>
public class BulkProduct
{
    /// <summary>The stable catalog product identifier or short name.</summary>
    public string? ProductIdentifier = null;

    /// <summary>The product title.</summary>
    public string? Title = null;

    /// <summary>The product description.</summary>
    public string? Description = null;

    /// <summary>The source publication-frequency label.</summary>
    public string? Frequency = null;

    /// <summary>The source days-of-week text.</summary>
    public string? DaysOfWeekText = null;

    /// <summary>The normalized days on which the product is published.</summary>
    public string[] DaysOfWeek = new string[0];

    /// <summary>The product labels assigned by the catalog.</summary>
    public string[] Labels = new string[0];

    /// <summary>The dataset types assigned by the catalog.</summary>
    public string[] DatasetTypes = new string[0];

    /// <summary>The dataset categories assigned by the catalog.</summary>
    public string[] Categories = new string[0];

    /// <summary>The source data dates covered by the product.</summary>
    public BulkDateRange DataDateRange = new BulkDateRange();

    /// <summary>The combined size of the product files in bytes.</summary>
    public long TotalFileSize = 0L;

    /// <summary>Whether <see cref="TotalFileSize"/> was supplied.</summary>
    public bool HasTotalFileSize = false;

    /// <summary>The total number of files reported for the product.</summary>
    public long FileCount = 0L;

    /// <summary>Whether <see cref="FileCount"/> was supplied.</summary>
    public bool HasFileCount = false;

    /// <summary>The number of file records included in this product response.</summary>
    public long ReturnedFileCount = 0L;

    /// <summary>Whether <see cref="ReturnedFileCount"/> was supplied.</summary>
    public bool HasReturnedFileCount = false;

    /// <summary>The parsed product last-modified time.</summary>
    public global::System.DateTimeOffset LastModified = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModified"/> was parsed successfully.</summary>
    public bool HasLastModified = false;

    /// <summary>The product last-modified time exactly as supplied.</summary>
    public string? LastModifiedText = null;

    /// <summary>The MIME type or format identifiers published for the product.</summary>
    public string[] MimeTypeIdentifiers = new string[0];

    /// <summary>The file records included with the product.</summary>
    public BulkProductFile[] Files = new BulkProductFile[0];

    /// <summary>The origin and retrieval context of the product metadata.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>A bounded retained raw exact-product response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Unmodeled product fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one value and count returned for a bulk catalog facet.
/// </summary>
public class BulkCatalogFacetValue
{
    /// <summary>The facet value.</summary>
    public string? Value = null;

    /// <summary>The number of matching products.</summary>
    public long Count = 0L;
}

/// <summary>
/// Holds all returned values for one bulk catalog facet field.
/// </summary>
public class BulkCatalogFacet
{
    /// <summary>The catalog field that was aggregated.</summary>
    public string? Field = null;

    /// <summary>The returned facet values and counts.</summary>
    public BulkCatalogFacetValue[] Values = new BulkCatalogFacetValue[0];
}

/// <summary>
/// Holds the currently available products for one logical dataset family.
/// </summary>
public class BulkDatasetPreset
{
    /// <summary>The logical dataset family.</summary>
    public DatasetFamily Family = DatasetFamily.Unknown;

    /// <summary>The matching catalog product identifiers.</summary>
    public string[] ProductIdentifiers = new string[0];

    /// <summary>The matching catalog products available at retrieval time.</summary>
    public BulkProduct[] Products = new BulkProduct[0];

    /// <summary>Whether at least one matching product is currently available.</summary>
    public bool IsAvailable = false;

    /// <summary>The client-derived provenance of the dynamic preset.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}

/// <summary>
/// Holds one page or bounded aggregate of USPTO bulk catalog products.
/// </summary>
public class BulkProductResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The returned bulk catalog products.</summary>
    public BulkProduct[] Products = new BulkProduct[0];

    /// <summary>The returned catalog facet data.</summary>
    public BulkCatalogFacet[] Facets = new BulkCatalogFacet[0];

    /// <summary>The service-reported product count.</summary>
    public long Count = 0L;

    /// <summary>Whether <see cref="Count"/> was supplied.</summary>
    public bool HasCount = false;

    /// <summary>The number of product records held in this response.</summary>
    public int ReturnedCount = 0;

    /// <summary>The offset-page coordinates associated with the response.</summary>
    public OffsetPage Page = new OffsetPage();

    /// <summary>The service request identifier.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and retrieval context of the response.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>A bounded retained raw response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether all requested product records were processed.</summary>
    public bool IsComplete = false;

    /// <summary>Unmodeled response fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}
