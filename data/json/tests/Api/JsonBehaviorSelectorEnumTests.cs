namespace json.Tests;

public class JsonBehaviorSelectorEnumTests
{
    [TestCase("1", JsonNumberKind.Integer)]
    [TestCase("1.0", JsonNumberKind.Fraction)]
    [TestCase("1e0", JsonNumberKind.Exponent)]
    [TestCase("1.0e0", JsonNumberKind.FractionWithExponent)]
    public void EveryNumberKindClassifiesItsLexicalForm(
        string lexeme,
        JsonNumberKind expected)
    {
        JsonValue value = new JsonValueFactory().CreateNumber(lexeme);

        Assert.That(value.Number?.Kind, Is.EqualTo(expected));
    }

    [Test]
    public void ComparisonModesSelectExactOrJsonSemanticEquality()
    {
        JsonValue integer = new JsonValueFactory().CreateNumber("1");
        JsonValue fraction = new JsonValueFactory().CreateNumber("1.0");
        IJsonValueComparer comparer = new JsonValueComparer();

        Assert.Multiple(() =>
        {
            Assert.That(comparer.CompareValues(
                integer,
                fraction,
                JsonComparisonMode.ExactModel), Is.False);
            Assert.That(comparer.CompareValues(
                integer,
                fraction,
                JsonComparisonMode.JsonSemantic), Is.True);
        });
    }

    [Test]
    public void PointerEncodingsSelectStringOrUriFragmentRepresentation()
    {
        IJsonPointerCodec codec = new JsonPointerCodec();
        JsonPointer text = codec.ParseStringPointer("/a b/~0~1");
        JsonPointer fragment = codec.ParseUriFragment("#/a%20b/~0~1");

        Assert.Multiple(() =>
        {
            Assert.That(text.Encoding, Is.EqualTo(JsonPointerEncoding.String));
            Assert.That(fragment.Encoding,
                Is.EqualTo(JsonPointerEncoding.UriFragment));
            Assert.That(text.ReferenceTokens,
                Is.EqualTo(new[] { "a b", "~/" }));
            Assert.That(fragment.ReferenceTokens,
                Is.EqualTo(text.ReferenceTokens));
            Assert.That(codec.FormatStringPointer(fragment),
                Is.EqualTo("/a b/~0~1"));
            Assert.That(codec.FormatUriFragment(text),
                Is.EqualTo("#/a%20b/~0~1"));
        });
    }

