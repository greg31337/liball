namespace uri;

/// <summary>
/// Identifies the operation family that produced a diagnostic.
/// </summary>
public enum DiagnosticDomain
{
    /// <summary>A diagnostic that is not specific to one identifier format.</summary>
    General = 0,

    /// <summary>A URI parsing, validation, composition, normalization, or resolution diagnostic.</summary>
    Uri = 1,

    /// <summary>An IRI parsing, conversion, normalization, or comparison diagnostic.</summary>
    Iri = 2,

    /// <summary>A percent-encoding or UTF-8 decoding diagnostic.</summary>
    PercentEncoding = 3,

    /// <summary>A URI Template parsing, expansion, composition, or inspection diagnostic.</summary>
    UriTemplate = 4,

    /// <summary>A URN parsing, normalization, equivalence, or locator diagnostic.</summary>
    Urn = 5,

    /// <summary>A Web Link parsing, serialization, resolution, or filtering diagnostic.</summary>
    WebLink = 6
}
