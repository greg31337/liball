namespace json.Tests;

public class JsonDuplicatePolicyTests
{
    [TestCase(JsonDuplicateKeyPolicy.Preserve, 0)]
    [TestCase(JsonDuplicateKeyPolicy.KeepFirst, 1)]
    [TestCase(JsonDuplicateKeyPolicy.KeepLast, 2)]
    public void EveryRetentionPolicyUsesDecodedOrdinalNames(
        JsonDuplicateKeyPolicy policy,
        int expectedDuplicateDecisionValue)
    {
        string jsonText =
            "{\"\":0,\"\":1,\"é\":2,\"\\u00E9\":3,\"e\\u0301\":4," +
            "\"😀\":5,\"\\uD83D\\uDE00\":6,\"/~\":7,\"\\u002F\\u007E\":8}";
        (List<(string Name, string Pointer, JsonDuplicateDecision Decision)> Properties, int TokenCount) result =
            ParseProperties(jsonText, policy);
        List<(string Name, string Pointer, JsonDuplicateDecision Decision)> properties = result.Properties;
        JsonDuplicateDecisionKind expectedDuplicateDecision =
            (JsonDuplicateDecisionKind)expectedDuplicateDecisionValue;

        Assert.Multiple(() =>
        {
            Assert.That(result.TokenCount, Is.EqualTo(20));
            Assert.That(properties.Select(property => property.Name), Is.EqualTo(new[]
            {
                "", "", "é", "é", "e\u0301", "😀", "😀", "/~", "/~"
            }));
            Assert.That(properties.Select(property => property.Pointer), Is.EqualTo(new[]
            {
                "/", "/", "/é", "/é", "/e\u0301", "/😀", "/😀", "/~1~0", "/~1~0"
            }));
            Assert.That(properties.Select(property => property.Decision.IsDuplicate), Is.EqualTo(new[]
            {
                false, true, false, true, false, false, true, false, true
            }));
        });

        int[] uniqueIndexes = { 0, 0, 1, 1, 2, 3, 3, 4, 4 };
        for (int index = 0; index < properties.Count; index++)
        {
            JsonDuplicateDecision decision = properties[index].Decision;
            if (!decision.IsDuplicate || policy == JsonDuplicateKeyPolicy.Preserve)
            {
                Assert.That(decision.Kind, Is.EqualTo(JsonDuplicateDecisionKind.Append), $"property {index}");
            }
            else
            {
                Assert.That(decision.Kind, Is.EqualTo(expectedDuplicateDecision), $"property {index}");
            }

            int expectedIndex = policy == JsonDuplicateKeyPolicy.Preserve ? index : uniqueIndexes[index];
            Assert.That(decision.MemberIndex, Is.EqualTo(expectedIndex), $"property {index}");
        }
    }

