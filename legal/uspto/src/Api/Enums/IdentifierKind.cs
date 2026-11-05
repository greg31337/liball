namespace uspto;

/// <summary>
/// Identifies the form of a patent-related identifier.
/// </summary>
public enum IdentifierKind
{
    /// <summary>The identifier kind is not known.</summary>
    Unknown = 0,

    /// <summary>A United States patent application number.</summary>
    PatentApplication = 1,

    /// <summary>A United States pre-grant publication number.</summary>
    PatentPublication = 2,

    /// <summary>A United States patent grant number.</summary>
    PatentGrant = 3,

    /// <summary>An international Patent Cooperation Treaty application number.</summary>
    PctApplication = 4,

    /// <summary>An input that can validly denote more than one identifier kind.</summary>
    Ambiguous = 5
}
