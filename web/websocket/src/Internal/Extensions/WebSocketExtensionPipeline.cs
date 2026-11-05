using System;

namespace websocket;

internal class WebSocketExtensionPipeline
{
    internal IWebSocketExtension[] Extensions;
    internal WebSocketExtensionNegotiation[] Negotiations;
    internal bool OutgoingMessageActive;
    internal bool IncomingMessageActive;
    internal WebSocketMessageType OutgoingMessageType;
    internal WebSocketMessageType IncomingMessageType;
    internal bool OutgoingPastFirstFragment;
    internal bool IncomingPastFirstFragment;
    internal bool OutgoingFinalFragmentStarted;
    internal bool IncomingFinalFragmentStarted;
    internal WebSocketExtensionContext? OutgoingPendingContext;
    internal WebSocketExtensionContext? IncomingPendingContext;
    internal byte[][] OutgoingPendingInputs;
    internal byte[][] IncomingPendingInputs;
    internal int OutgoingResumeStageCount;
    internal int IncomingResumeStageCount;
    internal bool Failed;
    internal bool Released;
    internal WebSocketParseStatus FailureStatus = WebSocketParseStatus.ProtocolFailure;
    internal WebSocketError FailureError = new WebSocketError();

    internal WebSocketExtensionPipeline(
        IWebSocketExtension[] extensions,
        WebSocketExtensionNegotiation[] negotiations)
    {
        if (extensions == null ||
            negotiations == null ||
            extensions.Length != negotiations.Length ||
            !WebSocketExtensionClaimValidator.ValidateClaims(negotiations))
        {
            throw new WebSocketStateException();
        }

        Extensions = new IWebSocketExtension[extensions.Length];
        int extensionIndex = 0;
        while (extensionIndex < extensions.Length)
        {
            if (extensions[extensionIndex] == null ||
                negotiations[extensionIndex].Status !=
                    WebSocketExtensionStatus.Negotiated)
            {
                throw new WebSocketStateException();
            }

            int priorIndex = 0;
            while (priorIndex < extensionIndex)
            {
                if (object.ReferenceEquals(
                    extensions[priorIndex],
                    extensions[extensionIndex]))
                {
                    throw new WebSocketStateException();
                }

                priorIndex = priorIndex + 1;
            }

            Extensions[extensionIndex] = extensions[extensionIndex];
            extensionIndex = extensionIndex + 1;
        }

        Negotiations = WebSocketFrameCodec.CopyExtensions(negotiations);
        OutgoingPendingInputs = CreatePendingInputs(extensions.Length);
        IncomingPendingInputs = CreatePendingInputs(extensions.Length);
    }

    internal WebSocketTransformResult TransformOutgoing(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        if (Released)
        {
            return CreateReleasedFailure();
        }

        if (Failed)
        {
            return CopyLatchedFailure();
        }

        if (context == null)
        {
            return LatchFailure(CreateFailure(
                "extension-transform-context-invalid",
                "The extension transform context is required."));
        }

        WebSocketExtensionContext transformContext = CopyContext(context, true);
        if (!ValidateContextClaims(transformContext))
        {
            return LatchFailure(CreateClaimFailure());
        }

        if (!ValidateOutgoingLifecycle(transformContext))
        {
            return LatchFailure(CreateStateFailure());
        }

        WebSocketTransformResult result = TransformStages(
            transformContext,
            source,
            true);
        if (result.HasError)
        {
            return LatchFailure(result);
        }

        if (result.Status == WebSocketParseStatus.Complete)
        {
            CompleteOutgoingLifecycle(transformContext);
        }

        return result;
    }

    internal WebSocketTransformResult TransformIncoming(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        if (Released)
        {
            return CreateReleasedFailure();
        }

        if (Failed)
        {
            return CopyLatchedFailure();
        }

        if (context == null)
        {
            return LatchFailure(CreateFailure(
                "extension-transform-context-invalid",
                "The extension transform context is required."));
        }

        WebSocketExtensionContext transformContext = CopyContext(context, false);
        if (!ValidateContextClaims(transformContext))
        {
            return LatchFailure(CreateClaimFailure());
        }

        if (!ValidateIncomingLifecycle(transformContext))
        {
            return LatchFailure(CreateStateFailure());
        }

        WebSocketTransformResult result = TransformStages(
            transformContext,
            source,
            false);
        if (result.HasError)
        {
            return LatchFailure(result);
        }

        if (result.Status == WebSocketParseStatus.Complete)
        {
            CompleteIncomingLifecycle(transformContext);
        }

        return result;
    }

