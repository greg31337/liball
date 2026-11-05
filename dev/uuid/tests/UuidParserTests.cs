using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UuidParserTests
{
    [Test]
    public void Parse_AcceptsCanonicalUuidAndNormalizesToLowercase()
    {
        UuidParseResult result = UuidParser.Parse("00112233-4455-6677-8899-AABBCCDDEEFF");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.NormalizedText, Is.EqualTo("00112233-4455-6677-8899-aabbccddeeff"));
        Assert.That(result.Value.ToBytes(), Is.EqualTo(ExampleBytes()));
    }

    [Test]
    public void Parse_AcceptsCompactUuidAndNormalizesToCanonicalText()
    {
        UuidParseResult result = UuidParser.Parse("00112233445566778899AABBCCDDEEFF");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.NormalizedText, Is.EqualTo("00112233-4455-6677-8899-aabbccddeeff"));
        Assert.That(result.Value.ToBytes(), Is.EqualTo(ExampleBytes()));
    }

    [Test]
    public void Parse_AcceptsUuidUrnCaseInsensitively()
    {
        UuidParseResult result = UuidParser.Parse("URN:UUID:00112233-4455-6677-8899-AABBCCDDEEFF");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.NormalizedText, Is.EqualTo("00112233-4455-6677-8899-aabbccddeeff"));
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.ToCompactString(), Is.EqualTo("00112233445566778899aabbccddeeff"));
    }

    [Test]
    public void Parse_RejectsInvalidShapes()
    {
        AssertParseFailure((string?)null, "UUID text is null");
        AssertParseFailure("00112233-4455-6677-8899-aabbccddeef", "UUID text length must be 32 compact hex characters, 36 canonical characters, or urn:uuid: plus canonical UUID");
        AssertParseFailure("00112233445566778899aabbccddeezz", "invalid UUID hex character at index 30");
        AssertParseFailure("0011223-34455-6677-8899-aabbccddeeff", "canonical UUID hyphens are not in expected positions");
        AssertParseFailure("urn:uuid:00112233445566778899aabbccddeeff", "UUID URN must contain canonical UUID text");
    }

    [Test]
    public void ParseCanonical_RejectsBadHyphensAndBadHex()
    {
        AssertParseFailure(UuidParser.ParseCanonical("0011223-34455-6677-8899-aabbccddeeff"), "canonical UUID hyphens are not in expected positions");
        AssertParseFailure(UuidParser.ParseCanonical("00112233-4455-6677-8899-aabbccddeefg"), "invalid UUID hex character at index 35");
    }

    [Test]
    public void ParseCompact_RequiresThirtyTwoHexCharacters()
    {
        AssertParseFailure(UuidParser.ParseCompact("001122"), "compact UUID text must be 32 hex characters");
        AssertParseFailure(UuidParser.ParseCompact("00112233445566778899aabbccddeezz"), "invalid UUID hex character at index 30");
    }

    [Test]
    public void ParseValue_ReturnsValueOrThrows()
    {
        UuidValue value = UuidParser.ParseValue("00112233-4455-6677-8899-aabbccddeeff");

        Assert.That(value.ToCompactString(), Is.EqualTo("00112233445566778899aabbccddeeff"));
        Assert.That(() => UuidParser.ParseValue("not-a-uuid"), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void UuidFacade_DelegatesToParser()
    {
        UuidParseResult parse = new Uuid().Parse("00112233445566778899aabbccddeeff");
        UuidParseResult tryParse = new Uuid().TryParse("00112233445566778899aabbccddeeff");
        UuidValue value = new Uuid().ParseValue("00112233445566778899aabbccddeeff");

        Assert.That(parse.Ok, Is.True);
        Assert.That(tryParse.Ok, Is.True);
        Assert.That(value.ToCanonicalString(), Is.EqualTo("00112233-4455-6677-8899-aabbccddeeff"));
    }

    static void AssertParseFailure(string? text, string error)
    {
        UuidParseResult result = UuidParser.Parse(text);

        AssertParseFailure(result, error);
    }

    static void AssertParseFailure(UuidParseResult result, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
    }

    static byte[] ExampleBytes()
    {
        return new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
    }
}
