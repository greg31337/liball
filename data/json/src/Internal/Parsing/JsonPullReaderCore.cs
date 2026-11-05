namespace json;

internal class JsonPullReaderCore
{
    JsonParserCore parser;
    bool inputCompleted;
    bool streamCompleted;
    bool failed;
    bool hasStagedEvent;
    JsonReadStatus stagedStatus;
    JsonToken? stagedToken;
    JsonDiagnostic? stagedDiagnostic;
    JsonDuplicateDecision? stagedDuplicateDecision;
    JsonDiagnostic? failureDiagnostic;
    bool skipActive;
    int skipDepth;
    JsonValueCaptureBuilder captureBuilder;
    bool captureActive;
    JsonValue? capturedValue;

    internal JsonPullReaderCore(JsonReaderOptions? options)
    {
        parser = new JsonParserCore(options);
        JsonLimits normalizedLimits = JsonOptionNormalizer.NormalizeLimits(options?.Limits);
        bool captureSourceRanges = options != null && options.CaptureSourceRanges;
        captureBuilder = new JsonValueCaptureBuilder(
            captureSourceRanges,
            normalizedLimits.MaximumDomNodes);
        stagedStatus = JsonReadStatus.NeedMoreData;
    }

    internal JsonFeedResult FeedBytes(byte[] utf8)
    {
        if (inputCompleted)
        {
            throw new global::System.InvalidOperationException("Input cannot be supplied after final input.");
        }

        JsonFeedResult result = new JsonFeedResult();
        if (failed)
        {
            result.ReadStatus = JsonReadStatus.Error;
            result.Diagnostic = failureDiagnostic;
            return result;
        }

        long acceptedBefore = parser.GetAcceptedByteCount();
        long consumedBefore = parser.GetConsumedByteCount();
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => FeedAndStage(utf8));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        SetFeedCounts(result, acceptedBefore, consumedBefore);
        if (failure != null)
        {
            SetTerminalFailure(GetFailureDiagnostic(failure));
        }

