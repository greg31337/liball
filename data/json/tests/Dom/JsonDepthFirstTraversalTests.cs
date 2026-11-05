namespace json.Tests;

public class JsonDepthFirstTraversalTests
{
    [Test]
    public void TraversalVisitsPreorderContextAndHonorsSkipChildren()
    {
        JsonSourceRange itemRange = new JsonSourceRange();
        JsonValue first = StringValue("first");
        first.Range = itemRange;
        JsonValue skipped = ObjectValue(Member("hidden", StringValue("hidden")));
        JsonValue third = StringValue("third");
        JsonValue items = ArrayValue(first, skipped, third);
        JsonValue tail = BooleanValue(true);
        JsonValue root = ObjectValue(
            Member("items", items),
            Member("tail", tail));
        RecordingVisitor visitor = new RecordingVisitor();
        visitor.SkipPointer = "/items/1";
        IJsonTraversal traversal = new JsonDepthFirstTraversal();

        JsonVisitAction result = traversal.TraverseValues(root, visitor);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(JsonVisitAction.Continue));
            Assert.That(visitor.Visits.Select(PointerOf), Is.EqualTo(new[]
            {
                "", "/items", "/items/0", "/items/1", "/items/2", "/tail"
            }));
            Assert.That(visitor.Visits.Select(visit => visit.Depth),
                Is.EqualTo(new[] { 0, 1, 2, 2, 2, 1 }));
            Assert.That(visitor.Visits[0].Parent, Is.Null);
            Assert.That(visitor.Visits[0].MemberName, Is.Null);
            Assert.That(visitor.Visits[0].ArrayIndex, Is.EqualTo(-1));
            Assert.That(visitor.Visits[1].Parent, Is.SameAs(root));
            Assert.That(visitor.Visits[1].MemberName, Is.EqualTo("items"));
            Assert.That(visitor.Visits[2].Parent, Is.SameAs(items));
            Assert.That(visitor.Visits[2].MemberName, Is.Null);
            Assert.That(visitor.Visits[2].ArrayIndex, Is.Zero);
            Assert.That(visitor.Visits[2].Range, Is.SameAs(itemRange));
            Assert.That(visitor.Visits[5].Parent, Is.SameAs(root));
            Assert.That(visitor.Visits[5].MemberName, Is.EqualTo("tail"));
        });
    }

    [Test]
    public void ParsedDuplicatesPreserveOrderRangesAndEscapedPointers()
    {
        const string jsonText =
            "{\"a/b~c\":{\"\":[0]},\"dup\":1,\"dup\":2}";
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Preserve;
        options.CaptureSourceRanges = true;
        JsonDocument document = new JsonDocumentParser().ParseString(
            jsonText,
            options);
        JsonValue root = document.Root
            ?? throw new InvalidOperationException("Expected a parsed root.");
        RecordingVisitor visitor = new RecordingVisitor();
        IJsonTraversal traversal = new JsonDepthFirstTraversal();

        JsonVisitAction result = traversal.TraverseValues(root, visitor);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(JsonVisitAction.Continue));
            Assert.That(visitor.Visits.Select(PointerOf), Is.EqualTo(new[]
            {
                "",
                "/a~1b~0c",
                "/a~1b~0c/",
                "/a~1b~0c//0",
                "/dup",
                "/dup"
            }));
            Assert.That(visitor.Visits.Select(visit => visit.Depth),
                Is.EqualTo(new[] { 0, 1, 2, 3, 1, 1 }));
            Assert.That(visitor.Visits.Select(visit => visit.MemberName),
                Is.EqualTo(new string?[]
                {
                    null, "a/b~c", "", null, "dup", "dup"
                }));
            Assert.That(visitor.Visits.Select(visit => visit.ArrayIndex),
                Is.EqualTo(new[] { -1, -1, -1, 0, -1, -1 }));
            Assert.That(visitor.Visits.All(visit => visit.Range != null), Is.True);
            Assert.That(visitor.Visits.All(visit =>
                ReferenceEquals(visit.Range, visit.Value?.Range)), Is.True);
            Assert.That(visitor.Visits[4].Value?.Number?.Lexeme, Is.EqualTo("1"));
            Assert.That(visitor.Visits[5].Value?.Number?.Lexeme, Is.EqualTo("2"));
        });
    }

    [Test]
    public void TraversalReturnsTheActionThatTerminatesVisiting()
    {
        JsonValue root = ArrayValue(
            StringValue("zero"),
            StringValue("one"),
            StringValue("two"));
        RecordingVisitor stopping = new RecordingVisitor();
        stopping.StopPointer = "/1";
        RecordingVisitor skippingRoot = new RecordingVisitor();
        skippingRoot.SkipPointer = "";
        IJsonTraversal traversal = new JsonDepthFirstTraversal();

        JsonVisitAction stopped = traversal.TraverseValues(root, stopping);
        JsonVisitAction skipped = traversal.TraverseValues(root, skippingRoot);

        Assert.Multiple(() =>
        {
            Assert.That(stopped, Is.EqualTo(JsonVisitAction.Stop));
            Assert.That(stopping.Visits.Select(PointerOf),
                Is.EqualTo(new[] { "", "/0", "/1" }));
            Assert.That(skipped, Is.EqualTo(JsonVisitAction.SkipChildren));
            Assert.That(skippingRoot.Visits, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void TokenPublicationMatchesDepthFirstParserTokenShape()
    {
        JsonSourceRange rootRange = new JsonSourceRange();
        JsonSourceRange nameRange = new JsonSourceRange();
        JsonSourceRange arrayRange = new JsonSourceRange();
        JsonSourceRange numberRange = new JsonSourceRange();
        JsonValue number = NumberValue("-1.25e2");
        number.Range = numberRange;
        JsonValue array = ArrayValue(number, BooleanValue(false));
        array.Range = arrayRange;
        JsonMember arrayMember = Member("a/b", array);
        arrayMember.NameRange = nameRange;
        JsonValue text = StringValue("value");
        JsonValue root = ObjectValue(arrayMember, Member("text", text));
        root.Range = rootRange;
        RecordingTokenHandler handler = new RecordingTokenHandler();
        IJsonTraversal traversal = new JsonDepthFirstTraversal();

        JsonOperationResult result = traversal.PublishValueTokens(root, handler);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartObject,
                JsonTokenKind.PropertyName,
                JsonTokenKind.StartArray,
                JsonTokenKind.Number,
                JsonTokenKind.Boolean,
                JsonTokenKind.EndArray,
                JsonTokenKind.PropertyName,
                JsonTokenKind.String,
                JsonTokenKind.EndObject
            }));
            Assert.That(handler.Tokens.Select(token => token.Depth),
                Is.EqualTo(new[] { 0, 1, 1, 2, 2, 1, 1, 1, 0 }));
            Assert.That(handler.Tokens.Select(TokenPointerOf), Is.EqualTo(new[]
            {
                "", "/a~1b", "/a~1b", "/a~1b/0", "/a~1b/1",
                "/a~1b", "/text", "/text", ""
            }));
            Assert.That(handler.Tokens[1].StringValue, Is.EqualTo("a/b"));
            Assert.That(handler.Tokens[3].Number, Is.SameAs(number.Number));
            Assert.That(handler.Tokens[3].Number?.Lexeme, Is.EqualTo("-1.25e2"));
            Assert.That(handler.Tokens[4].BooleanValue, Is.False);
            Assert.That(handler.Tokens[7].StringValue, Is.EqualTo("value"));
            Assert.That(handler.Tokens[0].Range, Is.SameAs(rootRange));
            Assert.That(handler.Tokens[1].Range, Is.SameAs(nameRange));
            Assert.That(handler.Tokens[2].Range, Is.SameAs(arrayRange));
            Assert.That(handler.Tokens[3].Range, Is.SameAs(numberRange));
            Assert.That(handler.Tokens[5].Range, Is.SameAs(arrayRange));
            Assert.That(handler.Tokens.All(token => token.RawUtf8 == null), Is.True);
        });
    }

    [Test]
    public void PublishedTokensAreSemanticallyEquivalentToPullParserTokens()
    {
        const string jsonText =
            "{\"a/b~\":[null,true,false,-1.2e3,\"x\"]," +
            "\"o\":{\"d\":0,\"d\":1}}";
        JsonReaderOptions parseOptions = new JsonReaderOptions();
        parseOptions.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Preserve;
        List<JsonToken> parsedTokens = ReadPullTokens(jsonText, parseOptions);
        JsonDocument document = new JsonDocumentParser().ParseString(
            jsonText,
            parseOptions);
        JsonValue root = document.Root
            ?? throw new InvalidOperationException("Expected a parsed root.");
        RecordingTokenHandler handler = new RecordingTokenHandler();
        IJsonTraversal traversal = new JsonDepthFirstTraversal();

        JsonOperationResult result = traversal.PublishValueTokens(root, handler);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(
                handler.Tokens.Select(CreateSemanticTokenSignature),
                Is.EqualTo(parsedTokens.Select(CreateSemanticTokenSignature)));
            Assert.That(
                handler.Tokens.Where(token => token.Kind == JsonTokenKind.PropertyName)
                    .Select(token => token.StringValue),
                Is.EqualTo(new[] { "a/b~", "o", "d", "d" }));
            Assert.That(
                handler.Tokens.Where(token => token.Kind == JsonTokenKind.PropertyName)
                    .Select(TokenPointerOf),
                Is.EqualTo(new[] { "/a~1b~0", "/o", "/o/d", "/o/d" }));
        });
    }

    [Test]
    public void TokenPublicationHonorsStopAndRejectsPause()
    {
        JsonValue root = ArrayValue(
            StringValue("zero"),
            StringValue("one"));
        RecordingTokenHandler stopping = new RecordingTokenHandler();
        stopping.StopAtInvocation = 1;
        RecordingTokenHandler pausing = new RecordingTokenHandler();
        pausing.PauseAtInvocation = 0;
        IJsonTraversal traversal = new JsonDepthFirstTraversal();

        JsonOperationResult stopped = traversal.PublishValueTokens(root, stopping);
        JsonOperationResult paused = traversal.PublishValueTokens(root, pausing);

        Assert.Multiple(() =>
        {
            Assert.That(stopped.Success, Is.True);
            Assert.That(stopping.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartArray,
                JsonTokenKind.String
            }));
            Assert.That(paused.Success, Is.False);
            Assert.That(paused.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
            Assert.That(pausing.Tokens, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void TraversalAndPublicationUseExplicitStacksForDeepGraphs()
    {
        const int depth = 1024;
        JsonValue root = NullValue();
        int index = 0;
        while (index < depth)
        {
            root = ArrayValue(root);
            index++;
        }

        RecordingVisitor visitor = new RecordingVisitor();
        RecordingTokenHandler handler = new RecordingTokenHandler();
        IJsonTraversal traversal = new JsonDepthFirstTraversal();

        JsonVisitAction visitResult = traversal.TraverseValues(root, visitor);
        JsonOperationResult tokenResult = traversal.PublishValueTokens(root, handler);

        Assert.Multiple(() =>
        {
            Assert.That(visitResult, Is.EqualTo(JsonVisitAction.Continue));
            Assert.That(visitor.Visits, Has.Count.EqualTo(depth + 1));
            Assert.That(visitor.Visits[^1].Depth, Is.EqualTo(depth));
            Assert.That(tokenResult.Success, Is.True);
            Assert.That(handler.Tokens, Has.Count.EqualTo((depth * 2) + 1));
            Assert.That(handler.Tokens[depth].Kind, Is.EqualTo(JsonTokenKind.Null));
            Assert.That(handler.Tokens[depth].Depth, Is.EqualTo(depth));
        });
    }

    [Test]
    public void PublicationDiagnosesCyclesAndNullArrayItems()
    {
        JsonValue cyclic = ArrayValue();
        cyclic.ArrayItems!.Add(cyclic);
        JsonValue nullItem = ArrayValue();
        nullItem.ArrayItems!.Add(null!);
        RecordingTokenHandler cycleHandler = new RecordingTokenHandler();
        RecordingTokenHandler nullHandler = new RecordingTokenHandler();
        IJsonTraversal traversal = new JsonDepthFirstTraversal();

        JsonOperationResult cycle = traversal.PublishValueTokens(cyclic, cycleHandler);
        JsonOperationResult invalid = traversal.PublishValueTokens(nullItem, nullHandler);

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Success, Is.False);
            Assert.That(cycle.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.ModelCycle));
            Assert.That(cycle.Diagnostic?.Pointer, Is.EqualTo("/0"));
            Assert.That(cycle.Diagnostic?.Depth, Is.EqualTo(1));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(invalid.Diagnostic?.Pointer, Is.EqualTo("/0"));
        });
    }

    static string PointerOf(JsonVisit visit)
    {
        return visit.Pointer?.OriginalRepresentation ?? "<null>";
    }

    static string TokenPointerOf(JsonToken token)
    {
        return token.Pointer?.OriginalRepresentation ?? "<null>";
    }

    static string CreateSemanticTokenSignature(JsonToken token)
    {
        return token.Kind + "|" +
            (token.StringValue ?? "<null>") + "|" +
            (token.Number?.Lexeme ?? "<null>") + "|" +
            token.BooleanValue + "|" +
            token.Depth + "|" +
            TokenPointerOf(token);
    }

    static List<JsonToken> ReadPullTokens(
        string jsonText,
        JsonReaderOptions options)
    {
        IJsonPullReader reader = new JsonPullReader(options);
        reader.FeedUtf8(System.Text.Encoding.UTF8.GetBytes(jsonText));
        reader.CompleteInput();
        List<JsonToken> tokens = new List<JsonToken>();
        while (true)
        {
            JsonTokenResult read = reader.ReadToken();
            if (read.Status == JsonReadStatus.TokenAvailable)
            {
                JsonToken token = read.Token
                    ?? throw new InvalidOperationException("Expected a pull token.");
                tokens.Add(token);
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

            throw new InvalidOperationException(
                "Unexpected pull-reader status: " + read.Status);
        }
    }

    static JsonMember Member(string name, JsonValue value)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.Value = value;
        return member;
    }

    static JsonValue ObjectValue(params JsonMember[] members)
    {
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Object;
        value.ObjectMembers = new List<JsonMember>(members);
        return value;
    }

    static JsonValue ArrayValue(params JsonValue[] items)
    {
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Array;
        value.ArrayItems = new List<JsonValue>(items);
        return value;
    }

    static JsonValue StringValue(string text)
    {
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.String;
        value.StringValue = text;
        return value;
    }

    static JsonValue BooleanValue(bool booleanValue)
    {
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Boolean;
        value.BooleanValue = booleanValue;
        return value;
    }

    static JsonValue NumberValue(string lexeme)
    {
        return new JsonValueFactory().CreateNumber(lexeme);
    }

    static JsonValue NullValue()
    {
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Null;
        return value;
    }

    class RecordingVisitor : IJsonValueVisitor
    {
        internal List<JsonVisit> Visits = new List<JsonVisit>();
        internal string? SkipPointer;
        internal string? StopPointer;

        public JsonVisitAction VisitValue(JsonVisit visit)
        {
            Visits.Add(visit);
            string pointer = PointerOf(visit);
            if (pointer == StopPointer)
            {
                return JsonVisitAction.Stop;
            }

            if (pointer == SkipPointer)
            {
                return JsonVisitAction.SkipChildren;
            }

            return JsonVisitAction.Continue;
        }
    }

    class RecordingTokenHandler : IJsonTokenHandler
    {
        internal List<JsonToken> Tokens = new List<JsonToken>();
        internal int StopAtInvocation = -1;
        internal int PauseAtInvocation = -1;

        public JsonHandlerAction HandleToken(JsonToken token)
        {
            int invocation = Tokens.Count;
            Tokens.Add(token);
            if (invocation == StopAtInvocation)
            {
                return JsonHandlerAction.Stop;
            }

            if (invocation == PauseAtInvocation)
            {
                return JsonHandlerAction.Pause;
            }

            return JsonHandlerAction.Continue;
        }
    }
}
