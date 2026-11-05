namespace http;

/// <summary>
/// Represents one available representation and deterministic server ordering metadata.
/// </summary>
public class HttpNegotiationCandidate
{
    /// <summary>
    /// The available representation, or <c>null</c> when not assigned.
    /// </summary>
    public HttpRepresentation? Representation;

    /// <summary>
    /// The server preference, where larger values are preferred.
    /// </summary>
    public int ServerPreference;

    /// <summary>
    /// The zero-based position in the server candidate list.
    /// </summary>
    public int SourceOrder;
}
