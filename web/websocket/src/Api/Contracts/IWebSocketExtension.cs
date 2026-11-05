using System;

namespace websocket;

/// <summary>
/// Defines WebSocket extension negotiation, transformation, and lifecycle operations.
/// </summary>
public interface IWebSocketExtension
{
    /// <summary>
    /// Creates this extension's client offer.
    /// </summary>
    /// <returns>The extension offer.</returns>
    WebSocketExtensionOffer CreateClientExtensionOffer();

    /// <summary>
    /// Evaluates a client extension offer for server negotiation.
    /// </summary>
    /// <param name="offer">The client offer.</param>
    /// <returns>The server negotiation result.</returns>
    WebSocketExtensionNegotiation AcceptClientExtensionOffer(WebSocketExtensionOffer offer);

    /// <summary>
    /// Validates a server extension response against retained client offers.
    /// </summary>
    /// <param name="response">The server extension response.</param>
    /// <returns>The client validation result.</returns>
    WebSocketExtensionNegotiation ValidateServerExtensionResponse(WebSocketExtensionOffer response);

    /// <summary>
    /// Transforms an outbound message chunk.
    /// </summary>
    /// <param name="context">The transform context.</param>
    /// <param name="source">The source message bytes.</param>
    /// <returns>The incremental transform result.</returns>
    WebSocketTransformResult TransformOutboundChunk(WebSocketExtensionContext context, ReadOnlySpan<byte> source);

    /// <summary>
    /// Transforms an inbound message chunk.
    /// </summary>
    /// <param name="context">The transform context.</param>
    /// <param name="source">The source message bytes.</param>
    /// <returns>The incremental transform result.</returns>
    WebSocketTransformResult TransformInboundChunk(WebSocketExtensionContext context, ReadOnlySpan<byte> source);

    /// <summary>
    /// Completes extension state at a message boundary.
    /// </summary>
    /// <param name="context">The final message context.</param>
    void CompleteExtensionMessage(WebSocketExtensionContext context);

    /// <summary>
    /// Resets all per-connection extension state.
    /// </summary>
    void ResetExtensionState();
}
