namespace json;

internal enum JsonFramingAction
{
    SkipByte = 0,
    StartDocument = 1,
    ContinueDocument = 2,
    CompleteDocument = 3,
    RecoverableError = 4
}

internal enum JsonFramingState
{
    AwaitDocument = 0,
    InDocument = 1,
    AfterDocument = 2,
    AwaitRecordSeparator = 3,
    AwaitRecordValue = 4,
    AwaitSequenceTerminator = 5,
    Recovering = 6
}

internal class JsonSequenceFramer
{
    JsonInputMode inputMode;
    JsonSequenceErrorPolicy errorPolicy;
    JsonFramingState state;
    bool separatorSeen;
    bool recordSawWhitespace;
    bool streamComplete;
    bool completeAfterError;
    int completedDocumentCount;
    JsonDiagnostic? currentDiagnostic;

    internal JsonSequenceFramer(JsonInputMode inputMode, JsonSequenceErrorPolicy errorPolicy)
    {
        if (inputMode != JsonInputMode.SingleValue &&
            inputMode != JsonInputMode.WhitespaceSeparated &&
            inputMode != JsonInputMode.JsonTextSequence)
        {
            throw CreateOptionsFailure("InputMode has an undefined value.");
        }

        if (errorPolicy != JsonSequenceErrorPolicy.Stop &&
            errorPolicy != JsonSequenceErrorPolicy.Resynchronize)
        {
            throw CreateOptionsFailure("SequenceErrorPolicy has an undefined value.");
        }

        if (errorPolicy == JsonSequenceErrorPolicy.Resynchronize && inputMode != JsonInputMode.JsonTextSequence)
        {
            throw CreateOptionsFailure("Resynchronize is valid only for JSON text sequence input.");
        }

        this.inputMode = inputMode;
        this.errorPolicy = errorPolicy;
        state = GetInitialState(inputMode);
    }

    internal JsonInputMode GetInputMode()
    {
        return inputMode;
    }

    internal JsonSequenceErrorPolicy GetErrorPolicy()
    {
        return errorPolicy;
    }

    internal JsonFramingState GetFramingState()
    {
        return state;
    }

    internal int GetCompletedDocumentCount()
    {
        return completedDocumentCount;
    }

    internal JsonDiagnostic? GetCurrentDiagnostic()
    {
        return currentDiagnostic;
    }

    internal bool IsDocumentActive()
    {
        return state == JsonFramingState.InDocument || state == JsonFramingState.AwaitSequenceTerminator;
    }

    internal JsonFramingAction ProcessByte(byte value)
    {
        if (streamComplete || completeAfterError)
        {
            throw CreateStateFailure("Input cannot continue after final completion.");
        }

        currentDiagnostic = null;
        if (state == JsonFramingState.Recovering)
        {
            if (value == JsonSyntaxBytes.RecordSeparator)
            {
                state = JsonFramingState.AwaitRecordValue;
                recordSawWhitespace = false;
            }

            return JsonFramingAction.SkipByte;
        }

        if (state == JsonFramingState.AwaitRecordSeparator)
        {
            if (value != JsonSyntaxBytes.RecordSeparator)
            {
                throw CreateFramingFailure(
                    JsonDiagnosticCode.MissingRecordSeparator,
                    "An RFC 7464 sequence record must begin with a record separator.");
            }

            state = JsonFramingState.AwaitRecordValue;
            recordSawWhitespace = false;
            return JsonFramingAction.SkipByte;
        }

        if (state == JsonFramingState.AwaitRecordValue)
        {
            return ProcessRecordStartByte(value);
        }

        if (state == JsonFramingState.AwaitSequenceTerminator)
        {
            return ProcessSequenceTerminatorByte(value);
        }

        if (state == JsonFramingState.InDocument)
        {
            if (inputMode == JsonInputMode.JsonTextSequence && value == JsonSyntaxBytes.RecordSeparator)
            {
                return HandleDetectedSequenceFailure(
                    JsonDiagnosticCode.IncompleteSequenceRecord,
                    "The RFC 7464 record ended before its JSON value was complete.",
                    JsonFramingState.AwaitRecordValue);
            }

            return JsonFramingAction.ContinueDocument;
        }

        if (state == JsonFramingState.AfterDocument)
        {
            return ProcessAfterDocumentByte(value);
        }

        if (IsJsonWhitespace(value))
        {
            return JsonFramingAction.SkipByte;
        }

        state = JsonFramingState.InDocument;
        return JsonFramingAction.StartDocument;
    }

