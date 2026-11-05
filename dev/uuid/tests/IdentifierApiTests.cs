using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class IdentifierApiTests
{
    [Test]
    public void ValidateParseAndNormalize_ReturnCanonicalText()
    {
        IdentifierValidationResult validation = new Identifier().Validate("507F1F77BCF86CD799439011");
        IdentifierParseResult parse = new Identifier().Parse("507F1F77BCF86CD799439011");
        string normalized = new Identifier().NormalizeText("507F1F77BCF86CD799439011");

        Assert.That(validation.Ok, Is.True);
        Assert.That(validation.Kind, Is.EqualTo(IdentifierKind.ObjectId));
        Assert.That(validation.NormalizedText, Is.EqualTo("507f1f77bcf86cd799439011"));
        Assert.That(parse.Ok, Is.True);
        Assert.That(parse.Kind, Is.EqualTo(IdentifierKind.ObjectId));
        Assert.That(parse.NormalizedText, Is.EqualTo("507f1f77bcf86cd799439011"));
        Assert.That(normalized, Is.EqualTo("507f1f77bcf86cd799439011"));
    }

    [Test]
    public void ParseAndNormalize_RejectUnknownText()
    {
        IdentifierParseResult parse = new Identifier().Parse("not an identifier");

        Assert.That(parse.Ok, Is.False);
        Assert.That(parse.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(parse.Error, Is.EqualTo("identifier kind is unknown"));
        Assert.That(() => new Identifier().NormalizeText("not an identifier"), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void Timestamp_ExtractsUuidV7Timestamp()
    {
        UuidValue value = new Uuid().NewV7AtUnixMilliseconds(1469922850259);

        IdentifierTimestampResult result = new Identifier().TimestampText(value.ToCanonicalString());

        AssertTimestamp(result, IdentifierKind.Uuid, 1469922850259, 1469922850);
    }

    [Test]
    public void Timestamp_ExtractsUlidTimestamp()
    {
        UlidValue value = new Ulid().NewAtUnixMilliseconds(1469922850259);

        IdentifierTimestampResult result = new Identifier().TimestampText(value.ToCanonicalString());

        AssertTimestamp(result, IdentifierKind.Ulid, 1469922850259, 1469922850);
    }

    [Test]
    public void Timestamp_ExtractsKsuidTimestamp()
    {
        long unixSeconds = KsuidLimits.EpochUnixSeconds + 12345;
        KsuidValue value = new Ksuid().NewAtUnixSeconds(unixSeconds);

        IdentifierTimestampResult result = new Identifier().TimestampText(value.ToCanonicalString());

        AssertTimestamp(result, IdentifierKind.Ksuid, unixSeconds * 1000, unixSeconds);
    }

    [Test]
    public void Timestamp_ExtractsObjectIdTimestamp()
    {
        ObjectIdValue value = ObjectIdGenerator.FromParts(1600000000, new byte[] { 1, 2, 3, 4, 5 }, 7);

        IdentifierTimestampResult result = new Identifier().TimestampText(value.ToCanonicalString());

        AssertTimestamp(result, IdentifierKind.ObjectId, 1600000000000, 1600000000);
    }

    [Test]
    public void Timestamp_ExtractsSnowflakeTimestamp()
    {
        SnowflakeSettings settings = SnowflakeSettings.TwitterDefault();
        ulong raw = SnowflakeGenerator.Pack(settings.EpochUnixMilliseconds + 1234, 0, 0, settings);

        IdentifierTimestampResult result = new Identifier().TimestampText(raw.ToString());

        AssertTimestamp(result, IdentifierKind.Snowflake, settings.EpochUnixMilliseconds + 1234, 1288834975);
    }

    [Test]
    public void Timestamp_ReturnsFailureForFormatsWithoutSupportedTimestamp()
    {
        string nanoId = NanoIdSettings.DefaultAlphabet.Substring(0, NanoIdSettings.DefaultSize);
        IdentifierTimestampResult uuid = new Identifier().TimestampText("550e8400-e29b-41d4-a716-446655440000");
        IdentifierTimestampResult nano = new Identifier().TimestampText(nanoId);
        IdentifierTimestampResult unknown = new Identifier().TimestampText("not an identifier");

        Assert.That(uuid.Ok, Is.False);
        Assert.That(uuid.Error, Is.EqualTo("UUID version does not carry a supported timestamp"));
        Assert.That(nano.Ok, Is.False);
        Assert.That(nano.Error, Is.EqualTo("identifier kind does not carry a timestamp"));
        Assert.That(unknown.Ok, Is.False);
        Assert.That(unknown.Error, Is.EqualTo("identifier kind is unknown"));
    }

    [Test]
    public void Compare_OrdersValuesWithinSameKind()
    {
        Assert.That(new Identifier().CompareText("000000000000000000000001", "000000000000000000000010"), Is.LessThan(0));
        Assert.That(new Identifier().CompareText("000000000000000000000010", "000000000000000000000001"), Is.GreaterThan(0));
        Assert.That(new Identifier().CompareText("000000000000000000000001", "000000000000000000000001"), Is.EqualTo(0));
        Assert.That(new Identifier().CompareText("9", "10"), Is.LessThan(0));
    }

    [Test]
    public void Compare_OrdersDifferentKindsByIdentifierKind()
    {
        Assert.That(new Identifier().CompareText("550e8400-e29b-41d4-a716-446655440000", "01ARZ3NDEKTSV4RRFFQ69G5FAV"), Is.LessThan(0));
        Assert.That(new Identifier().CompareText("01ARZ3NDEKTSV4RRFFQ69G5FAV", "550e8400-e29b-41d4-a716-446655440000"), Is.GreaterThan(0));
    }

    [Test]
    public void Compare_RejectsUnknownText()
    {
        Assert.That(() => new Identifier().CompareText("not an identifier", "1"), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new Identifier().CompareText("1", "not an identifier"), Throws.TypeOf<IdentifierException>());
    }

    static void AssertTimestamp(IdentifierTimestampResult result, IdentifierKind kind, long unixMilliseconds, long unixSeconds)
    {
        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(kind));
        Assert.That(result.UnixMilliseconds, Is.EqualTo(unixMilliseconds));
        Assert.That(result.UnixSeconds, Is.EqualTo(unixSeconds));
        Assert.That(result.Time, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds)));
        Assert.That(result.Error, Is.EqualTo(""));
    }
}
