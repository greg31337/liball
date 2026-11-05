namespace uspto;

/// <summary>
/// Identifies the confidence assigned to a client-derived citation match.
/// </summary>
public enum CitationMatchConfidence
{
    /// <summary>No corresponding source record was found.</summary>
    None = 0,

    /// <summary>The records share weak citation characteristics.</summary>
    Low = 1,

    /// <summary>The records share normalized reference text.</summary>
    Medium = 2,

    /// <summary>The records share a normalized document identifier or case-insensitive literature text.</summary>
    High = 3,

    /// <summary>The records share an exact canonical identifier or exact literature text.</summary>
    Exact = 4
}
