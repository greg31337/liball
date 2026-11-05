namespace json.Tests;

public class JsonDocumentParserTests
{
    [Test]
    public void Utf8ParsingBuildsAnOwnedDocumentThroughTheInterface()
    {
        byte[] input = System.Text.Encoding.UTF8.GetBytes(
            "\n{\"n\":-0.50e+2,\"s\":\"€\",\"a\":[true,null]}");
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureSourceRanges = true;
        options.TrackLineInformation = true;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocument document = parser.ParseUtf8(input, options);
        JsonValue root = document.Root
            ?? throw new InvalidOperationException("Expected a document root.");
        List<JsonMember> members = root.ObjectMembers
            ?? throw new InvalidOperationException("Expected object members.");
        input[2] = 0x78;

        Assert.Multiple(() =>
        {
            Assert.That(root.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(members.Select(member => member.Name), Is.EqualTo(new[]
            {
                "n", "s", "a"
            }));
            Assert.That(members[0].Value?.Number?.Lexeme, Is.EqualTo("-0.50e+2"));
            Assert.That(members[0].Value?.Number?.IsNegative, Is.True);
            Assert.That(members[1].Value?.StringValue, Is.EqualTo("€"));
            Assert.That(members[2].Value?.ArrayItems?[0].BooleanValue, Is.True);
            Assert.That(members[2].Value?.ArrayItems?[1].Kind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(root.Range?.Start?.ByteOffset, Is.EqualTo(1));
            Assert.That(root.Range?.Start?.Line, Is.EqualTo(2));
            Assert.That(root.Range?.End?.ByteOffset, Is.EqualTo(document.SourceByteCount));
            Assert.That(document.SourceByteCount, Is.EqualTo(input.Length));
            Assert.That(document.DuplicateKeyPolicy, Is.EqualTo(JsonDuplicateKeyPolicy.Preserve));
        });
    }

    [Test]
    public void TryUtf8AndThrowingUtf8HaveDiagnosticParity()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("[0,]");
        JsonReaderOptions options = new JsonReaderOptions();
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocumentResult result = parser.TryParseUtf8(input, options);
        JsonParseException? failure = Assert.Throws<JsonParseException>(() =>
            parser.ParseUtf8(input, options));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(failure?.Diagnostic.Code, Is.EqualTo(result.Diagnostic?.Code));
            Assert.That(failure?.Diagnostic.Range?.Start?.ByteOffset,
                Is.EqualTo(result.Diagnostic?.Range?.Start?.ByteOffset));
            Assert.That(failure?.Diagnostic.Pointer, Is.EqualTo(result.Diagnostic?.Pointer));
        });
    }

    [Test]
    public void StringParsingValidatesUtf16BeforeUtf8Conversion()
    {
        string text = "{\"music\":\"𝄞\",\"emoji\":\"😀\"}";
        string invalid = new string(new[] { '"', '\ud800', '"' });
        JsonReaderOptions options = new JsonReaderOptions();
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocument document = parser.ParseString(text, options);
        JsonDocumentResult invalidResult = parser.TryParseString(invalid, options);
        JsonParseException? invalidFailure = Assert.Throws<JsonParseException>(() =>
            parser.ParseString(invalid, options));

        Assert.Multiple(() =>
        {
            Assert.That(document.Root?.ObjectMembers?[0].Value?.StringValue, Is.EqualTo("𝄞"));
            Assert.That(document.Root?.ObjectMembers?[1].Value?.StringValue, Is.EqualTo("😀"));
            Assert.That(
                document.SourceByteCount,
                Is.EqualTo(System.Text.Encoding.UTF8.GetByteCount(text)));
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidResult.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
            Assert.That(invalidFailure?.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
        });
    }

    [Test]
    public void StreamParsingUsesBufferedInputWithoutClosingTheCallerStream()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes(
            "{\"a\":[0,1,2],\"b\":false}");
        TrackingDocumentStream stream = new TrackingDocumentStream(input);
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 2;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocument document = parser.ParseStream(stream, options);

        Assert.Multiple(() =>
        {
            Assert.That(document.Root?.ObjectMembers?[0].Value?.ArrayItems, Has.Count.EqualTo(3));
            Assert.That(document.Root?.ObjectMembers?[1].Value?.BooleanValue, Is.False);
            Assert.That(document.SourceByteCount, Is.EqualTo(input.Length));
            Assert.That(stream.Position, Is.EqualTo(input.Length));
            Assert.That(stream.WasDisposed, Is.False);
            Assert.That(stream.CanRead, Is.True);
        });
    }

    [Test]
    public void TryAndThrowingMethodsPreserveDomLimitFailureType()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("{\"a\":0}");
        JsonLimits limits = new JsonLimits();
        limits.MaximumDomNodes = 2;
        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocumentResult result = parser.TryParseUtf8(input, options);
        JsonLimitException? failure = Assert.Throws<JsonLimitException>(() =>
            parser.ParseUtf8(input, options));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.DomNodeLimitExceeded));
            Assert.That(failure?.Diagnostic.Code, Is.EqualTo(result.Diagnostic?.Code));
            Assert.That(failure?.Diagnostic.Pointer, Is.EqualTo("/a"));
        });
    }

    [Test]
    public void DocumentParserRequiresExactlyOneValueInEveryFramingMode()
    {
        JsonReaderOptions multipleOptions = new JsonReaderOptions();
        multipleOptions.InputMode = JsonInputMode.WhitespaceSeparated;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocumentResult multiple = parser.TryParseUtf8(
            System.Text.Encoding.ASCII.GetBytes("0 1"),
            multipleOptions);
        JsonDocumentResult empty = parser.TryParseUtf8(
            global::System.ReadOnlySpan<byte>.Empty,
            multipleOptions);

        Assert.Multiple(() =>
        {
            Assert.That(multiple.Success, Is.False);
            Assert.That(multiple.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.TrailingContent));
            Assert.That(empty.Success, Is.False);
            Assert.That(empty.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedEndOfInput));
        });
    }

    [Test]
    public void StreamIoFailuresHaveTryAndThrowingParityAndPreserveOwnership()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("[0,1]");
        FailingDocumentStream tryStream = new FailingDocumentStream(input, 3);
        FailingDocumentStream throwingStream = new FailingDocumentStream(input, 3);
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 2;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocumentResult result = parser.TryParseStream(tryStream, options);
        JsonParseException? failure = Assert.Throws<JsonParseException>(() =>
            parser.ParseStream(throwingStream, options));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(failure?.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(tryStream.WasDisposed, Is.False);
            Assert.That(throwingStream.WasDisposed, Is.False);
        });
    }

    class TrackingDocumentStream : MemoryStream
    {
        internal bool WasDisposed;

        internal TrackingDocumentStream(byte[] input) : base(input)
        {
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    class FailingDocumentStream : Stream
    {
        byte[] input;
        int failureOffset;
        int position;

        internal bool WasDisposed;

        internal FailingDocumentStream(byte[] input, int failureOffset)
        {
            this.input = input;
            this.failureOffset = failureOffset;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => input.Length;

        public override long Position
        {
            get => position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (position >= failureOffset)
            {
                throw new IOException("Injected document stream failure.");
            }

            int available = failureOffset - position;
            int length = count < available ? count : available;
            Array.Copy(input, position, buffer, offset, length);
            position += length;
            return length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
