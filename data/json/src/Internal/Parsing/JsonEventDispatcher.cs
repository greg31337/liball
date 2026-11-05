namespace json;

internal class JsonEventDispatcher
{
    JsonParserCore parser;
    IJsonTokenHandler handler;
    bool inputCompleted;
    bool paused;
    bool stopped;
    bool streamCompleted;
    bool failed;
    bool handlerInvocationActive;
    JsonReadStatus currentStatus;
    JsonDiagnostic? currentDiagnostic;
    JsonDiagnostic? failureDiagnostic;

    internal JsonEventDispatcher(JsonReaderOptions? options, IJsonTokenHandler handler)
    {
        if (handler == null)
        {
            throw new global::System.ArgumentNullException("handler");
        }

        parser = new JsonParserCore(options);
        this.handler = handler;
        currentStatus = JsonReadStatus.NeedMoreData;
    }

    internal JsonFeedResult FeedDispatchBytes(byte[] utf8)
    {
        if (inputCompleted)
        {
            throw new global::System.InvalidOperationException("Input cannot be supplied after final input.");
        }

        JsonFeedResult result = new JsonFeedResult();
        if (stopped || failed)
        {
            SetResultState(result);
            return result;
        }

        long acceptedBefore = parser.GetAcceptedByteCount();
        long consumedBefore = parser.GetConsumedByteCount();
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => FeedAndDispatch(utf8));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        SetResultCounts(result, acceptedBefore, consumedBefore);
        if (failure != null)
        {
            HandleOperationFailure(failure);
        }

        SetResultState(result);
        return result;
    }

    internal JsonFeedResult CompleteDispatchInput()
    {
        JsonFeedResult result = new JsonFeedResult();
        if (stopped || failed || streamCompleted)
        {
            SetResultState(result);
            return result;
        }

        inputCompleted = true;
        if (paused)
        {
            SetResultState(result);
            return result;
        }

        long acceptedBefore = parser.GetAcceptedByteCount();
        long consumedBefore = parser.GetConsumedByteCount();
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => DispatchAvailableEvents(true));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        SetResultCounts(result, acceptedBefore, consumedBefore);
        if (failure != null)
        {
            HandleOperationFailure(failure);
        }

        SetResultState(result);
        return result;
    }

    internal JsonFeedResult ResumeDispatching()
    {
        JsonFeedResult result = new JsonFeedResult();
        if (stopped || failed || streamCompleted)
        {
            SetResultState(result);
            return result;
        }

        if (!paused)
        {
            throw new global::System.InvalidOperationException("The push parser is not paused.");
        }

        paused = false;
        long acceptedBefore = parser.GetAcceptedByteCount();
        long consumedBefore = parser.GetConsumedByteCount();
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => DispatchAvailableEvents(inputCompleted));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        SetResultCounts(result, acceptedBefore, consumedBefore);
        if (failure != null)
        {
            HandleOperationFailure(failure);
        }

        SetResultState(result);
        return result;
    }

    internal long GetDispatchAcceptedByteCount()
    {
        return parser.GetAcceptedByteCount();
    }

    internal long GetDispatchConsumedByteCount()
    {
        return parser.GetConsumedByteCount();
    }

    internal void ResetDispatcher()
    {
        parser.ResetParser();
        inputCompleted = false;
        paused = false;
        stopped = false;
        streamCompleted = false;
        failed = false;
        handlerInvocationActive = false;
        currentStatus = JsonReadStatus.NeedMoreData;
        currentDiagnostic = null;
        failureDiagnostic = null;
    }

    void FeedAndDispatch(byte[] utf8)
    {
        parser.FeedBytes(utf8);
        if (!paused)
        {
            DispatchAvailableEvents(false);
        }
    }

    void DispatchAvailableEvents(bool finalInput)
    {
        while (!paused && !stopped && !streamCompleted)
        {
            JsonReadStatus status = parser.ReadNextEvent(finalInput);
            if (status == JsonReadStatus.TokenAvailable)
            {
                JsonToken token = parser.GetCurrentToken()
                    ?? throw new global::System.InvalidOperationException("An available parser token is required.");
                DispatchToken(token);
                continue;
            }

            if (status == JsonReadStatus.DocumentComplete)
            {
                DispatchToken(CreateDocumentBoundaryToken());
                continue;
            }

            currentStatus = status;
            currentDiagnostic = parser.GetCurrentDiagnostic();
            if (status == JsonReadStatus.StreamComplete)
            {
                streamCompleted = true;
            }

            return;
        }
    }

    void DispatchToken(JsonToken token)
    {
        handlerInvocationActive = true;
        JsonHandlerAction action = handler.HandleToken(token);
        handlerInvocationActive = false;
        if (action == JsonHandlerAction.Continue)
        {
            currentStatus = JsonReadStatus.TokenAvailable;
            currentDiagnostic = null;
            return;
        }

        if (action == JsonHandlerAction.Pause)
        {
            paused = true;
            currentStatus = JsonReadStatus.Paused;
            currentDiagnostic = null;
            return;
        }

        if (action == JsonHandlerAction.Stop)
        {
            stopped = true;
            currentStatus = JsonReadStatus.Stopped;
            currentDiagnostic = null;
            return;
        }

        throw new global::System.InvalidOperationException("The token handler returned an undefined action.");
    }

    JsonToken CreateDocumentBoundaryToken()
    {
        JsonPointer pointer = new JsonPointer();
        pointer.Encoding = JsonPointerEncoding.String;
        pointer.OriginalRepresentation = "";
        pointer.ReferenceTokens = new global::System.Collections.Generic.List<string>();

        JsonToken token = new JsonToken();
        token.Kind = JsonTokenKind.DocumentBoundary;
        token.Depth = 0;
        token.Pointer = pointer;
        return token;
    }

    void HandleOperationFailure(global::System.Exception failure)
    {
        if (handlerInvocationActive)
        {
            handlerInvocationActive = false;
            throw failure;
        }

        JsonDiagnostic? diagnostic = GetKnownFailureDiagnostic(failure);
        if (diagnostic == null)
        {
            throw failure;
        }

        failed = true;
        paused = false;
        currentStatus = JsonReadStatus.Error;
        currentDiagnostic = diagnostic;
        failureDiagnostic = diagnostic;
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

    void SetResultCounts(JsonFeedResult result, long acceptedBefore, long consumedBefore)
    {
        result.BytesAccepted = (int)(parser.GetAcceptedByteCount() - acceptedBefore);
        result.BytesConsumed = (int)(parser.GetConsumedByteCount() - consumedBefore);
    }

    void SetResultState(JsonFeedResult result)
    {
        if (failed)
        {
            result.ReadStatus = JsonReadStatus.Error;
            result.Diagnostic = failureDiagnostic;
            return;
        }

        if (stopped)
        {
            result.ReadStatus = JsonReadStatus.Stopped;
            return;
        }

        if (paused)
        {
            result.ReadStatus = JsonReadStatus.Paused;
            return;
        }

        if (streamCompleted)
        {
            result.ReadStatus = JsonReadStatus.StreamComplete;
            return;
        }

        result.ReadStatus = currentStatus;
        result.Diagnostic = currentDiagnostic;
    }
}