    [TestCase("{\"\":0,\"\":1}", "")]
    [TestCase("{\"a\":0,\"\\u0061\":1}", "a")]
    [TestCase("{\"é\":0,\"\\u00E9\":1}", "é")]
    [TestCase("{\"😀\":0,\"\\uD83D\\uDE00\":1}", "😀")]
    [TestCase("{\"/~\":0,\"\\u002F\\u007E\":1}", "/~")]
    public void RejectPolicyFailsOnEachDecodedEquivalentName(string jsonText, string expectedName)
    {
        JsonParseException exception = ParseRejectedDuplicate(jsonText);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(exception.Diagnostic.ActualToken, Is.EqualTo(expectedName));
            Assert.That(exception.Diagnostic.Depth, Is.EqualTo(1));
        });
    }

    [Test]
    public void EscapedControlNamesParticipateInDuplicateComparison()
    {
        JsonParseException exception = ParseRejectedDuplicate("{\"\\u0000\":0,\"\\u0000\":1}");
        string actualName = exception.Diagnostic.ActualToken
            ?? throw new InvalidOperationException("Expected decoded duplicate name.");

        Assert.Multiple(() =>
        {
            Assert.That(actualName, Has.Length.EqualTo(1));
            Assert.That(actualName[0], Is.EqualTo('\0'));
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
        });
    }

    [Test]
    public void OrdinalComparisonDoesNotNormalizeOrFoldCase()
    {
        string jsonText = "{\"é\":0,\"e\\u0301\":1,\"A\":2,\"a\":3}";
        (List<(string Name, string Pointer, JsonDuplicateDecision Decision)> Properties, int TokenCount) result =
            ParseProperties(jsonText, JsonDuplicateKeyPolicy.Reject);

        Assert.Multiple(() =>
        {
            Assert.That(result.Properties.Select(property => property.Name), Is.EqualTo(new[]
            {
                "é", "e\u0301", "A", "a"
            }));
            Assert.That(result.Properties.All(property => !property.Decision.IsDuplicate), Is.True);
            Assert.That(result.Properties.Select(property => property.Decision.MemberIndex), Is.EqualTo(new[]
            {
                0, 1, 2, 3
            }));
        });
    }

    [Test]
    public void NestedObjectsUseIndependentNameScopesAndRestoreTheirParents()
    {
        string jsonText =
            "{\"x\":0,\"child\":{\"x\":1,\"x\":2},\"x\":3," +
            "\"array\":[{\"x\":4},{\"x\":5,\"x\":6}]}";
        (List<(string Name, string Pointer, JsonDuplicateDecision Decision)> Properties, int TokenCount) result =
            ParseProperties(jsonText, JsonDuplicateKeyPolicy.KeepFirst);
        List<(string Name, string Pointer, JsonDuplicateDecision Decision)> properties = result.Properties;

        Assert.Multiple(() =>
        {
            Assert.That(properties.Select(property => property.Pointer), Is.EqualTo(new[]
            {
                "/x",
                "/child",
                "/child/x",
                "/child/x",
                "/x",
                "/array",
                "/array/0/x",
                "/array/1/x",
                "/array/1/x"
            }));
            Assert.That(properties.Select(property => property.Decision.Kind), Is.EqualTo(new[]
            {
                JsonDuplicateDecisionKind.Append,
                JsonDuplicateDecisionKind.Append,
                JsonDuplicateDecisionKind.Append,
                JsonDuplicateDecisionKind.Skip,
                JsonDuplicateDecisionKind.Skip,
                JsonDuplicateDecisionKind.Append,
                JsonDuplicateDecisionKind.Append,
                JsonDuplicateDecisionKind.Append,
                JsonDuplicateDecisionKind.Skip
            }));
            Assert.That(properties.Select(property => property.Decision.MemberIndex), Is.EqualTo(new[]
            {
                0, 1, 0, 0, 0, 2, 0, 0, 0
            }));
        });
    }

    [TestCase("{\"x\":0,\"child\":{\"x\":1,\"x\":2}}", 2)]
    [TestCase("{\"x\":0,\"child\":{\"x\":1},\"x\":2}", 1)]
    public void RejectPolicyReportsTheScopeDepthContainingTheDuplicate(
        string jsonText,
        int expectedDepth)
    {
        JsonParseException exception = ParseRejectedDuplicate(jsonText);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(exception.Diagnostic.ActualToken, Is.EqualTo("x"));
            Assert.That(exception.Diagnostic.Depth, Is.EqualTo(expectedDepth));
        });
    }

    static (
        List<(string Name, string Pointer, JsonDuplicateDecision Decision)> Properties,
        int TokenCount) ParseProperties(string jsonText, JsonDuplicateKeyPolicy policy)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = policy;
        JsonParserCore parser = new JsonParserCore(options);
        parser.FeedBytes(System.Text.Encoding.UTF8.GetBytes(jsonText));
        List<(string Name, string Pointer, JsonDuplicateDecision Decision)> properties =
            new List<(string Name, string Pointer, JsonDuplicateDecision Decision)>();
        int tokenCount = 0;
        int documentCount = 0;

        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(true);
            if (status == JsonReadStatus.TokenAvailable)
            {
                tokenCount++;
                JsonToken token = parser.GetCurrentToken()
                    ?? throw new InvalidOperationException("Expected token.");
                JsonDuplicateDecision? decision = parser.GetCurrentDuplicateDecision();
                if (decision != null)
                {
                    properties.Add((
                        token.StringValue ?? throw new InvalidOperationException("Expected property name."),
                        token.Pointer?.OriginalRepresentation
                            ?? throw new InvalidOperationException("Expected property pointer."),
                        decision));
                }

                continue;
            }

            if (status == JsonReadStatus.DocumentComplete)
            {
                documentCount++;
                continue;
            }

            Assert.That(status, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(documentCount, Is.EqualTo(1));
            return (properties, tokenCount);
        }
    }

    static JsonParseException ParseRejectedDuplicate(string jsonText)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        JsonParserCore parser = new JsonParserCore(options);
        parser.FeedBytes(System.Text.Encoding.UTF8.GetBytes(jsonText));

        return Assert.Throws<JsonParseException>(() => ReadToCompletion(parser))
            ?? throw new InvalidOperationException("Expected JsonParseException.");
    }

    static void ReadToCompletion(JsonParserCore parser)
    {
        int eventCount = 0;
        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(true);
            eventCount++;
            Assert.That(eventCount, Is.LessThan(1000));
            if (status == JsonReadStatus.StreamComplete)
            {
                return;
            }
        }
    }
}
