using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class ComplexInvalidInputTests
{
    [Test]
    public void UuidValidation_RejectsNonRfcVariantAcrossEquivalentTextForms()
    {
        string canonical = "01890f13-66b8-7f7a-1f2b-6f3f9c2d8a1b";
        string compact = "01890F1366B87F7A1F2B6F3F9C2D8A1B";
        string urn = "URN:UUID:01890F13-66B8-7F7A-1F2B-6F3F9C2D8A1B";

        AssertValidationFailure(new Uuid().Validate(canonical), "UUID does not use RFC 9562 variant bits");
        AssertValidationFailure(new Uuid().Validate(compact), "UUID does not use RFC 9562 variant bits");
        AssertValidationFailure(new Uuid().Validate(urn), "UUID does not use RFC 9562 variant bits");
        AssertParseFailure(new Identifier().Parse(urn), "identifier kind is unknown");
    }

    [Test]
    public void EncodedFormatParsers_RejectInvalidTextWithoutParsedValues()
    {
        UlidParseResult ulid = new Ulid().Parse("01arz3ndektsv4rrffq69g5fau");
        KsuidParseResult ksuid = new Ksuid().Parse("zzzzzzzzzzzzzzzzzzzzzzzzzzz");
        ObjectIdParseResult objectId = new ObjectId().Parse("507f1f77bcf86cd79943901\n");

        Assert.That(ulid.Ok, Is.False);
        Assert.That(ulid.Value, Is.Null);
        Assert.That(ulid.NormalizedText, Is.EqualTo(""));
        Assert.That(ulid.Error, Is.EqualTo("invalid Crockford Base32 character at index 25"));
        Assert.That(ksuid.Ok, Is.False);
        Assert.That(ksuid.Value, Is.Null);
        Assert.That(ksuid.NormalizedText, Is.EqualTo(""));
        Assert.That(ksuid.Error, Is.EqualTo("KSUID text overflows 160 bits"));
        Assert.That(objectId.Ok, Is.False);
        Assert.That(objectId.Value, Is.Null);
        Assert.That(objectId.NormalizedText, Is.EqualTo(""));
        Assert.That(objectId.Error, Is.EqualTo("invalid hex character at index 23"));
    }

    [Test]
    public void SnowflakeParsing_RejectsOverflowAfterLeadingZeros()
    {
        SnowflakeSettings settings = SnowflakeSettings.TwitterDefault();

        SnowflakeParseResult direct = SnowflakeGenerator.ParseDecimalString("0009223372036854775808", settings);
        IdentifierValidationResult classified = new Identifier().Validate("0009223372036854775808");

        Assert.That(direct.Ok, Is.False);
        Assert.That(direct.Value, Is.Null);
        Assert.That(direct.NormalizedText, Is.EqualTo(""));
        Assert.That(direct.Error, Is.EqualTo("Snowflake decimal value exceeds 63-bit range"));
        AssertValidationFailure(classified, "identifier kind is unknown");
        Assert.That(() => new Identifier().NormalizeText("0009223372036854775808"), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void TimestampFacade_RejectsSpecialUuidValuesWithoutTimestampFields()
    {
        IdentifierTimestampResult nil = new Identifier().TimestampText("00000000-0000-0000-0000-000000000000");
        IdentifierTimestampResult max = new Identifier().TimestampText("ffffffff-ffff-ffff-ffff-ffffffffffff");

        AssertTimestampFailure(nil, "UUID does not use RFC 9562 variant bits");
        AssertTimestampFailure(max, "UUID does not use RFC 9562 variant bits");
    }

    static void AssertValidationFailure(IdentifierValidationResult result, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
    }

    static void AssertParseFailure(IdentifierParseResult result, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
    }

    static void AssertTimestampFailure(IdentifierTimestampResult result, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.UnixMilliseconds, Is.EqualTo(0));
        Assert.That(result.UnixSeconds, Is.EqualTo(0));
        Assert.That(result.Time, Is.EqualTo(DateTimeOffset.UnixEpoch));
        Assert.That(result.Error, Is.EqualTo(error));
    }
}
