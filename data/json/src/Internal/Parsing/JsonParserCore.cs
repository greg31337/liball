namespace json;

internal class JsonParserCore
{
    JsonLexer lexer;
    JsonByteQueue inputQueue;
    JsonContainerStack containerStack;
    JsonDuplicateTracker duplicateTracker;
    JsonSequenceFramer sequenceFramer;
    JsonPathTracker pathTracker;
    JsonDiagnosticFactory diagnosticFactory;
    byte[] singleByteBuffer = new byte[1];
    int maximumDocumentBytes;
    int maximumNestingDepth;
    int maximumArrayItems;
    int maximumObjectMembers;
    int documentByteCount;
    byte currentInputByte;
    bool finalInputSeen;
    bool pendingDocumentComplete;
    bool streamComplete;
    JsonToken? currentToken;
    JsonDiagnostic? currentDiagnostic;
    JsonDuplicateDecision? currentDuplicateDecision;

    internal JsonParserCore(JsonReaderOptions? options)
    {
        JsonReaderOptions normalized = JsonOptionNormalizer.NormalizeReaderOptions(options);
        JsonLimits limits = normalized.Limits ?? JsonOptionNormalizer.NormalizeLimits(null);
        lexer = new JsonLexer(normalized);
        inputQueue = new JsonByteQueue(limits.MaximumInputBufferBytes);
        containerStack = new JsonContainerStack(limits.MaximumNestingDepth);
        duplicateTracker = new JsonDuplicateTracker(normalized.DuplicateKeyPolicy);
        sequenceFramer = new JsonSequenceFramer(normalized.InputMode, normalized.SequenceErrorPolicy);
        pathTracker = new JsonPathTracker();
        diagnosticFactory = new JsonDiagnosticFactory(limits.MaximumDiagnosticExcerptBytes);
        maximumDocumentBytes = limits.MaximumDocumentBytes;
        maximumNestingDepth = limits.MaximumNestingDepth;
        maximumArrayItems = limits.MaximumArrayItems;
        maximumObjectMembers = limits.MaximumObjectMembers;
    }

    internal void FeedBytes(global::System.ReadOnlySpan<byte> bytes)
    {
        if (finalInputSeen || streamComplete)
        {
            throw new global::System.InvalidOperationException("Input cannot be supplied after final input.");
        }

        inputQueue.EnqueueBytes(bytes);
    }

    internal JsonReadStatus ReadNextEvent(bool finalInput)
    {
        ClearCurrentEvent();
        if (streamComplete)
        {
            return JsonReadStatus.StreamComplete;
        }

        if (finalInput)
        {
            finalInputSeen = true;
        }

        if (pendingDocumentComplete)
        {
            pendingDocumentComplete = false;
            ResetDocumentState();
            return JsonReadStatus.DocumentComplete;
        }

        while (true)
        {
            JsonReadStatus lexerStatus = lexer.ReadNextToken(false);
            if (lexerStatus == JsonReadStatus.TokenAvailable)
            {
                JsonReadStatus tokenStatus = ProcessLexerEvent();
                if (tokenStatus != JsonReadStatus.NeedMoreData)
                {
                    return tokenStatus;
                }

                continue;
            }

            if (inputQueue.GetBufferedCount() > 0)
            {
                JsonReadStatus inputStatus = ProcessNextInputByte();
                if (inputStatus != JsonReadStatus.NeedMoreData)
                {
                    return inputStatus;
                }

                continue;
            }

            if (!finalInputSeen)
            {
                return JsonReadStatus.NeedMoreData;
            }

            JsonReadStatus finalLexerStatus = ReadFinalLexerEvent();
            if (finalLexerStatus == JsonReadStatus.TokenAvailable)
            {
                JsonReadStatus finalTokenStatus = ProcessLexerEvent();
                if (finalTokenStatus != JsonReadStatus.NeedMoreData)
                {
                    return finalTokenStatus;
                }

                continue;
            }

            JsonReadStatus framingStatus = ReadFinalFramingEvent();
            if (framingStatus == JsonReadStatus.Error)
            {
                JsonDiagnostic framingDiagnostic = sequenceFramer.GetCurrentDiagnostic()
                    ?? throw new global::System.InvalidOperationException("A framing error requires a diagnostic.");
                currentDiagnostic = CopyFramingDiagnostic(framingDiagnostic, false, 0);
                ResetDocumentState();
                return JsonReadStatus.Error;
            }

            streamComplete = true;
            return JsonReadStatus.StreamComplete;
        }
    }

