namespace json;

internal class JsonDocumentParsingRunner
{
    JsonReaderOptions? options;
    JsonParserCore? parser;
    JsonDomBuilder? builder;
    JsonDocumentResult? result;
    bool limitFailure;

    internal JsonDocumentParsingRunner(JsonReaderOptions? options)
    {
        this.options = options;
    }

    internal void ExecuteUtf8Document(byte[] utf8)
    {
        InitializeDocumentParsing();
        JsonParserCore activeParser = parser
            ?? throw new global::System.InvalidOperationException("A parser is required.");
        activeParser.FeedBytes(utf8);
        DrainDocumentEvents(true);
    }

    internal void ExecuteStringDocument(string text)
    {
        byte[] utf8 = JsonUtf16Input.EncodeJsonStringInput(text);
        ExecuteUtf8Document(utf8);
    }

    internal void ExecuteStreamDocument(global::System.IO.Stream stream)
    {
        JsonReaderOptions normalized = InitializeDocumentParsing();
        JsonLimits limits = normalized.Limits
            ?? throw new global::System.InvalidOperationException("Normalized limits are required.");
        int bufferBytes = normalized.StreamBufferBytes;
        if (bufferBytes > limits.MaximumInputBufferBytes)
        {
            bufferBytes = limits.MaximumInputBufferBytes;
        }

        JsonStreamInput input = new JsonStreamInput(stream, bufferBytes);
        JsonParserCore activeParser = parser
            ?? throw new global::System.InvalidOperationException("A parser is required.");
        while (result == null)
        {
            JsonStreamReadResult read = input.ReadStreamBlock();
            if (read.Diagnostic != null)
            {
                SetDocumentFailure(read.Diagnostic, false);
                return;
            }

            if (read.EndOfStream)
            {
                DrainDocumentEvents(true);
                return;
            }

            byte[] bytes = read.Bytes
                ?? throw new global::System.InvalidOperationException("A stream read requires bytes.");
            activeParser.FeedBytes(bytes);
            DrainDocumentEvents(false);
        }
    }

    internal JsonDocumentResult GetDocumentResult()
    {
        return result
            ?? throw new global::System.InvalidOperationException("Document parsing did not produce a result.");
    }

    internal void RecordDocumentFailure(global::System.Exception failure)
    {
        JsonParseException? parseFailure = failure as JsonParseException;
        if (parseFailure != null)
        {
            SetDocumentFailure(parseFailure.Diagnostic, false);
            return;
        }

        JsonLimitException? resourceFailure = failure as JsonLimitException;
        if (resourceFailure != null)
        {
            SetDocumentFailure(resourceFailure.Diagnostic, true);
            return;
        }

        throw failure;
    }

    internal bool WasDocumentLimitFailure()
    {
        return limitFailure;
    }

    JsonReaderOptions InitializeDocumentParsing()
    {
        if (parser != null || builder != null || result != null)
        {
            throw new global::System.InvalidOperationException("The document parsing runner cannot be reused.");
        }

        JsonReaderOptions normalized = JsonOptionNormalizer.NormalizeReaderOptions(options);
        parser = new JsonParserCore(normalized);
        builder = new JsonDomBuilder(normalized);
        return normalized;
    }

    void DrainDocumentEvents(bool finalInput)
    {
        JsonParserCore activeParser = parser
            ?? throw new global::System.InvalidOperationException("A parser is required.");
        JsonDomBuilder activeBuilder = builder
            ?? throw new global::System.InvalidOperationException("A DOM builder is required.");
        while (result == null)
        {
            JsonReadStatus status = activeParser.ReadNextEvent(finalInput);
            if (status == JsonReadStatus.NeedMoreData)
            {
                return;
            }

            if (status == JsonReadStatus.TokenAvailable)
            {
                JsonToken token = activeParser.GetCurrentToken()
                    ?? throw new global::System.InvalidOperationException("An available parser token is required.");
                if (activeBuilder.IsDomComplete())
                {
                    throw CreateAdditionalDocumentFailure(token);
                }

                activeBuilder.AcceptDomToken(
                    token,
                    activeParser.GetCurrentDuplicateDecision());
                continue;
            }

            if (status == JsonReadStatus.DocumentComplete)
            {
                continue;
            }

            if (status == JsonReadStatus.Error)
            {
                JsonDiagnostic diagnostic = activeParser.GetCurrentDiagnostic()
                    ?? throw new global::System.InvalidOperationException("A parser error requires a diagnostic.");
                SetDocumentFailure(diagnostic, false);
                return;
            }

            if (status == JsonReadStatus.StreamComplete)
            {
                if (!activeBuilder.IsDomComplete())
                {
                    throw CreateMissingDocumentFailure();
                }

                JsonDocumentResult success = new JsonDocumentResult();
                success.Success = true;
                success.Status = JsonReadStatus.StreamComplete;
                success.Document = activeBuilder.GetDomDocument(
                    activeParser.GetConsumedByteCount());
                result = success;
                return;
            }

            throw new global::System.InvalidOperationException("The parser returned an unsupported document status.");
        }
    }

    void SetDocumentFailure(JsonDiagnostic diagnostic, bool isLimitFailure)
    {
        JsonDocumentResult failure = new JsonDocumentResult();
        failure.Success = false;
        failure.Status = JsonReadStatus.Error;
        failure.Diagnostic = diagnostic;
        result = failure;
        limitFailure = isLimitFailure;
    }

    JsonParseException CreateAdditionalDocumentFailure(JsonToken token)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.TrailingContent;
        diagnostic.Message = "Document parsing requires exactly one framed JSON value.";
        diagnostic.Range = token.Range;
        diagnostic.Pointer = token.Pointer?.OriginalRepresentation;
        diagnostic.Depth = token.Depth;
        diagnostic.ActualToken = token.Kind.ToString();
        return new JsonParseException(diagnostic);
    }

    JsonParseException CreateMissingDocumentFailure()
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.UnexpectedEndOfInput;
        diagnostic.Message = "Document parsing requires one JSON value.";
        return new JsonParseException(diagnostic);
    }
}
