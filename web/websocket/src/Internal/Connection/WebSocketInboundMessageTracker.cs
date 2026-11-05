namespace websocket;

internal class WebSocketInboundMessageTrackResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal bool Accepted = true;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
    internal WebSocketFrameHeader Header = new WebSocketFrameHeader();
    internal bool FrameStarted;
    internal bool FrameEnded;
    internal bool DataChunk;
    internal bool ControlChunk;
    internal bool ExtensionChunk;
    internal bool InterleavedControl;
    internal bool HasMessageType;
    internal WebSocketMessageType MessageType;
    internal bool MessageStarted;
    internal bool MessageEnded;
    internal int FragmentCount;
    internal byte[] Payload = [];
    internal int PayloadOffset;
    internal int PayloadCount;
}

internal class WebSocketInboundMessageTracker
{
    int MaximumFragmentCount;
    bool FrameActive;
    WebSocketFrameHeader FrameHeader = new WebSocketFrameHeader();
    long FramePayloadCount;
    bool FrameData;
    bool FrameControl;
    bool FrameExtension;
    bool FrameInterleavedControl;
    bool FrameHasMessageType;
    WebSocketMessageType FrameMessageType;
    bool FrameStartsMessage;
    bool FrameEndsMessage;
    int FrameFragmentCount;
    bool MessageActive;
    WebSocketMessageType ActiveMessageType;
    int ActiveFragmentCount;
    bool Failed;
    WebSocketParseStatus FailureStatus = WebSocketParseStatus.ProtocolFailure;
    WebSocketError FailureError = new WebSocketError();

    internal WebSocketInboundMessageTracker(int maximumFragmentCount)
    {
        if (maximumFragmentCount <= 0)
        {
            throw new WebSocketLimitException();
        }

        MaximumFragmentCount = maximumFragmentCount;
    }

    internal WebSocketInboundMessageTrackResult TrackInboundFrameChunk(
        WebSocketInboundFrameChunk chunk)
    {
        if (Failed)
        {
            return CreateLatchedFailure();
        }

        if (!ValidateChunk(chunk))
        {
            return LatchProtocolFailure(
                "message-frame-chunk-invalid",
                "The inbound frame chunk is inconsistent.");
        }

        if (chunk.FirstChunk)
        {
            if (FrameActive)
            {
                return LatchProtocolFailure(
                    "message-frame-overlap",
                    "A new frame started before the active frame ended.");
            }

            WebSocketInboundMessageTrackResult? startFailure =
                StartFrame(chunk.Header);
            if (startFailure != null)
            {
                return startFailure;
            }
        }
        else
        {
            if (!FrameActive || !HeadersMatch(FrameHeader, chunk.Header))
            {
                return LatchProtocolFailure(
                    "message-frame-continuation-invalid",
                    "The frame chunk does not continue the active frame.");
            }
        }

        long remaining = FrameHeader.PayloadLength - FramePayloadCount;
        if (chunk.PayloadCount > remaining ||
            (chunk.EndOfFrame && chunk.PayloadCount != remaining) ||
            (!chunk.EndOfFrame && chunk.PayloadCount == remaining))
        {
            return LatchProtocolFailure(
                "message-frame-payload-length-invalid",
                "The frame chunks do not match the declared payload length.");
        }

        FramePayloadCount = FramePayloadCount + chunk.PayloadCount;
        WebSocketInboundMessageTrackResult result = CreateResult(chunk);
        if (chunk.EndOfFrame)
        {
            CompleteFrame();
        }

        return result;
    }

    internal void ResetInboundMessageTracker()
    {
        FrameActive = false;
        FrameHeader = new WebSocketFrameHeader();
        FramePayloadCount = 0;
        FrameData = false;
        FrameControl = false;
        FrameExtension = false;
        FrameInterleavedControl = false;
        FrameHasMessageType = false;
        FrameMessageType = WebSocketMessageType.Text;
        FrameStartsMessage = false;
        FrameEndsMessage = false;
        FrameFragmentCount = 0;
        MessageActive = false;
        ActiveMessageType = WebSocketMessageType.Text;
        ActiveFragmentCount = 0;
        Failed = false;
        FailureStatus = WebSocketParseStatus.ProtocolFailure;
        FailureError = new WebSocketError();
    }

