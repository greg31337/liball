namespace json.Tests;

public class JsonPublicContractBehaviorTests
{
    [Test]
    public void DocumentValidationSerializationAndCanonicalizationInterfacesCoverEveryMethod()
    {
        JsonReaderOptions readerOptions = new JsonReaderOptions();
        IJsonDocumentParser parser = new JsonDocumentParser();

        JsonDocument utf8Document = parser.ParseUtf8(Bytes("null"), readerOptions);
        JsonDocumentResult tryUtf8 = parser.TryParseUtf8(Bytes("true"), readerOptions);
        JsonDocument stringDocument = parser.ParseString("0", readerOptions);
        JsonDocumentResult tryString = parser.TryParseString("\"text\"", readerOptions);
        MemoryStream parseStream = new MemoryStream(Bytes("[]"));
        JsonDocument streamDocument = parser.ParseStream(parseStream, readerOptions);
        MemoryStream tryParseStream = new MemoryStream(Bytes("{}"));
        JsonDocumentResult tryStream = parser.TryParseStream(
            tryParseStream,
            readerOptions);

        IJsonUtf8Validator validator = new JsonUtf8Validator();
        JsonOperationResult validatedBytes = validator.ValidateUtf8(
            Bytes("[0]"),
            readerOptions);
        MemoryStream validationStream = new MemoryStream(Bytes("{\"a\":1}"));
        JsonOperationResult validatedStream = validator.ValidateStream(
            validationStream,
            readerOptions);

        JsonDocument document = CreateDocument(CreateObjectValue());
        JsonWriterOptions writerOptions = new JsonWriterOptions();
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        byte[] serializedUtf8 = serializer.SerializeUtf8(document, writerOptions);
        string serializedString = serializer.SerializeString(document, writerOptions);
        System.Buffers.ArrayBufferWriter<byte> serializedTarget =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget serializerTarget = new JsonWriterTarget();
        serializerTarget.BufferWriter = serializedTarget;
        JsonOperationResult serialized = serializer.SerializeTarget(
            document,
            writerOptions,
            serializerTarget);

        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();
        byte[] canonicalUtf8 = canonicalizer.CanonicalizeUtf8(
            Bytes("{ \"b\" : 2, \"a\" : 1 }"),
            readerOptions);
        byte[] canonicalDocument = canonicalizer.CanonicalizeDocument(document);
        System.Buffers.ArrayBufferWriter<byte> canonicalTarget =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget canonicalWriterTarget = new JsonWriterTarget();
        canonicalWriterTarget.BufferWriter = canonicalTarget;
        JsonOperationResult canonicalWrite = canonicalizer.WriteCanonicalTarget(
            document,
            canonicalWriterTarget);
        JsonOperationResult canonicalVerification = canonicalizer.VerifyCanonicalUtf8(
            canonicalUtf8,
            readerOptions);

        Assert.Multiple(() =>
        {
            Assert.That(utf8Document.Root?.Kind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(tryUtf8.Success, Is.True);
            Assert.That(tryUtf8.Document?.Root?.BooleanValue, Is.True);
            Assert.That(stringDocument.Root?.Number?.Lexeme, Is.EqualTo("0"));
            Assert.That(tryString.Success, Is.True);
            Assert.That(tryString.Document?.Root?.StringValue, Is.EqualTo("text"));
            Assert.That(streamDocument.Root?.Kind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(tryStream.Success, Is.True);
            Assert.That(tryStream.Document?.Root?.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(validatedBytes.Success, Is.True);
            Assert.That(validatedStream.Success, Is.True);
            Assert.That(Text(serializedUtf8), Is.EqualTo("{\"value\":[null,true,1]}"));
            Assert.That(serializedString, Is.EqualTo(Text(serializedUtf8)));
            Assert.That(serialized.Success, Is.True);
            Assert.That(Text(serializedTarget.WrittenSpan.ToArray()),
                Is.EqualTo(Text(serializedUtf8)));
            Assert.That(Text(canonicalUtf8), Is.EqualTo("{\"a\":1,\"b\":2}"));
            Assert.That(Text(canonicalDocument),
                Is.EqualTo("{\"value\":[null,true,1]}"));
            Assert.That(canonicalWrite.Success, Is.True);
            Assert.That(Text(canonicalTarget.WrittenSpan.ToArray()),
                Is.EqualTo(Text(canonicalDocument)));
            Assert.That(canonicalVerification.Success, Is.True);
        });
    }

    [Test]
    public void ModelFactoryValidationComparisonNavigationAndTraversalInterfacesCoverEveryMethod()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue nullValue = factory.CreateNull();
        JsonValue booleanValue = factory.CreateBoolean(true);
        JsonValue numberValue = factory.CreateNumber("1.0");
        JsonValueResult tryNumber = factory.TryCreateNumber("2e0");
        JsonValue stringValue = factory.CreateString("text");
        JsonValue arrayValue = factory.CreateArray(new List<JsonValue>
        {
            nullValue,
            booleanValue
        });
        JsonValue objectValue = factory.CreateObject(new List<JsonMember>
        {
            CreateMember("same", nullValue),
            CreateMember("same", booleanValue)
        });
        JsonValue clone = factory.CloneValue(objectValue);

        IJsonValueValidator validator = new JsonValueValidator();
        JsonOperationResult validValue = validator.ValidateValue(
            clone,
            new JsonLimits());
        JsonOperationResult validDocument = validator.ValidateDocument(
            CreateDocument(arrayValue),
            new JsonLimits());

        IJsonValueComparer comparer = new JsonValueComparer();
        bool equal = comparer.CompareValues(
            objectValue,
            clone,
            JsonComparisonMode.ExactModel);

        IJsonDomNavigator navigator = new JsonDomNavigator();
        JsonValue firstArrayItem = navigator.GetArrayItem(arrayValue, 0);
        JsonValue firstObjectValue = navigator.GetObjectValue(
            objectValue,
            "same",
            JsonDuplicateKeyPolicy.KeepFirst);
        List<JsonMember> matches = navigator.GetObjectMatches(
            objectValue,
            "same");
        JsonOperationResult appendArray = navigator.AppendArrayItem(
            arrayValue,
            stringValue);
        JsonOperationResult insertArray = navigator.InsertArrayItem(
            arrayValue,
            1,
            numberValue);
        JsonOperationResult replaceArray = navigator.ReplaceArrayItem(
            arrayValue,
            0,
            stringValue);
        JsonOperationResult removeArray = navigator.RemoveArrayItem(arrayValue, 1);
        JsonOperationResult clearArray = navigator.ClearArrayItems(arrayValue);
        JsonOperationResult appendObject = navigator.AppendObjectMember(
            objectValue,
            CreateMember("append", stringValue));
        JsonOperationResult insertObject = navigator.InsertObjectMember(
            objectValue,
            1,
            CreateMember("insert", numberValue));
        JsonOperationResult replaceObject = navigator.ReplaceObjectMember(
            objectValue,
            0,
            CreateMember("replace", booleanValue));
        JsonOperationResult removeObject = navigator.RemoveObjectMember(
            objectValue,
            1);
        JsonOperationResult clearObject = navigator.ClearObjectMembers(objectValue);

        JsonValue traversalRoot = CreateObjectValue();
        ContractVisitor visitor = new ContractVisitor();
        IJsonTraversal traversal = new JsonDepthFirstTraversal();
        JsonVisitAction visitAction = traversal.TraverseValues(
            traversalRoot,
            visitor);
        ContractHandler handler = new ContractHandler();
        JsonOperationResult publication = traversal.PublishValueTokens(
            traversalRoot,
            handler);

        Assert.Multiple(() =>
        {
            Assert.That(tryNumber.Success, Is.True);
            Assert.That(tryNumber.Value?.Number?.Lexeme, Is.EqualTo("2e0"));
            Assert.That(validValue.Success, Is.True);
            Assert.That(validDocument.Success, Is.True);
            Assert.That(equal, Is.True);
            Assert.That(firstArrayItem, Is.SameAs(nullValue));
            Assert.That(firstObjectValue, Is.SameAs(nullValue));
            Assert.That(matches, Has.Count.EqualTo(2));
            Assert.That(appendArray.Success, Is.True);
            Assert.That(insertArray.Success, Is.True);
            Assert.That(replaceArray.Success, Is.True);
            Assert.That(removeArray.Success, Is.True);
            Assert.That(clearArray.Success, Is.True);
            Assert.That(arrayValue.ArrayItems, Is.Empty);
            Assert.That(appendObject.Success, Is.True);
            Assert.That(insertObject.Success, Is.True);
            Assert.That(replaceObject.Success, Is.True);
            Assert.That(removeObject.Success, Is.True);
            Assert.That(clearObject.Success, Is.True);
            Assert.That(objectValue.ObjectMembers, Is.Empty);
            Assert.That(visitAction, Is.EqualTo(JsonVisitAction.Continue));
            Assert.That(visitor.Visits, Is.EqualTo(5));
            Assert.That(publication.Success, Is.True);
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartObject,
                JsonTokenKind.PropertyName,
                JsonTokenKind.StartArray,
                JsonTokenKind.Null,
                JsonTokenKind.Boolean,
                JsonTokenKind.Number,
                JsonTokenKind.EndArray,
                JsonTokenKind.EndObject
            }));
        });
    }

    [Test]
    public void NumberAndPointerInterfacesCoverEveryMethod()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        IJsonNumberConverter converter = new JsonNumberConverter();
        JsonNumber signedNumber = factory.CreateNumber("-2").Number
            ?? throw new InvalidOperationException("Expected number.");
        JsonNumber unsignedNumber = factory.CreateNumber("2").Number
            ?? throw new InvalidOperationException("Expected number.");
        JsonNumber decimalNumber = factory.CreateNumber("2.5").Number
            ?? throw new InvalidOperationException("Expected number.");
        JsonNumber doubleNumber = factory.CreateNumber("2.5e1").Number
            ?? throw new InvalidOperationException("Expected number.");

        long signed = converter.ToInt64(signedNumber);
        JsonInt64Result trySigned = converter.TryInt64(signedNumber);
        ulong unsigned = converter.ToUInt64(unsignedNumber);
        JsonUInt64Result tryUnsigned = converter.TryUInt64(unsignedNumber);
        decimal decimalValue = converter.ToDecimal(decimalNumber);
        JsonDecimalResult tryDecimal = converter.TryDecimal(decimalNumber);
        double doubleValue = converter.ToDouble(doubleNumber);
        JsonDoubleResult tryDouble = converter.TryDouble(doubleNumber);

        IJsonPointerCodec codec = new JsonPointerCodec();
        JsonPointer stringPointer = codec.ParseStringPointer("/a~1b/~0");
        string formattedString = codec.FormatStringPointer(stringPointer);
        JsonPointer fragmentPointer = codec.ParseUriFragment("#/a%20b/%E2%82%AC");
        string formattedFragment = codec.FormatUriFragment(fragmentPointer);

        JsonValue root = factory.CreateObject(new List<JsonMember>
        {
            CreateMember("a/b", factory.CreateArray(new List<JsonValue>
            {
                factory.CreateString("found")
            })),
            CreateMember("duplicate", factory.CreateNumber("1")),
            CreateMember("duplicate", factory.CreateNumber("2"))
        });
        IJsonPointerNavigator navigator = new JsonPointerNavigator();
        JsonPointer valuePointer = codec.ParseStringPointer("/a~1b/0");
        JsonValue resolved = navigator.ResolvePointer(
            root,
            valuePointer,
            JsonDuplicateKeyPolicy.Preserve);
        JsonPointerResult tryResolved = navigator.TryResolvePointer(
            root,
            valuePointer,
            JsonDuplicateKeyPolicy.Preserve);
        JsonPointerResult allMatches = navigator.ResolveAllPointerMatches(
            root,
            codec.ParseStringPointer("/duplicate"));

        Assert.Multiple(() =>
        {
            Assert.That(signed, Is.EqualTo(-2));
            Assert.That(trySigned.Success, Is.True);
            Assert.That(unsigned, Is.EqualTo(2));
            Assert.That(tryUnsigned.Success, Is.True);
            Assert.That(decimalValue, Is.EqualTo(2.5m));
            Assert.That(tryDecimal.Success, Is.True);
            Assert.That(doubleValue, Is.EqualTo(25.0d));
            Assert.That(tryDouble.Success, Is.True);
            Assert.That(formattedString, Is.EqualTo("/a~1b/~0"));
            Assert.That(formattedFragment, Is.EqualTo("#/a%20b/%E2%82%AC"));
            Assert.That(resolved.StringValue, Is.EqualTo("found"));
            Assert.That(tryResolved.Success, Is.True);
            Assert.That(tryResolved.Value?.StringValue, Is.EqualTo("found"));
            Assert.That(allMatches.Success, Is.True);
            Assert.That(allMatches.Values, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void PullAndPushInterfacesCoverEveryMethod()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());
        JsonFeedResult pullFeed = reader.FeedUtf8(Bytes("[{\"a\":0},1]"));
        JsonTokenResult peek = reader.PeekToken();
        JsonTokenResult start = reader.ReadToken();
        JsonValueResult captured = reader.CaptureValue();
        JsonOperationResult skipped = reader.SkipValue();
        JsonTokenResult end = reader.ReadToken();
        JsonFeedResult pullCompleted = reader.CompleteInput();
        DrainPull(reader);
        long pullAccepted = reader.GetBytesAccepted();
        long pullConsumed = reader.GetBytesConsumed();
        reader.ResetReader();
        MemoryStream pullStream = new MemoryStream(Bytes("null"));
        JsonFeedResult pullStreamFeed = reader.FeedStream(pullStream);
        List<JsonTokenKind> streamKinds = DrainPull(reader);

        ContractHandler handler = new ContractHandler();
        handler.PauseAtInvocation = 0;
        IJsonPushParser parser = new JsonPushParser(
            new JsonReaderOptions(),
            handler);
        JsonFeedResult pushFeed = parser.FeedPushUtf8(Bytes("[0]"));
        long pushAccepted = parser.GetPushBytesAccepted();
        long pushConsumed = parser.GetPushBytesConsumed();
        JsonFeedResult resumed = parser.ResumePushParser();
        JsonFeedResult pushCompleted = parser.CompletePushInput();
        parser.ResetPushParser();
        handler.Tokens.Clear();
        handler.PauseAtInvocation = -1;
        MemoryStream pushStream = new MemoryStream(Bytes("true"));
        JsonFeedResult pushStreamFeed = parser.FeedPushStream(pushStream);

        Assert.Multiple(() =>
        {
            Assert.That(pullFeed.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(peek.Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));
            Assert.That(start.Token, Is.SameAs(peek.Token));
            Assert.That(captured.Success, Is.True);
            Assert.That(captured.Value?.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(skipped.Success, Is.True);
            Assert.That(end.Token?.Kind, Is.EqualTo(JsonTokenKind.EndArray));
            Assert.That(pullCompleted.Diagnostic, Is.Null);
            Assert.That(pullAccepted, Is.EqualTo(Bytes("[{\"a\":0},1]").Length));
            Assert.That(pullConsumed, Is.EqualTo(pullAccepted));
            Assert.That(pullStreamFeed.Diagnostic, Is.Null);
            Assert.That(streamKinds, Is.EqualTo(new[] { JsonTokenKind.Null }));
            Assert.That(pushFeed.ReadStatus, Is.EqualTo(JsonReadStatus.Paused));
            Assert.That(pushAccepted, Is.GreaterThan(0));
            Assert.That(pushConsumed, Is.GreaterThan(0));
            Assert.That(resumed.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(pushCompleted.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(pushStreamFeed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.Boolean,
                JsonTokenKind.DocumentBoundary
            }));
        });
    }

    [Test]
    public void TokenWriterInterfaceCoversEveryMethod()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        IJsonTokenWriter writer = new JsonUtf8Writer(
            new JsonWriterOptions(),
            target);
        IJsonValueFactory factory = new JsonValueFactory();

        AssertSuccess(writer.WriteStartArray());
        AssertSuccess(writer.WriteNull());
        AssertSuccess(writer.WriteBoolean(true));
        AssertSuccess(writer.WriteString("text"));
        AssertSuccess(writer.WriteNumberLexeme("1.25"));
        AssertSuccess(writer.WriteInt64(-2));
        AssertSuccess(writer.WriteUInt64(3));
        AssertSuccess(writer.WriteDecimal(4.5m));
        AssertSuccess(writer.WriteDouble(6.25d));
        AssertSuccess(writer.WriteStartObject());
        AssertSuccess(writer.WritePropertyName("property"));
        AssertSuccess(writer.WriteStartArray());
        AssertSuccess(writer.WriteEndArray());
        AssertSuccess(writer.WriteEndObject());
        AssertSuccess(writer.WriteJsonValue(factory.CreateString("model")));
        AssertSuccess(writer.WriteRawValue(Bytes("{\"raw\":null}")));
        AssertSuccess(writer.WriteEndArray());
        long pendingBeforeFlush = writer.GetBytesPending();
        JsonOperationResult flushed = writer.FlushWriter();
        long committedAfterFlush = writer.GetBytesCommitted();
        JsonOperationResult completed = writer.CompleteWriter();
        writer.ResetWriter();

        Assert.Multiple(() =>
        {
            Assert.That(pendingBeforeFlush, Is.GreaterThan(0));
            Assert.That(flushed.Success, Is.True);
            Assert.That(committedAfterFlush, Is.EqualTo(destination.WrittenCount));
            Assert.That(completed.Success, Is.True);
            Assert.That(Text(destination.WrittenSpan.ToArray()), Is.EqualTo(
                "[null,true,\"text\",1.25,-2,3,4.5,6.25," +
                "{\"property\":[]},\"model\",{\"raw\":null}]"));
            Assert.That(writer.GetBytesPending(), Is.Zero);
            Assert.That(writer.GetBytesCommitted(), Is.Zero);
        });
    }

    [Test]
    public void EveryImplementationExposesItsApplicableFailureClassesThroughTheInterface()
    {
        JsonReaderOptions readerOptions = new JsonReaderOptions();
        IJsonDocumentParser parser = new JsonDocumentParser();
        JsonDocumentResult parseResult = parser.TryParseUtf8(
            Bytes("[0,]"),
            readerOptions);
        JsonParseException parseException = Assert.Throws<JsonParseException>(() =>
            parser.ParseUtf8(Bytes("[0,]"), readerOptions))
            ?? throw new InvalidOperationException("Expected parse exception.");

        JsonLimits inputLimits = new JsonLimits();
        inputLimits.MaximumArrayItems = 1;
        JsonReaderOptions limitedReaderOptions = new JsonReaderOptions();
        limitedReaderOptions.Limits = inputLimits;
        JsonOperationResult validatorLimit = new JsonUtf8Validator().ValidateUtf8(
            Bytes("[0,1]"),
            limitedReaderOptions);

        IJsonValueFactory factory = new JsonValueFactory();
        JsonValueResult invalidNumber = factory.TryCreateNumber("01");
        JsonParseException numberFactoryException =
            Assert.Throws<JsonParseException>(() => factory.CreateNumber("01"))
            ?? throw new InvalidOperationException("Expected number exception.");
        JsonValue malformed = new JsonValue();
        JsonOperationResult invalidValue = new JsonValueValidator().ValidateValue(
            malformed,
            new JsonLimits());
        Assert.Throws<InvalidOperationException>(() =>
            new JsonValueComparer().CompareValues(
                malformed,
                factory.CreateNull(),
                JsonComparisonMode.ExactModel));

        IJsonDomNavigator dom = new JsonDomNavigator();
        JsonOperationResult domFailure = dom.AppendArrayItem(
            factory.CreateNull(),
            factory.CreateNull());
        Assert.Throws<JsonPointerException>(() =>
            dom.GetArrayItem(factory.CreateArray(new List<JsonValue>()), 0));

        IJsonNumberConverter numbers = new JsonNumberConverter();
        JsonNumber huge = factory.CreateNumber("1e400").Number
            ?? throw new InvalidOperationException("Expected number.");
        JsonDoubleResult conversionFailure = numbers.TryDouble(huge);
        Assert.Throws<JsonParseException>(() => numbers.ToDouble(huge));

        IJsonPointerCodec codec = new JsonPointerCodec();
        Assert.Throws<JsonPointerException>(() => codec.ParseStringPointer("bad"));
        JsonPointer malformedPointer = new JsonPointer();
        Assert.Throws<JsonPointerException>(() =>
            codec.FormatStringPointer(malformedPointer));
        IJsonPointerNavigator pointerNavigator = new JsonPointerNavigator();
        JsonPointer missingPointer = codec.ParseStringPointer("/missing");
        JsonPointerResult pointerFailure = pointerNavigator.TryResolvePointer(
            factory.CreateObject(new List<JsonMember>()),
            missingPointer,
            JsonDuplicateKeyPolicy.Preserve);
        Assert.Throws<JsonPointerException>(() =>
            pointerNavigator.ResolvePointer(
                factory.CreateObject(new List<JsonMember>()),
                missingPointer,
                JsonDuplicateKeyPolicy.Preserve));

        IJsonTraversal traversal = new JsonDepthFirstTraversal();
        ContractHandler pausedHandler = new ContractHandler();
        pausedHandler.PauseAtInvocation = 0;
        JsonOperationResult traversalFailure = traversal.PublishValueTokens(
            factory.CreateNull(),
            pausedHandler);
        Assert.Throws<ArgumentNullException>(() =>
            traversal.TraverseValues(factory.CreateNull(), null!));

        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        JsonDocument invalidDocument = new JsonDocument();
        JsonOperationResult serializationFailure = serializer.SerializeTarget(
            invalidDocument,
            new JsonWriterOptions(),
            BufferTarget());
        Assert.Throws<JsonWriteException>(() =>
            serializer.SerializeUtf8(invalidDocument, new JsonWriterOptions()));

        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();
        JsonOperationResult verificationFailure = canonicalizer.VerifyCanonicalUtf8(
            Bytes("{ \"a\" : 1 }"),
            readerOptions);
        JsonOperationResult canonicalFailure = canonicalizer.WriteCanonicalTarget(
            invalidDocument,
            BufferTarget());
        Assert.Throws<JsonWriteException>(() =>
            canonicalizer.CanonicalizeDocument(invalidDocument));

        IJsonPullReader pull = new JsonPullReader(readerOptions);
        pull.FeedUtf8(Bytes("[0,]"));
        JsonTokenResult pullFailure = ReadPullFailure(pull);
        ContractHandler pushHandler = new ContractHandler();
        IJsonPushParser push = new JsonPushParser(readerOptions, pushHandler);
        JsonFeedResult pushFailure = push.FeedPushUtf8(Bytes("[0,]"));

        IJsonTokenWriter writer = new JsonUtf8Writer(
            new JsonWriterOptions(),
            BufferTarget());
        JsonOperationResult stateFailure = writer.WriteEndArray();
        JsonOperationResult numberFailure = writer.WriteNumberLexeme("01");
        JsonOperationResult doubleFailure = writer.WriteDouble(double.NaN);
        JsonOperationResult rawFailure = writer.WriteRawValue(Bytes("[0,]"));

        Assert.Multiple(() =>
        {
            Assert.That(parseResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(parseException.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(validatorLimit.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.ArrayItemLimitExceeded));
            Assert.That(invalidNumber.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidNumber));
            Assert.That(numberFactoryException.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidNumber));
            Assert.That(invalidValue.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(domFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTypeMismatch));
            Assert.That(conversionFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.NumericConversionFailed));
            Assert.That(pointerFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(traversalFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
            Assert.That(serializationFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(verificationFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidRawValue));
            Assert.That(canonicalFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(pullFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(pushFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(stateFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidWriterState));
            Assert.That(numberFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidNumber));
            Assert.That(doubleFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.NonFiniteNumber));
            Assert.That(rawFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidRawValue));
        });
    }

    [Test]
    public void StreamAndOutputInterfacesExposeIoFailuresWithoutTakingOwnership()
    {
        JsonReaderOptions readerOptions = new JsonReaderOptions();
        readerOptions.StreamBufferBytes = 1;
        IJsonDocumentParser documentParser = new JsonDocumentParser();
        ThrowingReadStream tryParseStream = new ThrowingReadStream();
        ThrowingReadStream parseStream = new ThrowingReadStream();
        JsonDocumentResult parseResult = documentParser.TryParseStream(
            tryParseStream,
            readerOptions);
        JsonParseException parseException = Assert.Throws<JsonParseException>(() =>
            documentParser.ParseStream(parseStream, readerOptions))
            ?? throw new InvalidOperationException("Expected stream parse exception.");

        ThrowingReadStream validationStream = new ThrowingReadStream();
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        JsonOperationResult validation = validator.ValidateStream(
            validationStream,
            readerOptions);

        ThrowingReadStream pullStream = new ThrowingReadStream();
        IJsonPullReader pull = new JsonPullReader(readerOptions);
        JsonFeedResult pullFailure = pull.FeedStream(pullStream);

        ThrowingReadStream pushStream = new ThrowingReadStream();
        IJsonPushParser push = new JsonPushParser(
            readerOptions,
            new ContractHandler());
        JsonFeedResult pushFailure = push.FeedPushStream(pushStream);

        JsonDocument document = CreateDocument(CreateObjectValue());
        ThrowingWriteStream serializerStream = new ThrowingWriteStream();
        JsonWriterTarget serializerTarget = new JsonWriterTarget();
        serializerTarget.Stream = serializerStream;
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        JsonOperationResult serialization = serializer.SerializeTarget(
            document,
            new JsonWriterOptions(),
            serializerTarget);

        ThrowingWriteStream canonicalStream = new ThrowingWriteStream();
        JsonWriterTarget canonicalTarget = new JsonWriterTarget();
        canonicalTarget.Stream = canonicalStream;
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();
        JsonOperationResult canonical = canonicalizer.WriteCanonicalTarget(
            document,
            canonicalTarget);

        ThrowingWriteStream writerStream = new ThrowingWriteStream();
        JsonWriterTarget writerTarget = new JsonWriterTarget();
        writerTarget.Stream = writerStream;
        IJsonTokenWriter writer = new JsonUtf8Writer(
            new JsonWriterOptions(),
            writerTarget);
        AssertSuccess(writer.WriteNull());
        JsonOperationResult writerFailure = writer.CompleteWriter();

        Assert.Multiple(() =>
        {
            Assert.That(parseResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(parseException.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(validation.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(pullFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(pushFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(serialization.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(canonical.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(writerFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(tryParseStream.WasDisposed, Is.False);
            Assert.That(parseStream.WasDisposed, Is.False);
            Assert.That(validationStream.WasDisposed, Is.False);
            Assert.That(pullStream.WasDisposed, Is.False);
            Assert.That(pushStream.WasDisposed, Is.False);
            Assert.That(serializerStream.WasDisposed, Is.False);
            Assert.That(canonicalStream.WasDisposed, Is.False);
            Assert.That(writerStream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void ParsingCanonicalizationAndWritingInterfacesExposeConfiguredLimits()
    {
        JsonLimits inputLimits = new JsonLimits();
        inputLimits.MaximumDocumentBytes = 2;
        JsonReaderOptions inputOptions = new JsonReaderOptions();
        inputOptions.Limits = inputLimits;
        byte[] input = Bytes("[0]");

        IJsonDocumentParser documentParser = new JsonDocumentParser();
        JsonDocumentResult parseResult = documentParser.TryParseUtf8(
            input,
            inputOptions);
        JsonLimitException parseException = Assert.Throws<JsonLimitException>(() =>
            documentParser.ParseUtf8(input, inputOptions))
            ?? throw new InvalidOperationException("Expected input limit exception.");
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        JsonOperationResult validation = validator.ValidateUtf8(
            input,
            inputOptions);
        IJsonPullReader pull = new JsonPullReader(inputOptions);
        pull.FeedUtf8(input);
        JsonTokenResult pullFailure = ReadPullFailure(pull);
        IJsonPushParser push = new JsonPushParser(
            inputOptions,
            new ContractHandler());
        JsonFeedResult pushFailure = push.FeedPushUtf8(input);

        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();
        JsonOperationResult canonicalResult = canonicalizer.VerifyCanonicalUtf8(
            input,
            inputOptions);
        JsonLimitException canonicalException = Assert.Throws<JsonLimitException>(() =>
            canonicalizer.CanonicalizeUtf8(input, inputOptions))
            ?? throw new InvalidOperationException(
                "Expected canonical input limit exception.");

        JsonLimits outputLimits = new JsonLimits();
        outputLimits.MaximumOutputBytes = 3;
        JsonWriterOptions outputOptions = new JsonWriterOptions();
        outputOptions.Limits = outputLimits;
        JsonDocument outputDocument = CreateDocument(
            new JsonValueFactory().CreateString("long"));
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        JsonOperationResult serialization = serializer.SerializeTarget(
            outputDocument,
            outputOptions,
            BufferTarget());
        JsonLimitException serializationException =
            Assert.Throws<JsonLimitException>(() =>
                serializer.SerializeUtf8(outputDocument, outputOptions))
            ?? throw new InvalidOperationException(
                "Expected serialization limit exception.");
        IJsonTokenWriter writer = new JsonUtf8Writer(
            outputOptions,
            BufferTarget());
        JsonOperationResult writerFailure = writer.WriteString("long");

        Assert.Multiple(() =>
        {
            Assert.That(parseResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DocumentByteLimitExceeded));
            Assert.That(parseException.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.DocumentByteLimitExceeded));
            Assert.That(validation.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DocumentByteLimitExceeded));
            Assert.That(pullFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DocumentByteLimitExceeded));
            Assert.That(pushFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DocumentByteLimitExceeded));
            Assert.That(canonicalResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DocumentByteLimitExceeded));
            Assert.That(canonicalException.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.DocumentByteLimitExceeded));
            Assert.That(serialization.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
            Assert.That(serializationException.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
            Assert.That(writerFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
        });
    }

    static JsonValue CreateObjectValue()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        return factory.CreateObject(new List<JsonMember>
        {
            CreateMember("value", factory.CreateArray(new List<JsonValue>
            {
                factory.CreateNull(),
                factory.CreateBoolean(true),
                factory.CreateNumber("1")
            }))
        });
    }

    static JsonDocument CreateDocument(JsonValue root)
    {
        JsonDocument document = new JsonDocument();
        document.Root = root;
        return document;
    }

    static JsonMember CreateMember(string name, JsonValue value)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.Value = value;
        return member;
    }

    static JsonWriterTarget BufferTarget()
    {
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = new System.Buffers.ArrayBufferWriter<byte>();
        return target;
    }

    static byte[] Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    static string Text(byte[] utf8)
    {
        return System.Text.Encoding.UTF8.GetString(utf8);
    }

    static void AssertSuccess(JsonOperationResult result)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
    }

    static List<JsonTokenKind> DrainPull(IJsonPullReader reader)
    {
        List<JsonTokenKind> kinds = new List<JsonTokenKind>();
        int attempts = 0;
        while (true)
        {
            attempts++;
            Assert.That(attempts, Is.LessThan(100));
            JsonTokenResult read = reader.ReadToken();
            if (read.Status == JsonReadStatus.TokenAvailable)
            {
                kinds.Add(read.Token?.Kind
                    ?? throw new InvalidOperationException("Expected token."));
                continue;
            }

            if (read.Status == JsonReadStatus.DocumentComplete)
            {
                continue;
            }

            if (read.Status == JsonReadStatus.StreamComplete)
            {
                return kinds;
            }

            Assert.Fail("Unexpected pull status: " + read.Status);
        }
    }

    static JsonTokenResult ReadPullFailure(IJsonPullReader reader)
    {
        int attempts = 0;
        while (true)
        {
            attempts++;
            Assert.That(attempts, Is.LessThan(100));
            JsonTokenResult read = reader.ReadToken();
            if (read.Status == JsonReadStatus.Error)
            {
                return read;
            }

            Assert.That(read.Status, Is.EqualTo(JsonReadStatus.TokenAvailable));
        }
    }

    class ContractHandler : IJsonTokenHandler
    {
        internal List<JsonToken> Tokens = new List<JsonToken>();
        internal int PauseAtInvocation = -1;

        public JsonHandlerAction HandleToken(JsonToken token)
        {
            int invocation = Tokens.Count;
            Tokens.Add(token);
            if (invocation == PauseAtInvocation)
            {
                return JsonHandlerAction.Pause;
            }

            return JsonHandlerAction.Continue;
        }
    }

    class ContractVisitor : IJsonValueVisitor
    {
        internal int Visits;

        public JsonVisitAction VisitValue(JsonVisit visit)
        {
            Visits++;
            return JsonVisitAction.Continue;
        }
    }

    class ThrowingReadStream : Stream
    {
        internal bool WasDisposed;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => 0;

        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new IOException("Injected read failure.");
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

    class ThrowingWriteStream : Stream
    {
        internal bool WasDisposed;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => 0;

        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
            throw new IOException("Injected write failure.");
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
