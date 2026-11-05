namespace uspto;

/// <summary>
/// Provides media-type values accepted or returned by supported data services.
/// </summary>
public static class UsptoMediaTypes
{
    /// <summary>The JSON media type.</summary>
    public const string Json = "application/json";

    /// <summary>The XML media type.</summary>
    public const string Xml = "application/xml";

    /// <summary>The alternate text XML media type.</summary>
    public const string TextXml = "text/xml";

    /// <summary>The HTML media type.</summary>
    public const string Html = "text/html";

    /// <summary>The PDF media type.</summary>
    public const string Pdf = "application/pdf";

    /// <summary>The ZIP archive media type.</summary>
    public const string Zip = "application/zip";

    /// <summary>The gzip media type.</summary>
    public const string Gzip = "application/gzip";

    /// <summary>The tar archive media type.</summary>
    public const string Tar = "application/x-tar";

    /// <summary>The CSV media type.</summary>
    public const string Csv = "text/csv";

    /// <summary>The tab-separated-values media type.</summary>
    public const string TabSeparatedValues = "text/tab-separated-values";

    /// <summary>The plain-text media type.</summary>
    public const string Text = "text/plain";

    /// <summary>The TIFF raster-image media type used by native document pages.</summary>
    public const string Tiff = "image/tiff";

    /// <summary>The JPEG raster-image media type used by native document pages.</summary>
    public const string Jpeg = "image/jpeg";

    /// <summary>The PNG raster-image media type used by native document pages.</summary>
    public const string Png = "image/png";

    /// <summary>The form-encoded request media type used by Office Action searches.</summary>
    public const string FormUrlEncoded = "application/x-www-form-urlencoded";

    /// <summary>The generic binary media type.</summary>
    public const string Binary = "application/octet-stream";
}
