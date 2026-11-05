namespace pptx;

/// <summary>
/// Describes how the library handles a feature found in a presentation package.
/// </summary>
public enum PptxCompatibilityLevel
{
    /// <summary>The feature has public creation or mutation operations.</summary>
    Editable = 0,
    /// <summary>The feature is represented by a readable model but has limited mutation support.</summary>
    Readable = 1,
    /// <summary>The feature is retained during round-trip but is not modeled for reading or editing.</summary>
    PreservedOnly = 2,
    /// <summary>The recognized feature is outside the supported library scope.</summary>
    Unsupported = 3,
    /// <summary>The feature can execute, link to, or embed content that requires explicit trust.</summary>
    Unsafe = 4
}