    internal JsonToken? GetCurrentToken()
    {
        return currentToken;
    }

    internal JsonDiagnostic? GetCurrentDiagnostic()
    {
        return currentDiagnostic;
    }

    internal JsonDuplicateDecision? GetCurrentDuplicateDecision()
    {
        return currentDuplicateDecision;
    }

    internal long GetAcceptedByteCount()
    {
        return inputQueue.GetAcceptedByteCount();
    }

    internal long GetConsumedByteCount()
    {
        return inputQueue.GetConsumedByteCount();
    }

    internal int GetCurrentDocumentByteCount()
    {
        return documentByteCount;
    }

    internal int GetCompletedDocumentCount()
    {
        return sequenceFramer.GetCompletedDocumentCount();
    }

    internal JsonOperationResult ValidateCompleteInput()
    {
        JsonOperationResult result = new JsonOperationResult();
        while (true)
        {
            JsonReadStatus status = ReadNextEvent(true);
            if (status == JsonReadStatus.Error)
            {
                result.Success = false;
                result.BytesConsumed = GetConsumedByteCount();
                result.Diagnostic = currentDiagnostic;
                return result;
            }

            if (status == JsonReadStatus.StreamComplete)
            {
                result.Success = true;
                result.BytesConsumed = GetConsumedByteCount();
                return result;
            }
        }
    }

    internal void ResetParser()
    {
        lexer.ResetLexer();
        inputQueue.ResetQueue();
        containerStack.ResetStack();
        duplicateTracker.ResetDuplicates();
        sequenceFramer.ResetFramer();
        pathTracker.ResetPath();
        documentByteCount = 0;
        currentInputByte = 0;
        finalInputSeen = false;
        pendingDocumentComplete = false;
        streamComplete = false;
        ClearCurrentEvent();
    }

    JsonReadStatus ProcessNextInputByte()
    {
        byte value = inputQueue.GetByteAt(0);
        JsonFramingAction action = sequenceFramer.ProcessByte(value);
        if (action == JsonFramingAction.RecoverableError)
        {
            JsonDiagnostic framingDiagnostic = sequenceFramer.GetCurrentDiagnostic()
                ?? throw new global::System.InvalidOperationException("A recoverable framing error requires a diagnostic.");
            currentDiagnostic = CopyFramingDiagnostic(framingDiagnostic, true, value);
            lexer.ResetTokenForRecovery();
            if (lexer.GetBufferedByteCount() == 1)
            {
                lexer.ConsumeBufferedFramingByte();
            }
            else
            {
                lexer.ConsumeFramingByte(value);
            }

            inputQueue.ConsumeBytes(1);
            ResetDocumentState();
            return JsonReadStatus.Error;
        }

        if (action == JsonFramingAction.SkipByte)
        {
            lexer.ConsumeFramingByte(value);
            inputQueue.ConsumeBytes(1);
            return JsonReadStatus.NeedMoreData;
        }

        if (action == JsonFramingAction.CompleteDocument)
        {
            lexer.ConsumeFramingByte(value);
            inputQueue.ConsumeBytes(1);
            ResetDocumentState();
            return JsonReadStatus.DocumentComplete;
        }

        EnsureDocumentByteCapacity(value);
        currentInputByte = value;
        singleByteBuffer[0] = value;
        lexer.FeedBytes(singleByteBuffer);
        documentByteCount++;
        inputQueue.ConsumeBytes(1);
        return JsonReadStatus.NeedMoreData;
    }