    [Test]
    public void HandlerActionsSelectContinuationPauseOrStop()
    {
        ActionHandler continuingHandler = new ActionHandler(JsonHandlerAction.Continue);
        IJsonPushParser continuing = new JsonPushParser(
            new JsonReaderOptions(),
            continuingHandler);
        JsonFeedResult continued = continuing.FeedPushUtf8(Bytes("[0]"));
        JsonFeedResult completed = continuing.CompletePushInput();

        ActionHandler pausingHandler = new ActionHandler(JsonHandlerAction.Pause);
        IJsonPushParser pausing = new JsonPushParser(
            new JsonReaderOptions(),
            pausingHandler);
        JsonFeedResult paused = pausing.FeedPushUtf8(Bytes("[0]"));

        ActionHandler stoppingHandler = new ActionHandler(JsonHandlerAction.Stop);
        IJsonPushParser stopping = new JsonPushParser(
            new JsonReaderOptions(),
            stoppingHandler);
        JsonFeedResult stopped = stopping.FeedPushUtf8(Bytes("[0]"));

        Assert.Multiple(() =>
        {
            Assert.That(continued.ReadStatus,
                Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(completed.ReadStatus,
                Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(continuingHandler.Count, Is.EqualTo(4));
            Assert.That(paused.ReadStatus, Is.EqualTo(JsonReadStatus.Paused));
            Assert.That(pausingHandler.Count, Is.EqualTo(1));
            Assert.That(stopped.ReadStatus, Is.EqualTo(JsonReadStatus.Stopped));
            Assert.That(stoppingHandler.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void VisitActionsSelectTraversalContinuationSkippingOrStop()
    {
        JsonValue root = new JsonValueFactory().CreateArray(new List<JsonValue>
        {
            new JsonValueFactory().CreateArray(new List<JsonValue>
            {
                new JsonValueFactory().CreateNull()
            }),
            new JsonValueFactory().CreateBoolean(true)
        });
        IJsonTraversal traversal = new JsonDepthFirstTraversal();
        ActionVisitor continuing = new ActionVisitor(JsonVisitAction.Continue);
        ActionVisitor skipping = new ActionVisitor(JsonVisitAction.SkipChildren);
        ActionVisitor stopping = new ActionVisitor(JsonVisitAction.Stop);

        JsonVisitAction continued = traversal.TraverseValues(root, continuing);
        JsonVisitAction skipped = traversal.TraverseValues(root, skipping);
        JsonVisitAction stopped = traversal.TraverseValues(root, stopping);

        Assert.Multiple(() =>
        {
            Assert.That(continued, Is.EqualTo(JsonVisitAction.Continue));
            Assert.That(continuing.Count, Is.EqualTo(4));
            Assert.That(skipped, Is.EqualTo(JsonVisitAction.SkipChildren));
            Assert.That(skipping.Count, Is.EqualTo(1));
            Assert.That(stopped, Is.EqualTo(JsonVisitAction.Stop));
            Assert.That(stopping.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void EveryValueKindSelectsItsSerializedModelBehavior()
    {
        JsonValueFactory factory = new JsonValueFactory();
        List<JsonValue> values = new List<JsonValue>
        {
            new JsonValue(),
            factory.CreateNull(),
            factory.CreateBoolean(true),
            factory.CreateNumber("1"),
            factory.CreateString("s"),
            factory.CreateArray(new List<JsonValue>()),
            factory.CreateObject(new List<JsonMember>())
        };
        string[] expected = new[]
        {
            "", "null", "true", "1", "\"s\"", "[]", "{}"
        };

        int index = 0;
        while (index < values.Count)
        {
            JsonValue value = values[index];
            JsonDocument document = new JsonDocument();
            document.Root = value;
            System.Buffers.ArrayBufferWriter<byte> destination =
                new System.Buffers.ArrayBufferWriter<byte>();
            JsonWriterTarget target = new JsonWriterTarget();
            target.BufferWriter = destination;
            JsonOperationResult result = new JsonDocumentSerializer().SerializeTarget(
                document,
                new JsonWriterOptions(),
                target);

            Assert.That(value.Kind, Is.EqualTo((JsonValueKind)index));
            if (value.Kind == JsonValueKind.Uninitialized)
            {
                Assert.That(result.Diagnostic?.Code,
                    Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            }
            else
            {
                Assert.That(result.Success, Is.True);
                Assert.That(Text(destination.WrittenSpan.ToArray()),
                    Is.EqualTo(expected[index]));
            }

            index++;
        }
    }

    [Test]
    public void EveryTokenKindIsProducedByItsLexicalRole()
    {
        CollectingHandler handler = new CollectingHandler();
        IJsonPushParser parser = new JsonPushParser(
            new JsonReaderOptions(),
            handler);

        parser.FeedPushUtf8(Bytes("{\"p\":[null,true,0,\"s\"]}"));
        JsonFeedResult completed = parser.CompletePushInput();
        List<JsonTokenKind> kinds = handler.Kinds;

        Assert.Multiple(() =>
        {
            Assert.That(new JsonToken().Kind, Is.EqualTo(JsonTokenKind.None));
            Assert.That(completed.ReadStatus,
                Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(kinds, Is.EqualTo(new[]
            {
                JsonTokenKind.StartObject,
                JsonTokenKind.PropertyName,
                JsonTokenKind.StartArray,
                JsonTokenKind.Null,
                JsonTokenKind.Boolean,
                JsonTokenKind.Number,
                JsonTokenKind.String,
                JsonTokenKind.EndArray,
                JsonTokenKind.EndObject,
                JsonTokenKind.DocumentBoundary
            }));
        });
    }

    [Test]
    public void EveryReadStatusDescribesAnObservableParserState()
    {
        IJsonPullReader waiting = new JsonPullReader(new JsonReaderOptions());
        JsonReadStatus needMore = waiting.PeekToken().Status;

        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());
        JsonReadStatus tokenAvailable = reader.FeedUtf8(Bytes("null ")).ReadStatus;
        reader.CompleteInput();
        reader.ReadToken();
        JsonReadStatus documentComplete = reader.ReadToken().Status;
        JsonReadStatus streamComplete = reader.ReadToken().Status;

        ActionHandler pausingHandler = new ActionHandler(JsonHandlerAction.Pause);
        JsonReadStatus paused = new JsonPushParser(
            new JsonReaderOptions(),
            pausingHandler).FeedPushUtf8(Bytes("null ")).ReadStatus;
        ActionHandler stoppingHandler = new ActionHandler(JsonHandlerAction.Stop);
        JsonReadStatus stopped = new JsonPushParser(
            new JsonReaderOptions(),
            stoppingHandler).FeedPushUtf8(Bytes("null ")).ReadStatus;
        JsonReadStatus error = new JsonPullReader(new JsonReaderOptions())
            .FeedUtf8(Bytes("x")).ReadStatus;

        Assert.That(new[]
        {
            tokenAvailable,
            needMore,
            documentComplete,
            streamComplete,
            paused,
            stopped,
            error
        }, Is.EqualTo(new[]
        {
            JsonReadStatus.TokenAvailable,
            JsonReadStatus.NeedMoreData,
            JsonReadStatus.DocumentComplete,
            JsonReadStatus.StreamComplete,
            JsonReadStatus.Paused,
            JsonReadStatus.Stopped,
            JsonReadStatus.Error
        }));
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return System.Text.Encoding.UTF8.GetString(value);
    }

    class ActionHandler : IJsonTokenHandler
    {
        readonly JsonHandlerAction action;

        internal int Count;

        internal ActionHandler(JsonHandlerAction action)
        {
            this.action = action;
        }

        public JsonHandlerAction HandleToken(JsonToken token)
        {
            Count++;
            return action;
        }
    }

    class ActionVisitor : IJsonValueVisitor
    {
        readonly JsonVisitAction action;

        internal int Count;

        internal ActionVisitor(JsonVisitAction action)
        {
            this.action = action;
        }

        public JsonVisitAction VisitValue(JsonVisit visit)
        {
            Count++;
            return action;
        }
    }

    class CollectingHandler : IJsonTokenHandler
    {
        internal List<JsonTokenKind> Kinds = new List<JsonTokenKind>();

        public JsonHandlerAction HandleToken(JsonToken token)
        {
            Kinds.Add(token.Kind);
            return JsonHandlerAction.Continue;
        }
    }
}
