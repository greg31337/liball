namespace json.Tests;

public class JsonDocumentParserConformanceTests
{
    [TestCase("null", JsonValueKind.Null, "null")]
    [TestCase("true", JsonValueKind.Boolean, "true")]
    [TestCase("false", JsonValueKind.Boolean, "false")]
    [TestCase("-0.50e+2", JsonValueKind.Number, "-0.50e+2")]
    [TestCase("\"text €\"", JsonValueKind.String, "text €")]
    [TestCase("[0]", JsonValueKind.Array, "1")]
    [TestCase("{\"a\":0}", JsonValueKind.Object, "1")]
    public void EveryJsonRootFormIsEquivalentThroughEveryInputAndFailureModel(
        string text,
        JsonValueKind expectedKind,
        string expectedPayload)
    {
        IJsonDocumentParser parser = new JsonDocumentParser();
        JsonReaderOptions options = new JsonReaderOptions();

        List<JsonDocument> documents = ParseWithEveryEntryPoint(parser, text, options);

        Assert.That(documents, Has.Count.EqualTo(6));
        foreach (JsonDocument document in documents)
        {
            AssertRoot(document, expectedKind, expectedPayload);
            Assert.That(
                document.SourceByteCount,
                Is.EqualTo(System.Text.Encoding.UTF8.GetByteCount(text)));
        }
    }

    [TestCase(JsonConformanceProfile.SyntaxCompatible)]
    [TestCase(JsonConformanceProfile.Portable)]
    [TestCase(JsonConformanceProfile.IJson)]
    [TestCase(JsonConformanceProfile.Jcs)]
    public void EveryConformanceProfileBuildsADocumentWithItsExactMetadata(
        JsonConformanceProfile profile)
    {
        string text = "{\"music\":\"𝄞\"}";
        JsonReaderOptions options = CreateProfileOptions(profile);
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocument document = parser.ParseString(text, options);

        Assert.Multiple(() =>
        {
            Assert.That(document.Root?.ObjectMembers?[0].Value?.StringValue, Is.EqualTo("𝄞"));
            Assert.That(document.ConformanceProfile, Is.EqualTo(profile));
            Assert.That(document.DuplicateKeyPolicy, Is.EqualTo(options.DuplicateKeyPolicy));
            Assert.That(document.UnicodePolicy, Is.EqualTo(options.UnicodePolicy));
            Assert.That(document.InputMode, Is.EqualTo(JsonInputMode.SingleValue));
            Assert.That(document.BomPolicy, Is.EqualTo(JsonBomPolicy.Reject));
            Assert.That(document.SourceByteCount,
                Is.EqualTo(System.Text.Encoding.UTF8.GetByteCount(text)));
        });
    }

    [TestCase(JsonInputMode.SingleValue, "{\"a\":0}")]
    [TestCase(JsonInputMode.WhitespaceSeparated, " \n{\"a\":0}\t")]
    [TestCase(JsonInputMode.JsonTextSequence, "\u001e{\"a\":0}\n")]
    public void EveryFramingPolicyCanBuildExactlyOneDocument(
        JsonInputMode inputMode,
        string text)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = inputMode;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocument document = parser.ParseString(text, options);

        Assert.Multiple(() =>
        {
            Assert.That(document.Root?.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(document.Root?.ObjectMembers?[0].Name, Is.EqualTo("a"));
            Assert.That(document.InputMode, Is.EqualTo(inputMode));
            Assert.That(document.SourceByteCount,
                Is.EqualTo(System.Text.Encoding.UTF8.GetByteCount(text)));
        });
    }

