namespace svg;

/// <summary>Describes one caller-mediated root resource request.</summary>
public class SvgResourceRequest
{
    /// <summary>The URI exactly supplied by the caller.</summary>
    public string SourceUri;
    /// <summary>The current normalized URI text.</summary>
    public string NormalizedUri;
    /// <summary>The requested media kind.</summary>
    public string MediaKind;
    /// <summary>The URI supplying relative-resolution context, or an empty string.</summary>
    public string BaseUri;
    /// <summary>The purpose of the requested resource.</summary>
    public SvgResourceKind ResourceKind;
    /// <summary>The source span that requested the resource, or <c>null</c> for a root request.</summary>
    public SvgSourceSpan? Source;
    /// <summary>The zero-based redirect count.</summary>
    public int RedirectCount;
    /// <summary>The zero-based nested-reference depth.</summary>
    public int ReferenceDepth;
    /// <summary>The stable zero-based resource ordinal within the requesting resource.</summary>
    public int ResourceIndex;
    /// <summary>The synchronous cancellation token.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates a root resource request.</summary>
    /// <param name="sourceUri">The URI supplied by the caller.</param>
    /// <param name="mediaKind">The requested media kind.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public SvgResourceRequest(string sourceUri, string mediaKind, System.Threading.CancellationToken cancellationToken)
    {
        if (sourceUri == null || sourceUri.Length == 0) throw new System.ArgumentOutOfRangeException("sourceUri");
        if (mediaKind == null || mediaKind.Length == 0) throw new System.ArgumentOutOfRangeException("mediaKind");
        SourceUri = sourceUri;
        NormalizedUri = sourceUri;
        MediaKind = mediaKind;
        BaseUri = "";
        ResourceKind = SvgResourceKind.RootDocument;
        Source = null;
        RedirectCount = 0;
        ReferenceDepth = 0;
        ResourceIndex = 0;
        CancellationToken = cancellationToken;
    }
}
