namespace json.Tests;

public class JsonParserCoreTests
{
    [Test]
    public void ParserPublishesNestedTokensWithDepthPointersAndPropertyRoles()
    {
        string jsonText = "{\"a/b\":[1,true,{\"x\":null}],\"s\":\"v\"}";
        JsonParserCore parser = new JsonParserCore(null);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonText);
        parser.FeedBytes(bytes);

        List<JsonToken> tokens = new List<JsonToken>();
        List<JsonDuplicateDecision?> decisions = new List<JsonDuplicateDecision?>();
        int documentBoundaries = 0;
        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(true);
            if (status == JsonReadStatus.TokenAvailable)
            {
                tokens.Add(parser.GetCurrentToken()
                    ?? throw new InvalidOperationException("Expected token."));
                decisions.Add(parser.GetCurrentDuplicateDecision());
                continue;
            }

            if (status == JsonReadStatus.DocumentComplete)
            {
                documentBoundaries++;
                continue;
            }

            Assert.That(status, Is.EqualTo(JsonReadStatus.StreamComplete));
            break;
        }

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartObject,
                JsonTokenKind.PropertyName,
                JsonTokenKind.StartArray,
                JsonTokenKind.Number,
                JsonTokenKind.Boolean,
                JsonTokenKind.StartObject,
                JsonTokenKind.PropertyName,
                JsonTokenKind.Null,
                JsonTokenKind.EndObject,
                JsonTokenKind.EndArray,
                JsonTokenKind.PropertyName,
                JsonTokenKind.String,
                JsonTokenKind.EndObject
            }));
            Assert.That(tokens.Select(token => token.Depth), Is.EqualTo(new[]
            {
                0, 1, 1, 2, 2, 2, 3, 3, 2, 1, 1, 1, 0
            }));
            Assert.That(tokens.Select(token => token.Pointer?.OriginalRepresentation), Is.EqualTo(new[]
            {
                "", "/a~1b", "/a~1b", "/a~1b/0", "/a~1b/1", "/a~1b/2",
                "/a~1b/2/x", "/a~1b/2/x", "/a~1b/2", "/a~1b", "/s", "/s", ""
            }));
            Assert.That(tokens[1].StringValue, Is.EqualTo("a/b"));
            Assert.That(tokens[6].StringValue, Is.EqualTo("x"));
            Assert.That(tokens[10].StringValue, Is.EqualTo("s"));
            Assert.That(decisions[1]?.Kind, Is.EqualTo(JsonDuplicateDecisionKind.Append));
            Assert.That(decisions[6]?.Kind, Is.EqualTo(JsonDuplicateDecisionKind.Append));
            Assert.That(decisions[10]?.Kind, Is.EqualTo(JsonDuplicateDecisionKind.Append));
            Assert.That(decisions.Count(decision => decision != null), Is.EqualTo(3));
            Assert.That(documentBoundaries, Is.EqualTo(1));
            Assert.That(parser.GetCompletedDocumentCount(), Is.EqualTo(1));
            Assert.That(parser.GetAcceptedByteCount(), Is.EqualTo(bytes.Length));
            Assert.That(parser.GetConsumedByteCount(), Is.EqualTo(bytes.Length));
        });
    }

    [Test]
    public void EveryByteIncrementalFeedsMatchOneShotEvents()
    {
        string jsonText = " [\"€\",true,{\"a/b\":-1.2e3}] \r\n";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonText);
        List<string> expected = CollectOneShotEvents(bytes, null);
        JsonParserCore incremental = new JsonParserCore(null);
        List<string> actual = new List<string>();

        for (int index = 0; index < bytes.Length; index++)
        {
            incremental.FeedBytes(new byte[] { bytes[index] });
            DrainAvailableEvents(incremental, false, actual);
        }

        DrainFinalEvents(incremental, actual);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(JsonDuplicateKeyPolicy.Preserve, 0)]
    [TestCase(JsonDuplicateKeyPolicy.KeepFirst, 1)]
    [TestCase(JsonDuplicateKeyPolicy.KeepLast, 2)]
    public void ParserAppliesDuplicateMaterializationDecisions(
        JsonDuplicateKeyPolicy policy,
        int expectedDuplicateDecisionValue)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = policy;
        JsonParserCore parser = new JsonParserCore(options);
        parser.FeedBytes(System.Text.Encoding.ASCII.GetBytes("{\"a\":0,\"\\u0061\":1}"));
        List<JsonDuplicateDecision> decisions = new List<JsonDuplicateDecision>();

        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(true);
            if (status == JsonReadStatus.TokenAvailable && parser.GetCurrentDuplicateDecision() != null)
            {
                decisions.Add(parser.GetCurrentDuplicateDecision()
                    ?? throw new InvalidOperationException("Expected duplicate decision."));
            }

            if (status == JsonReadStatus.StreamComplete)
            {
                break;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(decisions, Has.Count.EqualTo(2));
            Assert.That(decisions[0].Kind, Is.EqualTo(JsonDuplicateDecisionKind.Append));
            Assert.That(decisions[0].IsDuplicate, Is.False);
            Assert.That(decisions[1].Kind, Is.EqualTo((JsonDuplicateDecisionKind)expectedDuplicateDecisionValue));
            Assert.That(decisions[1].IsDuplicate, Is.True);
            Assert.That(decisions[1].MemberIndex, Is.EqualTo(
                policy == JsonDuplicateKeyPolicy.Preserve ? 1 : 0));
        });
    }

    [Test]
    public void RejectDuplicatePolicyUsesDecodedNamesAndParserCanResetAfterFailure()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        JsonParserCore parser = new JsonParserCore(options);
        parser.FeedBytes(System.Text.Encoding.ASCII.GetBytes("{\"a\":0,\"\\u0061\":1}"));

        JsonParseException exception = Assert.Throws<JsonParseException>(() => ReadUntilStreamComplete(parser))
            ?? throw new InvalidOperationException("Expected JsonParseException.");
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(exception.Diagnostic.ActualToken, Is.EqualTo("a"));
        });

        parser.ResetParser();
        Assert.Multiple(() =>
        {
            Assert.That(parser.GetAcceptedByteCount(), Is.Zero);
            Assert.That(parser.GetConsumedByteCount(), Is.Zero);
            Assert.That(parser.GetCompletedDocumentCount(), Is.Zero);
        });

        parser.FeedBytes(System.Text.Encoding.ASCII.GetBytes("null"));
        List<string> events = new List<string>();
        DrainFinalEvents(parser, events);
        Assert.That(events, Is.EqualTo(new[]
        {
            "T:Null:0:",
            "D",
            "S"
        }));
    }

    [Test]
    public void ParserEnforcesDocumentDepthAndCollectionLimitsBeforeMutation()
    {
        JsonLimits documentLimits = new JsonLimits();
        documentLimits.MaximumDocumentBytes = 2;
        AssertLimitFailure(documentLimits, "[0]", JsonDiagnosticCode.DocumentByteLimitExceeded);

        JsonLimits depthLimits = new JsonLimits();
        depthLimits.MaximumNestingDepth = 1;
        AssertLimitFailure(depthLimits, "[[]]", JsonDiagnosticCode.NestingDepthLimitExceeded);

        JsonLimits arrayLimits = new JsonLimits();
        arrayLimits.MaximumArrayItems = 1;
        AssertLimitFailure(arrayLimits, "[0,1]", JsonDiagnosticCode.ArrayItemLimitExceeded);

        JsonLimits objectLimits = new JsonLimits();
        objectLimits.MaximumObjectMembers = 1;
        AssertLimitFailure(objectLimits, "{\"a\":0,\"b\":1}", JsonDiagnosticCode.ObjectMemberLimitExceeded);
    }

    [Test]
    public void WhitespaceSeparatedAndSequenceModesPublishDocumentBoundaries()
    {
        JsonReaderOptions whitespaceOptions = new JsonReaderOptions();
        whitespaceOptions.InputMode = JsonInputMode.WhitespaceSeparated;
        List<string> whitespaceEvents = CollectOneShotEvents(
            System.Text.Encoding.ASCII.GetBytes("1 \ntrue {}"),
            whitespaceOptions);

        Assert.Multiple(() =>
        {
            Assert.That(whitespaceEvents.Count(value => value == "D"), Is.EqualTo(3));
            Assert.That(whitespaceEvents.Last(), Is.EqualTo("S"));
        });

        JsonReaderOptions sequenceOptions = new JsonReaderOptions();
        sequenceOptions.InputMode = JsonInputMode.JsonTextSequence;
        List<byte> sequenceBytes = new List<byte>();
        sequenceBytes.Add(JsonSyntaxBytes.RecordSeparator);
        sequenceBytes.AddRange(System.Text.Encoding.ASCII.GetBytes("{}\n"));
        sequenceBytes.Add(JsonSyntaxBytes.RecordSeparator);
        sequenceBytes.AddRange(System.Text.Encoding.ASCII.GetBytes("1\n"));
        sequenceBytes.Add(JsonSyntaxBytes.RecordSeparator);
        sequenceBytes.AddRange(System.Text.Encoding.ASCII.GetBytes("\"x\"\n"));
        List<string> sequenceEvents = CollectOneShotEvents(sequenceBytes.ToArray(), sequenceOptions);

        Assert.Multiple(() =>
        {
            Assert.That(sequenceEvents.Count(value => value == "D"), Is.EqualTo(3));
            Assert.That(sequenceEvents.Last(), Is.EqualTo("S"));
            Assert.That(sequenceEvents, Does.Contain("T:Number:0:"));
            Assert.That(sequenceEvents, Does.Contain("T:String:0:"));
        });
    }

    [Test]
    public void SequenceRecoveryDropsAnIncompleteScalarAndContinuesAtTheSameSeparator()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.JsonTextSequence;
        options.SequenceErrorPolicy = JsonSequenceErrorPolicy.Resynchronize;
        JsonParserCore parser = new JsonParserCore(options);
        byte[] bytes = new byte[]
        {
            JsonSyntaxBytes.RecordSeparator,
            0x31,
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginObject,
            JsonSyntaxBytes.EndObject,
            JsonSyntaxBytes.LineFeed
        };
        parser.FeedBytes(bytes);
        List<string> events = new List<string>();

        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(true);
            if (status == JsonReadStatus.Error)
            {
                JsonDiagnostic diagnostic = parser.GetCurrentDiagnostic()
                    ?? throw new InvalidOperationException("Expected diagnostic.");
                events.Add("E:" + diagnostic.Code);
                Assert.Multiple(() =>
                {
                    Assert.That(diagnostic.Pointer, Is.EqualTo(""));
                    Assert.That(diagnostic.Range?.Start?.ByteOffset, Is.EqualTo(2));
                    Assert.That(diagnostic.SourceExcerpt, Is.EqualTo(new byte[] { JsonSyntaxBytes.RecordSeparator }));
                });
                continue;
            }

            AddEvent(parser, status, events);
            if (status == JsonReadStatus.StreamComplete)
            {
                break;
            }
        }

        Assert.That(events, Is.EqualTo(new[]
        {
            "E:IncompleteSequenceRecord",
            "T:StartObject:0:",
            "T:EndObject:0:",
            "D",
            "S"
        }));
    }

    [Test]
    public void FinalInputValidationRejectsIncompleteGrammarAndFinalFeedIsTerminal()
    {
        JsonParserCore incomplete = new JsonParserCore(null);
        incomplete.FeedBytes(System.Text.Encoding.ASCII.GetBytes("["));
        JsonParseException exception = Assert.Throws<JsonParseException>(() => ReadUntilStreamComplete(incomplete))
            ?? throw new InvalidOperationException("Expected JsonParseException.");
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedEndOfInput));

        JsonParserCore complete = new JsonParserCore(null);
        complete.FeedBytes(System.Text.Encoding.ASCII.GetBytes("true"));
        ReadUntilStreamComplete(complete);
        Assert.Throws<InvalidOperationException>(() => complete.FeedBytes(new byte[] { JsonSyntaxBytes.Space }));
        Assert.That(complete.ReadNextEvent(true), Is.EqualTo(JsonReadStatus.StreamComplete));
    }

    static List<string> CollectOneShotEvents(byte[] bytes, JsonReaderOptions? options)
    {
        JsonParserCore parser = new JsonParserCore(options);
        parser.FeedBytes(bytes);
        List<string> events = new List<string>();
        DrainFinalEvents(parser, events);
        return events;
    }

    static void DrainAvailableEvents(JsonParserCore parser, bool finalInput, List<string> events)
    {
        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(finalInput);
            if (status == JsonReadStatus.NeedMoreData)
            {
                return;
            }

            AddEvent(parser, status, events);
            if (status == JsonReadStatus.StreamComplete)
            {
                return;
            }
        }
    }

    static void DrainFinalEvents(JsonParserCore parser, List<string> events)
    {
        int eventCount = 0;
        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(true);
            Assert.That(status, Is.Not.EqualTo(JsonReadStatus.NeedMoreData));
            AddEvent(parser, status, events);
            eventCount++;
            Assert.That(eventCount, Is.LessThan(1000));
            if (status == JsonReadStatus.StreamComplete)
            {
                return;
            }
        }
    }

    static void AddEvent(JsonParserCore parser, JsonReadStatus status, List<string> events)
    {
        if (status == JsonReadStatus.TokenAvailable)
        {
            JsonToken token = parser.GetCurrentToken()
                ?? throw new InvalidOperationException("Expected token.");
            events.Add(
                "T:" + token.Kind + ":" + token.Depth + ":" +
                (token.Pointer?.OriginalRepresentation ?? "<null>"));
            return;
        }

        if (status == JsonReadStatus.DocumentComplete)
        {
            events.Add("D");
            return;
        }

        if (status == JsonReadStatus.StreamComplete)
        {
            events.Add("S");
            return;
        }

        if (status == JsonReadStatus.Error)
        {
            events.Add("E:" + parser.GetCurrentDiagnostic()?.Code);
            return;
        }

        throw new InvalidOperationException("Unexpected parser status: " + status);
    }

    static void ReadUntilStreamComplete(JsonParserCore parser)
    {
        int eventCount = 0;
        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(true);
            eventCount++;
            if (status == JsonReadStatus.StreamComplete)
            {
                return;
            }

            if (status == JsonReadStatus.Error)
            {
                throw new InvalidOperationException("Unexpected recoverable parser error.");
            }

            Assert.That(eventCount, Is.LessThan(1000));
        }
    }

    static void AssertLimitFailure(JsonLimits limits, string jsonText, JsonDiagnosticCode expectedCode)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;
        JsonParserCore parser = new JsonParserCore(options);
        parser.FeedBytes(System.Text.Encoding.UTF8.GetBytes(jsonText));

        JsonLimitException exception = Assert.Throws<JsonLimitException>(() => ReadUntilStreamComplete(parser))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(expectedCode));
            Assert.That(exception.Diagnostic.Range, Is.Not.Null);
            Assert.That(exception.Diagnostic.Pointer, Is.Not.Null);
            Assert.That(exception.Diagnostic.SourceExcerpt, Is.Not.Null);
        });
    }
}