    internal void ResetPipeline()
    {
        if (Released)
        {
            return;
        }

        ResetExtensions();
        ClearPipelineState();
    }

    internal void ReleasePipeline()
    {
        if (Released)
        {
            return;
        }

        ResetExtensions();
        ClearPipelineState();
        Extensions = [];
        Negotiations = [];
        OutgoingPendingInputs = [];
        IncomingPendingInputs = [];
        Released = true;
    }

    void ResetExtensions()
    {
        int extensionIndex = 0;
        while (extensionIndex < Extensions.Length)
        {
            Extensions[extensionIndex].ResetExtensionState();
            extensionIndex = extensionIndex + 1;
        }
    }

    void ClearPipelineState()
    {
        OutgoingMessageActive = false;
        IncomingMessageActive = false;
        OutgoingPastFirstFragment = false;
        IncomingPastFirstFragment = false;
        OutgoingFinalFragmentStarted = false;
        IncomingFinalFragmentStarted = false;
        OutgoingPendingContext = null;
        IncomingPendingContext = null;
        ClearPendingInputs(OutgoingPendingInputs);
        ClearPendingInputs(IncomingPendingInputs);
        OutgoingResumeStageCount = 0;
        IncomingResumeStageCount = 0;
        Failed = false;
        FailureStatus = WebSocketParseStatus.ProtocolFailure;
        FailureError = new WebSocketError();
    }

