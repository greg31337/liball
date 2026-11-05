namespace uspto;

/// <summary>
/// Identifies the confidence assigned to a client-derived prosecution timeline match.
/// </summary>
public enum ProsecutionMatchConfidence
{
    /// <summary>No match was derived.</summary>
    None = 0,

    /// <summary>The records share only a document or event code.</summary>
    Low = 1,

    /// <summary>The records share an event date.</summary>
    Medium = 2,

    /// <summary>The records share both an event date and a document or event code.</summary>
    High = 3,

    /// <summary>The records share a source document identifier.</summary>
    Exact = 4
}
