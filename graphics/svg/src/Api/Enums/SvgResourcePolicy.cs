namespace svg;

/// <summary>Identifies the purpose of a caller-mediated resource.</summary>
public enum SvgResourceKind
{
    /// <summary>A root SVG document explicitly requested by the caller.</summary>
    RootDocument = 0,
    /// <summary>A nested SVG or XML document.</summary>
    Document = 1,
    /// <summary>An imported CSS stylesheet.</summary>
    StyleSheet = 2,
    /// <summary>An SVG or raster image.</summary>
    Image = 3,
    /// <summary>A font resource.</summary>
    Font = 4,
    /// <summary>Another caller-defined resource kind.</summary>
    Other = 5
}

/// <summary>Identifies why a resource policy denied a request.</summary>
public enum SvgResourceDenialReason
{
    /// <summary>The request was allowed.</summary>
    None = 0,
    /// <summary>The policy request or one of its normalized URIs was invalid.</summary>
    InvalidRequest = 1,
    /// <summary>Caller-resolved root URI loading was disabled.</summary>
    RootUriDenied = 2,
    /// <summary>Same-document resource references were disabled.</summary>
    SameDocumentDenied = 3,
    /// <summary>Imported stylesheets were disabled.</summary>
    StyleSheetDenied = 4,
    /// <summary>Relative resource references were disabled.</summary>
    RelativeUriDenied = 5,
    /// <summary>The normalized URI scheme was not allowed.</summary>
    SchemeDenied = 6,
    /// <summary>Data URLs were disabled for the requested resource kind.</summary>
    DataUriDenied = 7,
    /// <summary>File URLs were disabled for the requested resource kind.</summary>
    FileUriDenied = 8,
    /// <summary>A remote file authority was disabled.</summary>
    RemoteFileDenied = 9,
    /// <summary>The file URL was outside the configured file root.</summary>
    FileRootDenied = 10,
    /// <summary>The target crossed an origin boundary.</summary>
    CrossOriginDenied = 11,
    /// <summary>The declared or actual media type was not allowed.</summary>
    MediaTypeDenied = 12,
    /// <summary>Redirect processing was disabled.</summary>
    RedirectDenied = 13,
    /// <summary>The configured redirect count was exceeded.</summary>
    RedirectLimitExceeded = 14,
    /// <summary>The redirect crossed an origin boundary.</summary>
    CrossOriginRedirectDenied = 15
}
