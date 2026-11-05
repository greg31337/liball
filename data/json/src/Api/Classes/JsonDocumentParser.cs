namespace json;

/// <summary>Parses UTF-8 JSON into an owned document model.</summary>
public class JsonDocumentParser : IJsonDocumentParser
{
    /// <summary>Initializes a reusable JSON document parser.</summary>
    public JsonDocumentParser()
    {
    }

    /// <summary>Parses one complete UTF-8 input.</summary>
    /// <param name="utf8">The complete UTF-8 input.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The parsed document.</returns>
    public JsonDocument ParseUtf8(
        global::System.ReadOnlySpan<byte> utf8,
        JsonReaderOptions options)
    {
        byte[] ownedInput = new byte[utf8.Length];
        utf8.CopyTo(ownedInput);
        JsonDocumentParsingRunner runner = RunUtf8DocumentParsing(
            ownedInput,
            options);
        return GetThrowingDocument(runner);
    }

    /// <summary>Attempts to parse one complete UTF-8 input without throwing for invalid JSON.</summary>
    /// <param name="utf8">The complete UTF-8 input.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The document parse outcome.</returns>
    public JsonDocumentResult TryParseUtf8(
        global::System.ReadOnlySpan<byte> utf8,
        JsonReaderOptions options)
    {
        byte[] ownedInput = new byte[utf8.Length];
        utf8.CopyTo(ownedInput);
        return RunUtf8DocumentParsing(ownedInput, options).GetDocumentResult();
    }

    /// <summary>Parses one complete UTF-16 string input.</summary>
    /// <param name="text">The complete UTF-16 input.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The parsed document.</returns>
    public JsonDocument ParseString(string text, JsonReaderOptions options)
    {
        JsonDocumentParsingRunner runner = RunStringDocumentParsing(text, options);
        return GetThrowingDocument(runner);
    }

    /// <summary>Attempts to parse one complete UTF-16 string input without throwing for invalid JSON.</summary>
    /// <param name="text">The complete UTF-16 input.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The document parse outcome.</returns>
    public JsonDocumentResult TryParseString(string text, JsonReaderOptions options)
    {
        return RunStringDocumentParsing(text, options).GetDocumentResult();
    }

    /// <summary>Parses JSON from a caller-owned stream.</summary>
    /// <param name="stream">The caller-owned input stream.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The parsed document.</returns>
    public JsonDocument ParseStream(
        global::System.IO.Stream stream,
        JsonReaderOptions options)
    {
        JsonDocumentParsingRunner runner = RunStreamDocumentParsing(stream, options);
        return GetThrowingDocument(runner);
    }

    /// <summary>Attempts to parse JSON from a caller-owned stream without throwing for invalid JSON.</summary>
    /// <param name="stream">The caller-owned input stream.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The document parse outcome.</returns>
    public JsonDocumentResult TryParseStream(
        global::System.IO.Stream stream,
        JsonReaderOptions options)
    {
        return RunStreamDocumentParsing(stream, options).GetDocumentResult();
    }

    JsonDocumentParsingRunner RunUtf8DocumentParsing(
        byte[] utf8,
        JsonReaderOptions options)
    {
        JsonDocumentParsingRunner runner = new JsonDocumentParsingRunner(options);
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => runner.ExecuteUtf8Document(utf8));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            runner.RecordDocumentFailure(failure);
        }

        return runner;
    }

    JsonDocumentParsingRunner RunStringDocumentParsing(
        string text,
        JsonReaderOptions options)
    {
        JsonDocumentParsingRunner runner = new JsonDocumentParsingRunner(options);
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => runner.ExecuteStringDocument(text));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            runner.RecordDocumentFailure(failure);
        }

        return runner;
    }

    JsonDocumentParsingRunner RunStreamDocumentParsing(
        global::System.IO.Stream stream,
        JsonReaderOptions options)
    {
        JsonDocumentParsingRunner runner = new JsonDocumentParsingRunner(options);
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => runner.ExecuteStreamDocument(stream));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            runner.RecordDocumentFailure(failure);
        }

        return runner;
    }

    JsonDocument GetThrowingDocument(JsonDocumentParsingRunner runner)
    {
        JsonDocumentResult result = runner.GetDocumentResult();
        if (result.Success && result.Document != null)
        {
            return result.Document;
        }

        JsonDiagnostic diagnostic = result.Diagnostic
            ?? throw new global::System.InvalidOperationException("A failed document parse requires a diagnostic.");
        if (runner.WasDocumentLimitFailure())
        {
            throw new JsonLimitException(diagnostic);
        }

        throw new JsonParseException(diagnostic);
    }
}
