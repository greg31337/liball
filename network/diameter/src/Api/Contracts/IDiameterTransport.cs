using System;

namespace diameter;

/// <summary>
/// Creates Diameter transport listeners and active connections.
/// </summary>
public interface IDiameterTransportFactory
{
    /// <summary>Probes support for a transport and security configuration.</summary>
    /// <param name="transport">The transport configuration to probe.</param>
    /// <param name="security">The security configuration to probe.</param>
    /// <returns>The support outcome and capability metadata.</returns>
    DiameterTransportSupportResult ProbeDiameterTransportSupport(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security);

    /// <summary>Creates a passive Diameter transport listener.</summary>
    /// <param name="transport">The listener transport configuration.</param>
    /// <param name="security">The security applied to accepted connections.</param>
    /// <returns>The listener creation outcome.</returns>
    DiameterListenerResult CreateDiameterListener(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security);

    /// <summary>Creates an active connection to a Diameter endpoint.</summary>
    /// <param name="endpoint">The remote transport endpoint.</param>
    /// <param name="transport">The local transport configuration.</param>
    /// <param name="security">The security applied to the connection.</param>
    /// <param name="timeoutMilliseconds">The positive connection timeout in milliseconds.</param>
    /// <returns>The connection outcome.</returns>
    DiameterConnectionResult ConnectDiameterEndpoint(
        DiameterEndpoint? endpoint,
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security,
        int timeoutMilliseconds);
}

/// <summary>
/// Accepts inbound Diameter transport connections.
/// </summary>
public interface IDiameterListener
{
    /// <summary>Waits for one inbound connection.</summary>
    /// <param name="timeoutMilliseconds">The positive accept timeout in milliseconds.</param>
    /// <returns>The accepted connection outcome.</returns>
    DiameterConnectionResult AcceptDiameterConnection(int timeoutMilliseconds);

    /// <summary>Reads an independent listener metadata snapshot.</summary>
    /// <returns>The current listener metadata.</returns>
    DiameterListenerMetadata ReadDiameterListenerMetadata();

    /// <summary>Closes the listener idempotently.</summary>
    /// <returns>The close outcome.</returns>
    DiameterTransportOperationResult CloseDiameterListener();
}

/// <summary>
/// Exchanges bytes over one established Diameter transport connection.
/// </summary>
public interface IDiameterConnection
{
    /// <summary>Sends bytes, blocking until progress, timeout, closure, or failure.</summary>
    /// <param name="source">The bytes to send.</param>
    /// <param name="timeoutMilliseconds">The positive send timeout in milliseconds.</param>
    /// <returns>The send outcome and transferred byte count.</returns>
    DiameterTransportIoResult SendDiameterConnectionBytes(
        ReadOnlySpan<byte> source,
        int timeoutMilliseconds);

    /// <summary>Receives bytes, blocking until progress, timeout, closure, or failure.</summary>
    /// <param name="destination">The destination for received bytes.</param>
    /// <param name="timeoutMilliseconds">The positive receive timeout in milliseconds.</param>
    /// <returns>The receive outcome and transferred byte count.</returns>
    DiameterTransportIoResult ReceiveDiameterConnectionBytes(
        Span<byte> destination,
        int timeoutMilliseconds);

    /// <summary>Reads an independent connection metadata snapshot.</summary>
    /// <returns>The current connection metadata.</returns>
    DiameterConnectionMetadata ReadDiameterConnectionMetadata();

    /// <summary>Closes the connection idempotently.</summary>
    /// <returns>The close outcome.</returns>
    DiameterTransportOperationResult CloseDiameterConnection();
}