    JsonReadStatus ProcessLexerEvent()
    {
        JsonToken? token = lexer.GetCurrentToken();
        if (token == null)
        {
            byte syntaxByte = lexer.GetCurrentSyntaxByte();
            if (syntaxByte == JsonSyntaxBytes.NameSeparator)
            {
                containerStack.AcceptNameSeparator();
                return JsonReadStatus.NeedMoreData;
            }

            if (syntaxByte == JsonSyntaxBytes.ValueSeparator)
            {
                containerStack.AcceptValueSeparator();
                return JsonReadStatus.NeedMoreData;
            }

            throw new global::System.InvalidOperationException("The lexer produced an unknown structural event.");
        }

        currentToken = token;
        if (token.Kind == JsonTokenKind.StartArray)
        {
            return ProcessArrayStart(token);
        }

        if (token.Kind == JsonTokenKind.StartObject)
        {
            return ProcessObjectStart(token);
        }

        if (token.Kind == JsonTokenKind.EndArray)
        {
            return ProcessArrayEnd(token);
        }

        if (token.Kind == JsonTokenKind.EndObject)
        {
            return ProcessObjectEnd(token);
        }

        if (token.Kind == JsonTokenKind.String && containerStack.GetCurrentFrame().CanAcceptPropertyName())
        {
            return ProcessPropertyName(token);
        }

        return ProcessScalarValue(token);
    }

    JsonReadStatus ProcessArrayStart(JsonToken token)
    {
        EnsureCollectionValueCapacity(token);
        EnsureNestingCapacity(token);
        int tokenDepth = pathTracker.GetDepth();
        containerStack.OpenArray();
        JsonPointer pointer = pathTracker.OpenArray();
        SetTokenContext(token, pointer, tokenDepth);
        return JsonReadStatus.TokenAvailable;
    }

    JsonReadStatus ProcessObjectStart(JsonToken token)
    {
        EnsureCollectionValueCapacity(token);
        EnsureNestingCapacity(token);
        int tokenDepth = pathTracker.GetDepth();
        containerStack.OpenObject();
        JsonPointer pointer = pathTracker.OpenObject();
        duplicateTracker.EnterObject();
        SetTokenContext(token, pointer, tokenDepth);
        return JsonReadStatus.TokenAvailable;
    }

    JsonReadStatus ProcessArrayEnd(JsonToken token)
    {
        int tokenDepth = pathTracker.GetDepth() - 1;
        containerStack.CloseArray();
        JsonPointer pointer = pathTracker.CloseArray();
        SetTokenContext(token, pointer, tokenDepth);
        if (containerStack.IsDocumentComplete())
        {
            return FinishDocumentToken(true);
        }

        return JsonReadStatus.TokenAvailable;
    }

    JsonReadStatus ProcessObjectEnd(JsonToken token)
    {
        int tokenDepth = pathTracker.GetDepth() - 1;
        containerStack.CloseObject();
        duplicateTracker.ExitObject();
        JsonPointer pointer = pathTracker.CloseObject();
        SetTokenContext(token, pointer, tokenDepth);
        if (containerStack.IsDocumentComplete())
        {
            return FinishDocumentToken(true);
        }

        return JsonReadStatus.TokenAvailable;
    }

    JsonReadStatus ProcessPropertyName(JsonToken token)
    {
        EnsureObjectMemberCapacity(token);
        string name = token.StringValue
            ?? throw new global::System.InvalidOperationException("A property-name token requires a decoded name.");
        containerStack.AcceptPropertyName();
        JsonPointer pointer = pathTracker.AcceptPropertyName(name);
        token.Kind = JsonTokenKind.PropertyName;
        SetTokenContext(token, pointer, pathTracker.GetDepth());
        if (duplicateTracker.GetPolicy() == JsonDuplicateKeyPolicy.Reject &&
            duplicateTracker.ContainsPropertyName(name))
        {
            throw CreateDuplicatePropertyFailure(token, name);
        }

        currentDuplicateDecision = duplicateTracker.RegisterPropertyName(name);
        return JsonReadStatus.TokenAvailable;
    }

