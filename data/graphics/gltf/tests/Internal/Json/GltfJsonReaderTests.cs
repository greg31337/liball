using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Json;

[TestFixture]
public class GltfJsonReaderTests
{
    [Test]
    public void Read_ProducesDecodedValuesTokensPointersAndByteOffsets()
    {
        string json = "{\"a/b\":{\"~key\":[true,false,null,\"é\",-1.25e+2]}}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        GltfJsonReadResult result = new GltfJsonReader().Read(
            bytes,
            new GltfReadLimits(),
            GltfDuplicateMemberPolicy.Reject);
        GltfJsonToken numberToken = result.Tokens.Single(token => token.Type == JsonTokenType.Number);
        GltfJsonToken stringToken = result.Tokens.Single(token => token.Type == JsonTokenType.String);
        int numberCharacterOffset = json.IndexOf("-1.25e+2", StringComparison.Ordinal);
        int expectedNumberByteOffset = Encoding.UTF8.GetByteCount(json[..numberCharacterOffset]);
        JsonObject root = (JsonObject)result.Root!;
        JsonArray values = (JsonArray)root["a/b"]!["~key"]!;

        Assert.Multiple(() =>
        {
            Assert.That(result.HasRoot, Is.True);
            Assert.That(result.Tokens, Has.Count.EqualTo(13));
            Assert.That(values[0]!.GetValue<bool>(), Is.True);
            Assert.That(values[1]!.GetValue<bool>(), Is.False);
            Assert.That(values[2], Is.Null);
            Assert.That(values[3]!.GetValue<string>(), Is.EqualTo("é"));
            Assert.That(values[4]!.GetValue<double>(), Is.EqualTo(-125.0));
            Assert.That(stringToken.JsonPointer, Is.EqualTo("/a~1b/~0key/3"));
            Assert.That(stringToken.Text, Is.EqualTo("é"));
            Assert.That(numberToken.JsonPointer, Is.EqualTo("/a~1b/~0key/4"));
            Assert.That(numberToken.ByteOffset, Is.EqualTo(expectedNumberByteOffset));
            Assert.That(numberToken.ByteLength, Is.EqualTo(8));
            Assert.That(numberToken.Text, Is.EqualTo("-1.25e+2"));
            Assert.That(numberToken.HasNumberValue, Is.True);
            Assert.That(numberToken.NumberValue, Is.EqualTo(-125.0));
        });
    }

    [Test]
    public void Read_AppliesEveryDuplicateMemberPolicy()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("{\"a\":1,\"a\":2}");
        GltfReadLimits limits = new GltfReadLimits();
        GltfJsonReader reader = new GltfJsonReader();

        Assert.That(
            () => reader.Read(bytes, limits, GltfDuplicateMemberPolicy.Reject),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo("Duplicate JSON object member 'a' at byte 7 and JSON pointer '/a'."));

        GltfJsonReadResult firstResult = reader.Read(bytes, limits, GltfDuplicateMemberPolicy.KeepFirst);
        GltfJsonReadResult lastResult = reader.Read(bytes, limits, GltfDuplicateMemberPolicy.KeepLast);
        JsonObject first = (JsonObject)firstResult.Root!;
        JsonObject last = (JsonObject)lastResult.Root!;

        Assert.Multiple(() =>
        {
            Assert.That(first["a"]!.GetValue<double>(), Is.EqualTo(1.0));
            Assert.That(last["a"]!.GetValue<double>(), Is.EqualTo(2.0));
            Assert.That(
                firstResult.Tokens.Count(token => token.Type == JsonTokenType.PropertyName && token.DuplicateMember),
                Is.EqualTo(1));
            Assert.That(
                lastResult.Tokens.Count(token => token.Type == JsonTokenType.PropertyName && token.DuplicateMember),
                Is.EqualTo(1));
        });
    }

    [TestCase("{\"a\":01}", "leading zeroes", "/a")]
    [TestCase("{\"a\":1,}", "Trailing commas", "")]
    [TestCase("[1 2]", "Expected ',' or ']'", "")]
    [TestCase("{\"a\":\"\\uD800\"}", "unpaired high surrogate", "/a")]
    [TestCase("{\"a\":1/*x*/}", "Expected ',' or '}'", "")]
    public void Read_RejectsNonRfc8259Grammar(string json, string expectedMessage, string expectedPointer)
    {
        Assert.That(
            () => new GltfJsonReader().Read(
                Encoding.UTF8.GetBytes(json),
                new GltfReadLimits(),
                GltfDuplicateMemberPolicy.Reject),
            Throws.TypeOf<GltfParseException>()
                .With.Message.Contains(expectedMessage)
                .And.Message.EndsWith($"JSON pointer '{expectedPointer}'."));
    }

    [Test]
    public void Read_RejectsMalformedUtf8AtItsAbsoluteLocation()
    {
        byte[] bytes = [0x7B, 0x22, 0x61, 0x22, 0x3A, 0x22, 0xC0, 0x80, 0x22, 0x7D];

        Assert.That(
            () => new GltfJsonReader().Read(
                bytes,
                new GltfReadLimits(),
                GltfDuplicateMemberPolicy.Reject),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo("JSON string contains invalid UTF-8 at byte 6 and JSON pointer '/a'."));
    }

    [Test]
    public void Read_EnforcesDepthAndTokenLimitsAtTheNextToken()
    {
        GltfReadLimits depthLimits = new GltfReadLimits();
        depthLimits.MaximumJsonDepth = 1;
        GltfReadLimits tokenLimits = new GltfReadLimits();
        tokenLimits.MaximumJsonTokenCount = 1;
        GltfJsonReader reader = new GltfJsonReader();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.Read(
                    Encoding.UTF8.GetBytes("[[]]"),
                    depthLimits,
                    GltfDuplicateMemberPolicy.Reject),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo(
                        "JSON depth exceeds the configured maximum 1 at byte 1 and JSON pointer '/0'."));
            Assert.That(
                () => reader.Read(
                    Encoding.UTF8.GetBytes("{}"),
                    tokenLimits,
                    GltfDuplicateMemberPolicy.Reject),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo(
                        "JSON token count exceeds the configured maximum 1 at byte 1 and JSON pointer ''."));
        });
    }

    [Test]
    public void Read_EnforcesDecodedStringAndSourceNumberByteLimits()
    {
        GltfReadLimits stringLimits = new GltfReadLimits();
        stringLimits.MaximumJsonStringByteLength = 1;
        GltfReadLimits numberLimits = new GltfReadLimits();
        numberLimits.MaximumJsonNumberByteLength = 3;
        GltfJsonReader reader = new GltfJsonReader();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.Read(
                    Encoding.UTF8.GetBytes("{\"a\":\"\\u00E9\"}"),
                    stringLimits,
                    GltfDuplicateMemberPolicy.Reject),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo(
                        "JSON string byte length 2 exceeds the configured maximum 1 at byte 5 and JSON pointer '/a'."));
            Assert.That(
                () => reader.Read(
                    Encoding.UTF8.GetBytes("[1234]"),
                    numberLimits,
                    GltfDuplicateMemberPolicy.Reject),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo(
                        "JSON number byte length 4 exceeds the configured maximum 3 at byte 1 and JSON pointer '/0'."));
        });
    }

    [Test]
    public void Read_RejectsNumbersThatCannotBeRepresentedAsFiniteValues()
    {
        Assert.That(
            () => new GltfJsonReader().Read(
                Encoding.UTF8.GetBytes("[1e999]"),
                new GltfReadLimits(),
                GltfDuplicateMemberPolicy.Reject),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo("JSON number is not finite at byte 1 and JSON pointer '/0'."));
    }

    [Test]
    public void Read_IgnoresOneLeadingBomButReportsOriginalByteOffsets()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, 0x7B, 0x7D];

        GltfJsonReadResult result = new GltfJsonReader().Read(
            bytes,
            new GltfReadLimits(),
            GltfDuplicateMemberPolicy.Reject);

        Assert.Multiple(() =>
        {
            Assert.That(result.Tokens[0].Type, Is.EqualTo(JsonTokenType.StartObject));
            Assert.That(result.Tokens[0].ByteOffset, Is.EqualTo(3));
            Assert.That(result.Tokens[1].Type, Is.EqualTo(JsonTokenType.EndObject));
            Assert.That(result.Tokens[1].ByteOffset, Is.EqualTo(4));
        });
    }

    [Test]
    public void DocumentReadBytes_TokenizesWhileRawContainerReadingRemainsIndependent()
    {
        byte[] malformedJson = Encoding.UTF8.GetBytes("{\"a\":01}");
        byte[] duplicateJson = Encoding.UTF8.GetBytes("{\"a\":1,\"a\":2}");
        byte[] validDuplicateJson = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"a\":1,\"a\":2}");
        GltfReader reader = new GltfReader();
        GltfReadOptions keepLast = new GltfReadOptions();
        keepLast.DuplicateMemberPolicy = GltfDuplicateMemberPolicy.KeepLast;

        GltfReadResult rawResult = reader.ReadContainerBytes(malformedJson, new GltfReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(rawResult.Container, Is.Not.Null);
            Assert.That(rawResult.Container!.JsonBytes, Is.EqualTo(malformedJson));
            Assert.That(
                () => reader.ReadBytes(malformedJson, new GltfReadOptions()),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo(
                        "JSON numbers cannot contain leading zeroes at byte 6 and JSON pointer '/a'."));
            Assert.That(
                () => reader.ReadBytes(duplicateJson, new GltfReadOptions()),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo(
                        "Duplicate JSON object member 'a' at byte 7 and JSON pointer '/a'."));
            Assert.That(reader.ReadBytes(validDuplicateJson, keepLast).Document, Is.Not.Null);
        });
    }
}
