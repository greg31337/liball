using System;

namespace websocket;

/// <summary>
/// Composes incremental WebSocket frame parsing, validation, masking, and writing.
/// </summary>
public class WebSocketFrameCodec : IWebSocketFrameCodec
{
    internal WebSocketRole Role;
    internal int MaximumFramePayloadLength;
    internal WebSocketExtensionNegotiation[] Extensions;
    internal FrameReadState ReadState;
    internal FrameWriteState WriteState;
    internal bool ReadHeaderValidated;
    internal bool ReadHeaderReturned;
    internal bool ReadFailed;
    internal WebSocketParseStatus ReadFailureStatus = WebSocketParseStatus.ProtocolFailure;
    internal WebSocketError ReadError = new WebSocketError();

    /// <summary>
    /// Initializes a frame codec with inbound role, payload limit, and negotiated extension claims.
    /// </summary>
    /// <param name="role">The local endpoint role used for inbound and outbound masking rules.</param>
    /// <param name="maximumFramePayloadLength">The maximum accepted or written frame payload length.</param>
    /// <param name="extensions">The completed extension negotiations whose claims are allowed on frames.</param>
    /// <exception cref="WebSocketStateException">Thrown when the role or extension metadata is invalid.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the frame payload limit is not positive.</exception>
    public WebSocketFrameCodec(
        WebSocketRole role,
        int maximumFramePayloadLength,
        WebSocketExtensionNegotiation[] extensions)
    {
        if (role != WebSocketRole.Client && role != WebSocketRole.Server)
        {
            throw new WebSocketStateException();
        }

        if (maximumFramePayloadLength <= 0)
        {
            throw new WebSocketLimitException();
        }

        if (extensions == null ||
            !WebSocketExtensionClaimValidator.ValidateClaims(extensions))
        {
            throw new WebSocketStateException();
        }

        Role = role;
        MaximumFramePayloadLength = maximumFramePayloadLength;
        Extensions = CopyExtensions(extensions);
        ReadState = new FrameReadState();
        WriteState = new FrameWriteState();
    }

    /// <summary>
    /// Reads frame data from the supplied source bytes.
    /// </summary>
    /// <param name="source">The next contiguous input bytes.</param>
    /// <returns>The incremental frame read result.</returns>
    public WebSocketFrameReadResult ReadFrame(ReadOnlySpan<byte> source)
    {
        WebSocketFrameReadResult result = new WebSocketFrameReadResult();
        if (ReadFailed)
        {
            result.Status = ReadFailureStatus;
            result.HasError = true;
            result.Error = CopyError(ReadError);
            return result;
        }

        int sourceIndex = 0;
        if (!ReadState.HeaderComplete)
        {
            FrameHeaderReadResult headerResult = ReadState.ReadHeader(source);
            sourceIndex = sourceIndex + headerResult.ConsumedCount;
            if (headerResult.Status == WebSocketParseStatus.ProtocolFailure)
            {
                SetReadFailure(
                    WebSocketParseStatus.ProtocolFailure,
                    FrameValidator.CreateFailure(
                        WebSocketParseStatus.ProtocolFailure,
                        WebSocketErrorKind.Protocol,
                        WebSocketCloseCodes.ProtocolError,
                        "frame-header-invalid",
                        "The frame header length encoding is invalid.").Error);
                result.Status = ReadFailureStatus;
                result.ConsumedCount = sourceIndex;
                result.HasError = true;
                result.Error = CopyError(ReadError);
                return result;
            }

            if (headerResult.Status != WebSocketParseStatus.Complete)
            {
                result.Status = WebSocketParseStatus.NeedInput;
                result.ConsumedCount = sourceIndex;
                return result;
            }
        }

        if (!ReadHeaderValidated)
        {
            FrameValidationResult validation = FrameValidator.ValidateHeader(
                ReadState.Header,
                Role,
                MaximumFramePayloadLength,
                Extensions);
            if (!validation.Valid)
            {
                SetReadFailure(validation.Status, validation.Error);
                result.Status = ReadFailureStatus;
                result.ConsumedCount = sourceIndex;
                result.HasError = true;
                result.Error = CopyError(ReadError);
                return result;
            }

            ReadHeaderValidated = true;
        }

        if (!ReadHeaderReturned)
        {
            result.HasHeader = true;
            result.Header = CopyHeader(ReadState.Header);
            ReadHeaderReturned = true;
        }

        FramePayloadReadResult payloadResult = ReadState.ReadPayload(
            source.Slice(sourceIndex),
            MaximumFramePayloadLength);
        sourceIndex = sourceIndex + payloadResult.ConsumedCount;
        result.Status = payloadResult.Status;
        result.ConsumedCount = sourceIndex;
        result.Payload = payloadResult.Payload;
        result.PayloadCount = payloadResult.PayloadCount;
        result.EndOfFrame = payloadResult.EndOfFrame;
        if (payloadResult.HasError)
        {
            SetReadFailure(payloadResult.Status, payloadResult.Error);
            result.HasError = true;
            result.Error = CopyError(ReadError);
        }

        return result;
    }

