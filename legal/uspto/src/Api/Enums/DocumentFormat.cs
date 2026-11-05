namespace uspto;

/// <summary>
/// Defines a supported document or response representation.
/// </summary>
public enum DocumentFormat
{
    /// <summary>The representation is unknown.</summary>
    Unknown = 0,

    /// <summary>JavaScript Object Notation.</summary>
    Json = 1,

    /// <summary>Extensible Markup Language.</summary>
    Xml = 2,

    /// <summary>Hypertext Markup Language.</summary>
    Html = 3,

    /// <summary>Portable Document Format.</summary>
    Pdf = 4,

    /// <summary>A ZIP archive.</summary>
    Zip = 5,

    /// <summary>Comma-separated values.</summary>
    Csv = 6,

    /// <summary>Plain text.</summary>
    Text = 7
}
