namespace svg;

/// <summary>Defines diagnostic severity levels in increasing order.</summary>
public enum SvgDiagnosticSeverity
{
    /// <summary>Provides informational context without indicating a problem.</summary>
    Info = 0,

    /// <summary>Reports a recoverable problem or compatibility decision.</summary>
    Warning = 1,

    /// <summary>Reports a conformance or processing failure.</summary>
    Error = 2
}

/// <summary>Defines how processing responds to error diagnostics.</summary>
public enum SvgDiagnosticBehavior
{
    /// <summary>Stops processing after the first error diagnostic.</summary>
    Strict = 0,

    /// <summary>Continues processing after recoverable errors.</summary>
    Compatible = 1
}

/// <summary>Defines stable append-only diagnostic codes.</summary>
/// <remarks>Numeric values are permanent and are never reassigned.</remarks>
public enum SvgDiagnosticCode
{
    /// <summary>No diagnostic code.</summary>
    None = 0,

    /// <summary>The configured diagnostic bound omitted diagnostics.</summary>
    DiagnosticsTruncated = 1,

    /// <summary>A public API argument was invalid.</summary>
    InvalidArgument = 2,

    /// <summary>A configured security or processing limit was exceeded.</summary>
    InputLimitExceeded = 3,

    /// <summary>XML input was malformed.</summary>
    MalformedXml = 100,

    /// <summary>An XML namespace rule was violated.</summary>
    XmlNamespace = 101,

    /// <summary>The loaded XML did not have a valid SVG root.</summary>
    InvalidSvgRoot = 102,

    /// <summary>The input container was malformed or unsupported.</summary>
    InvalidContainer = 103,

    /// <summary>The XML byte encoding was unsupported, malformed, or inconsistent.</summary>
    UnsupportedEncoding = 104,

    /// <summary>More than one element used the same nonempty ID.</summary>
    DuplicateId = 105,

    /// <summary>An SVG or CSS value was invalid.</summary>
    InvalidValue = 200,

    /// <summary>Compatible mode applied a specified recovery.</summary>
    CompatibilityRecovery = 201,

    /// <summary>CSS syntax or cascade data was invalid.</summary>
    InvalidCss = 300,

    /// <summary>A reference was missing or invalid.</summary>
    InvalidReference = 400,

    /// <summary>A reference cycle was detected.</summary>
    ReferenceCycle = 401,

    /// <summary>Resource policy denied a request.</summary>
    ResourceDenied = 500,

    /// <summary>A permitted resource request failed.</summary>
    ResourceFailed = 501,

    /// <summary>A feature is unsupported.</summary>
    UnsupportedFeature = 600,

    /// <summary>A feature was preserved but not computed or rendered.</summary>
    PreservedFeature = 601,

    /// <summary>A feature requires a caller callback.</summary>
    CallbackRequired = 602,

    /// <summary>A font container, table, checksum, or required metric was malformed.</summary>
    InvalidFont = 603,

    /// <summary>Layout could not be completed.</summary>
    LayoutFailed = 700,

    /// <summary>Rendering could not be completed.</summary>
    RenderingFailed = 800,

    /// <summary>A requested operation made a lossy change.</summary>
    LossyChange = 900,

    /// <summary>A structural patch precondition conflicted with the target document.</summary>
    PatchConflict = 902,

    /// <summary>Processing was cancelled.</summary>
    Cancelled = 901,

    /// <summary>An internal invariant failed.</summary>
    InternalInvariant = 9999
}
