namespace image;

/// <summary>Controls how much non-fatal parser information is recorded in the result.</summary>
public enum MetadataDiagnosticsVerbosity
{
    /// <summary>Suppress informational and unsupported-feature diagnostics.</summary>
    Quiet = 0,
    /// <summary>Record actionable warnings, limits, truncation, and recoverable errors.</summary>
    Normal = 1,
    /// <summary>Record normal diagnostics plus additional parser detail.</summary>
    Verbose = 2
}