        SetFeedState(result);
        return result;
    }

    internal JsonFeedResult CompleteReaderInput()
    {
        JsonFeedResult result = new JsonFeedResult();
        if (failed)
        {
            result.ReadStatus = JsonReadStatus.Error;
            result.Diagnostic = failureDiagnostic;
            return result;
        }

        inputCompleted = true;
        long acceptedBefore = parser.GetAcceptedByteCount();
        long consumedBefore = parser.GetConsumedByteCount();
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => StageNextEvent(true));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        SetFeedCounts(result, acceptedBefore, consumedBefore);
        if (failure != null)
        {
            SetTerminalFailure(GetFailureDiagnostic(failure));
        }

        SetFeedState(result);
        return result;
    }

    internal JsonTokenResult PeekNextToken()
    {
        RejectTokenReadDuringValueOperation();
        JsonTokenResult result = new JsonTokenResult();
        if (failed)
        {
            result.Status = JsonReadStatus.Error;
            result.Diagnostic = failureDiagnostic;
            return result;
        }

        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => StageNextEvent(inputCompleted));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            SetTerminalFailure(GetFailureDiagnostic(failure));
        }

        SetTokenState(result);
        return result;
    }

    internal JsonTokenResult ReadNextToken()
    {
        RejectTokenReadDuringValueOperation();
        JsonTokenResult result = new JsonTokenResult();
        if (failed)
        {
            result.Status = JsonReadStatus.Error;
            result.Diagnostic = failureDiagnostic;
            return result;
        }

        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => StageNextEvent(inputCompleted));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            SetTerminalFailure(GetFailureDiagnostic(failure));
        }

        SetTokenState(result);
        if (hasStagedEvent)
        {
            ClearStagedEvent();
        }

        return result;
    }

    internal JsonOperationResult SkipCurrentValue()
    {
        if (captureActive)
        {
            throw new global::System.InvalidOperationException("A partial value capture must complete before a value can be skipped.");
        }

        JsonOperationResult result = new JsonOperationResult();
        if (failed)
        {
            result.Success = false;
            result.BytesConsumed = parser.GetConsumedByteCount();
            result.Diagnostic = failureDiagnostic;
            return result;
        }

        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => ContinueValueSkip());
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            SetTerminalFailure(GetFailureDiagnostic(failure));
            skipActive = false;
            skipDepth = 0;
        }

        result.BytesConsumed = parser.GetConsumedByteCount();
        if (failed)
        {
            result.Success = false;
            result.Diagnostic = failureDiagnostic;
            return result;
        }

        if (skipActive)
        {
            result.Success = false;
            return result;
        }

        if (hasStagedEvent && stagedStatus == JsonReadStatus.Error)
        {
            result.Success = false;
            result.Diagnostic = stagedDiagnostic;
            ClearStagedEvent();
            return result;
        }

        result.Success = true;
        return result;
    }

    internal JsonValueResult CaptureCurrentValue()
    {
        if (skipActive)
        {
            throw new global::System.InvalidOperationException("A partial value skip must complete before a value can be captured.");
        }

        JsonValueResult result = new JsonValueResult();
        if (failed)
        {
            result.Success = false;
            result.Status = JsonReadStatus.Error;
            result.Diagnostic = failureDiagnostic;
            return result;
        }

        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => ContinueValueCapture());
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            JsonDiagnostic? knownFailure = GetKnownFailureDiagnostic(failure);
            if (knownFailure == null)
            {
                captureActive = false;
                capturedValue = null;
                captureBuilder.ResetCaptureBuilder();
                throw failure;
            }

            SetTerminalFailure(knownFailure);
        }

        if (failed)
        {
            result.Success = false;
            result.Status = JsonReadStatus.Error;
            result.Diagnostic = failureDiagnostic;
            return result;
        }

        if (captureActive)
        {
            result.Success = false;
            result.Status = JsonReadStatus.NeedMoreData;
            return result;
        }

        if (hasStagedEvent && stagedStatus == JsonReadStatus.Error)
        {
            result.Success = false;
            result.Status = JsonReadStatus.Error;
            result.Diagnostic = stagedDiagnostic;
            ClearStagedEvent();
            captureBuilder.ResetCaptureBuilder();
            return result;
        }

        result.Success = true;
        result.Status = JsonReadStatus.TokenAvailable;
        result.Value = capturedValue;
        capturedValue = null;
        captureBuilder.ResetCaptureBuilder();
        return result;
    }

    internal long GetAcceptedByteCount()
    {
        return parser.GetAcceptedByteCount();
    }

    internal long GetConsumedByteCount()
    {
        return parser.GetConsumedByteCount();
    }

    internal void ResetPullReader()
    {
        parser.ResetParser();
        inputCompleted = false;
        streamCompleted = false;
        failed = false;
        failureDiagnostic = null;
        skipActive = false;
        skipDepth = 0;
        captureActive = false;
        capturedValue = null;
        captureBuilder.ResetCaptureBuilder();
        ClearStagedEvent();
    }

    void FeedAndStage(byte[] utf8)
    {
        parser.FeedBytes(utf8);
        StageNextEvent(false);
    }

    void StageNextEvent(bool finalInput)
    {
        if (hasStagedEvent || streamCompleted)
        {
            return;
        }

        JsonReadStatus status = parser.ReadNextEvent(finalInput);
        if (status == JsonReadStatus.NeedMoreData)
        {
            return;
        }

        if (status == JsonReadStatus.StreamComplete)
        {
            streamCompleted = true;
            return;
        }

        hasStagedEvent = true;
        stagedStatus = status;
        stagedToken = parser.GetCurrentToken();
        stagedDiagnostic = parser.GetCurrentDiagnostic();
        stagedDuplicateDecision = parser.GetCurrentDuplicateDecision();
    }

    void ContinueValueSkip()
    {
        if (!skipActive)
        {
            StageNextEvent(inputCompleted);
            if (!hasStagedEvent)
            {
                if (streamCompleted)
                {
                    throw new global::System.InvalidOperationException("There is no JSON value to skip.");
                }

                skipActive = true;
                skipDepth = 0;
                return;
            }

            if (stagedStatus == JsonReadStatus.Error)
            {
                return;
            }

            if (stagedStatus != JsonReadStatus.TokenAvailable || stagedToken == null)
            {
                throw new global::System.InvalidOperationException("The current reader event is not a JSON value.");
            }

            JsonTokenKind initialKind = stagedToken.Kind;
            if (IsScalarValue(initialKind))
            {
                ClearStagedEvent();
                return;
            }

            if (!IsContainerStart(initialKind))
            {
                throw new global::System.InvalidOperationException("The current token is not the start of a JSON value.");
            }

            skipActive = true;
            skipDepth = 1;
            ClearStagedEvent();
        }

        while (skipActive)
        {
            StageNextEvent(inputCompleted);
            if (!hasStagedEvent)
            {
                if (streamCompleted)
                {
                    throw new global::System.InvalidOperationException("A skipped container ended without a closing token.");
                }

                return;
            }

            if (stagedStatus == JsonReadStatus.Error)
            {
                skipActive = false;
                skipDepth = 0;
                return;
            }

            if (stagedStatus != JsonReadStatus.TokenAvailable || stagedToken == null)
            {
                throw new global::System.InvalidOperationException("A skipped container ended before its closing token.");
            }

            JsonTokenKind kind = stagedToken.Kind;
            if (skipDepth == 0)
            {
                if (IsScalarValue(kind))
                {
                    ClearStagedEvent();
                    skipActive = false;
                    return;
                }

                if (!IsContainerStart(kind))
                {
                    throw new global::System.InvalidOperationException("The current token is not the start of a JSON value.");
                }

                skipDepth = 1;
                ClearStagedEvent();
                continue;
            }

            if (IsContainerStart(kind))
            {
                skipDepth++;
            }

            if (IsContainerEnd(kind))
            {
                skipDepth--;
            }

            ClearStagedEvent();
            if (skipDepth == 0)
            {
                skipActive = false;
            }
        }
    }

    void ContinueValueCapture()
    {
        if (!captureActive)
        {
            captureBuilder.ResetCaptureBuilder();
            capturedValue = null;
            captureActive = true;
        }

        while (captureActive)
        {
            StageNextEvent(inputCompleted);
            if (!hasStagedEvent)
            {
                if (streamCompleted)
                {
                    throw new global::System.InvalidOperationException("There is no JSON value to capture.");
                }

                return;
            }

            if (stagedStatus == JsonReadStatus.Error)
            {
                captureActive = false;
                capturedValue = null;
                return;
            }

            if (stagedStatus != JsonReadStatus.TokenAvailable || stagedToken == null)
            {
                throw new global::System.InvalidOperationException("The current reader event is not a JSON value.");
            }

            bool complete = captureBuilder.AcceptCaptureToken(
                stagedToken,
                stagedDuplicateDecision);
            ClearStagedEvent();
            if (complete)
            {
                capturedValue = captureBuilder.GetCapturedValue();
                captureActive = false;
            }
        }
    }

    void SetFeedCounts(JsonFeedResult result, long acceptedBefore, long consumedBefore)
    {
        result.BytesAccepted = (int)(parser.GetAcceptedByteCount() - acceptedBefore);
        result.BytesConsumed = (int)(parser.GetConsumedByteCount() - consumedBefore);
    }

    void SetFeedState(JsonFeedResult result)
    {
        if (failed)
        {
            result.ReadStatus = JsonReadStatus.Error;
            result.Diagnostic = failureDiagnostic;
            return;
        }

        if (hasStagedEvent)
        {
            result.ReadStatus = stagedStatus;
            result.Diagnostic = stagedDiagnostic;
            return;
        }

        result.ReadStatus = streamCompleted
            ? JsonReadStatus.StreamComplete
            : JsonReadStatus.NeedMoreData;
    }

    void SetTokenState(JsonTokenResult result)
    {
        if (failed)
        {
            result.Status = JsonReadStatus.Error;
            result.Diagnostic = failureDiagnostic;
            return;
        }

        if (hasStagedEvent)
        {
            result.Status = stagedStatus;
            result.Token = stagedToken;
            result.Diagnostic = stagedDiagnostic;
            return;
        }

        result.Status = streamCompleted
            ? JsonReadStatus.StreamComplete
            : JsonReadStatus.NeedMoreData;
    }

    void SetTerminalFailure(JsonDiagnostic diagnostic)
    {
        failed = true;
        failureDiagnostic = diagnostic;
        skipActive = false;
        skipDepth = 0;
        captureActive = false;
        capturedValue = null;
        captureBuilder.ResetCaptureBuilder();
        ClearStagedEvent();
    }

    void RejectTokenReadDuringValueOperation()
    {
        if (skipActive)
        {
            throw new global::System.InvalidOperationException("A partial value skip must complete before tokens can be read.");
        }

        if (captureActive)
        {
            throw new global::System.InvalidOperationException("A partial value capture must complete before tokens can be read.");
        }
    }

    bool IsScalarValue(JsonTokenKind kind)
    {
        return kind == JsonTokenKind.String ||
            kind == JsonTokenKind.Number ||
            kind == JsonTokenKind.Boolean ||
            kind == JsonTokenKind.Null;
    }

    bool IsContainerStart(JsonTokenKind kind)
    {
        return kind == JsonTokenKind.StartArray || kind == JsonTokenKind.StartObject;
    }

    bool IsContainerEnd(JsonTokenKind kind)
    {
        return kind == JsonTokenKind.EndArray || kind == JsonTokenKind.EndObject;
    }

    JsonDiagnostic GetFailureDiagnostic(global::System.Exception failure)
    {
        JsonDiagnostic? diagnostic = GetKnownFailureDiagnostic(failure);
        if (diagnostic != null)
        {
            return diagnostic;
        }

        throw failure;
    }

    JsonDiagnostic? GetKnownFailureDiagnostic(global::System.Exception failure)
    {
        JsonParseException? parseFailure = failure as JsonParseException;
        if (parseFailure != null)
        {
            return parseFailure.Diagnostic;
        }

        JsonLimitException? limitFailure = failure as JsonLimitException;
        if (limitFailure != null)
        {
            return limitFailure.Diagnostic;
        }

        return null;
    }

    void ClearStagedEvent()
    {
        hasStagedEvent = false;
        stagedStatus = JsonReadStatus.NeedMoreData;
        stagedToken = null;
        stagedDiagnostic = null;
        stagedDuplicateDecision = null;
    }
}
