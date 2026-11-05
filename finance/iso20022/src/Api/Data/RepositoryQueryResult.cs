using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains deterministic repository query matches and result metadata.</summary>
public class RepositoryQueryResult
{
    /// <summary>Whether the request was valid and evaluated successfully.</summary>
    public bool Success;
    /// <summary>The matching repository entries in deterministic snapshot order.</summary>
    public List<RepositoryQueryItem> Items = new List<RepositoryQueryItem>();
    /// <summary>The total number of matches before applying the result limit.</summary>
    public int TotalMatchCount;
    /// <summary>Whether matches were omitted because the result limit was reached.</summary>
    public bool Truncated;
    /// <summary>A stable error code when the request was invalid.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when the request was invalid.</summary>
    public string ErrorMessage = string.Empty;
}
