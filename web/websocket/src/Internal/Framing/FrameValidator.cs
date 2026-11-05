namespace websocket;

internal class FrameValidationResult
{
    internal bool Valid;
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal WebSocketError Error = new WebSocketError();
}

internal class FrameExtensionClaims
{
    internal bool Rsv1;
    internal bool Rsv2;
    internal bool Rsv3;
    internal bool[] Opcodes = new bool[16];
}

internal static class FrameValidator
{
    internal static FrameValidationResult ValidateHeader(
        WebSocketFrameHeader header,
        WebSocketRole localRole,
        int maximumFramePayloadLength,
        WebSocketExtensionNegotiation[]? extensions)
    {
        if (localRole != WebSocketRole.Client && localRole != WebSocketRole.Server)
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.State,
                WebSocketCloseCodes.ProtocolError,
                "frame-role-invalid",
                "The local endpoint role is invalid.");
        }

        int opcode = (int)header.Opcode;
        if (opcode < WebSocketProtocolConstants.MinimumOpcode ||
            opcode > WebSocketProtocolConstants.MaximumOpcode)
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "frame-opcode-out-of-range",
                "The frame opcode is outside the four-bit wire range.");
        }

        if (header.PayloadLength < 0)
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "frame-payload-negative",
                "The frame payload length is negative.");
        }

        if (localRole == WebSocketRole.Server && !header.Masked)
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "frame-mask-required",
                "A frame received by a server must be masked.");
        }

        if (localRole == WebSocketRole.Client && header.Masked)
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "frame-mask-forbidden",
                "A frame received by a client must not be masked.");
        }

        FrameExtensionClaims claims = CollectClaims(extensions);
        if (header.Rsv1 && !claims.Rsv1)
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "frame-rsv1-unclaimed",
                "The frame uses RSV1 without a negotiated extension claim.");
        }

        if (header.Rsv2 && !claims.Rsv2)
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "frame-rsv2-unclaimed",
                "The frame uses RSV2 without a negotiated extension claim.");
        }

        if (header.Rsv3 && !claims.Rsv3)
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "frame-rsv3-unclaimed",
                "The frame uses RSV3 without a negotiated extension claim.");
        }

        bool reservedOpcode = (opcode >= 3 && opcode <= 7) || opcode >= 11;
        if (reservedOpcode && !claims.Opcodes[opcode])
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "frame-opcode-unclaimed",
                "The frame uses a reserved opcode without a negotiated extension claim.");
        }

        bool controlFrame = opcode >= 8;
        if (controlFrame && !header.Final)
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "frame-control-fragmented",
                "A control frame must set the FIN bit.");
        }

        if (controlFrame &&
            header.PayloadLength > WebSocketProtocolConstants.ControlPayloadLengthMaximum)
        {
            return CreateFailure(
                WebSocketParseStatus.ProtocolFailure,
                WebSocketErrorKind.Protocol,
                WebSocketCloseCodes.ProtocolError,
                "frame-control-too-large",
                "A control frame payload cannot exceed 125 bytes.");
        }

        if (maximumFramePayloadLength <= 0)
        {
            return CreateFailure(
                WebSocketParseStatus.LimitRejected,
                WebSocketErrorKind.State,
                WebSocketCloseCodes.MessageTooBig,
                "frame-limit-invalid",
                "The configured frame payload limit is invalid.");
        }

        if (header.PayloadLength > maximumFramePayloadLength)
        {
            return CreateFailure(
                WebSocketParseStatus.LimitRejected,
                WebSocketErrorKind.Limit,
                WebSocketCloseCodes.MessageTooBig,
                "frame-payload-too-large",
                "The frame payload exceeds the configured limit.");
        }

        FrameValidationResult result = new FrameValidationResult();
        result.Valid = true;
        result.Status = WebSocketParseStatus.Complete;
        return result;
    }

    internal static FrameExtensionClaims CollectClaims(WebSocketExtensionNegotiation[]? extensions)
    {
        FrameExtensionClaims claims = new FrameExtensionClaims();
        if (extensions == null)
        {
            return claims;
        }

        int extensionIndex = 0;
        while (extensionIndex < extensions.Length)
        {
            WebSocketExtensionNegotiation extension = extensions[extensionIndex];
            if (extension != null && extension.Status == WebSocketExtensionStatus.Negotiated)
            {
                if (extension.ClaimsRsv1)
                {
                    claims.Rsv1 = true;
                }

                if (extension.ClaimsRsv2)
                {
                    claims.Rsv2 = true;
                }

                if (extension.ClaimsRsv3)
                {
                    claims.Rsv3 = true;
                }

                if (extension.ClaimedOpcodes != null)
                {
                    int opcodeIndex = 0;
                    while (opcodeIndex < extension.ClaimedOpcodes.Length)
                    {
                        int claimedOpcode = (int)extension.ClaimedOpcodes[opcodeIndex];
                        if (claimedOpcode >= WebSocketProtocolConstants.MinimumOpcode &&
                            claimedOpcode <= WebSocketProtocolConstants.MaximumOpcode)
                        {
                            claims.Opcodes[claimedOpcode] = true;
                        }

                        opcodeIndex = opcodeIndex + 1;
                    }
                }
            }

            extensionIndex = extensionIndex + 1;
        }

        return claims;
    }

    internal static FrameValidationResult CreateFailure(
        WebSocketParseStatus status,
        WebSocketErrorKind kind,
        int closeCode,
        string identifier,
        string diagnostic)
    {
        FrameValidationResult result = new FrameValidationResult();
        result.Status = status;
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