    WebSocketInboundMessageTrackResult? StartFrame(
        WebSocketFrameHeader header)
    {
        int opcode = (int)header.Opcode;
        if (opcode < 0 || opcode > 15)
        {
            return LatchProtocolFailure(
                "message-opcode-invalid",
                "The frame opcode is outside the four-bit opcode range.");
        }

        FrameHeader = CopyHeader(header);
        FramePayloadCount = 0;
        FrameData = false;
        FrameControl = false;
        FrameExtension = false;
        FrameInterleavedControl = false;
        FrameHasMessageType = false;
        FrameMessageType = WebSocketMessageType.Text;
        FrameStartsMessage = false;
        FrameEndsMessage = false;
        FrameFragmentCount = 0;

        if (header.Opcode == WebSocketOpcode.Text ||
            header.Opcode == WebSocketOpcode.Binary)
        {
            if (MessageActive)
            {
                return LatchProtocolFailure(
                    "message-fragment-overlap",
                    "A data message started before the fragmented message ended.");
            }

            WebSocketMessageType messageType = WebSocketMessageType.Text;
            if (header.Opcode == WebSocketOpcode.Binary)
            {
                messageType = WebSocketMessageType.Binary;
            }

            FrameData = true;
            FrameHasMessageType = true;
            FrameMessageType = messageType;
            FrameStartsMessage = true;
            FrameEndsMessage = header.Final;
            FrameFragmentCount = 1;
            MessageActive = !header.Final;
            ActiveMessageType = messageType;
            ActiveFragmentCount = 1;
        }
        else if (header.Opcode == WebSocketOpcode.Continuation)
        {
            if (!MessageActive)
            {
                return LatchProtocolFailure(
                    "message-continuation-unexpected",
                    "A continuation frame arrived without an active fragmented message.");
            }

            int fragmentCount = ActiveFragmentCount + 1;
            if (fragmentCount > MaximumFragmentCount)
            {
                return LatchLimitFailure(
                    "message-fragment-limit-exceeded",
                    "The fragmented message contains too many frames.");
            }

            FrameData = true;
            FrameHasMessageType = true;
            FrameMessageType = ActiveMessageType;
            FrameEndsMessage = header.Final;
            FrameFragmentCount = fragmentCount;
            ActiveFragmentCount = fragmentCount;
        }
        else if (opcode >= 8)
        {
            if (!header.Final)
            {
                return LatchProtocolFailure(
                    "message-control-fragmented",
                    "A control frame cannot be fragmented.");
            }

            FrameControl = true;
            FrameInterleavedControl = MessageActive;
        }
        else
        {
            FrameExtension = true;
        }

        FrameActive = true;
        return null;
    }

    WebSocketInboundMessageTrackResult CreateResult(
        WebSocketInboundFrameChunk chunk)
    {
        WebSocketInboundMessageTrackResult result =
            new WebSocketInboundMessageTrackResult();
        result.Header = CopyHeader(FrameHeader);
        result.FrameStarted = chunk.FirstChunk;
        result.FrameEnded = chunk.EndOfFrame;
        result.DataChunk = FrameData;
        result.ControlChunk = FrameControl;
        result.ExtensionChunk = FrameExtension;
        result.InterleavedControl = FrameInterleavedControl;
        result.HasMessageType = FrameHasMessageType;
        result.MessageType = FrameMessageType;
        result.MessageStarted = FrameStartsMessage && chunk.FirstChunk;
        result.MessageEnded = FrameEndsMessage && chunk.EndOfFrame;
        result.FragmentCount = FrameFragmentCount;
        result.Payload = chunk.Payload;
        result.PayloadOffset = chunk.PayloadOffset;
        result.PayloadCount = chunk.PayloadCount;
        return result;
    }

