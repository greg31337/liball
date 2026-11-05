namespace json.Tests;

public class JsonLocationIntegrationTests
{
    [TestCase("\n", 2)]
    [TestCase("\r", 2)]
    [TestCase("\r\n", 3)]
    public void NewlineFormsStartTheNextTokenOnLineTwo(
        string newline,
        int expectedOffset)
    {
        List<JsonToken> tokens = ReadWholePull(
            System.Text.Encoding.UTF8.GetBytes("[" + newline + "0]"),
            CreateLocationOptions());
        JsonToken number = tokens.Single(token => token.Kind == JsonTokenKind.Number);

        AssertPosition(number.Range?.Start, expectedOffset, 2, 1, 1);
        AssertPosition(number.Range?.End, expectedOffset + 1, 2, 2, 2);
    }

    [Test]
    public void MultibyteAndEscapedStringsUseSourceByteAndScalarColumns()
    {
        List<JsonToken> tokens = ReadWholePull(
            System.Text.Encoding.UTF8.GetBytes("[\"é\",\"😀\",\"\\u20ac\\n\"]"),
            CreateLocationOptions());
        List<JsonToken> strings = tokens
            .Where(token => token.Kind == JsonTokenKind.String)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(strings.Select(token => token.StringValue),
                Is.EqualTo(new[] { "é", "😀", "€\n" }));
            Assert.That(strings.Select(token => token.Pointer?.OriginalRepresentation),
                Is.EqualTo(new[] { "/0", "/1", "/2" }));
        });
        AssertPosition(strings[0].Range?.Start, 1, 1, 2, 2);
        AssertPosition(strings[0].Range?.End, 5, 1, 6, 5);
        AssertPosition(strings[1].Range?.Start, 6, 1, 7, 6);
        AssertPosition(strings[1].Range?.End, 12, 1, 13, 9);
        AssertPosition(strings[2].Range?.Start, 13, 1, 14, 10);
        AssertPosition(strings[2].Range?.End, 23, 1, 24, 20);
    }

    [Test]
    public void LocationsMatchAcrossWholeChunkBytewisePushAndStreamInput()
    {
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(
            "{\r\n\"é\":[\"😀\",\"\\u20ac\\n\"]\n}");
        List<string> expected = ReadWholePull(utf8, CreateLocationOptions())
            .Select(CreateTokenSignature)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(ReadBytewisePull(utf8).Select(CreateTokenSignature),
                Is.EqualTo(expected));
            Assert.That(ReadBytewisePush(utf8).Select(CreateTokenSignature),
                Is.EqualTo(expected));
            Assert.That(ReadOneByteStream(utf8).Select(CreateTokenSignature),
                Is.EqualTo(expected));
        });
    }

    [Test]
    public void NestedPointersEscapeNamesAtTheTokenLocations()
    {
        List<JsonToken> tokens = ReadWholePull(
            System.Text.Encoding.UTF8.GetBytes("{\"a/b~\":[{\"é\":{\"x\":0}}]}"),
            CreateLocationOptions());
        JsonToken number = tokens.Single(token => token.Kind == JsonTokenKind.Number);
        JsonToken innerObjectEnd = tokens
            .Where(token => token.Kind == JsonTokenKind.EndObject)
            .First();

        Assert.Multiple(() =>
        {
            Assert.That(number.Pointer?.OriginalRepresentation,
                Is.EqualTo("/a~1b~0/0/é/x"));
            Assert.That(number.Depth, Is.EqualTo(4));
            Assert.That(innerObjectEnd.Pointer?.OriginalRepresentation,
                Is.EqualTo("/a~1b~0/0/é"));
            Assert.That(innerObjectEnd.Depth, Is.EqualTo(3));
        });
        AssertPosition(number.Range?.Start, 20, 1, 21, 20);
        AssertPosition(number.Range?.End, 21, 1, 22, 21);
    }

    [Test]
    public void DuplicateDecodedNameReportsItsNestedPointerAndNameRange()
    {
        string jsonText =
            "{\n\"outer\":[{\"a/b~\":{\"dup\":1,\n\"\\u0064up\":2}}]}";
        JsonReaderOptions options = CreateLocationOptions();
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocumentResult result = parser.TryParseString(jsonText, options);
        JsonDiagnostic diagnostic = result.Diagnostic
            ?? throw new InvalidOperationException("Expected duplicate diagnostic.");
        int duplicateOffset = jsonText.IndexOf("\"\\u0064up\"", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(diagnostic.Pointer, Is.EqualTo("/outer/0/a~1b~0/dup"));
            Assert.That(diagnostic.Depth, Is.EqualTo(4));
            Assert.That(diagnostic.ActualToken, Is.EqualTo("dup"));
        });
        AssertPosition(diagnostic.Range?.Start, duplicateOffset, 3, 1, 1);
        AssertPosition(diagnostic.Range?.End, duplicateOffset + 10, 3, 11, 11);
    }

    [Test]
    public void StructuralEndOfInputReportsThePendingValuePoint()
    {
        JsonReaderOptions options = CreateLocationOptions();
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocumentResult result = parser.TryParseString("{\r\n\"a\":", options);
        JsonDiagnostic diagnostic = result.Diagnostic
            ?? throw new InvalidOperationException("Expected end-of-input diagnostic.");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedEndOfInput));
            Assert.That(diagnostic.Pointer, Is.EqualTo("/a"));
            Assert.That(diagnostic.Depth, Is.EqualTo(1));
        });
        AssertPosition(diagnostic.Range?.Start, 7, 2, 5, 5);
        AssertPosition(diagnostic.Range?.End, 7, 2, 5, 5);
    }

    [Test]
    public void UnterminatedMultibyteStringReportsItsSourceSpanAtEndOfInput()
    {
        JsonReaderOptions options = CreateLocationOptions();
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocumentResult result = parser.TryParseString("[\"é", options);
        JsonDiagnostic diagnostic = result.Diagnostic
            ?? throw new InvalidOperationException("Expected end-of-input diagnostic.");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedEndOfInput));
            Assert.That(diagnostic.Pointer, Is.EqualTo("/0"));
            Assert.That(diagnostic.Depth, Is.EqualTo(1));
        });
        AssertPosition(diagnostic.Range?.Start, 1, 1, 2, 2);
        AssertPosition(diagnostic.Range?.End, 4, 1, 5, 4);
    }

    static JsonReaderOptions CreateLocationOptions()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        return options;
    }

    static List<JsonToken> ReadWholePull(byte[] utf8, JsonReaderOptions options)
    {
        IJsonPullReader reader = new JsonPullReader(options);
        Assert.That(reader.FeedUtf8(utf8).ReadStatus, Is.Not.EqualTo(JsonReadStatus.Error));
        Assert.That(reader.CompleteInput().ReadStatus, Is.Not.EqualTo(JsonReadStatus.Error));
        return DrainPullTokens(reader);
    }

    static List<JsonToken> ReadBytewisePull(byte[] utf8)
    {
        IJsonPullReader reader = new JsonPullReader(CreateLocationOptions());
        List<JsonToken> tokens = new List<JsonToken>();
        for (int index = 0; index < utf8.Length; index++)
        {
            JsonFeedResult feed = reader.FeedUtf8(
                new global::System.ReadOnlySpan<byte>(utf8, index, 1));
            Assert.That(feed.ReadStatus, Is.Not.EqualTo(JsonReadStatus.Error));
            DrainAvailablePullTokens(reader, tokens);
        }

        Assert.That(reader.CompleteInput().ReadStatus, Is.Not.EqualTo(JsonReadStatus.Error));
        DrainAvailablePullTokens(reader, tokens);
        return tokens;
    }

    static List<JsonToken> ReadBytewisePush(byte[] utf8)
    {
        LocationTokenHandler handler = new LocationTokenHandler();
        IJsonPushParser parser = new JsonPushParser(CreateLocationOptions(), handler);
        for (int index = 0; index < utf8.Length; index++)
        {
            JsonFeedResult feed = parser.FeedPushUtf8(
                new global::System.ReadOnlySpan<byte>(utf8, index, 1));
            Assert.That(feed.ReadStatus, Is.Not.EqualTo(JsonReadStatus.Error));
        }

        Assert.That(parser.CompletePushInput().ReadStatus,
            Is.EqualTo(JsonReadStatus.StreamComplete));
        return handler.Tokens
            .Where(token => token.Kind != JsonTokenKind.DocumentBoundary)
            .ToList();
    }

    static List<JsonToken> ReadOneByteStream(byte[] utf8)
    {
        JsonReaderOptions options = CreateLocationOptions();
        options.StreamBufferBytes = 1;
        IJsonPullReader reader = new JsonPullReader(options);
        using MemoryStream stream = new MemoryStream(utf8);

        Assert.That(reader.FeedStream(stream).ReadStatus,
            Is.Not.EqualTo(JsonReadStatus.Error));
        return DrainPullTokens(reader);
    }

    static List<JsonToken> DrainPullTokens(IJsonPullReader reader)
    {
        List<JsonToken> tokens = new List<JsonToken>();
        int attempts = 0;
        while (true)
        {
            attempts++;
            Assert.That(attempts, Is.LessThan(1000));
            JsonTokenResult read = reader.ReadToken();
            if (read.Status == JsonReadStatus.TokenAvailable)
            {
                tokens.Add(read.Token
                    ?? throw new InvalidOperationException("Expected token."));
                continue;
            }

            if (read.Status == JsonReadStatus.DocumentComplete)
            {
                continue;
            }

            if (read.Status == JsonReadStatus.StreamComplete)
            {
                return tokens;
            }

            Assert.Fail("Unexpected pull status: " + read.Status);
        }
    }

    static void DrainAvailablePullTokens(
        IJsonPullReader reader,
        List<JsonToken> tokens)
    {
        while (true)
        {
            JsonTokenResult read = reader.ReadToken();
            if (read.Status == JsonReadStatus.TokenAvailable)
            {
                tokens.Add(read.Token
                    ?? throw new InvalidOperationException("Expected token."));
                continue;
            }

            if (read.Status == JsonReadStatus.DocumentComplete)
            {
                continue;
            }

            if (read.Status == JsonReadStatus.NeedMoreData ||
                read.Status == JsonReadStatus.StreamComplete)
            {
                return;
            }

            Assert.Fail("Unexpected pull status: " + read.Status);
        }
    }

    static string CreateTokenSignature(JsonToken token)
    {
        return token.Kind + "|" +
            (token.Pointer?.OriginalRepresentation ?? "<null>") + "|" +
            token.Depth + "|" +
            FormatPosition(token.Range?.Start) + "|" +
            FormatPosition(token.Range?.End);
    }

    static string FormatPosition(JsonSourcePosition? position)
    {
        if (position == null)
        {
            return "<null>";
        }

        return position.ByteOffset + ":" + position.Line + ":" +
            position.ByteColumn + ":" + position.ScalarColumn;
    }

    static void AssertPosition(
        JsonSourcePosition? position,
        long byteOffset,
        int line,
        int byteColumn,
        int scalarColumn)
    {
        Assert.Multiple(() =>
        {
            Assert.That(position, Is.Not.Null);
            Assert.That(position?.ByteOffset, Is.EqualTo(byteOffset));
            Assert.That(position?.Line, Is.EqualTo(line));
            Assert.That(position?.ByteColumn, Is.EqualTo(byteColumn));
            Assert.That(position?.ScalarColumn, Is.EqualTo(scalarColumn));
        });
    }

    class LocationTokenHandler : IJsonTokenHandler
    {
        internal List<JsonToken> Tokens = new List<JsonToken>();

        public JsonHandlerAction HandleToken(JsonToken token)
        {
            Tokens.Add(token);
            return JsonHandlerAction.Continue;
        }
    }
}