    /// <summary>
    /// Measures the complete encoded size of a frame write request.
    /// </summary>
    /// <param name="request">The frame write request.</param>
    /// <returns>The complete encoded frame size in bytes.</returns>
    /// <exception cref="WebSocketProtocolException">Thrown when the frame header is invalid.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the frame exceeds the configured or signed size limit.</exception>
    /// <exception cref="WebSocketStateException">Thrown when the write request is inconsistent.</exception>
    public int MeasureFrame(WebSocketFrameWriteRequest request)
    {
        FrameMeasurementResult measurement = FrameWriteState.MeasureFrame(request);
        if (measurement.Status != WebSocketParseStatus.Complete)
        {
            throw CreateWriteException(measurement.Status, measurement.Error);
        }

        FrameValidationResult validation = ValidateOutboundHeader(request.Header);
        if (!validation.Valid)
        {
            throw CreateWriteException(validation.Status, validation.Error);
        }

        return measurement.EncodedLength;
    }

    /// <summary>
    /// Writes the next encoded frame bytes and advances the consumed request payload range.
    /// </summary>
    /// <param name="request">The current frame payload chunk and continuation offset.</param>
    /// <param name="destination">The bounded output destination.</param>
    /// <returns>The number of encoded bytes produced.</returns>
    /// <exception cref="WebSocketProtocolException">Thrown when the frame header is invalid.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the frame exceeds the configured or signed size limit.</exception>
    /// <exception cref="WebSocketStateException">Thrown when the write request is inconsistent.</exception>
    public int WriteFrame(WebSocketFrameWriteRequest request, Span<byte> destination)
    {
        if (WriteState.Complete)
        {
            WriteState.ResetFrame();
        }

        if (!WriteState.Active)
        {
            FrameMeasurementResult measurement = FrameWriteState.MeasureFrame(request);
            if (measurement.Status != WebSocketParseStatus.Complete)
            {
                throw CreateWriteException(measurement.Status, measurement.Error);
            }

            FrameValidationResult validation = ValidateOutboundHeader(request.Header);
            if (!validation.Valid)
            {
                throw CreateWriteException(validation.Status, validation.Error);
            }
        }

        FrameWriteResult writeResult = WriteState.WriteFrame(request, destination);
        if (writeResult.HasError)
        {
            throw CreateWriteException(writeResult.Status, writeResult.Error);
        }

        request.PayloadOffset = request.PayloadOffset + writeResult.ConsumedCount;
        request.PayloadCount = request.PayloadCount - writeResult.ConsumedCount;
        request.MaskingOffset = request.MaskingOffset + writeResult.ConsumedCount;
        return writeResult.ProducedCount;
    }

