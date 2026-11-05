namespace json;

internal class JsonStreamValidationRunner
{
    global::System.IO.Stream stream;
    JsonReaderOptions? options;
    JsonParserCore? parser;
    JsonOperationResult? result;

    internal JsonStreamValidationRunner(
        global::System.IO.Stream stream,
        JsonReaderOptions? options)
    {
        this.stream = stream;
        this.options = options;
    }

    internal void ExecuteStreamValidation()
    {
        JsonReaderOptions normalized = JsonOptionNormalizer.NormalizeReaderOptions(options);
        JsonLimits limits = normalized.Limits
            ?? throw new global::System.InvalidOperationException("Normalized limits are required.");
        int bufferBytes = normalized.StreamBufferBytes;
        if (bufferBytes > limits.MaximumInputBufferBytes)
        {
            bufferBytes = limits.MaximumInputBufferBytes;
        }

        JsonStreamInput input = new JsonStreamInput(stream, bufferBytes);
        parser = new JsonParserCore(normalized);
        while (result == null)
        {
            JsonStreamReadResult read = input.ReadStreamBlock();
            if (read.Diagnostic != null)
            {
                result = CreateIoFailureResult(read.Diagnostic);
                return;
            }

            if (read.EndOfStream)
            {
                result = parser.ValidateCompleteInput();
                return;
            }

            byte[] bytes = read.Bytes
                ?? throw new global::System.InvalidOperationException("A stream read requires bytes.");
            parser.FeedBytes(bytes);
            DrainAvailableEvents();
        }
    }

    internal JsonOperationResult GetStreamValidationResult()
    {
        return result
            ?? throw new global::System.InvalidOperationException("Stream validation did not produce a result.");
    }

    internal JsonOperationResult CreateStreamFailureResult(
        global::System.Exception failure)
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
            throw failure;
        }

        JsonOperationResult failureResult = new JsonOperationResult();
        failureResult.Success = false;
        failureResult.Diagnostic = diagnostic;
        if (parser != null)
        {
            failureResult.BytesConsumed = parser.GetConsumedByteCount();
        }

        return failureResult;
    }

    void DrainAvailableEvents()
    {
        if (parser == null)
        {
            throw new global::System.InvalidOperationException("A parser is required.");
        }

        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(false);
            if (status == JsonReadStatus.NeedMoreData)
            {
                return;
            }

            if (status == JsonReadStatus.Error)
            {
                JsonOperationResult failureResult = new JsonOperationResult();
                failureResult.Success = false;
                failureResult.BytesConsumed = parser.GetConsumedByteCount();
                failureResult.Diagnostic = parser.GetCurrentDiagnostic();
                result = failureResult;
                return;
            }
        }
    }

    JsonOperationResult CreateIoFailureResult(JsonDiagnostic diagnostic)
    {
        JsonOperationResult failureResult = new JsonOperationResult();
        failureResult.Success = false;
        failureResult.Diagnostic = diagnostic;
        if (parser != null)
        {
            failureResult.BytesConsumed = parser.GetConsumedByteCount();
        }

        return failureResult;
    }
}
