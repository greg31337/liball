using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class KsuidParserTests
{
    [Test]
    public void Parse_AcceptsCanonicalZeroKsuid()
    {
        KsuidParseResult result = new Ksuid().Parse("000000000000000000000000000");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.NormalizedText, Is.EqualTo("000000000000000000000000000"));
        Assert.That(result.Value.AdjustedSeconds(), Is.EqualTo(0));
        Assert.That(result.Value.UnixSeconds(), Is.EqualTo(KsuidLimits.EpochUnixSeconds));
        Assert.That(result.Value.PayloadBytes(), Is.EqualTo(new byte[16]));
    }

    [Test]
    public void Parse_RoundTripsArbitraryTwentyByteValue()
    {
        byte[] bytes = new byte[] { 0x00, 0x01, 0xE2, 0x40, 0x1D, 0xDE, 0xAA, 0xD7, 0x13, 0xF8, 0xA5, 0x9E, 0x5C, 0xA5, 0x7C, 0x2B, 0x47, 0x72, 0x6F, 0xC7 };
        KsuidValue value = new KsuidValue(bytes);

        KsuidParseResult result = new Ksuid().Parse(value.ToCanonicalString());

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.EqualsValue(value), Is.True);
        Assert.That(result.NormalizedText, Is.EqualTo(value.ToCanonicalString()));
    }

    [Test]
    public void Validate_ReturnsNormalizedValidationResult()
    {
        IdentifierValidationResult result = new Ksuid().Validate("000000000000000000000000000");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Ksuid));
        Assert.That(result.NormalizedText, Is.EqualTo("000000000000000000000000000"));
        Assert.That(result.Error, Is.EqualTo(""));
    }

    [Test]
    public void Parse_RejectsInvalidLengthCharactersAndOverflow()
    {
        AssertParseFailure(null, "KSUID text is null");
        AssertParseFailure("000", "expected 27 Base62 characters but found 3");
        AssertParseFailure("00000000000000000000000000_", "invalid Base62 character at index 26");
        AssertParseFailure("zzzzzzzzzzzzzzzzzzzzzzzzzzz", "KSUID text overflows 160 bits");
    }

    [Test]
    public void TryParse_DelegatesToParse()
    {
        KsuidParseResult result = new Ksuid().TryParse("000000000000000000000000000");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.NormalizedText, Is.EqualTo("000000000000000000000000000"));
    }

    [Test]
    public void ParseValue_ReturnsValueOrThrows()
    {
        KsuidValue value = new Ksuid().ParseValue("000000000000000000000000000");

        Assert.That(value.AdjustedSeconds(), Is.EqualTo(0));
        Assert.That(() => new Ksuid().ParseValue("not-a-ksuid"), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void Format_ReturnsCanonicalTextOrThrowsForNull()
    {
        KsuidValue value = new Ksuid().ParseValue("000000000000000000000000000");

        Assert.That(new Ksuid().Format(value), Is.EqualTo("000000000000000000000000000"));
        Assert.That(() => new Ksuid().Format(null), Throws.TypeOf<ArgumentNullException>());
    }

    static void AssertParseFailure(string? text, string error)
    {
        KsuidParseResult result = new Ksuid().Parse(text);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
    }
}
