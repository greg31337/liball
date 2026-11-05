namespace json.Tests;

public class JsonDeterministicOutputTests
{
    const string ForwardJson =
        "{\"\\ue000\":{\"z\":0,\"a\":\"<é/>\"}," +
        "\"\\ud83d\\ude00\":[{\"z\":2,\"a\":1},3]," +
        "\"é\":\"precomposed\",\"e\\u0301\":\"decomposed\"," +
        "\"aa\":true,\"a\":null,\"\":-0.50E+2}";

    const string ReverseJson =
        "{\"\":-50.0,\"a\":null,\"aa\":true," +
        "\"é\":\"decomposed\",\"\\u00e9\":\"precomposed\"," +
        "\"😀\":[{\"a\":1.00,\"z\":2e0},3.0]," +
        "\"\":{\"a\":\"<\\u00e9\\u002f>\",\"z\":0.0}}";

    [Test]
    public void SortedSerializerModesAreByteStableAcrossOriginsAndRuns()
    {
        List<JsonDocument> documents = CreateEquivalentDocuments(false);
        List<WriterCase> cases = CreateWriterCases();
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        int caseIndex = 0;
        while (caseIndex < cases.Count)
        {
            WriterCase current = cases[caseIndex];
            byte[] expected = serializer.SerializeUtf8(
                documents[0],
                current.Options);
            int documentIndex = 0;
            while (documentIndex < documents.Count)
            {
                int repetition = 0;
                while (repetition < 4)
                {
                    byte[] actual = serializer.SerializeUtf8(
                        documents[documentIndex],
                        current.Options);
                    Assert.That(
                        actual,
                        Is.EqualTo(expected),
                        current.Name + " document " + documentIndex +
                        " repetition " + repetition);
                    repetition++;
                }

                documentIndex++;
            }

            caseIndex++;
        }
    }

    [Test]
    public void CanonicalOutputIsByteStableAcrossNumericSpellingsAndOrigins()
    {
        List<JsonDocument> documents = CreateEquivalentDocuments(true);
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();
        byte[] expected = canonicalizer.CanonicalizeDocument(documents[0]);
        int documentIndex = 0;
        while (documentIndex < documents.Count)
        {
            int repetition = 0;
            while (repetition < 4)
            {
                byte[] actual = canonicalizer.CanonicalizeDocument(
                    documents[documentIndex]);
                Assert.That(
                    actual,
                    Is.EqualTo(expected),
                    "Canonical document " + documentIndex +
                    " repetition " + repetition);
                repetition++;
            }

            documentIndex++;
        }

        JsonOperationResult verification =
            canonicalizer.VerifyCanonicalUtf8(
                expected,
                new JsonReaderOptions());
        Assert.Multiple(() =>
        {
            Assert.That(verification.Success, Is.True);
            Assert.That(verification.BytesConsumed,
                Is.EqualTo(expected.Length));
        });
    }

    static List<JsonDocument> CreateEquivalentDocuments(
        bool varyNumericSpellings)
    {
        JsonDocument forward = CreateManualDocument(
            false,
            "-0.50E+2",
            "0",
            "1",
            "2",
            "3");
        JsonDocument reverse = CreateManualDocument(
            true,
            varyNumericSpellings ? "-50.0" : "-0.50E+2",
            varyNumericSpellings ? "0.0" : "0",
            varyNumericSpellings ? "1.00" : "1",
            varyNumericSpellings ? "2e0" : "2",
            varyNumericSpellings ? "3.0" : "3");
        JsonValueFactory factory = new JsonValueFactory();
        JsonValue forwardRoot = forward.Root
            ?? throw new InvalidOperationException("Expected a root value.");
        JsonDocument clone = Document(factory.CloneValue(forwardRoot));
        IJsonDocumentParser parser = new JsonDocumentParser();
        JsonDocument utf8 = parser.ParseUtf8(
            Bytes(ForwardJson),
            new JsonReaderOptions());
        JsonDocument text = parser.ParseString(
            varyNumericSpellings ? ReverseJson : ForwardJson,
            new JsonReaderOptions());
        MemoryStream stream = new MemoryStream(Bytes(
            varyNumericSpellings ? ReverseJson : ForwardJson));
        JsonDocument streamed = parser.ParseStream(
            stream,
            new JsonReaderOptions());

        return new List<JsonDocument>
        {
            forward,
            reverse,
            clone,
            utf8,
            text,
            streamed
        };
    }

