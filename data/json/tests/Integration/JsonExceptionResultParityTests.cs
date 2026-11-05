namespace json.Tests;

public class JsonExceptionResultParityTests
{
    [Test]
    public void ParsingFailureHasCompleteExceptionAndResultParity()
    {
        byte[] input = System.Text.Encoding.UTF8.GetBytes(
            "{\n\"a\":[0,]}");
        JsonReaderOptions options = CreateTrackedOptions();
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocumentResult result = parser.TryParseUtf8(input, options);
        JsonParseException exception = Assert.Throws<JsonParseException>(() =>
            parser.ParseUtf8(input, options))
            ?? throw new InvalidOperationException("Expected parse exception.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(result.Document, Is.Null);
            Assert.That(exception.Message, Is.EqualTo(result.Diagnostic?.Message));
            AssertDiagnosticParity(result.Diagnostic, exception.Diagnostic);
        });
    }

    [Test]
    public void LimitFailureHasCompleteExceptionAndResultParity()
    {
        byte[] input = System.Text.Encoding.UTF8.GetBytes(
            "{\n\"a\":[0,1]}");
        JsonLimits limits = new JsonLimits();
        limits.MaximumArrayItems = 1;
        JsonReaderOptions options = CreateTrackedOptions();
        options.Limits = limits;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocumentResult result = parser.TryParseUtf8(input, options);
        JsonLimitException exception = Assert.Throws<JsonLimitException>(() =>
            parser.ParseUtf8(input, options))
            ?? throw new InvalidOperationException("Expected limit exception.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.ArrayItemLimitExceeded));
            Assert.That(exception.Message, Is.EqualTo(result.Diagnostic?.Message));
            AssertDiagnosticParity(result.Diagnostic, exception.Diagnostic);
        });
    }

    [Test]
    public void NumericConversionFailureHasCompleteExceptionAndResultParity()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonNumber number = factory.CreateNumber("1e400").Number
            ?? throw new InvalidOperationException("Expected number.");
        IJsonNumberConverter converter = new JsonNumberConverter();

        JsonDoubleResult result = converter.TryDouble(number);
        JsonParseException exception = Assert.Throws<JsonParseException>(() =>
            converter.ToDouble(number))
            ?? throw new InvalidOperationException("Expected conversion exception.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Value, Is.Zero);
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.NumericConversionFailed));
            Assert.That(exception.Message, Is.EqualTo(result.Diagnostic?.Message));
            AssertDiagnosticParity(result.Diagnostic, exception.Diagnostic);
        });
    }

    [Test]
    public void PointerFailureHasCompleteExceptionAndResultParity()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue root = factory.CreateArray(new List<JsonValue>
        {
            factory.CreateNull()
        });
        JsonPointer pointer = new JsonPointerCodec().ParseStringPointer("/2");
        IJsonPointerNavigator navigator = new JsonPointerNavigator();

        JsonPointerResult result = navigator.TryResolvePointer(
            root,
            pointer,
            JsonDuplicateKeyPolicy.Preserve);
        JsonPointerException exception = Assert.Throws<JsonPointerException>(() =>
            navigator.ResolvePointer(
                root,
                pointer,
                JsonDuplicateKeyPolicy.Preserve))
            ?? throw new InvalidOperationException("Expected pointer exception.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.FailedSegmentIndex, Is.Zero);
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(exception.Message, Is.EqualTo(result.Diagnostic?.Message));
            AssertDiagnosticParity(result.Diagnostic, exception.Diagnostic);
        });
    }

    [Test]
    public void DomValidationResultMatchesSerializerException()
    {
        JsonValue invalidRoot = new JsonValue();
        invalidRoot.Kind = JsonValueKind.Array;
        invalidRoot.ArrayItems = new List<JsonValue>
        {
            null!
        };
        JsonDocument document = new JsonDocument();
        document.Root = invalidRoot;
        IJsonValueValidator validator = new JsonValueValidator();
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();

        JsonOperationResult result = validator.ValidateDocument(
            document,
            new JsonLimits());
        JsonWriteException exception = Assert.Throws<JsonWriteException>(() =>
            serializer.SerializeUtf8(document, new JsonWriterOptions()))
            ?? throw new InvalidOperationException("Expected model exception.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(exception.Message, Is.EqualTo(result.Diagnostic?.Message));
            AssertDiagnosticParity(result.Diagnostic, exception.Diagnostic);
        });
    }

    [Test]
    public void WriterStateResultPromotesWithoutDiagnosticLoss()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        IJsonTokenWriter writer = new JsonUtf8Writer(
            new JsonWriterOptions(),
            target);

        JsonOperationResult result = writer.WriteEndArray();
        JsonDiagnostic diagnostic = result.Diagnostic
            ?? throw new InvalidOperationException("Expected writer diagnostic.");
        JsonWriteException exception = new JsonWriteException(diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidWriterState));
            Assert.That(exception.Message, Is.EqualTo(diagnostic.Message));
            Assert.That(exception.Diagnostic, Is.SameAs(diagnostic));
            AssertDiagnosticParity(result.Diagnostic, exception.Diagnostic);
            Assert.That(destination.WrittenCount, Is.Zero);
        });
    }

    [Test]
    public void CanonicalizationResultMatchesOwnedOutputException()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonDocument document = new JsonDocument();
        document.Root = factory.CreateObject(new List<JsonMember>
        {
            CreateMember("duplicate", factory.CreateNull()),
            CreateMember("duplicate", factory.CreateBoolean(true))
        });
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        JsonOperationResult result = canonicalizer.WriteCanonicalTarget(
            document,
            target);
        JsonWriteException exception = Assert.Throws<JsonWriteException>(() =>
            canonicalizer.CanonicalizeDocument(document))
            ?? throw new InvalidOperationException(
                "Expected canonicalization exception.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(exception.Message, Is.EqualTo(result.Diagnostic?.Message));
            AssertDiagnosticParity(result.Diagnostic, exception.Diagnostic);
            Assert.That(destination.WrittenCount, Is.Zero);
        });
    }

    [Test]
    public void StreamIoFailureHasCompleteExceptionAndResultParity()
    {
        byte[] input = System.Text.Encoding.UTF8.GetBytes("[0,1]");
        FailingReadStream resultStream = new FailingReadStream(input, 3);
        FailingReadStream exceptionStream = new FailingReadStream(input, 3);
        JsonReaderOptions options = CreateTrackedOptions();
        options.StreamBufferBytes = 2;
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocumentResult result = parser.TryParseStream(resultStream, options);
        JsonParseException exception = Assert.Throws<JsonParseException>(() =>
            parser.ParseStream(exceptionStream, options))
            ?? throw new InvalidOperationException("Expected I/O exception.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(exception.Message, Is.EqualTo(result.Diagnostic?.Message));
            AssertDiagnosticParity(result.Diagnostic, exception.Diagnostic);
            Assert.That(resultStream.Position, Is.EqualTo(exceptionStream.Position));
            Assert.That(resultStream.WasDisposed, Is.False);
            Assert.That(exceptionStream.WasDisposed, Is.False);
        });
    }

    static JsonReaderOptions CreateTrackedOptions()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureRawTokens = true;
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        return options;
    }

    static JsonMember CreateMember(string name, JsonValue value)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.Value = value;
        return member;
    }

    static void AssertDiagnosticParity(
        JsonDiagnostic? result,
        JsonDiagnostic exception)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(exception.Code, Is.EqualTo(result?.Code));
            Assert.That(exception.Message, Is.EqualTo(result?.Message));
            Assert.That(exception.Pointer, Is.EqualTo(result?.Pointer));
            Assert.That(exception.Depth, Is.EqualTo(result?.Depth));
            Assert.That(exception.ExpectedTokens, Is.EqualTo(result?.ExpectedTokens));
            Assert.That(exception.ActualToken, Is.EqualTo(result?.ActualToken));
            Assert.That(exception.SourceExcerpt, Is.EqualTo(result?.SourceExcerpt));
        });
        AssertRangeParity(result?.Range, exception.Range);
    }

    static void AssertRangeParity(
        JsonSourceRange? result,
        JsonSourceRange? exception)
    {
        Assert.Multiple(() =>
        {
            AssertPositionParity(result?.Start, exception?.Start);
            AssertPositionParity(result?.End, exception?.End);
        });
    }

    static void AssertPositionParity(
        JsonSourcePosition? result,
        JsonSourcePosition? exception)
    {
        Assert.Multiple(() =>
        {
            Assert.That(exception?.ByteOffset, Is.EqualTo(result?.ByteOffset));
            Assert.That(exception?.Line, Is.EqualTo(result?.Line));
            Assert.That(exception?.ByteColumn, Is.EqualTo(result?.ByteColumn));
            Assert.That(exception?.ScalarColumn, Is.EqualTo(result?.ScalarColumn));
        });
    }

    class FailingReadStream : Stream
    {
        byte[] input;
        int failureOffset;
        int position;

        internal bool WasDisposed;

        internal FailingReadStream(byte[] input, int failureOffset)
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
                throw new IOException("Injected read failure.");
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
