using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UuidTimestampTests
{
    [Test]
    public void Extract_ReturnsTimestampForVersionOneUuid()
    {
        UuidValue value = new Uuid().ParseValue("c232ab00-9414-11ec-b3c8-9f6bdeced846");

        IdentifierTimestampResult result = UuidTimestamp.Extract(value);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Uuid));
        Assert.That(result.UnixMilliseconds, Is.EqualTo(1645557742000));
        Assert.That(result.UnixSeconds, Is.EqualTo(1645557742));
        Assert.That(result.Time, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1645557742000)));
        Assert.That(UuidTimestamp.ExtractV1GregorianTimestamp(value), Is.EqualTo(138648505420000000));
    }

    [Test]
    public void Extract_ReturnsSameTimestampForConvertedVersionSixUuid()
    {
        UuidValue value = new Uuid().ParseValue("1ec9414c-232a-6b00-b3c8-9f6bdeced846");

        IdentifierTimestampResult result = UuidTimestamp.Extract(value);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.UnixMilliseconds, Is.EqualTo(1645557742000));
        Assert.That(UuidTimestamp.ExtractV6GregorianTimestamp(value), Is.EqualTo(138648505420000000));
    }

    [Test]
    public void Extract_ReturnsUnixMillisecondsForVersionSevenUuid()
    {
        UuidValue value = new Uuid().NewV7AtUnixMilliseconds(1469922850259);

        IdentifierTimestampResult result = value.Timestamp();

        Assert.That(result.Ok, Is.True);
        Assert.That(result.UnixMilliseconds, Is.EqualTo(1469922850259));
        Assert.That(value.UnixMilliseconds(), Is.EqualTo(1469922850259));
        Assert.That(value.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1469922850259)));
    }

    [Test]
    public void FacadeExtractsTimestampFromValueOrText()
    {
        UuidValue value = new Uuid().NewV7AtUnixMilliseconds(123456789);

        IdentifierTimestampResult fromValue = new Uuid().TimestampValue(value);
        IdentifierTimestampResult fromText = new Uuid().TimestampText(value.ToCanonicalString());

        Assert.That(fromValue.Ok, Is.True);
        Assert.That(fromText.Ok, Is.True);
        Assert.That(fromValue.UnixMilliseconds, Is.EqualTo(123456789));
        Assert.That(fromText.UnixMilliseconds, Is.EqualTo(123456789));
    }

    [Test]
    public void Extract_RejectsUnsupportedVersionsAndVariants()
    {
        UuidValue version4 = new Uuid().ParseValue("01890f13-66b8-4f7a-9f2b-6f3f9c2d8a1b");
        UuidValue wrongVariant = new Uuid().ParseValue("01890f13-66b8-7f7a-1f2b-6f3f9c2d8a1b");

        IdentifierTimestampResult unsupported = UuidTimestamp.Extract(version4);
        IdentifierTimestampResult invalidVariant = UuidTimestamp.Extract(wrongVariant);

        Assert.That(unsupported.Ok, Is.False);
        Assert.That(unsupported.Error, Is.EqualTo("UUID version does not carry a supported timestamp"));
        Assert.That(invalidVariant.Ok, Is.False);
        Assert.That(invalidVariant.Error, Is.EqualTo("UUID does not use RFC 9562 variant bits"));
        Assert.That(() => UuidTimestamp.ExtractUnixMilliseconds(version4), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void Extract_PropagatesParseFailureForText()
    {
        IdentifierTimestampResult result = new Uuid().TimestampText("not-a-uuid");

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Error, Is.EqualTo("UUID text length must be 32 compact hex characters, 36 canonical characters, or urn:uuid: plus canonical UUID"));
    }

    [Test]
    public void GregorianTimestampToUnixMilliseconds_FloorsNegativeFractions()
    {
        Assert.That(UuidTimestamp.GregorianTimestampToUnixMilliseconds(UuidTimestamp.GregorianUnixOffsetTicks), Is.EqualTo(0));
        Assert.That(UuidTimestamp.GregorianTimestampToUnixMilliseconds(UuidTimestamp.GregorianUnixOffsetTicks + 9999), Is.EqualTo(0));
        Assert.That(UuidTimestamp.GregorianTimestampToUnixMilliseconds(UuidTimestamp.GregorianUnixOffsetTicks + 10000), Is.EqualTo(1));
        Assert.That(UuidTimestamp.GregorianTimestampToUnixMilliseconds(UuidTimestamp.GregorianUnixOffsetTicks - 1), Is.EqualTo(-1));
        Assert.That(UuidTimestamp.GregorianTimestampToUnixMilliseconds(UuidTimestamp.GregorianUnixOffsetTicks - 10000), Is.EqualTo(-1));
        Assert.That(UuidTimestamp.GregorianTimestampToUnixMilliseconds(UuidTimestamp.GregorianUnixOffsetTicks - 10001), Is.EqualTo(-2));
    }
}