    static JsonDocument CreateManualDocument(
        bool reverse,
        string rootNumber,
        string zero,
        string one,
        string two,
        string three)
    {
        JsonValueFactory factory = new JsonValueFactory();
        List<JsonMember> privateMembers = new List<JsonMember>
        {
            Member("z", factory.CreateNumber(zero)),
            Member("a", factory.CreateString("<é/>"))
        };
        List<JsonMember> arrayObjectMembers = new List<JsonMember>
        {
            Member("z", factory.CreateNumber(two)),
            Member("a", factory.CreateNumber(one))
        };
        if (reverse)
        {
            privateMembers.Reverse();
            arrayObjectMembers.Reverse();
        }

        JsonValue array = factory.CreateArray(new List<JsonValue>
        {
            factory.CreateObject(arrayObjectMembers),
            factory.CreateNumber(three)
        });
        List<JsonMember> rootMembers = new List<JsonMember>
        {
            Member("\uE000", factory.CreateObject(privateMembers)),
            Member("😀", array),
            Member("é", factory.CreateString("precomposed")),
            Member("e\u0301", factory.CreateString("decomposed")),
            Member("aa", factory.CreateBoolean(true)),
            Member("a", factory.CreateNull()),
            Member("", factory.CreateNumber(rootNumber))
        };
        if (reverse)
        {
            rootMembers.Reverse();
        }

        return Document(factory.CreateObject(rootMembers));
    }

    static List<WriterCase> CreateWriterCases()
    {
        List<WriterCase> cases = new List<WriterCase>();

        JsonWriterOptions utf16 = BaseWriterOptions();
        utf16.PropertyOrder = JsonPropertyOrder.OrdinalUtf16;
        cases.Add(WriterCaseOf("ordinal-utf16", utf16));

        JsonWriterOptions scalar = BaseWriterOptions();
        scalar.Formatting = JsonFormatting.Pretty;
        scalar.StringEscapeMode = JsonStringEscapeMode.AsciiOnly;
        scalar.PropertyOrder = JsonPropertyOrder.OrdinalUnicodeScalar;
        scalar.EscapeSolidus = true;
        scalar.UseUppercaseHexadecimalEscapes = true;
        scalar.Indentation = "\t";
        scalar.NewLine = "\r\n";
        cases.Add(WriterCaseOf("ordinal-scalar", scalar));

        JsonWriterOptions jcs = BaseWriterOptions();
        jcs.Formatting = JsonFormatting.Pretty;
        jcs.StringEscapeMode = JsonStringEscapeMode.HtmlSafe;
        jcs.PropertyOrder = JsonPropertyOrder.Jcs;
        jcs.EscapeSolidus = true;
        jcs.OmitSpaceAfterColon = true;
        jcs.Indentation = " ";
        jcs.NewLine = "\n";
        cases.Add(WriterCaseOf("jcs-order", jcs));

        return cases;
    }

    static JsonWriterOptions BaseWriterOptions()
    {
        JsonWriterOptions options = new JsonWriterOptions();
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        return options;
    }

    static WriterCase WriterCaseOf(
        string name,
        JsonWriterOptions options)
    {
        WriterCase result = new WriterCase();
        result.Name = name;
        result.Options = options;
        return result;
    }

    static JsonDocument Document(JsonValue root)
    {
        JsonDocument document = new JsonDocument();
        document.Root = root;
        return document;
    }

    static JsonMember Member(string name, JsonValue value)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.Value = value;
        return member;
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    class WriterCase
    {
        internal string Name = "";
        internal JsonWriterOptions Options = new JsonWriterOptions();
    }
}
