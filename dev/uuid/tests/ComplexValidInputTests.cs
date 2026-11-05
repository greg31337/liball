using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class ComplexValidInputTests
{
    [Test]
    public void IdentifierFacade_AcceptsEquivalentUuidV7TextForms()
    {
        UuidValue value = new Uuid().NewV7AtUnixMilliseconds(1645557742123);
        string canonical = value.ToUpperString();
        string compact = value.ToCompactUpperString();
        string urn = "URN:UUID:" + canonical;

        IdentifierParseResult canonicalResult = new Identifier().Parse(canonical);
        IdentifierParseResult compactResult = new Identifier().Parse(compact);
        IdentifierParseResult urnResult = new Identifier().Parse(urn);

        AssertParsedUuid(canonicalResult, value.ToCanonicalString());
        AssertParsedUuid(compactResult, value.ToCanonicalString());
        AssertParsedUuid(urnResult, value.ToCanonicalString());
        AssertTimestamp(new Identifier().TimestampText(compact), IdentifierKind.Uuid, 1645557742123);
        AssertTimestamp(new Identifier().TimestampText(urn), IdentifierKind.Uuid, 1645557742123);
    }

    [Test]
    public void IdentifierTimestamp_ExtractsConstructedValidTimestampValuesWithPayloads()
    {
        byte[] ulidBytes = new byte[16];
        BigEndianTools.WriteUInt48(ulidBytes, 0, 1645557742123);
        FillIncrementing(ulidBytes, 6, 0xA0);
        string ulid = new UlidValue(ulidBytes).ToCanonicalString().ToLowerInvariant();

        byte[] ksuidBytes = new byte[20];
        BigEndianTools.WriteUInt32(ksuidBytes, 0, 0x01020304);
        FillIncrementing(ksuidBytes, 4, 0x10);
        string ksuid = new KsuidValue(ksuidBytes).ToCanonicalString();

        ObjectIdValue objectId = ObjectIdGenerator.FromParts(0xF1234567, new byte[] { 0xA0, 0xB1, 0xC2, 0xD3, 0xE4 }, 0x0A0B0C);

        SnowflakeSettings snowflakeSettings = SnowflakeSettings.TwitterDefault();
        ulong snowflakeRaw = SnowflakeGenerator.Pack(snowflakeSettings.EpochUnixMilliseconds + 987654321, 513, 4094, snowflakeSettings);

        AssertTimestamp(new Identifier().TimestampText(ulid), IdentifierKind.Ulid, 1645557742123);
        Assert.That(new Identifier().NormalizeText(ulid), Is.EqualTo(new UlidValue(ulidBytes).ToCanonicalString()));
        AssertTimestamp(new Identifier().TimestampText(ksuid), IdentifierKind.Ksuid, (KsuidLimits.EpochUnixSeconds + 0x01020304L) * 1000);
        AssertTimestamp(new Identifier().TimestampText(objectId.ToUpperHexString()), IdentifierKind.ObjectId, 0xF1234567L * 1000);
        AssertTimestamp(new Identifier().TimestampText("000" + snowflakeRaw.ToString()), IdentifierKind.Snowflake, snowflakeSettings.EpochUnixMilliseconds + 987654321);
    }

    [Test]
    public void IdentifierCompare_OrdersLargeSnowflakeDecimalStringsByNumericValue()
    {
        string smaller = "9223372036854775806";
        string largerWithLeadingZeros = "0009223372036854775807";

        Assert.That(new Identifier().CompareText(largerWithLeadingZeros, smaller), Is.GreaterThan(0));
        Assert.That(new Identifier().CompareText(smaller, largerWithLeadingZeros), Is.LessThan(0));
        Assert.That(new Identifier().NormalizeText(largerWithLeadingZeros), Is.EqualTo("9223372036854775807"));
    }

    static void AssertParsedUuid(IdentifierParseResult result, string normalizedText)
    {
        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Uuid));
        Assert.That(result.NormalizedText, Is.EqualTo(normalizedText));
        Assert.That(result.Error, Is.EqualTo(""));
    }

    static void AssertTimestamp(IdentifierTimestampResult result, IdentifierKind kind, long unixMilliseconds)
    {
        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(kind));
        Assert.That(result.UnixMilliseconds, Is.EqualTo(unixMilliseconds));
        Assert.That(result.UnixSeconds, Is.EqualTo(UnixTimeTools.MillisecondsToSeconds(unixMilliseconds)));
        Assert.That(result.Time, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds)));
        Assert.That(result.Error, Is.EqualTo(""));
    }

    static void FillIncrementing(byte[] bytes, int startIndex, byte firstValue)
    {
        int index = startIndex;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(firstValue + index - startIndex);
            index = index + 1;
        }
    }
}