    WebSocketTransformResult TransformStages(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source,
        bool outgoing)
    {
        if (context.MaximumBufferedOutputBytes <= 0)
        {
            return CreateFailure(
                "extension-transform-limit-invalid",
                "The extension transform output limit must be positive.");
        }

        if (Extensions.Length == 0 &&
            source.Length > context.MaximumBufferedOutputBytes)
        {
            return CreateLimitFailure();
        }

        ReadOnlySpan<byte> current = source;
        bool setRsv1 = false;
        bool setRsv2 = false;
        bool setRsv3 = false;
        WebSocketParseStatus pipelineStatus = WebSocketParseStatus.Complete;
        int consumedSourceCount = source.Length;
        int completedStages = IncomingResumeStageCount;
        if (outgoing)
        {
            completedStages = OutgoingResumeStageCount;
        }

        if (source.Length != 0)
        {
            completedStages = 0;
        }

        int resumeStageCount = Extensions.Length;
        int stageIndex = Extensions.Length - 1 - completedStages;
        int step = -1;
        if (outgoing)
        {
            stageIndex = completedStages;
            step = 1;
        }

        while (completedStages < Extensions.Length)
        {
            byte[] pendingInput;
            if (outgoing)
            {
                pendingInput = OutgoingPendingInputs[stageIndex];
                OutgoingPendingInputs[stageIndex] = Array.Empty<byte>();
            }
            else
            {
                pendingInput = IncomingPendingInputs[stageIndex];
                IncomingPendingInputs[stageIndex] = Array.Empty<byte>();
            }

            if (pendingInput.Length != 0)
            {
                if (pendingInput.Length >
                    context.MaximumBufferedOutputBytes - current.Length)
                {
                    return CreateLimitFailure();
                }

                current = CombineInput(pendingInput, current);
            }

            WebSocketExtensionNegotiation negotiation = Negotiations[stageIndex];
            WebSocketExtensionContext stageContext = CreateStageContext(
                context,
                outgoing,
                negotiation);
            WebSocketTransformResult stageResult;
            if (outgoing)
            {
                stageResult = Extensions[stageIndex].TransformOutboundChunk(
                    stageContext,
                    current);
            }
            else
            {
                stageResult = Extensions[stageIndex].TransformInboundChunk(
                    stageContext,
                    current);
            }

            if (stageResult == null)
            {
                return CreateFailure(
                    "extension-transform-result-invalid",
                    "An extension returned an inconsistent transform result.");
            }

            if (stageResult.ProducedCount > context.MaximumBufferedOutputBytes)
            {
                return CreateLimitFailure();
            }

            if (!ValidateStageResult(stageResult, current.Length))
            {
                return CreateFailure(
                    "extension-transform-result-invalid",
                    "An extension returned an inconsistent transform result.");
            }

            if ((stageResult.SetRsv1 && !negotiation.ClaimsRsv1) ||
                (stageResult.SetRsv2 && !negotiation.ClaimsRsv2) ||
                (stageResult.SetRsv3 && !negotiation.ClaimsRsv3))
            {
                return CreateFailure(
                    "extension-transform-rsv-unclaimed",
                    "An extension transform set an RSV bit it does not own.");
            }

            setRsv1 = setRsv1 || stageResult.SetRsv1;
            setRsv2 = setRsv2 || stageResult.SetRsv2;
            setRsv3 = setRsv3 || stageResult.SetRsv3;
            if (stageResult.HasError)
            {
                return MapExtensionFailure(stageResult);
            }

            if (stageResult.Status != WebSocketParseStatus.Complete)
            {
                if (completedStages < resumeStageCount)
                {
                    resumeStageCount = completedStages;
                }

                if (pipelineStatus == WebSocketParseStatus.Complete ||
                    stageResult.Status == WebSocketParseStatus.NeedOutputCapacity)
                {
                    pipelineStatus = stageResult.Status;
                }

                if (completedStages == 0)
                {
                    consumedSourceCount = stageResult.ConsumedCount;
                }
                else
                {
                    byte[] remaining = CopyRemainingInput(
                        current,
                        stageResult.ConsumedCount);
                    if (outgoing)
                    {
                        OutgoingPendingInputs[stageIndex] = remaining;
                    }
                    else
                    {
                        IncomingPendingInputs[stageIndex] = remaining;
                    }
                }
            }

            current = CopyOutput(stageResult);
            stageIndex = stageIndex + step;
            completedStages = completedStages + 1;
        }

        if (pipelineStatus == WebSocketParseStatus.Complete)
        {
            if (outgoing)
            {
                OutgoingResumeStageCount = 0;
            }
            else
            {
                IncomingResumeStageCount = 0;
            }
        }
        else if (outgoing)
        {
            OutgoingResumeStageCount = resumeStageCount;
        }
        else
        {
            IncomingResumeStageCount = resumeStageCount;
        }

        WebSocketTransformResult result = new WebSocketTransformResult();
        result.Status = pipelineStatus;
        result.ConsumedCount = consumedSourceCount;
        result.ProducedCount = current.Length;
        result.Output = current.ToArray();
        result.SetRsv1 = setRsv1;
        result.SetRsv2 = setRsv2;
        result.SetRsv3 = setRsv3;
        return result;
    }

    bool ValidateOutgoingLifecycle(WebSocketExtensionContext context)
    {
        if (!ValidateContext(context))
        {
            return false;
        }

        if (OutgoingPendingContext != null)
        {
            return ContextsMatch(OutgoingPendingContext, context);
        }

        if (!OutgoingMessageActive)
        {
            if (!context.FirstChunk || !context.FirstFragment)
            {
                return false;
            }

            OutgoingMessageActive = true;
            OutgoingMessageType = context.MessageType;
        }
        else if (context.FirstChunk || context.MessageType != OutgoingMessageType)
        {
            return false;
        }

        if ((OutgoingPastFirstFragment && context.FirstFragment) ||
            (OutgoingFinalFragmentStarted && !context.FinalFragment) ||
            (context.FinalChunk && !context.FinalFragment))
        {
            return false;
        }

        OutgoingPendingContext = CopyContext(context, true);
        return true;
    }

