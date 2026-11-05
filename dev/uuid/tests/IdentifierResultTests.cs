using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class IdentifierResultTests
{
    [Test]
    public void ParseSuccess_CarriesKindAndNormalizedText()
    {
        IdentifierParseResult result = IdentifierParseResult.Success(IdentifierKind.Uuid, "00000000-0000-0000-0000-000000000000");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Uuid));
        Assert.That(result.NormalizedText, Is.EqualTo("00000000-0000-0000-0000-000000000000"));
        Assert.That(result.Error, Is.EqualTo(""));
        Assert.That(result.Code, Is.EqualTo(IdentifierErrorCode.None));
    }

    [Test]
    public void ParseFailure_CarriesErrorWithoutParsedValue()
    {
        IdentifierParseResult result = IdentifierParseResult.Failure(IdentifierErrorCode.InvalidLength, "invalid length");

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo("invalid length"));
        Assert.That(result.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
    }

    [Test]
    public void TimestampSuccess_CarriesAllTimestampRepresentations()
    {
        DateTimeOffset time = DateTimeOffset.FromUnixTimeMilliseconds(1234567890);
        IdentifierTimestampResult result = IdentifierTimestampResult.Success(IdentifierKind.Ulid, 1234567890, 1234567, time);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Ulid));
        Assert.That(result.UnixMilliseconds, Is.EqualTo(1234567890));
        Assert.That(result.UnixSeconds, Is.EqualTo(1234567));
        Assert.That(result.Time, Is.EqualTo(time));
        Assert.That(result.Error, Is.EqualTo(""));
        Assert.That(result.Code, Is.EqualTo(IdentifierErrorCode.None));
    }

    [Test]
    public void TimestampFailure_UsesEpochAsNeutralTime()
    {
        IdentifierTimestampResult result = IdentifierTimestampResult.Failure(IdentifierErrorCode.UnsupportedKind, "format has no timestamp");

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.UnixMilliseconds, Is.EqualTo(0));
        Assert.That(result.UnixSeconds, Is.EqualTo(0));
        Assert.That(result.Time, Is.EqualTo(DateTimeOffset.UnixEpoch));
        Assert.That(result.Error, Is.EqualTo("format has no timestamp"));
        Assert.That(result.Code, Is.EqualTo(IdentifierErrorCode.UnsupportedKind));
    }

    [Test]
    public void ValidationSuccess_CarriesKindAndNormalizedText()
    {
        IdentifierValidationResult result = IdentifierValidationResult.Success(IdentifierKind.ObjectId, "000000000000000000000000");

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.ObjectId));
        Assert.That(result.NormalizedText, Is.EqualTo("000000000000000000000000"));
        Assert.That(result.Error, Is.EqualTo(""));
        Assert.That(result.Code, Is.EqualTo(IdentifierErrorCode.None));
    }

    [Test]
    public void ValidationFailure_CarriesErrorWithoutNormalizedText()
    {
        IdentifierValidationResult result = IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidCharacter, "invalid character");

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo("invalid character"));
        Assert.That(result.Code, Is.EqualTo(IdentifierErrorCode.InvalidCharacter));
    }
}
