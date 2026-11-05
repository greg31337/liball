using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class IdentifierCrossFormatTests
{
    [Test]
    public void AmbiguousNumericStrings_PreferSpecificFixedWidthFormatsBeforeSnowflake()
    {
        AssertClassification("00000000000000000000000000000000", IdentifierKind.Uuid, "00000000-0000-0000-0000-000000000000");
        AssertClassification("00000000000000000000000000", IdentifierKind.Ulid, "00000000000000000000000000");
        AssertClassification("000000000000000000000000000", IdentifierKind.Ksuid, "000000000000000000000000000");
        AssertClassification("000000000000000000000000", IdentifierKind.ObjectId, "000000000000000000000000");
    }

    [Test]
    public void AmbiguousDefaultNanoIdAndDecimalText_PrefersSnowflake()
    {
        AssertClassification("000000000000000000000", IdentifierKind.Snowflake, "0");
    }

    [Test]
    public void Normalize_AppliesFormatSpecificCanonicalText()
    {
        string nanoId = NanoIdSettings.DefaultAlphabet.Substring(0, NanoIdSettings.DefaultSize);

        Assert.That(new Identifier().NormalizeText("550E8400E29B41D4A716446655440000"), Is.EqualTo("550e8400-e29b-41d4-a716-446655440000"));
        Assert.That(new Identifier().NormalizeText("01arz3ndektsv4rrffq69g5fav"), Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(new Identifier().NormalizeText("507F1F77BCF86CD799439011"), Is.EqualTo("507f1f77bcf86cd799439011"));
        Assert.That(new Identifier().NormalizeText("000000123"), Is.EqualTo("123"));
        Assert.That(new Identifier().NormalizeText(nanoId), Is.EqualTo(nanoId));
    }

    [Test]
    public void Timestamp_WorksForAllTimestampBearingFormats()
    {
        SnowflakeSettings snowflakeSettings = SnowflakeSettings.TwitterDefault();
        string uuid = new Uuid().NewV7AtUnixMilliseconds(1469922850259).ToCanonicalString();
        string ulid = new Ulid().NewAtUnixMilliseconds(1469922850259).ToCanonicalString();
        string ksuid = new Ksuid().NewAtUnixSeconds(KsuidLimits.EpochUnixSeconds + 12345).ToCanonicalString();
        string objectId = ObjectIdGenerator.FromParts(1600000000, new byte[] { 1, 2, 3, 4, 5 }, 7).ToCanonicalString();
        string snowflake = SnowflakeGenerator.Pack(snowflakeSettings.EpochUnixMilliseconds + 1234, 0, 0, snowflakeSettings).ToString();

        Assert.That(new Identifier().TimestampText(uuid).Kind, Is.EqualTo(IdentifierKind.Uuid));
        Assert.That(new Identifier().TimestampText(ulid).Kind, Is.EqualTo(IdentifierKind.Ulid));
        Assert.That(new Identifier().TimestampText(ksuid).Kind, Is.EqualTo(IdentifierKind.Ksuid));
        Assert.That(new Identifier().TimestampText(objectId).Kind, Is.EqualTo(IdentifierKind.ObjectId));
        Assert.That(new Identifier().TimestampText(snowflake).Kind, Is.EqualTo(IdentifierKind.Snowflake));
    }

    [Test]
    public void UnsupportedValues_ReturnFailureResults()
    {
        AssertFailure(new Identifier().Validate("550e8400-e29b-41d4-1716-446655440000"), "identifier kind is unknown");
        AssertParseFailure(new Identifier().Parse("01ARZ3NDEKTSV4RRFFQ69G5FAI"), "identifier kind is unknown");
        AssertFailure(new Identifier().Validate("507f1f77bcf86cd79943901z"), "identifier kind is unknown");
        AssertFailure(new Identifier().Validate(""), "identifier kind is unknown");
        AssertFailure(new Identifier().Validate(null), "identifier text is null");
    }

    [Test]
    public void Compare_UsesNormalizedTextForCaseInsensitiveFormats()
    {
        Assert.That(new Identifier().CompareText("507F1F77BCF86CD799439011", "507f1f77bcf86cd799439011"), Is.EqualTo(0));
        Assert.That(new Identifier().CompareText("01arz3ndektsv4rrffq69g5fav", "01ARZ3NDEKTSV4RRFFQ69G5FAV"), Is.EqualTo(0));
        Assert.That(new Identifier().CompareText("000000123", "123"), Is.EqualTo(0));
    }

    static void AssertClassification(string text, IdentifierKind kind, string normalizedText)
    {
        IdentifierValidationResult result = new Identifier().Classify(text);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(kind));
        Assert.That(result.NormalizedText, Is.EqualTo(normalizedText));
    }

    static void AssertFailure(IdentifierValidationResult result, string error)
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
}
