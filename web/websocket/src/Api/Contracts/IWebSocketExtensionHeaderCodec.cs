using System;

namespace websocket;

/// <summary>
/// Defines bounded WebSocket extension-field parsing, measurement, and writing operations.
/// </summary>
public interface IWebSocketExtensionHeaderCodec
{
    /// <summary>
    /// Parses all <c>Sec-WebSocket-Extensions</c> fields in their received order.
    /// </summary>
    /// <param name="fields">The HTTP fields containing zero or more extension fields.</param>
    /// <returns>The ordered extension offers and parameters.</returns>
    /// <exception cref="WebSocketExtensionException">Thrown when field metadata or extension syntax is invalid.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when a configured parser limit is exceeded.</exception>
    WebSocketExtensionOffer[] ParseExtensionFields(WebSocketHeaderField[] fields);

    /// <summary>
    /// Measures one canonical combined extension field value.
    /// </summary>
    /// <param name="offers">The ordered extension offers to serialize.</param>
    /// <returns>The required destination length in characters.</returns>
    /// <exception cref="WebSocketExtensionException">Thrown when an offer or parameter is invalid.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when a configured serialization limit is exceeded.</exception>
    int MeasureExtensionFields(WebSocketExtensionOffer[] offers);

    /// <summary>
    /// Writes one canonical combined extension field value.
    /// </summary>
    /// <param name="offers">The ordered extension offers to serialize.</param>
    /// <param name="destination">The caller-provided character destination.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="WebSocketExtensionException">Thrown when an offer or parameter is invalid.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when a configured limit or destination capacity is exceeded.</exception>
    int WriteExtensionFields(
        WebSocketExtensionOffer[] offers,
        Span<char> destination);
}
