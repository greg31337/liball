namespace http;

/// <summary>
/// Identifies the outcome of HTTP representation negotiation.
/// </summary>
public enum HttpNegotiationOutcome
{
    /// <summary>
    /// An acceptable representation or dimension value was selected.
    /// </summary>
    Selected = 0,

    /// <summary>
    /// No available representation is acceptable.
    /// </summary>
    NoAcceptableRepresentation = 1,

    /// <summary>
    /// A supplied preference field is malformed.
    /// </summary>
    MalformedPreference = 2,

    /// <summary>
    /// The selecting preference dimension is absent.
    /// </summary>
    AbsentDimension = 3
}
