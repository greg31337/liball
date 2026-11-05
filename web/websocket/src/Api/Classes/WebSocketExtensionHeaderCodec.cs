using System;

namespace websocket;

/// <summary>
/// Parses and canonically serializes bounded <c>Sec-WebSocket-Extensions</c> field values.
/// </summary>
public class WebSocketExtensionHeaderCodec : IWebSocketExtensionHeaderCodec
{
    internal int MaximumFieldCount;
    internal int MaximumExtensionCount;
    internal int MaximumParameterCount;
    internal int MaximumTokenLength;
    internal int MaximumTotalLength;

    /// <summary>
    /// Initializes an extension-field codec with finite count and character limits.
    /// </summary>
    /// <param name="maximumFieldCount">The maximum repeated extension fields accepted while parsing.</param>
    /// <param name="maximumExtensionCount">The maximum extension alternatives accepted or written.</param>
    /// <param name="maximumParameterCount">The maximum total extension parameters accepted or written.</param>
    /// <param name="maximumTokenLength">The maximum extension, parameter, or decoded value token length.</param>
    /// <param name="maximumTotalLength">The maximum combined field-value length in characters.</param>
    /// <exception cref="WebSocketLimitException">Thrown when a limit is not positive or the token limit exceeds the total limit.</exception>
    public WebSocketExtensionHeaderCodec(
        int maximumFieldCount,
        int maximumExtensionCount,
        int maximumParameterCount,
        int maximumTokenLength,
        int maximumTotalLength)
    {
        if (maximumFieldCount <= 0 ||
            maximumExtensionCount <= 0 ||
            maximumParameterCount <= 0 ||
            maximumTokenLength <= 0 ||
            maximumTotalLength <= 0 ||
            maximumTokenLength > maximumTotalLength)
        {
            throw new WebSocketLimitException();
        }

        MaximumFieldCount = maximumFieldCount;
        MaximumExtensionCount = maximumExtensionCount;
        MaximumParameterCount = maximumParameterCount;
        MaximumTokenLength = maximumTokenLength;
        MaximumTotalLength = maximumTotalLength;
    }

    /// <summary>
    /// Parses all <c>Sec-WebSocket-Extensions</c> fields in their received order.
    /// </summary>
    /// <param name="fields">The HTTP fields containing zero or more extension fields.</param>
    /// <returns>The ordered extension offers and parameters.</returns>
    /// <exception cref="WebSocketExtensionException">Thrown when field metadata or extension syntax is invalid.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when a configured parser limit is exceeded.</exception>
    public WebSocketExtensionOffer[] ParseExtensionFields(WebSocketHeaderField[] fields)
    {
        if (fields == null)
        {
            throw new WebSocketExtensionException();
        }

        HeaderFieldLookupResult lookup = HeaderFieldLookup.FindFields(
            fields,
            WebSocketProtocolConstants.ExtensionsHeaderName);
        if (lookup.Status != WebSocketParseStatus.Complete)
        {
            throw CreateExtensionException(lookup.Status);
        }

        WebSocketExtensionResponseParseResult parsed =
            WebSocketExtensionResponseParser.ParseFields(
                lookup.Matches,
                MaximumFieldCount,
                MaximumExtensionCount,
                MaximumParameterCount,
                MaximumTokenLength,
                MaximumTotalLength);
        if (parsed.Status != WebSocketParseStatus.Complete)
        {
            throw CreateExtensionException(parsed.Status);
        }

        return parsed.Extensions;
    }

    /// <summary>
    /// Measures one canonical combined extension field value.
    /// </summary>
    /// <param name="offers">The ordered extension offers to serialize.</param>
    /// <returns>The required destination length in characters.</returns>
    /// <exception cref="WebSocketExtensionException">Thrown when an offer or parameter is invalid.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when a configured serialization limit is exceeded.</exception>
    public int MeasureExtensionFields(WebSocketExtensionOffer[] offers)
    {
        WebSocketExtensionOfferFormatResult result =
            WebSocketExtensionOfferFormatter.MeasureOffers(offers);
        ValidateFormatResult(result);
        return result.RequiredLength;
    }

    /// <summary>
    /// Writes one canonical combined extension field value.
    /// </summary>
    /// <param name="offers">The ordered extension offers to serialize.</param>
    /// <param name="destination">The caller-provided character destination.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="WebSocketExtensionException">Thrown when an offer or parameter is invalid.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when a configured limit or destination capacity is exceeded.</exception>
    public int WriteExtensionFields(
        WebSocketExtensionOffer[] offers,
        Span<char> destination)
    {
        int requiredLength = MeasureExtensionFields(offers);
        if (destination.Length < requiredLength)
        {
            throw new WebSocketLimitException();
        }

        WebSocketExtensionOfferFormatResult result =
            WebSocketExtensionOfferFormatter.WriteOffers(offers, destination);
        if (result.Status != WebSocketParseStatus.Complete)
        {
            throw CreateExtensionException(result.Status);
        }

        return result.ProducedCount;
    }

    void ValidateFormatResult(WebSocketExtensionOfferFormatResult result)
    {
        if (result.Status != WebSocketParseStatus.Complete)
        {
            throw CreateExtensionException(result.Status);
        }

        if (result.OfferCount > MaximumExtensionCount ||
            result.ParameterCount > MaximumParameterCount ||
            result.MaximumTokenLength > MaximumTokenLength ||
            result.RequiredLength > MaximumTotalLength)
        {
            throw new WebSocketLimitException();
        }
    }

    static System.Exception CreateExtensionException(WebSocketParseStatus status)
    {
        if (status == WebSocketParseStatus.LimitRejected ||
            status == WebSocketParseStatus.NeedOutputCapacity)
        {
            return new WebSocketLimitException();
        }

        return new WebSocketExtensionException();
    }
}