    [Test]
    public void SyntaxCompatibleAcceptsEscapedLoneSurrogatesAndEveryStrictProfileRejectsThem()
    {
        const string text = "\"\\uD800\"";
        IJsonDocumentParser parser = new JsonDocumentParser();
        JsonDocument compatible = parser.ParseString(
            text,
            CreateProfileOptions(JsonConformanceProfile.SyntaxCompatible));

        Assert.Multiple(() =>
        {
            Assert.That(compatible.Root?.StringValue, Has.Length.EqualTo(1));
            Assert.That(compatible.Root?.StringValue?[0], Is.EqualTo('\ud800'));
        });

        foreach (JsonConformanceProfile profile in new[]
        {
            JsonConformanceProfile.Portable,
            JsonConformanceProfile.IJson,
            JsonConformanceProfile.Jcs
        })
        {
            JsonDocumentResult result = parser.TryParseString(text, CreateProfileOptions(profile));
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False, profile.ToString());
                Assert.That(result.Document, Is.Null, profile.ToString());
                Assert.That(result.Diagnostic?.Code,
                    Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair),
                    profile.ToString());
            });
        }
    }

    [TestCase(JsonDuplicateKeyPolicy.Preserve, "0,1", "0,1")]
    [TestCase(JsonDuplicateKeyPolicy.KeepFirst, "0", "0")]
    [TestCase(JsonDuplicateKeyPolicy.KeepLast, "1", "1")]
    public void EveryRetainingDuplicatePolicyUsesDecodedNamesAndSourceOrdinals(
        JsonDuplicateKeyPolicy policy,
        string expectedValues,
        string expectedOrdinals)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = policy;
        IJsonDocumentParser parser = new JsonDocumentParser();
        byte[] input = System.Text.Encoding.ASCII.GetBytes("{\"a\":0,\"\\u0061\":1}");

        JsonDocument document = parser.ParseUtf8(input, options);
        List<JsonMember> members = document.Root?.ObjectMembers
            ?? throw new InvalidOperationException("Expected object members.");

        Assert.Multiple(() =>
        {
            Assert.That(members.Select(member => member.Name),
                Is.All.EqualTo("a"));
            Assert.That(string.Join(",", members.Select(member => member.Value?.Number?.Lexeme)),
                Is.EqualTo(expectedValues));
            Assert.That(string.Join(",", members.Select(member => member.SourceOrdinal)),
                Is.EqualTo(expectedOrdinals));
            Assert.That(document.DuplicateKeyPolicy, Is.EqualTo(policy));
        });
    }

    [Test]
    public void RejectDuplicatePolicyFailsOnDuplicateDecodedNames()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        IJsonDocumentParser parser = new JsonDocumentParser();
        byte[] input = System.Text.Encoding.ASCII.GetBytes("{\"a\":0,\"\\u0061\":1}");

        JsonDocumentResult result = parser.TryParseUtf8(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
        });
    }

    [Test]
    public void SequenceDocumentPublishesExactUtf8MetadataAndOwnedSourceRanges()
    {
        string text = "\u001e\n{\"x\":\"€\"}\n";
        byte[] input = System.Text.Encoding.UTF8.GetBytes(text);
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.KeepLast;
        options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        options.InputMode = JsonInputMode.JsonTextSequence;
        options.CaptureSourceRanges = true;
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocument document = parser.ParseUtf8(input, options);
        JsonValue root = document.Root
            ?? throw new InvalidOperationException("Expected a document root.");
        JsonMember member = root.ObjectMembers?[0]
            ?? throw new InvalidOperationException("Expected an object member.");
        JsonValue value = member.Value
            ?? throw new InvalidOperationException("Expected a member value.");
        input[4] = 0x79;

        Assert.Multiple(() =>
        {
            Assert.That(member.Name, Is.EqualTo("x"));
            Assert.That(value.StringValue, Is.EqualTo("€"));
            Assert.That(document.ConformanceProfile,
                Is.EqualTo(JsonConformanceProfile.SyntaxCompatible));
            Assert.That(document.DuplicateKeyPolicy,
                Is.EqualTo(JsonDuplicateKeyPolicy.KeepLast));
            Assert.That(document.UnicodePolicy, Is.EqualTo(JsonUnicodePolicy.UnicodeScalar));
            Assert.That(document.InputMode, Is.EqualTo(JsonInputMode.JsonTextSequence));
            Assert.That(document.BomPolicy, Is.EqualTo(JsonBomPolicy.Reject));
            Assert.That(document.SourceByteCount, Is.EqualTo(14));
            AssertRange(root.Range, 2, 13);
            AssertRange(member.NameRange, 3, 6);
            AssertRange(member.MemberRange, 3, 12);
            AssertRange(value.Range, 7, 12);
            AssertPosition(root.Range?.Start, 2, 2, 1, 1);
            AssertPosition(root.Range?.End, 13, 2, 12, 10);
        });
    }

    [Test]
    public void AllowedInitialBomCountsTowardSourceBytesButNotTheRootRange()
    {
        byte[] input = new byte[] { 0xEF, 0xBB, 0xBF, 0x5B, 0x30, 0x5D };
        JsonReaderOptions options = new JsonReaderOptions();
        options.BomPolicy = JsonBomPolicy.AllowInitial;
        options.CaptureSourceRanges = true;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocument document = parser.ParseUtf8(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(document.BomPolicy, Is.EqualTo(JsonBomPolicy.AllowInitial));
            Assert.That(document.SourceByteCount, Is.EqualTo(6));
            AssertRange(document.Root?.Range, 3, 6);
            AssertRange(document.Root?.ArrayItems?[0].Range, 4, 5);
        });
    }

    [Test]
    public void EveryInputFormHasCompleteTryAndThrowingDiagnosticParity()
    {
        const string text = "{\"a\":[0,]}";
        byte[] input = System.Text.Encoding.ASCII.GetBytes(text);
        JsonReaderOptions options = new JsonReaderOptions();
        options.TrackLineInformation = true;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocumentResult utf8Result = parser.TryParseUtf8(input, options);
        JsonParseException utf8Failure = Assert.Throws<JsonParseException>(() =>
            parser.ParseUtf8(input, options))
            ?? throw new InvalidOperationException("Expected a UTF-8 parse failure.");
        AssertDiagnosticParity(utf8Result, utf8Failure);

        JsonDocumentResult stringResult = parser.TryParseString(text, options);
        JsonParseException stringFailure = Assert.Throws<JsonParseException>(() =>
            parser.ParseString(text, options))
            ?? throw new InvalidOperationException("Expected a string parse failure.");
        AssertDiagnosticParity(stringResult, stringFailure);

        TrackingConformanceStream tryStream = new TrackingConformanceStream(input);
        TrackingConformanceStream throwingStream = new TrackingConformanceStream(input);
        JsonDocumentResult streamResult = parser.TryParseStream(tryStream, options);
        JsonParseException streamFailure = Assert.Throws<JsonParseException>(() =>
            parser.ParseStream(throwingStream, options))
            ?? throw new InvalidOperationException("Expected a stream parse failure.");
        AssertDiagnosticParity(streamResult, streamFailure);

        Assert.Multiple(() =>
        {
            Assert.That(utf8Result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(stringResult.Diagnostic?.Code, Is.EqualTo(utf8Result.Diagnostic?.Code));
            Assert.That(streamResult.Diagnostic?.Code, Is.EqualTo(utf8Result.Diagnostic?.Code));
            Assert.That(tryStream.WasDisposed, Is.False);
            Assert.That(throwingStream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void SlicedUtf8OptionsAndStreamsRemainCallerOwnedAndParserIsReusable()
    {
        byte[] backing = System.Text.Encoding.ASCII.GetBytes("xx{\"s\":\"owned\"}yy");
        global::System.ReadOnlySpan<byte> slice = new global::System.ReadOnlySpan<byte>(
            backing,
            2,
            backing.Length - 4);
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureSourceRanges = true;
        TrackingConformanceStream stream = new TrackingConformanceStream(
            System.Text.Encoding.ASCII.GetBytes("[1]"));
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocument sliced = parser.ParseUtf8(slice, options);
        JsonDocument streamed = parser.ParseStream(stream, options);
        JsonDocumentResult failure = parser.TryParseString("[", options);
        JsonDocument afterFailure = parser.ParseString("false", options);
        Array.Fill(backing, (byte)0x78);

        Assert.Multiple(() =>
        {
            Assert.That(sliced.Root?.ObjectMembers?[0].Name, Is.EqualTo("s"));
            Assert.That(sliced.Root?.ObjectMembers?[0].Value?.StringValue, Is.EqualTo("owned"));
            Assert.That(sliced.SourceByteCount, Is.EqualTo(13));
            AssertRange(sliced.Root?.Range, 0, 13);
            Assert.That(streamed.Root?.ArrayItems?[0].Number?.Lexeme, Is.EqualTo("1"));
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
            Assert.That(stream.WasDisposed, Is.False);
            Assert.That(stream.CanRead, Is.True);
            Assert.That(options.StreamBufferBytes, Is.Zero);
            Assert.That(options.Limits, Is.Null);
            Assert.That(failure.Success, Is.False);
            Assert.That(afterFailure.Root?.BooleanValue, Is.False);
        });
    }

    static List<JsonDocument> ParseWithEveryEntryPoint(
        IJsonDocumentParser parser,
        string text,
        JsonReaderOptions options)
    {
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(text);
        List<JsonDocument> documents = new List<JsonDocument>();
        documents.Add(parser.ParseUtf8(utf8, options));
        documents.Add(RequireDocument(parser.TryParseUtf8(utf8, options)));
        documents.Add(parser.ParseString(text, options));
        documents.Add(RequireDocument(parser.TryParseString(text, options)));

        TrackingConformanceStream throwingStream = new TrackingConformanceStream(utf8);
        TrackingConformanceStream resultStream = new TrackingConformanceStream(utf8);
        documents.Add(parser.ParseStream(throwingStream, options));
        documents.Add(RequireDocument(parser.TryParseStream(resultStream, options)));
        Assert.Multiple(() =>
        {
            Assert.That(throwingStream.WasDisposed, Is.False);
            Assert.That(resultStream.WasDisposed, Is.False);
        });
        return documents;
    }

    static JsonDocument RequireDocument(JsonDocumentResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(result.Diagnostic, Is.Null);
        });
        return result.Document
            ?? throw new InvalidOperationException("Expected a successful document result.");
    }

    static void AssertRoot(
        JsonDocument document,
        JsonValueKind expectedKind,
        string expectedPayload)
    {
        JsonValue root = document.Root
            ?? throw new InvalidOperationException("Expected a document root.");
        Assert.That(root.Kind, Is.EqualTo(expectedKind));

        switch (expectedKind)
        {
            case JsonValueKind.Null:
                Assert.That(expectedPayload, Is.EqualTo("null"));
                break;
            case JsonValueKind.Boolean:
                Assert.That(root.BooleanValue, Is.EqualTo(bool.Parse(expectedPayload)));
                break;
            case JsonValueKind.Number:
                Assert.That(root.Number?.Lexeme, Is.EqualTo(expectedPayload));
                break;
            case JsonValueKind.String:
                Assert.That(root.StringValue, Is.EqualTo(expectedPayload));
                break;
            case JsonValueKind.Array:
                Assert.That(root.ArrayItems, Has.Count.EqualTo(int.Parse(expectedPayload)));
                break;
            case JsonValueKind.Object:
                Assert.That(root.ObjectMembers, Has.Count.EqualTo(int.Parse(expectedPayload)));
                break;
            default:
                Assert.Fail("Unexpected materialized root kind.");
                break;
        }
    }

    static JsonReaderOptions CreateProfileOptions(JsonConformanceProfile profile)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.ConformanceProfile = profile;
        if (profile != JsonConformanceProfile.SyntaxCompatible)
        {
            options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
            options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
            options.BomPolicy = JsonBomPolicy.Reject;
        }

        return options;
    }

    static void AssertDiagnosticParity(JsonDocumentResult result, JsonParseException failure)
    {
        JsonDiagnostic diagnostic = result.Diagnostic
            ?? throw new InvalidOperationException("Expected a result diagnostic.");
        JsonDiagnostic thrown = failure.Diagnostic;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(result.Document, Is.Null);
            Assert.That(thrown.Code, Is.EqualTo(diagnostic.Code));
            Assert.That(thrown.Message, Is.EqualTo(diagnostic.Message));
            Assert.That(thrown.Range?.Start?.ByteOffset,
                Is.EqualTo(diagnostic.Range?.Start?.ByteOffset));
            Assert.That(thrown.Range?.End?.ByteOffset,
                Is.EqualTo(diagnostic.Range?.End?.ByteOffset));
            Assert.That(thrown.Range?.Start?.Line, Is.EqualTo(diagnostic.Range?.Start?.Line));
            Assert.That(thrown.Range?.Start?.ByteColumn,
                Is.EqualTo(diagnostic.Range?.Start?.ByteColumn));
            Assert.That(thrown.Pointer, Is.EqualTo(diagnostic.Pointer));
            Assert.That(thrown.Depth, Is.EqualTo(diagnostic.Depth));
            Assert.That(thrown.ExpectedTokens, Is.EqualTo(diagnostic.ExpectedTokens));
            Assert.That(thrown.ActualToken, Is.EqualTo(diagnostic.ActualToken));
            Assert.That(thrown.SourceExcerpt, Is.EqualTo(diagnostic.SourceExcerpt));
        });
    }

    static void AssertRange(JsonSourceRange? range, long start, long end)
    {
        Assert.Multiple(() =>
        {
            Assert.That(range, Is.Not.Null);
            Assert.That(range?.Start?.ByteOffset, Is.EqualTo(start));
            Assert.That(range?.End?.ByteOffset, Is.EqualTo(end));
        });
    }

    static void AssertPosition(
        JsonSourcePosition? position,
        long offset,
        int line,
        int byteColumn,
        int scalarColumn)
    {
        Assert.Multiple(() =>
        {
            Assert.That(position?.ByteOffset, Is.EqualTo(offset));
            Assert.That(position?.Line, Is.EqualTo(line));
            Assert.That(position?.ByteColumn, Is.EqualTo(byteColumn));
            Assert.That(position?.ScalarColumn, Is.EqualTo(scalarColumn));
        });
    }

    class TrackingConformanceStream : MemoryStream
    {
        internal bool WasDisposed;

        internal TrackingConformanceStream(byte[] input) : base(input)
        {
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