    bool ValidateIncomingLifecycle(WebSocketExtensionContext context)
    {
        if (!ValidateContext(context))
        {
            return false;
        }

        if (IncomingPendingContext != null)
        {
            return ContextsMatch(IncomingPendingContext, context);
        }

        if (!IncomingMessageActive)
        {
            if (!context.FirstChunk || !context.FirstFragment)
            {
                return false;
            }

            IncomingMessageActive = true;
            IncomingMessageType = context.MessageType;
        }
        else if (context.FirstChunk || context.MessageType != IncomingMessageType)
        {
            return false;
        }

        if ((IncomingPastFirstFragment && context.FirstFragment) ||
            (IncomingFinalFragmentStarted && !context.FinalFragment) ||
            (context.FinalChunk && !context.FinalFragment))
        {
            return false;
        }

        IncomingPendingContext = CopyContext(context, false);
        return true;
    }

    void CompleteOutgoingLifecycle(WebSocketExtensionContext context)
    {
        OutgoingPendingContext = null;
        if (!context.FirstFragment)
        {
            OutgoingPastFirstFragment = true;
        }

        if (context.FinalFragment)
        {
            OutgoingFinalFragmentStarted = true;
        }

        if (!context.FinalChunk)
        {
            return;
        }

        int extensionIndex = 0;
        while (extensionIndex < Extensions.Length)
        {
            Extensions[extensionIndex].CompleteExtensionMessage(
                CreateStageContext(
                    context,
                    true,
                    Negotiations[extensionIndex]));
            extensionIndex = extensionIndex + 1;
        }

        OutgoingMessageActive = false;
        OutgoingPastFirstFragment = false;
        OutgoingFinalFragmentStarted = false;
    }

    void CompleteIncomingLifecycle(WebSocketExtensionContext context)
    {
        IncomingPendingContext = null;
        if (!context.FirstFragment)
        {
            IncomingPastFirstFragment = true;
        }

        if (context.FinalFragment)
        {
            IncomingFinalFragmentStarted = true;
        }

        if (!context.FinalChunk)
        {
            return;
        }

        int extensionIndex = Extensions.Length - 1;
        while (extensionIndex >= 0)
        {
            Extensions[extensionIndex].CompleteExtensionMessage(
                CreateStageContext(
                    context,
                    false,
                    Negotiations[extensionIndex]));
            extensionIndex = extensionIndex - 1;
        }

        IncomingMessageActive = false;
        IncomingPastFirstFragment = false;
        IncomingFinalFragmentStarted = false;
    }

    WebSocketTransformResult LatchFailure(WebSocketTransformResult source)
    {
        Failed = true;
        FailureStatus = source.Status;
        FailureError = WebSocketFrameCodec.CopyError(source.Error);
        return CopyStageResult(
            source,
            source.SetRsv1,
            source.SetRsv2,
            source.SetRsv3);
    }

    WebSocketTransformResult CopyLatchedFailure()
    {
        WebSocketTransformResult result = new WebSocketTransformResult();
        result.Status = FailureStatus;
        result.HasError = true;
        result.Error = WebSocketFrameCodec.CopyError(FailureError);
        return result;
    }

    static bool ValidateContext(WebSocketExtensionContext context)
    {
        if ((context.MessageType != WebSocketMessageType.Text &&
                context.MessageType != WebSocketMessageType.Binary) ||
            context.MaximumBufferedOutputBytes <= 0)
        {
            return false;
        }

        if (!context.HasOpcode)
        {
            return true;
        }

        int opcode = (int)context.Opcode;
        return opcode >= 0 && opcode <= 15 &&
            (opcode < 8 || opcode > 10);
    }

    bool ValidateContextClaims(WebSocketExtensionContext context)
    {
        bool rsv1Owned = !context.Rsv1;
        bool rsv2Owned = !context.Rsv2;
        bool rsv3Owned = !context.Rsv3;
        bool opcodeOwned = !context.HasOpcode ||
            !IsReservedOpcode(context.Opcode);
        int extensionIndex = 0;
        while (extensionIndex < Negotiations.Length)
        {
            WebSocketExtensionNegotiation negotiation =
                Negotiations[extensionIndex];
            rsv1Owned = rsv1Owned || negotiation.ClaimsRsv1;
            rsv2Owned = rsv2Owned || negotiation.ClaimsRsv2;
            rsv3Owned = rsv3Owned || negotiation.ClaimsRsv3;
            if (context.HasOpcode &&
                NegotiationClaimsOpcode(negotiation, context.Opcode))
            {
                opcodeOwned = true;
            }

            extensionIndex = extensionIndex + 1;
        }

        return rsv1Owned && rsv2Owned && rsv3Owned && opcodeOwned;
    }

