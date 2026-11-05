namespace ttf;

/// <summary>Controls validation of table and whole-font checksums.</summary>
public enum FontChecksumPolicy
{
    /// <summary>Treat a checksum mismatch as a fatal load error.</summary>
    RequireValid = 0,

    /// <summary>Validate checksums, report mismatches, and continue when otherwise safe.</summary>
    ReportMismatch = 1,

    /// <summary>Do not calculate or validate input checksums.</summary>
    Ignore = 2
}
