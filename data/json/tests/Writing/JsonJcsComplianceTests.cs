namespace json.Tests;

public class JsonJcsComplianceTests
{
    [Test]
    public void StringsUseRequiredEscapesAndPreserveOtherScalars()
    {
        string value =
            "\0\u0001\b\t\n\u000b\f\r\u001f\"\\/A<&" +
            "\u007f\u0080é😀";
        string expected =
            "\"\\u0000\\u0001\\b\\t\\n\\u000b\\f\\r\\u001f" +
            "\\\"\\\\/A<&\u007f\u0080é😀\"";
        JsonValueFactory factory = new JsonValueFactory();
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        string actual = Text(canonicalizer.CanonicalizeDocument(
            Document(factory.CreateString(value))));

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void OutputIsCompactAndSortsEveryObjectInsideArrays()
    {
        byte[] input = Bytes(
            "{\n" +
            "  \"z\": [3, {\"z\": 2.00, \"a\": 1E0}," +
            " [{\"b\": false, \"a\": true}]],\n" +
            "  \"a\": null\n" +
            "}");
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        string actual = Text(canonicalizer.CanonicalizeUtf8(
            input,
            new JsonReaderOptions()));

        Assert.That(
            actual,
            Is.EqualTo(
                "{\"a\":null,\"z\":[3,{\"a\":1,\"z\":2}," +
                "[{\"a\":true,\"b\":false}]]}"));
    }

    [Test]
    public void CanonicallyEquivalentUnicodeSequencesAreNotNormalized()
    {
        string decomposed = "e\u0301";
        string precomposed = "é";
        JsonValueFactory factory = new JsonValueFactory();
        JsonDocument document = Document(factory.CreateObject(
            new List<JsonMember>
            {
                Member(precomposed, factory.CreateString(precomposed)),
                Member(decomposed, factory.CreateString(decomposed))
            }));
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        string actual = Text(canonicalizer.CanonicalizeDocument(document));

        Assert.That(
            actual,
            Is.EqualTo(
                "{\"" + decomposed + "\":\"" + decomposed +
                "\",\"" + precomposed + "\":\"" + precomposed +
                "\"}"));
    }

    [Test]
    public void SupplementaryNamesAreOrderedByUtf16CodeUnits()
    {
        string supplementary = "😀";
        string privateUse = "\uE000";
        JsonValueFactory factory = new JsonValueFactory();
        JsonDocument document = Document(factory.CreateObject(
            new List<JsonMember>
            {
                Member(privateUse, factory.CreateNumber("0")),
                Member(supplementary, factory.CreateNumber("1")),
                Member("a", factory.CreateNumber("2"))
            }));
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        string actual = Text(canonicalizer.CanonicalizeDocument(document));

        Assert.That(
            actual,
            Is.EqualTo(
                "{\"a\":2,\"" + supplementary + "\":1,\"" +
                privateUse + "\":0}"));
    }

    [Test]
    public void DecodedDuplicateNamesAreRejected()
    {
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        JsonParseException failure = Assert.Throws<JsonParseException>(() =>
            canonicalizer.CanonicalizeUtf8(
                Bytes("{\"a\":0,\"\\u0061\":1}"),
                new JsonReaderOptions()))
            ?? throw new InvalidOperationException(
                "Expected JsonParseException.");

        Assert.That(failure.Diagnostic.Code,
            Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
    }

    [TestCase("\"\\ud800\"")]
    [TestCase("\"\\udc00\"")]
    [TestCase("{\"\\ud800\":0}")]
    [TestCase("{\"\\udc00\":0}")]
    public void LoneSurrogatesInNamesAndValuesAreRejected(string input)
    {
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        JsonParseException failure = Assert.Throws<JsonParseException>(() =>
            canonicalizer.CanonicalizeUtf8(
                Bytes(input),
                new JsonReaderOptions()))
            ?? throw new InvalidOperationException(
                "Expected JsonParseException.");

        Assert.That(failure.Diagnostic.Code,
            Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
    }

    [Test]
    public void FiniteDoubleBoundaryIsAcceptedAndOverflowIsRejected()
    {
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        string maximum = Text(canonicalizer.CanonicalizeUtf8(
            Bytes("1.7976931348623157e308"),
            new JsonReaderOptions()));
        JsonWriteException positive = Assert.Throws<JsonWriteException>(() =>
            canonicalizer.CanonicalizeUtf8(
                Bytes("1.7976931348623159e308"),
                new JsonReaderOptions()))
            ?? throw new InvalidOperationException(
                "Expected JsonWriteException.");
        JsonWriteException negative = Assert.Throws<JsonWriteException>(() =>
            canonicalizer.CanonicalizeUtf8(
                Bytes("-1e999"),
                new JsonReaderOptions()))
            ?? throw new InvalidOperationException(
                "Expected JsonWriteException.");

        Assert.Multiple(() =>
        {
            Assert.That(maximum,
                Is.EqualTo("1.7976931348623157e+308"));
            Assert.That(positive.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.NumericConversionFailed));
            Assert.That(negative.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.NumericConversionFailed));
        });
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

    static string Text(byte[] value)
    {
        return System.Text.Encoding.UTF8.GetString(value);
    }
}