    static bool IsReservedOpcode(WebSocketOpcode opcode)
    {
        int value = (int)opcode;
        return (value >= 3 && value <= 7) ||
            (value >= 11 && value <= 15);
    }

    static bool NegotiationClaimsOpcode(
        WebSocketExtensionNegotiation negotiation,
        WebSocketOpcode opcode)
    {
        int opcodeIndex = 0;
        while (opcodeIndex < negotiation.ClaimedOpcodes.Length)
        {
            if (negotiation.ClaimedOpcodes[opcodeIndex] == opcode)
            {
                return true;
            }

            opcodeIndex = opcodeIndex + 1;
        }

        return false;
    }

    static WebSocketExtensionContext CreateStageContext(
        WebSocketExtensionContext source,
        bool outbound,
        WebSocketExtensionNegotiation negotiation)
    {
        WebSocketExtensionContext copy = CopyContext(source, outbound);
        copy.Rsv1 = source.Rsv1 && negotiation.ClaimsRsv1;
        copy.Rsv2 = source.Rsv2 && negotiation.ClaimsRsv2;
        copy.Rsv3 = source.Rsv3 && negotiation.ClaimsRsv3;
        if (source.HasOpcode &&
            IsReservedOpcode(source.Opcode) &&
            !NegotiationClaimsOpcode(negotiation, source.Opcode))
        {
            copy.HasOpcode = false;
            copy.Opcode = WebSocketOpcode.Continuation;
        }

        return copy;
    }

    static bool ContextsMatch(
        WebSocketExtensionContext expected,
        WebSocketExtensionContext actual)
    {
        return expected.Role == actual.Role &&
            expected.Outbound == actual.Outbound &&
            expected.RequestCompression == actual.RequestCompression &&
            expected.Rsv1 == actual.Rsv1 &&
            expected.Rsv2 == actual.Rsv2 &&
            expected.Rsv3 == actual.Rsv3 &&
            expected.HasOpcode == actual.HasOpcode &&
            expected.Opcode == actual.Opcode &&
            expected.MessageType == actual.MessageType &&
            expected.FirstChunk == actual.FirstChunk &&
            expected.FinalChunk == actual.FinalChunk &&
            expected.FirstFragment == actual.FirstFragment &&
            expected.FinalFragment == actual.FinalFragment &&
            expected.MaximumFramePayloadLength == actual.MaximumFramePayloadLength &&
            expected.MaximumMessagePayloadLength == actual.MaximumMessagePayloadLength &&
            expected.MaximumDecompressedMessagePayloadLength ==
                actual.MaximumDecompressedMessagePayloadLength;
    }

    static bool ValidateStageResult(
        WebSocketTransformResult result,
        int sourceLength)
    {
        if (result.Output == null ||
            result.Error == null ||
            result.ConsumedCount < 0 ||
            result.ConsumedCount > sourceLength ||
            result.ProducedCount < 0 ||
            result.ProducedCount > result.Output.Length)
        {
            return false;
        }

        if (result.Status == WebSocketParseStatus.Complete)
        {
            return result.ConsumedCount == sourceLength && !result.HasError;
        }

        if (result.Status == WebSocketParseStatus.NeedInput ||
            result.Status == WebSocketParseStatus.NeedOutputCapacity)
        {
            return !result.HasError;
        }

        if (result.Status == WebSocketParseStatus.LimitRejected ||
            result.Status == WebSocketParseStatus.ProtocolFailure)
        {
            return result.HasError;
        }

        return false;
    }