    JsonReadStatus ReadFinalLexerEvent()
    {
        JsonReadStatus status = JsonReadStatus.NeedMoreData;
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => status = lexer.ReadNextToken(true));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            EnrichFailureDiagnostic(failure);
            throw failure;
        }

        return status;
    }

    JsonReadStatus ReadFinalFramingEvent()
    {
        JsonReadStatus status = JsonReadStatus.NeedMoreData;
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => status = sequenceFramer.CompleteInput());
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            EnrichFailureDiagnostic(failure);
            throw failure;
        }

        return status;
    }

    void EnrichFailureDiagnostic(global::System.Exception failure)
    {
        JsonDiagnostic? diagnostic = null;
        JsonParseException? parseFailure = failure as JsonParseException;
        if (parseFailure != null)
        {
            diagnostic = parseFailure.Diagnostic;
        }

        JsonLimitException? limitFailure = failure as JsonLimitException;
        if (limitFailure != null)
        {
            diagnostic = limitFailure.Diagnostic;
        }

        if (diagnostic == null)
        {
            return;
        }

        if (diagnostic.Range == null)
        {
            diagnostic.Range = lexer.GetCurrentFailureRange();
        }

        if (diagnostic.Pointer == null)
        {
            diagnostic.Pointer = pathTracker.GetCurrentPointer().OriginalRepresentation;
        }

        diagnostic.Depth = containerStack.GetContainerDepth();
    }

    JsonParseException CreateDuplicatePropertyFailure(JsonToken token, string name)
    {
        JsonSourcePosition? start = token.Range?.Start;
        long sourceOffset = start == null ? 0 : start.ByteOffset;
        JsonDiagnostic diagnostic = diagnosticFactory.CreateDiagnostic(
            JsonDiagnosticCode.DuplicatePropertyName,
            "The object contains a duplicate decoded property name.",
            token.Range,
            token.Pointer?.OriginalRepresentation,
            token.Depth,
            "unique property name",
            name,
            global::System.ReadOnlySpan<byte>.Empty,
            sourceOffset);
        return new JsonParseException(diagnostic);
    }

    JsonReadStatus ProcessScalarValue(JsonToken token)
    {
        EnsureCollectionValueCapacity(token);
        int tokenDepth = pathTracker.GetDepth();
        containerStack.AcceptScalarValue();
        JsonPointer pointer = pathTracker.AcceptScalarValue();
        SetTokenContext(token, pointer, tokenDepth);
        if (containerStack.IsDocumentComplete())
        {
            bool selfDelimited = token.Kind == JsonTokenKind.String;
            return FinishDocumentToken(selfDelimited);
        }

        return JsonReadStatus.TokenAvailable;
    }

    JsonReadStatus FinishDocumentToken(bool selfDelimited)
    {
        bool boundaryReady = sequenceFramer.CompleteDocument(selfDelimited);
        if (lexer.GetBufferedByteCount() == 1)
        {
            JsonFramingAction replayAction = sequenceFramer.ProcessByte(currentInputByte);
            if (replayAction == JsonFramingAction.RecoverableError)
            {
                JsonDiagnostic framingDiagnostic = sequenceFramer.GetCurrentDiagnostic()
                    ?? throw new global::System.InvalidOperationException("A recoverable framing error requires a diagnostic.");
                currentDiagnostic = CopyFramingDiagnostic(framingDiagnostic, true, currentInputByte);
                currentToken = null;
                lexer.ResetTokenForRecovery();
                lexer.ConsumeBufferedFramingByte();
                ResetDocumentState();
                return JsonReadStatus.Error;
            }

            JsonReadStatus delimiterStatus = lexer.ReadNextToken(false);
            if (delimiterStatus != JsonReadStatus.NeedMoreData)
            {
                throw new global::System.InvalidOperationException("A root scalar delimiter must be JSON whitespace.");
            }

            if (replayAction == JsonFramingAction.CompleteDocument)
            {
                boundaryReady = true;
            }
        }

        if (boundaryReady)
        {
            pendingDocumentComplete = true;
        }

        return JsonReadStatus.TokenAvailable;
    }

    void SetTokenContext(JsonToken token, JsonPointer pointer, int depth)
    {
        token.Pointer = pointer;
        token.Depth = depth;
    }

    void EnsureDocumentByteCapacity(byte value)
    {
        if (documentByteCount < maximumDocumentBytes)
        {
            return;
        }

        JsonSourceRange range = CreateCurrentPointRange();
        throw CreateLimitFailure(
            JsonDiagnosticCode.DocumentByteLimitExceeded,
            "The configured JSON document byte limit was exceeded.",
            range,
            "document end",
            FormatByte(value),
            value);
    }

    void EnsureCollectionValueCapacity(JsonToken token)
    {
        JsonContainerFrame frame = containerStack.GetCurrentFrame();
        if (frame.GetContainerKind() != JsonContainerKind.Array || frame.GetValueCount() < maximumArrayItems)
        {
            return;
        }

        throw CreateLimitFailure(
            JsonDiagnosticCode.ArrayItemLimitExceeded,
            "The configured JSON array item limit was exceeded.",
            token.Range,
            "array end",
            token.Kind.ToString(),
            currentInputByte);
    }

    void EnsureObjectMemberCapacity(JsonToken token)
    {
        JsonContainerFrame frame = containerStack.GetCurrentFrame();
        if (frame.GetMemberCount() < maximumObjectMembers)
        {
            return;
        }

        throw CreateLimitFailure(
            JsonDiagnosticCode.ObjectMemberLimitExceeded,
            "The configured JSON object member limit was exceeded.",
            token.Range,
            "object end",
            token.StringValue,
            currentInputByte);
    }

    void EnsureNestingCapacity(JsonToken token)
    {
        if (containerStack.GetContainerDepth() < maximumNestingDepth)
        {
            return;
        }

        throw CreateLimitFailure(
            JsonDiagnosticCode.NestingDepthLimitExceeded,
            "The configured JSON container nesting depth limit was exceeded.",
            token.Range,
            "scalar value",
            token.Kind.ToString(),
            currentInputByte);
    }

    JsonLimitException CreateLimitFailure(
        JsonDiagnosticCode code,
        string message,
        JsonSourceRange? range,
        string? expectedTokens,
        string? actualToken,
        byte sourceByte)
    {
        JsonSourcePosition position = lexer.GetCurrentPosition();
        long sourceOffset = position.ByteOffset == 0 ? 0 : position.ByteOffset - 1;
        singleByteBuffer[0] = sourceByte;
        JsonPointer pointer = pathTracker.GetCurrentPointer();
        JsonDiagnostic diagnostic = diagnosticFactory.CreateDiagnostic(
            code,
            message,
            range,
            pointer.OriginalRepresentation,
            containerStack.GetContainerDepth(),
            expectedTokens,
            actualToken,
            singleByteBuffer,
            sourceOffset);
        return new JsonLimitException(diagnostic);
    }

    JsonDiagnostic CopyFramingDiagnostic(JsonDiagnostic source, bool hasActualByte, byte actualByte)
    {
        JsonSourceRange range = CreateCurrentPointRange();
        global::System.ReadOnlySpan<byte> excerpt = global::System.ReadOnlySpan<byte>.Empty;
        string? actualToken = source.ActualToken;
        long sourceOffset = range.Start == null ? 0 : range.Start.ByteOffset;
        if (hasActualByte)
        {
            singleByteBuffer[0] = actualByte;
            excerpt = singleByteBuffer;
            actualToken = FormatByte(actualByte);
        }

        JsonPointer pointer = pathTracker.GetCurrentPointer();
        return diagnosticFactory.CreateDiagnostic(
            source.Code,
            source.Message ?? "JSON framing failed.",
            range,
            pointer.OriginalRepresentation,
            containerStack.GetContainerDepth(),
            source.ExpectedTokens,
            actualToken,
            excerpt,
            sourceOffset);
    }

    JsonSourceRange CreateCurrentPointRange()
    {
        JsonSourcePosition position = lexer.GetCurrentPosition();
        JsonSourceRange range = new JsonSourceRange();
        range.Start = position;
        range.End = position;
        return range;
    }

    void ResetDocumentState()
    {
        containerStack.ResetStack();
        duplicateTracker.ResetDuplicates();
        pathTracker.ResetPath();
        documentByteCount = 0;
        currentDuplicateDecision = null;
    }

    void ClearCurrentEvent()
    {
        currentToken = null;
        currentDiagnostic = null;
        currentDuplicateDecision = null;
    }

    static string FormatByte(byte value)
    {
        if (value >= 0x20 && value <= 0x7E)
        {
            return ((char)value).ToString();
        }

        return "0x" + value.ToString("X2", global::System.Globalization.CultureInfo.InvariantCulture);
    }
}
