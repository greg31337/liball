using System.Collections.Generic;
using System.Threading;

namespace iso20022;

/// <summary>Specifies one explicit, bounded ISO catalogue artifact download.</summary>
public class CatalogueDownloadRequest
{
    /// <summary>The absolute HTTP or HTTPS URI to download.</summary>
    public string Uri = string.Empty;
    /// <summary>The coordinate assigned to downloaded content.</summary>
    public ArtifactCoordinate Coordinate = new ArtifactCoordinate();
    /// <summary>The media types accepted for the final response.</summary>
    public List<string> AcceptedMediaTypes = new List<string>();
    /// <summary>The entity tag supplied for conditional retrieval.</summary>
    public string IfNoneMatch = string.Empty;
    /// <summary>The last-modified time supplied for conditional retrieval when known.</summary>
    public IsoDateTimeValue? IfModifiedSince;
    /// <summary>The expected content hash when independently known.</summary>
    public IsoBinaryValue? ExpectedHash;
    /// <summary>The algorithm used by <see cref="ExpectedHash"/>.</summary>
    public string ExpectedHashAlgorithm = "SHA-256";
    /// <summary>The publication date recorded in the downloaded artifact manifest when known.</summary>
    public IsoPartialDateValue? PublicationDate;
    /// <summary>The license notice recorded in the downloaded artifact manifest.</summary>
    public string LicenseNotice = string.Empty;
    /// <summary>Whether the downloaded artifact is approved rather than draft.</summary>
    public bool IsApproved;
    /// <summary>The maximum number of redirects followed before failing.</summary>
    public int MaxRedirectCount = 10;
    /// <summary>The maximum number of response-content bytes accepted.</summary>
    public int MaxArtifactBytes = Iso20022Limits.MaxArtifactBytes;
    /// <summary>The cancellation request observed while downloading content.</summary>
    public CancellationToken CancellationToken;
}