    void CompleteFrame()
    {
        bool endsMessage = FrameData && FrameEndsMessage;
        FrameActive = false;
        FrameHeader = new WebSocketFrameHeader();
        FramePayloadCount = 0;
        FrameData = false;
        FrameControl = false;
        FrameExtension = false;
        FrameInterleavedControl = false;
        FrameHasMessageType = false;
        FrameMessageType = WebSocketMessageType.Text;
        FrameStartsMessage = false;
        FrameEndsMessage = false;
        FrameFragmentCount = 0;
        if (endsMessage)
        {
            MessageActive = false;
            ActiveMessageType = WebSocketMessageType.Text;
            ActiveFragmentCount = 0;
        }
    }

    WebSocketInboundMessageTrackResult LatchProtocolFailure(
        string identifier,
        string diagnostic)
    {
        return LatchFailure(
            WebSocketParseStatus.ProtocolFailure,
            WebSocketErrorKind.Protocol,
            WebSocketCloseCodes.ProtocolError,
            WebSocketCloseCode.ProtocolError,
            identifier,
            diagnostic);
    }

    WebSocketInboundMessageTrackResult LatchLimitFailure(
        string identifier,
        string diagnostic)
    {
        return LatchFailure(
            WebSocketParseStatus.LimitRejected,
            WebSocketErrorKind.Limit,
            WebSocketCloseCodes.MessageTooBig,
            WebSocketCloseCode.MessageTooBig,
            identifier,
            diagnostic);
    }

    WebSocketInboundMessageTrackResult LatchFailure(
        WebSocketParseStatus status,
        WebSocketErrorKind kind,
        int closeCode,
        WebSocketCloseCode knownCloseCode,
        string identifier,
        string diagnostic)
    {
        Failed = true;
        FailureStatus = status;
        FailureError = new WebSocketError();
        FailureError.Kind = kind;
        FailureError.HasCloseCode = true;
        FailureError.CloseCode = closeCode;
        FailureError.HasKnownCloseCode = true;
        FailureError.KnownCloseCode = knownCloseCode;
        FailureError.Identifier = identifier;
        FailureError.Diagnostic = diagnostic;
        return CreateLatchedFailure();
    }

    WebSocketInboundMessageTrackResult CreateLatchedFailure()
    {
        WebSocketInboundMessageTrackResult result =
            new WebSocketInboundMessageTrackResult();
        result.Status = FailureStatus;
        result.Accepted = false;
        result.HasError = true;
        result.Error = CopyError(FailureError);
        return result;
    }

    static bool ValidateChunk(WebSocketInboundFrameChunk chunk)
    {
        if (chunk == null ||
            chunk.Header == null ||
            chunk.Payload == null ||
            chunk.Header.PayloadLength < 0 ||
            chunk.PayloadOffset < 0 ||
            chunk.PayloadOffset > chunk.Payload.Length ||
            chunk.PayloadCount < 0 ||
            chunk.PayloadCount > chunk.Payload.Length - chunk.PayloadOffset)
        {
            return false;
        }

        return chunk.EndOfFrame || chunk.PayloadCount > 0;
    }

    static bool HeadersMatch(
        WebSocketFrameHeader first,
        WebSocketFrameHeader second)
    {
        return first.Final == second.Final &&
            first.Rsv1 == second.Rsv1 &&
            first.Rsv2 == second.Rsv2 &&
            first.Rsv3 == second.Rsv3 &&
            first.Opcode == second.Opcode &&
            first.Masked == second.Masked &&
            first.MaskingKey == second.MaskingKey &&
            first.PayloadLength == second.PayloadLength;
    }

    static WebSocketFrameHeader CopyHeader(WebSocketFrameHeader source)
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

    static WebSocketError CopyError(WebSocketError source)
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
}
