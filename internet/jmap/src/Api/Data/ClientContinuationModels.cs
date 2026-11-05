using System.Collections.Generic;

namespace jmap;

/// <summary>
/// Stores a complete ordered query result and the state needed to apply a later queryChanges result.
/// </summary>
public class JmapQuerySnapshot
{
    /// <summary>The account containing the query result.</summary>
    public string? AccountId;

    /// <summary>The query state represented by the identifiers.</summary>
    public string? QueryState;

    /// <summary>The complete ordered identifiers in the query result.</summary>
    public List<string> Ids = new List<string>();
}
