using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents RFC 9218 priority parameters and preserved extension members.
/// </summary>
public class HttpPriority
{
    /// <summary>
    /// Indicates that an urgency parameter was explicitly supplied.
    /// </summary>
    public bool HasUrgency;

    /// <summary>
    /// The urgency value from zero through seven.
    /// </summary>
    public int Urgency = 3;

    /// <summary>
    /// Indicates that an incremental parameter was explicitly supplied.
    /// </summary>
    public bool HasIncremental;

    /// <summary>
    /// The incremental-processing preference.
    /// </summary>
    public bool Incremental;

    /// <summary>
    /// The parsed Structured Dictionary, or <c>null</c> when no source value is retained.
    /// </summary>
    public StructuredFieldValue? StructuredValue;

    /// <summary>
    /// The ordered unrecognized Structured Dictionary members.
    /// </summary>
    public List<StructuredFieldMember> UnknownParameters = new List<StructuredFieldMember>();
}