    internal bool CompleteDocument(bool selfDelimited)
    {
        if (state != JsonFramingState.InDocument)
        {
            throw CreateStateFailure("There is no active JSON document to complete.");
        }

        currentDiagnostic = null;
        if (inputMode == JsonInputMode.JsonTextSequence && !selfDelimited)
        {
            state = JsonFramingState.AwaitSequenceTerminator;
            return false;
        }

        completedDocumentCount++;
        state = JsonFramingState.AfterDocument;
        separatorSeen = false;
        return true;
    }

    internal bool HandleDocumentError(JsonDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new global::System.ArgumentNullException("diagnostic");
        }

        if (state != JsonFramingState.InDocument && state != JsonFramingState.AwaitSequenceTerminator)
        {
            throw CreateStateFailure("There is no active JSON document failure to handle.");
        }

        if (inputMode != JsonInputMode.JsonTextSequence || errorPolicy != JsonSequenceErrorPolicy.Resynchronize)
        {
            return false;
        }

        currentDiagnostic = diagnostic;
        state = JsonFramingState.Recovering;
        return true;
    }

    internal JsonReadStatus CompleteInput()
    {
        currentDiagnostic = null;
        if (streamComplete)
        {
            return JsonReadStatus.StreamComplete;
        }

        if (completeAfterError)
        {
            completeAfterError = false;
            streamComplete = true;
            return JsonReadStatus.StreamComplete;
        }

        if (state == JsonFramingState.InDocument)
        {
            if (inputMode == JsonInputMode.JsonTextSequence)
            {
                return HandleFinalSequenceFailure(
                    JsonDiagnosticCode.IncompleteSequenceRecord,
                    "Input ended before the RFC 7464 record's JSON value was complete.");
            }

            throw CreateFramingFailure(
                JsonDiagnosticCode.UnexpectedEndOfInput,
                "Input ended before the JSON document was complete.");
        }

        if (state == JsonFramingState.AwaitSequenceTerminator)
        {
            return HandleFinalSequenceFailure(
                JsonDiagnosticCode.IncompleteSequenceRecord,
                "A non-self-delimited RFC 7464 value requires trailing JSON whitespace.");
        }

        if (state == JsonFramingState.AwaitRecordValue && recordSawWhitespace)
        {
            return HandleFinalSequenceFailure(
                JsonDiagnosticCode.EmptySequenceRecord,
                "The RFC 7464 record contains whitespace but no JSON value.");
        }

        if (state == JsonFramingState.AwaitDocument && inputMode == JsonInputMode.SingleValue)
        {
            throw CreateFramingFailure(
                JsonDiagnosticCode.UnexpectedEndOfInput,
                "Single-value input does not contain a JSON value.");
        }

        streamComplete = true;
        return JsonReadStatus.StreamComplete;
    }

    internal void ResetFramer()
    {
        state = GetInitialState(inputMode);
        separatorSeen = false;
        recordSawWhitespace = false;
        streamComplete = false;
        completeAfterError = false;
        completedDocumentCount = 0;
        currentDiagnostic = null;
    }

    JsonFramingAction ProcessRecordStartByte(byte value)
    {
        if (value == JsonSyntaxBytes.RecordSeparator)
        {
            if (recordSawWhitespace)
            {
                recordSawWhitespace = false;
                return HandleDetectedSequenceFailure(
                    JsonDiagnosticCode.EmptySequenceRecord,
                    "The RFC 7464 record contains whitespace but no JSON value.",
                    JsonFramingState.AwaitRecordValue);
            }

            return JsonFramingAction.SkipByte;
        }

        if (IsJsonWhitespace(value))
        {
            recordSawWhitespace = true;
            return JsonFramingAction.SkipByte;
        }

        recordSawWhitespace = false;
        state = JsonFramingState.InDocument;
        return JsonFramingAction.StartDocument;
    }

    JsonFramingAction ProcessSequenceTerminatorByte(byte value)
    {
        if (IsJsonWhitespace(value))
        {
            completedDocumentCount++;
            state = JsonFramingState.AfterDocument;
            separatorSeen = false;
            return JsonFramingAction.CompleteDocument;
        }

        JsonFramingState nextState = value == JsonSyntaxBytes.RecordSeparator
            ? JsonFramingState.AwaitRecordValue
            : JsonFramingState.Recovering;
        return HandleDetectedSequenceFailure(
            JsonDiagnosticCode.IncompleteSequenceRecord,
            "A non-self-delimited RFC 7464 value requires trailing JSON whitespace.",
            nextState);
    }

    JsonFramingAction ProcessAfterDocumentByte(byte value)
    {
        if (inputMode == JsonInputMode.SingleValue)
        {
            if (IsJsonWhitespace(value))
            {
                return JsonFramingAction.SkipByte;
            }

            throw CreateFramingFailure(
                JsonDiagnosticCode.TrailingContent,
                "Content follows the complete single JSON value.");
        }

        if (inputMode == JsonInputMode.WhitespaceSeparated)
        {
            if (IsJsonWhitespace(value))
            {
                separatorSeen = true;
                return JsonFramingAction.SkipByte;
            }

            if (!separatorSeen)
            {
                throw CreateFramingFailure(
                    JsonDiagnosticCode.UnexpectedToken,
                    "Whitespace is required between top-level JSON values.");
            }

            separatorSeen = false;
            state = JsonFramingState.InDocument;
            return JsonFramingAction.StartDocument;
        }

        if (IsJsonWhitespace(value))
        {
            return JsonFramingAction.SkipByte;
        }

        if (value == JsonSyntaxBytes.RecordSeparator)
        {
            state = JsonFramingState.AwaitRecordValue;
            recordSawWhitespace = false;
            return JsonFramingAction.SkipByte;
        }

        return HandleDetectedSequenceFailure(
            JsonDiagnosticCode.UnexpectedToken,
            "Non-whitespace content follows the complete RFC 7464 record value.",
            JsonFramingState.Recovering);
    }

    JsonFramingAction HandleDetectedSequenceFailure(
        JsonDiagnosticCode code,
        string message,
        JsonFramingState recoveryState)
    {
        if (errorPolicy == JsonSequenceErrorPolicy.Stop)
        {
            throw CreateFramingFailure(code, message);
        }

        currentDiagnostic = CreateDiagnostic(code, message);
        state = recoveryState;
        return JsonFramingAction.RecoverableError;
    }

    JsonReadStatus HandleFinalSequenceFailure(JsonDiagnosticCode code, string message)
    {
        if (errorPolicy == JsonSequenceErrorPolicy.Stop)
        {
            throw CreateFramingFailure(code, message);
        }

        currentDiagnostic = CreateDiagnostic(code, message);
        completeAfterError = true;
        return JsonReadStatus.Error;
    }

    static JsonFramingState GetInitialState(JsonInputMode inputMode)
    {
        if (inputMode == JsonInputMode.JsonTextSequence)
        {
            return JsonFramingState.AwaitRecordSeparator;
        }

        return JsonFramingState.AwaitDocument;
    }

    static bool IsJsonWhitespace(byte value)
    {
        return value == JsonSyntaxBytes.Space ||
            value == JsonSyntaxBytes.HorizontalTab ||
            value == JsonSyntaxBytes.LineFeed ||
            value == JsonSyntaxBytes.CarriageReturn;
    }

    static JsonParseException CreateOptionsFailure(string message)
    {
        return new JsonParseException(CreateDiagnostic(JsonDiagnosticCode.InvalidOptions, message));
    }

    static JsonParseException CreateFramingFailure(JsonDiagnosticCode code, string message)
    {
        return new JsonParseException(CreateDiagnostic(code, message));
    }

    static JsonDiagnostic CreateDiagnostic(JsonDiagnosticCode code, string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        return diagnostic;
    }

    static global::System.InvalidOperationException CreateStateFailure(string message)
    {
        return new global::System.InvalidOperationException(message);
    }
}
