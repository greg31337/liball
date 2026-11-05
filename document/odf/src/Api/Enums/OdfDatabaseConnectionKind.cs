namespace odf;

/// <summary>
/// Identifies the kind of an inert database connection description.
/// </summary>
public enum OdfDatabaseConnectionKind
{
    /// <summary>The connection kind is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>A general connection resource IRI.</summary>
    Resource = 1,
    /// <summary>A file-based database connection.</summary>
    File = 2,
    /// <summary>A server database connection.</summary>
    Server = 3
}