    /// <summary>
    /// Resets incremental input state before reading another frame.
    /// </summary>
    public void ResetFrameRead()
    {
        ReadState.ResetHeader();
        ReadHeaderValidated = false;
        ReadHeaderReturned = false;
        ReadFailed = false;
        ReadFailureStatus = WebSocketParseStatus.ProtocolFailure;
        ReadError = new WebSocketError();
    }

    internal FrameValidationResult ValidateOutboundHeader(WebSocketFrameHeader header)
    {
        WebSocketRole receivingRole = WebSocketRole.Client;
        if (Role == WebSocketRole.Client)
        {
            receivingRole = WebSocketRole.Server;
        }

        return FrameValidator.ValidateHeader(
            header,
            receivingRole,
            MaximumFramePayloadLength,
            Extensions);
    }

    internal void SetReadFailure(WebSocketParseStatus status, WebSocketError error)
    {
        ReadFailed = true;
        ReadFailureStatus = status;
        ReadError = CopyError(error);
    }

    internal static WebSocketFrameHeader CopyHeader(WebSocketFrameHeader source)
    {
        WebSocketFrameHeader copy = new WebSocketFrameHeader();
        copy.Final = source.Final;
        copy.Rsv1 = source.Rsv1;
        copy.Rsv2 = source.Rsv2;
        copy.Rsv3 = source.Rsv3;
        copy.Opcode = source.Opcode;
        copy.Masked = source.Masked;
        copy.MaskingKey = source.MaskingKey;
        copy.PayloadLength = source.PayloadLength;
        return copy;
    }

    internal static WebSocketError CopyError(WebSocketError source)
    {
        WebSocketError copy = new WebSocketError();
        copy.Kind = source.Kind;
        copy.HasCloseCode = source.HasCloseCode;
        copy.CloseCode = source.CloseCode;
        copy.HasKnownCloseCode = source.HasKnownCloseCode;
        copy.KnownCloseCode = source.KnownCloseCode;
        copy.HasHttpStatusCode = source.HasHttpStatusCode;
        copy.HttpStatusCode = source.HttpStatusCode;
        copy.Identifier = source.Identifier;
        copy.Diagnostic = source.Diagnostic;
        return copy;
    }

    internal static WebSocketExtensionNegotiation[] CopyExtensions(
        WebSocketExtensionNegotiation[] source)
    {
        WebSocketExtensionNegotiation[] copy = new WebSocketExtensionNegotiation[source.Length];
        int extensionIndex = 0;
        while (extensionIndex < source.Length)
        {
            WebSocketExtensionNegotiation extension = source[extensionIndex];
            if (extension == null || extension.ClaimedOpcodes == null)
            {
                throw new WebSocketStateException();
            }

            WebSocketExtensionNegotiation extensionCopy = new WebSocketExtensionNegotiation();
            extensionCopy.Status = extension.Status;
            extensionCopy.ClaimsRsv1 = extension.ClaimsRsv1;
            extensionCopy.ClaimsRsv2 = extension.ClaimsRsv2;
            extensionCopy.ClaimsRsv3 = extension.ClaimsRsv3;
            extensionCopy.ClaimedOpcodes = new WebSocketOpcode[extension.ClaimedOpcodes.Length];
            int opcodeIndex = 0;
            while (opcodeIndex < extension.ClaimedOpcodes.Length)
            {
                extensionCopy.ClaimedOpcodes[opcodeIndex] = extension.ClaimedOpcodes[opcodeIndex];
                opcodeIndex = opcodeIndex + 1;
            }

            copy[extensionIndex] = extensionCopy;
            extensionIndex = extensionIndex + 1;
        }

        return copy;
    }

    internal static System.Exception CreateWriteException(
        WebSocketParseStatus status,
        WebSocketError error)
    {
        if (status == WebSocketParseStatus.LimitRejected || error.Kind == WebSocketErrorKind.Limit)
        {
            return new WebSocketLimitException();
        }

        if (error.Kind == WebSocketErrorKind.State)
        {
            return new WebSocketStateException();
        }

        return new WebSocketProtocolException();
    }
}
