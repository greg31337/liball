using System;

namespace diameter;

/// <summary>
/// Receives structured operational events from a Diameter node.
/// </summary>
public interface IDiameterEventSink
{
    /// <summary>
    /// Handles an operational event.
    /// </summary>
    /// <param name="eventData">The event data to handle.</param>
    void HandleDiameterEvent(DiameterOperationalEvent eventData);
}

/// <summary>
/// Removes or replaces sensitive information before an operational event is published.
/// </summary>
public interface IRedactionPolicy
{
    /// <summary>
    /// Redacts one owned operational event without changing the supplied event.
    /// </summary>
    /// <param name="eventData">The owned event to redact.</param>
    /// <returns>The owned event that is safe to publish.</returns>
    DiameterOperationalEvent RedactDiameterEvent(
        DiameterOperationalEvent eventData);
}

/// <summary>
/// Releases a caller-supplied node dependency during deterministic shutdown.
/// </summary>
public interface IDiameterNodeResource
{
    /// <summary>Stops the resource and releases its owned state idempotently.</summary>
    void StopDiameterNodeResource();
}

/// <summary>
/// Provides UTC and monotonic time to Diameter components.
/// </summary>
public interface IDiameterClock
{
    /// <summary>
    /// Reads the current Coordinated Universal Time.
    /// </summary>
    /// <returns>The current UTC time.</returns>
    DateTimeOffset ReadUtcTime();

    /// <summary>
    /// Reads elapsed monotonic time in milliseconds.
    /// </summary>
    /// <returns>A monotonic millisecond value whose differences represent elapsed time.</returns>
    long ReadMonotonicMilliseconds();
}

/// <summary>
/// Creates identifiers used by the Diameter protocol and session layer.
/// </summary>
public interface IDiameterIdentifierSource
{
    /// <summary>
    /// Creates a Hop-by-Hop Identifier.
    /// </summary>
    /// <returns>The next Hop-by-Hop Identifier.</returns>
    uint CreateHopByHopIdentifier();

    /// <summary>
    /// Creates an End-to-End Identifier containing a time component.
    /// </summary>
    /// <returns>The next End-to-End Identifier.</returns>
    uint CreateEndToEndIdentifier();

    /// <summary>
    /// Creates a globally scoped Session-Id value.
    /// </summary>
    /// <param name="originHost">The Diameter identity that owns the session.</param>
    /// <param name="correlationText">Optional caller correlation text, or <c>null</c> to omit it.</param>
    /// <returns>The formatted Session-Id value.</returns>
    string CreateSessionIdentifier(DiameterIdentity originHost, string? correlationText);

    /// <summary>
    /// Creates the Origin-State-Id associated with the current source lifetime.
    /// </summary>
    /// <returns>The stable Origin-State-Id for the source.</returns>
    uint CreateOriginStateIdentifier();
}
