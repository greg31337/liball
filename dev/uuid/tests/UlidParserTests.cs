using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UlidParserTests
{
    [Test]
    public void Parse_AcceptsCanonicalUlid()
    {
        UlidParseResult result = new Ulid().Parse("01ARZ3NDEKTSV4RRFFQ69G5FAV");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.NormalizedText, Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(result.Value.UnixMilliseconds(), Is.EqualTo(1469922850259));
        Assert.That(result.Value.RandomnessBytes(), Is.EqualTo(new byte[] { 0xD6, 0x76, 0x4C, 0x61, 0xEF, 0xB9, 0x93, 0x02, 0xBD, 0x5B }));
    }

    [Test]
    public void Parse_AcceptsLowercaseAndNormalizesToUppercase()
    {
        UlidParseResult result = new Ulid().Parse("01arz3ndektsv4rrffq69g5fav");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.NormalizedText, Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.ToCanonicalString(), Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
    }

    [Test]
    public void Validate_ReturnsNormalizedValidationResult()
    {
        IdentifierValidationResult result = new Ulid().Validate("01arz3ndektsv4rrffq69g5fav");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Ulid));
        Assert.That(result.NormalizedText, Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(result.Error, Is.EqualTo(""));
    }

    [Test]
    public void Parse_RejectsInvalidLengthCharactersAndOverflow()
    {
        AssertParseFailure(null, "ULID text is null");
        AssertParseFailure("01ARZ3NDEK", "expected 26 Crockford Base32 characters but found 10");
        AssertParseFailure("01ARZ3NDEKTSV4RRFFQ69G5FAI", "invalid Crockford Base32 character at index 25");
        AssertParseFailure("81ARZ3NDEKTSV4RRFFQ69G5FAV", "ULID text overflows 128 bits");
    }

    [Test]
    public void TryParse_DelegatesToParse()
    {
        UlidParseResult result = new Ulid().TryParse("01ARZ3NDEKTSV4RRFFQ69G5FAV");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.NormalizedText, Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
    }

    [Test]
    public void ParseValue_ReturnsValueOrThrows()
    {
        UlidValue value = new Ulid().ParseValue("01ARZ3NDEKTSV4RRFFQ69G5FAV");

        Assert.That(value.UnixMilliseconds(), Is.EqualTo(1469922850259));
        Assert.That(() => new Ulid().ParseValue("not-a-ulid"), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void Format_ReturnsCanonicalTextOrThrowsForNull()
    {
        UlidValue value = new Ulid().ParseValue("01arz3ndektsv4rrffq69g5fav");

        Assert.That(new Ulid().Format(value), Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(() => new Ulid().Format(null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void UuidCompatibleByteFacade_ConvertsToAndFromRawBytes()
    {
        byte[] bytes = new byte[] { 0x01, 0x56, 0x3E, 0x3A, 0xB5, 0xD3, 0xD6, 0x76, 0x4C, 0x61, 0xEF, 0xB9, 0x93, 0x02, 0xBD, 0x5B };

        UlidValue value = new Ulid().FromUuidBytes(bytes);
        byte[] roundTrip = new Ulid().ToUuidBytes(value);
        roundTrip[0] = 0xFF;

        Assert.That(value.ToCanonicalString(), Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(new Ulid().ToUuidBytes(value), Is.EqualTo(bytes));
        Assert.That(() => new Ulid().FromUuidBytes(new byte[15]), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new Ulid().ToUuidBytes(null), Throws.TypeOf<ArgumentNullException>());
    }

    static void AssertParseFailure(string? text, string error)
    {
        UlidParseResult result = new Ulid().Parse(text);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
    }
}
