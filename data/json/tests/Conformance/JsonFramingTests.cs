namespace json.Tests;

public class JsonFramingTests
{
    [TestCase("null", JsonTokenKind.Null)]
    [TestCase("true", JsonTokenKind.Boolean)]
    [TestCase("0", JsonTokenKind.Number)]
    [TestCase("\"text\"", JsonTokenKind.String)]
    [TestCase("[]", JsonTokenKind.StartArray)]
    [TestCase("{}", JsonTokenKind.StartObject)]
    public void SingleValueModePublishesExactlyOneDocumentBoundary(
        string jsonText,
        JsonTokenKind firstTokenKind)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.SingleValue;

        JsonParserCore parser = CreateParser(" \t" + jsonText + "\r\n", options);
        List<ObservedEvent> events = ReadAll(parser);

        Assert.Multiple(() =>
        {
            Assert.That(events[0].Status, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(events[0].Token?.Kind, Is.EqualTo(firstTokenKind));
            Assert.That(events.Count(item => item.Status == JsonReadStatus.DocumentComplete), Is.EqualTo(1));
            Assert.That(events[^2].Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
            Assert.That(events[^1].Status, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(parser.GetCompletedDocumentCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void WhitespaceSeparatedModePublishesEveryBoundaryAndRequiresASeparator()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.WhitespaceSeparated;
        JsonParserCore parser = CreateParser("0 1\t2\n3\r4", options);

        List<ObservedEvent> events = ReadAll(parser);

        Assert.Multiple(() =>
        {
            Assert.That(
                events.Select(item => item.Status),
                Is.EqualTo(new[]
                {
                    JsonReadStatus.TokenAvailable, JsonReadStatus.DocumentComplete,
                    JsonReadStatus.TokenAvailable, JsonReadStatus.DocumentComplete,
                    JsonReadStatus.TokenAvailable, JsonReadStatus.DocumentComplete,
                    JsonReadStatus.TokenAvailable, JsonReadStatus.DocumentComplete,
                    JsonReadStatus.TokenAvailable, JsonReadStatus.DocumentComplete,
                    JsonReadStatus.StreamComplete
                }));
            Assert.That(
                events.Where(item => item.Token != null).Select(item => item.Token?.Pointer?.OriginalRepresentation),
                Is.All.EqualTo(""));
            Assert.That(parser.GetCompletedDocumentCount(), Is.EqualTo(5));
        });

        JsonParserCore empty = CreateParser(" \t\r\n", options);
        Assert.That(ReadAll(empty).Select(item => item.Status), Is.EqualTo(new[]
        {
            JsonReadStatus.StreamComplete
        }));

        JsonParserCore missingSeparator = CreateParser("{}[]", options);
        JsonParseException exception = Assert.Throws<JsonParseException>(() => ReadAll(missingSeparator))
            ?? throw new InvalidOperationException("Expected JsonParseException.");
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedToken));
    }

    [Test]
    public void JsonTextSequenceAcceptsOptionalLineFeedsAndPublishesRecordBoundaries()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.JsonTextSequence;
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        byte[] bytes = new byte[]
        {
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginObject,
            JsonSyntaxBytes.EndObject,
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginArray,
            JsonSyntaxBytes.EndArray,
            JsonSyntaxBytes.LineFeed,
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.StringDelimiter,
            0x78,
            JsonSyntaxBytes.StringDelimiter,
            JsonSyntaxBytes.RecordSeparator,
            0x31,
            JsonSyntaxBytes.LineFeed
        };

        JsonParserCore parser = CreateParser(bytes, options);
        List<ObservedEvent> events = ReadAll(parser);
        List<JsonToken> tokens = events
            .Where(item => item.Token != null)
            .Select(item => item.Token ?? throw new InvalidOperationException("Expected token."))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(events.Count(item => item.Status == JsonReadStatus.DocumentComplete), Is.EqualTo(4));
            Assert.That(events[^1].Status, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartObject,
                JsonTokenKind.EndObject,
                JsonTokenKind.StartArray,
                JsonTokenKind.EndArray,
                JsonTokenKind.String,
                JsonTokenKind.Number
            }));
            Assert.That(tokens.Select(token => token.Range?.Start?.ByteOffset), Is.EqualTo(new long[]
            {
                1, 2, 4, 5, 8, 12
            }));
            Assert.That(parser.GetCompletedDocumentCount(), Is.EqualTo(4));
            Assert.That(parser.GetAcceptedByteCount(), Is.EqualTo(bytes.Length));
            Assert.That(parser.GetConsumedByteCount(), Is.EqualTo(bytes.Length));
        });
    }

    [Test]
    public void JsonTextSequenceAllowsConsecutiveSeparatorsButRejectsWhitespaceOnlyRecords()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.JsonTextSequence;
        JsonParserCore emptyRecords = CreateParser(new byte[]
        {
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.RecordSeparator
        }, options);

        Assert.That(ReadAll(emptyRecords).Select(item => item.Status), Is.EqualTo(new[]
        {
            JsonReadStatus.StreamComplete
        }));

        JsonParserCore whitespaceOnly = CreateParser(new byte[]
        {
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.Space,
            JsonSyntaxBytes.RecordSeparator
        }, options);
        JsonParseException exception = Assert.Throws<JsonParseException>(() => ReadAll(whitespaceOnly))
            ?? throw new InvalidOperationException("Expected JsonParseException.");
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.EmptySequenceRecord));
    }

    [TestCase("[0")]
    [TestCase("true")]
    [TestCase("123")]
    public void StopPolicyRejectsIncompleteSequenceRecords(string record)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.JsonTextSequence;
        byte[] recordBytes = System.Text.Encoding.ASCII.GetBytes(record);
        byte[] bytes = new byte[recordBytes.Length + 1];
        bytes[0] = JsonSyntaxBytes.RecordSeparator;
        Array.Copy(recordBytes, 0, bytes, 1, recordBytes.Length);
        JsonParserCore parser = CreateParser(bytes, options);

        JsonParseException exception = Assert.Throws<JsonParseException>(() => ReadAll(parser))
            ?? throw new InvalidOperationException("Expected JsonParseException.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.IncompleteSequenceRecord));
            Assert.That(parser.GetCompletedDocumentCount(), Is.Zero);
        });
    }

    [Test]
    public void StopPolicyRejectsMalformedContentAfterACompleteRecord()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.JsonTextSequence;
        JsonParserCore parser = CreateParser(new byte[]
        {
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginObject,
            JsonSyntaxBytes.EndObject,
            0x78
        }, options);

        JsonParseException exception = Assert.Throws<JsonParseException>(() => ReadAll(parser))
            ?? throw new InvalidOperationException("Expected JsonParseException.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedToken));
            Assert.That(parser.GetCompletedDocumentCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ResynchronizeReportsBadRecordsAndContinuesAtRecordSeparators()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.JsonTextSequence;
        options.SequenceErrorPolicy = JsonSequenceErrorPolicy.Resynchronize;
        byte[] bytes = new byte[]
        {
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginArray,
            0x30,
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginObject,
            JsonSyntaxBytes.EndObject,
            0x78,
            0x79,
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginArray,
            JsonSyntaxBytes.EndArray,
            JsonSyntaxBytes.LineFeed
        };
        JsonParserCore parser = CreateParser(bytes, options);

        List<ObservedEvent> events = ReadAll(parser);
        List<ObservedEvent> errors = events
            .Where(item => item.Status == JsonReadStatus.Error)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(errors.Select(item => item.Diagnostic?.Code), Is.EqualTo(new[]
            {
                JsonDiagnosticCode.IncompleteSequenceRecord,
                JsonDiagnosticCode.UnexpectedToken
            }));
            Assert.That(errors[0].Diagnostic?.Pointer, Is.EqualTo("/0"));
            Assert.That(errors[0].Diagnostic?.Depth, Is.EqualTo(1));
            Assert.That(events.Count(item => item.Status == JsonReadStatus.DocumentComplete), Is.EqualTo(2));
            Assert.That(
                events.Where(item => item.Token != null).Select(item => item.Token?.Kind),
                Is.EqualTo(new[]
                {
                    JsonTokenKind.StartArray,
                    JsonTokenKind.StartObject,
                    JsonTokenKind.EndObject,
                    JsonTokenKind.StartArray,
                    JsonTokenKind.EndArray
                }));
            Assert.That(events[^1].Status, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(parser.GetCompletedDocumentCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void RecoveryResetsPointersWhileLocationsRemainAbsoluteAcrossRecords()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.JsonTextSequence;
        options.SequenceErrorPolicy = JsonSequenceErrorPolicy.Resynchronize;
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        byte[] bytes = new byte[]
        {
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginArray,
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginObject,
            JsonSyntaxBytes.EndObject,
            JsonSyntaxBytes.LineFeed,
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginArray,
            0x30,
            JsonSyntaxBytes.EndArray
        };
        JsonParserCore parser = CreateParser(bytes, options);

        List<ObservedEvent> events = ReadAll(parser);
        List<JsonToken> tokens = events
            .Where(item => item.Token != null)
            .Select(item => item.Token ?? throw new InvalidOperationException("Expected token."))
            .ToList();
        JsonDiagnostic error = events
            .Single(item => item.Status == JsonReadStatus.Error)
            .Diagnostic ?? throw new InvalidOperationException("Expected diagnostic.");

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartArray,
                JsonTokenKind.StartObject,
                JsonTokenKind.EndObject,
                JsonTokenKind.StartArray,
                JsonTokenKind.Number,
                JsonTokenKind.EndArray
            }));
            Assert.That(tokens.Select(token => token.Pointer?.OriginalRepresentation), Is.EqualTo(new[]
            {
                "", "", "", "", "/0", ""
            }));
            Assert.That(tokens.Select(token => token.Depth), Is.EqualTo(new[]
            {
                0, 0, 0, 0, 1, 0
            }));
            Assert.That(tokens.Select(token => token.Range?.Start?.ByteOffset), Is.EqualTo(new long[]
            {
                1, 3, 4, 7, 8, 9
            }));
            Assert.That(tokens[3].Range?.Start?.Line, Is.EqualTo(2));
            Assert.That(tokens[3].Range?.Start?.ByteColumn, Is.EqualTo(2));
            Assert.That(tokens[3].Range?.Start?.ScalarColumn, Is.EqualTo(2));
            Assert.That(error.Code, Is.EqualTo(JsonDiagnosticCode.IncompleteSequenceRecord));
            Assert.That(error.Pointer, Is.EqualTo("/0"));
            Assert.That(error.Range?.Start?.ByteOffset, Is.EqualTo(2));
            Assert.That(events.Count(item => item.Status == JsonReadStatus.DocumentComplete), Is.EqualTo(2));
        });
    }

    static JsonParserCore CreateParser(string text, JsonReaderOptions options)
    {
        return CreateParser(System.Text.Encoding.ASCII.GetBytes(text), options);
    }

    static JsonParserCore CreateParser(byte[] bytes, JsonReaderOptions options)
    {
        JsonParserCore parser = new JsonParserCore(options);
        parser.FeedBytes(bytes);
        return parser;
    }

    static List<ObservedEvent> ReadAll(JsonParserCore parser)
    {
        List<ObservedEvent> events = new List<ObservedEvent>();
        int eventCount = 0;
        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(true);
            ObservedEvent observed = new ObservedEvent();
            observed.Status = status;
            observed.Token = parser.GetCurrentToken();
            observed.Diagnostic = parser.GetCurrentDiagnostic();
            events.Add(observed);
            eventCount++;
            Assert.That(eventCount, Is.LessThan(1000));
            if (status == JsonReadStatus.StreamComplete)
            {
                return events;
            }
        }
    }

    class ObservedEvent
    {
        public JsonReadStatus Status;
        public JsonToken? Token;
        public JsonDiagnostic? Diagnostic;
    }
}
