using System;
using System.Text;

namespace websocket;

/// <summary>
/// Parses, measures, and writes complete RFC 6455 Close payloads.
/// </summary>
public class WebSocketCloseCodec : IWebSocketCloseCodec
{
    /// <summary>
    /// Initializes a stateless Close payload codec.
    /// </summary>
    public WebSocketCloseCodec()
    {
    }

    /// <summary>
    /// Reads a complete Close payload.
    /// </summary>
    /// <param name="source">The complete Close payload bytes.</param>
    /// <returns>The decoded close result.</returns>
    public WebSocketCloseReadResult ReadClosePayload(ReadOnlySpan<byte> source)
    {
        if (source.Length > WebSocketProtocolConstants.ControlPayloadLengthMaximum)
        {
            return CreateReadError(
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "close-payload-too-large",
                "The Close payload exceeds the control-frame limit.");
        }

        if (source.Length == 0)
        {
            WebSocketCloseReadResult emptyResult = new WebSocketCloseReadResult();
            emptyResult.Valid = true;
            return emptyResult;
        }

        if (source.Length == 1)
        {
            return CreateReadError(
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "close-payload-one-byte",
                "A Close payload cannot contain only one status-code byte.");
        }

        int code = ((int)source[0] << 8) | (int)source[1];
        CloseCodeClassification classification = CloseCodeClassifier.Classify(code);
        if (!classification.Valid)
        {
            return CreateReadError(
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "close-code-invalid",
                "The Close payload contains a code that is not valid on the wire.");
        }

        ReadOnlySpan<byte> reasonBytes = source.Slice(WebSocketProtocolConstants.CloseCodeLength);
        Utf8State utf8State = new Utf8State();
        if (!utf8State.Validate(reasonBytes) || !utf8State.Complete())
        {
            return CreateReadError(
                WebSocketErrorKind.Utf8,
                WebSocketCloseCodes.InvalidFramePayloadData,
                "close-reason-invalid-utf8",
                "The Close reason is not valid UTF-8.");
        }

        WebSocketCloseReadResult result = new WebSocketCloseReadResult();
        result.Valid = true;
        result.Close.HasCode = true;
        result.Close.Code = code;
        result.Close.HasKnownCode = classification.HasKnownCode;
        result.Close.KnownCode = classification.KnownCode;
        if (reasonBytes.Length != 0)
        {
            result.Close.HasReason = true;
            result.Close.Reason = Encoding.UTF8.GetString(reasonBytes);
        }

        return result;
    }

    /// <summary>
    /// Measures the encoded length of close information.
    /// </summary>
    /// <param name="close">The close information to encode.</param>
    /// <returns>The required payload length in bytes.</returns>
    /// <exception cref="WebSocketProtocolException">Thrown when the close information cannot form a valid wire payload.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the encoded reason exceeds the control-frame limit.</exception>
    public int MeasureClosePayload(WebSocketCloseInfo close)
    {
        if (close == null)
        {
            throw new WebSocketProtocolException();
        }

        if (!close.HasCode)
        {
            if (close.HasReason)
            {
                throw new WebSocketProtocolException();
            }

            return 0;
        }

        CloseCodeClassification classification = CloseCodeClassifier.Classify(close.Code);
        if (!classification.Valid)
        {
            throw new WebSocketProtocolException();
        }

        if (!close.HasReason)
        {
            return WebSocketProtocolConstants.CloseCodeLength;
        }

        CloseReasonValidationResult reasonValidation = ClosePayloadValidator.ValidateReason(close.Reason);
        if (reasonValidation.TooLong)
        {
            throw new WebSocketLimitException();
        }

        if (!reasonValidation.Valid)
        {
            throw new WebSocketProtocolException();
        }

        return WebSocketProtocolConstants.CloseCodeLength + reasonValidation.ByteCount;
    }

    /// <summary>
    /// Writes close information to a caller-provided destination.
    /// </summary>
    /// <param name="close">The close information to encode.</param>
    /// <param name="destination">The destination bytes.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="WebSocketProtocolException">Thrown when the close information cannot form a valid wire payload.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the payload exceeds the limit or the destination is too small.</exception>
    public int WriteClosePayload(WebSocketCloseInfo close, Span<byte> destination)
    {
        int payloadLength = MeasureClosePayload(close);
        if (destination.Length < payloadLength)
        {
            throw new WebSocketLimitException();
        }

        if (payloadLength == 0)
        {
            return 0;
        }

        destination[0] = (byte)((close.Code >> 8) & 255);
        destination[1] = (byte)(close.Code & 255);
        if (close.HasReason)
        {
            Encoding.UTF8.GetBytes(
                close.Reason,
                destination.Slice(WebSocketProtocolConstants.CloseCodeLength));
        }

        return payloadLength;
    }

    internal static WebSocketCloseReadResult CreateReadError(
        WebSocketErrorKind kind,
        int closeCode,
        string identifier,
        string diagnostic)
    {
        WebSocketCloseReadResult result = new WebSocketCloseReadResult();
        result.HasError = true;
        result.Error.Kind = kind;
        result.Error.HasCloseCode = true;
        result.Error.CloseCode = closeCode;
        CloseCodeClassification classification = CloseCodeClassifier.Classify(closeCode);
        result.Error.HasKnownCloseCode = classification.HasKnownCode;
        result.Error.KnownCloseCode = classification.KnownCode;
        result.Error.Identifier = identifier;
        result.Error.Diagnostic = diagnostic;
        return result;
    }
}
