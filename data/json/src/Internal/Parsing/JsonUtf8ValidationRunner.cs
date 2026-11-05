namespace json;

internal class JsonUtf8ValidationRunner
{
    byte[] utf8;
    JsonReaderOptions? options;
    JsonParserCore? parser;
    JsonOperationResult? result;

    internal JsonUtf8ValidationRunner(byte[] utf8, JsonReaderOptions? options)
    {
        this.utf8 = utf8;
        this.options = options;
    }

    internal void ExecuteValidation()
    {
        parser = new JsonParserCore(options);
        parser.FeedBytes(utf8);
        result = parser.ValidateCompleteInput();
    }

    internal JsonOperationResult GetResult()
    {
        return result ?? throw new global::System.InvalidOperationException("Validation did not produce a result.");
    }

    internal JsonOperationResult CreateFailureResult(global::System.Exception failure)
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
}