    static WebSocketTransformResult CopyStageResult(
        WebSocketTransformResult source,
        bool setRsv1,
        bool setRsv2,
        bool setRsv3)
    {
        WebSocketTransformResult copy = new WebSocketTransformResult();
        copy.Status = source.Status;
        copy.ConsumedCount = source.ConsumedCount;
        copy.Output = CopyOutput(source);
        copy.ProducedCount = copy.Output.Length;
        copy.SetRsv1 = setRsv1;
        copy.SetRsv2 = setRsv2;
        copy.SetRsv3 = setRsv3;
        copy.HasError = source.HasError;
        copy.Error = WebSocketFrameCodec.CopyError(source.Error);
        return copy;
    }

    static WebSocketTransformResult MapExtensionFailure(
        WebSocketTransformResult source)
    {
        WebSocketTransformResult result = new WebSocketTransformResult();
        result.ConsumedCount = source.ConsumedCount;
        result.HasError = true;
        result.Error.Identifier = source.Error.Identifier;
        result.Error.Diagnostic = source.Error.Diagnostic;
        if (result.Error.Identifier == null ||
            result.Error.Identifier.Length > 128 ||
            !AsciiToken.IsValid(result.Error.Identifier))
        {
            result.Error.Identifier = "extension-transform-failed";
        }

        if (result.Error.Diagnostic == null ||
            !IsSafeDiagnostic(result.Error.Diagnostic))
        {
            result.Error.Diagnostic =
                "A negotiated extension rejected the message transform.";
        }

        if (source.Status == WebSocketParseStatus.LimitRejected ||
            source.Error.Kind == WebSocketErrorKind.Limit)
        {
            result.Status = WebSocketParseStatus.LimitRejected;
            result.Error.Kind = WebSocketErrorKind.Limit;
            SetKnownCloseCode(result.Error, WebSocketCloseCodes.MessageTooBig);
        }
        else if (source.Error.Kind == WebSocketErrorKind.Utf8)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            result.Error.Kind = WebSocketErrorKind.Utf8;
            SetKnownCloseCode(
                result.Error,
                WebSocketCloseCodes.InvalidFramePayloadData);
        }
        else if (source.Error.Kind == WebSocketErrorKind.Protocol ||
            source.Error.Kind == WebSocketErrorKind.Compression)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            result.Error.Kind = source.Error.Kind;
            SetKnownCloseCode(result.Error, WebSocketCloseCodes.ProtocolError);
        }
        else
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            result.Error.Kind = WebSocketErrorKind.Compression;
            SetKnownCloseCode(result.Error, WebSocketCloseCodes.InternalError);
        }

        return result;
    }

    static bool IsSafeDiagnostic(string diagnostic)
    {
        if (diagnostic.Length == 0 || diagnostic.Length > 1024)
        {
            return false;
        }

        int index = 0;
        while (index < diagnostic.Length)
        {
            char value = diagnostic[index];
            if (value == '\r' || value == '\n' || value == '\0')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static void SetKnownCloseCode(WebSocketError error, int closeCode)
    {
        error.HasCloseCode = true;
        error.CloseCode = closeCode;
        error.HasKnownCloseCode = true;
        error.KnownCloseCode = (WebSocketCloseCode)closeCode;
    }

    static byte[] CopyOutput(WebSocketTransformResult source)
    {
        byte[] output = new byte[source.ProducedCount];
        int index = 0;
        while (index < output.Length)
        {
            output[index] = source.Output[index];
            index = index + 1;
        }

        return output;
    }

    static byte[][] CreatePendingInputs(int count)
    {
        byte[][] inputs = new byte[count][];
        int index = 0;
        while (index < inputs.Length)
        {
            inputs[index] = Array.Empty<byte>();
            index = index + 1;
        }

        return inputs;
    }

    static void ClearPendingInputs(byte[][] inputs)
    {
        int index = 0;
        while (index < inputs.Length)
        {
            inputs[index] = Array.Empty<byte>();
            index = index + 1;
        }
    }

    static byte[] CombineInput(
        byte[] pending,
        ReadOnlySpan<byte> current)
    {
        byte[] combined = new byte[pending.Length + current.Length];
        int index = 0;
        while (index < pending.Length)
        {
            combined[index] = pending[index];
            index = index + 1;
        }

        int currentIndex = 0;
        while (currentIndex < current.Length)
        {
            combined[index] = current[currentIndex];
            index = index + 1;
            currentIndex = currentIndex + 1;
        }

        return combined;
    }

    static byte[] CopyRemainingInput(
        ReadOnlySpan<byte> source,
        int consumedCount)
    {
        byte[] remaining = new byte[source.Length - consumedCount];
        int index = 0;
        while (index < remaining.Length)
        {
            remaining[index] = source[consumedCount + index];
            index = index + 1;
        }

        return remaining;
    }

    static WebSocketExtensionContext CopyContext(
        WebSocketExtensionContext source,
        bool outbound)
    {
        WebSocketExtensionContext copy = new WebSocketExtensionContext();
        copy.Role = source.Role;
        copy.Outbound = outbound;
        copy.RequestCompression = source.RequestCompression;
        copy.Rsv1 = source.Rsv1;
        copy.Rsv2 = source.Rsv2;
        copy.Rsv3 = source.Rsv3;
        copy.HasOpcode = source.HasOpcode;
        copy.Opcode = source.Opcode;
        copy.MessageType = source.MessageType;
        copy.FirstChunk = source.FirstChunk;
        copy.FinalChunk = source.FinalChunk;
        copy.FirstFragment = source.FirstFragment;
        copy.FinalFragment = source.FinalFragment;
        copy.MaximumFramePayloadLength = source.MaximumFramePayloadLength;
        copy.MaximumMessagePayloadLength = source.MaximumMessagePayloadLength;
        copy.MaximumDecompressedMessagePayloadLength =
            source.MaximumDecompressedMessagePayloadLength;
        copy.MaximumBufferedOutputBytes = source.MaximumBufferedOutputBytes;
        return copy;
    }

    static WebSocketTransformResult CreateFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketTransformResult result = new WebSocketTransformResult();
        result.Status = WebSocketParseStatus.ProtocolFailure;
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Compression;
        result.Error.HasCloseCode = true;
        result.Error.CloseCode = WebSocketCloseCodes.InternalError;
        result.Error.HasKnownCloseCode = true;
        result.Error.KnownCloseCode = WebSocketCloseCode.InternalError;
        result.Error.Identifier = identifier;
        result.Error.Diagnostic = diagnostic;
        return result;
    }

    static WebSocketTransformResult CreateReleasedFailure()
    {
        WebSocketTransformResult result = CreateFailure(
            "extension-pipeline-released",
            "The extension pipeline has released its per-connection state.");
        result.Error.Kind = WebSocketErrorKind.State;
        return result;
    }

    static WebSocketTransformResult CreateClaimFailure()
    {
        WebSocketTransformResult result = CreateStateFailure();
        result.Error.Identifier = "extension-transform-claim-unowned";
        result.Error.Diagnostic =
            "An extension transform claim is not owned by a negotiated extension.";
        return result;
    }

    static WebSocketTransformResult CreateStateFailure()
    {
        WebSocketTransformResult result = new WebSocketTransformResult();
        result.Status = WebSocketParseStatus.ProtocolFailure;
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Protocol;
        result.Error.HasCloseCode = true;
        result.Error.CloseCode = WebSocketCloseCodes.ProtocolError;
        result.Error.HasKnownCloseCode = true;
        result.Error.KnownCloseCode = WebSocketCloseCode.ProtocolError;
        result.Error.Identifier = "extension-message-state-invalid";
        result.Error.Diagnostic =
            "The extension message chunk or fragment lifecycle is invalid.";
        return result;
    }

    static WebSocketTransformResult CreateLimitFailure()
    {
        WebSocketTransformResult result = new WebSocketTransformResult();
        result.Status = WebSocketParseStatus.LimitRejected;
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Limit;
        result.Error.HasCloseCode = true;
        result.Error.CloseCode = WebSocketCloseCodes.MessageTooBig;
        result.Error.HasKnownCloseCode = true;
        result.Error.KnownCloseCode = WebSocketCloseCode.MessageTooBig;
        result.Error.Identifier = "extension-transform-output-too-large";
        result.Error.Diagnostic =
            "Extension transform output exceeds the configured buffer limit.";
        return result;
    }
}
