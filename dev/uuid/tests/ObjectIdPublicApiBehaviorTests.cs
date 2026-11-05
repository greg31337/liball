using System;
using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class ObjectIdPublicApiBehaviorTests
{
    [Test]
    public void ObjectIdInterface_ExposesEveryPublicOperation()
    {
        IObjectId objectId = new ObjectId();
        long before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        ObjectIdValue current = objectId.New();
        long after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        ObjectIdValue atTime = objectId.NewAt(DateTimeOffset.FromUnixTimeSeconds(1600000000));
        ObjectIdValue atSeconds = objectId.NewAtUnixSeconds(1600000000);
        ObjectIdParseResult parsed = objectId.Parse("507F1F77BCF86CD799439011");
        ObjectIdParseResult tried = objectId.TryParse("507f1f77bcf86cd799439011");
        ObjectIdValue parsedValue = objectId.ParseValue("507f1f77bcf86cd799439011");

        Assert.That(current.UnixSeconds(), Is.GreaterThanOrEqualTo(before));
        Assert.That(current.UnixSeconds(), Is.LessThanOrEqualTo(after));
        Assert.That(atTime.UnixSeconds(), Is.EqualTo(1600000000));
        Assert.That(atSeconds.UnixSeconds(), Is.EqualTo(1600000000));
        Assert.That(parsed.Ok, Is.True);
        Assert.That(tried.Ok, Is.True);
        Assert.That(parsedValue.EqualsValue(parsed.Value), Is.True);
        Assert.That(objectId.Format(parsedValue), Is.EqualTo("507f1f77bcf86cd799439011"));
        Assert.That(objectId.FormatUpper(parsedValue), Is.EqualTo("507F1F77BCF86CD799439011"));
        Assert.That(objectId.Validate("507F1F77BCF86CD799439011").NormalizedText, Is.EqualTo("507f1f77bcf86cd799439011"));
        Assert.That(objectId.CompareValues(parsedValue, parsedValue), Is.EqualTo(0));
    }

    [Test]
    public void ObjectIdValueInterfaces_ExposeEveryValueOperation()
    {
        ObjectIdValue concrete = new ObjectIdValue(ExampleBytes());
        IObjectIdValue value = concrete;
        IIdentifierValue identifier = concrete;
        ITimestampedIdentifierValue timestamped = concrete;
        byte[] destination = new byte[12];

        value.CopyTo(destination);

        Assert.That(value.ToBytes(), Is.EqualTo(ExampleBytes()));
        Assert.That(destination, Is.EqualTo(ExampleBytes()));
        Assert.That(value.UnixSeconds(), Is.EqualTo(1600000000));
        Assert.That(value.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(1600000000)));
        Assert.That(value.ToHexString(), Is.EqualTo("5f5e10001122334455010203"));
        Assert.That(value.ToUpperHexString(), Is.EqualTo("5F5E10001122334455010203"));
        Assert.That(value.EqualsValue(new ObjectIdValue(ExampleBytes())), Is.True);
        Assert.That(value.EqualsBytes(ExampleBytes()), Is.True);
        Assert.That(value.CompareTo(new ObjectIdValue(ExampleBytes())), Is.EqualTo(0));
        Assert.That(identifier.Kind(), Is.EqualTo(IdentifierKind.ObjectId));
        Assert.That(identifier.ToCanonicalString(), Is.EqualTo("5f5e10001122334455010203"));
        Assert.That(timestamped.Timestamp().Ok, Is.True);
        Assert.That(timestamped.Timestamp().Kind, Is.EqualTo(IdentifierKind.ObjectId));
        Assert.That(timestamped.Timestamp().UnixSeconds, Is.EqualTo(1600000000));
        Assert.That(timestamped.Timestamp().UnixMilliseconds, Is.EqualTo(1600000000000));
    }

    [Test]
    public void Limits_HaveExactSpecificationValues()
    {
        Assert.That(ObjectIdLimits.ByteLength, Is.EqualTo(12));
        Assert.That(ObjectIdLimits.ProcessUniqueByteLength, Is.EqualTo(5));
        Assert.That(ObjectIdLimits.MaxCounter, Is.EqualTo(16777215));
        Assert.That(ObjectIdLimits.MaxUnixSeconds, Is.EqualTo(4294967295));
    }

    [TestCase(null, IdentifierErrorCode.NullInput, "ObjectId text is null", "hex text is null")]
    [TestCase("short", IdentifierErrorCode.InvalidLength, "expected 24 hex characters but found 5", "expected 24 hex characters but found 5")]
    [TestCase("507f1f77bcf86cd79943901z", IdentifierErrorCode.InvalidCharacter, "invalid hex character at index 23", "invalid hex character at index 23")]
    public void ParseValidateAndThrowingParse_PreserveEveryFailure(string? text, IdentifierErrorCode code, string error, string validationError)
    {
        IObjectId objectId = new ObjectId();

        ObjectIdParseResult parsed = objectId.Parse(text);
        ObjectIdParseResult tried = objectId.TryParse(text);
        IdentifierValidationResult validation = objectId.Validate(text);

        AssertParseFailure(parsed, code, error);
        AssertParseFailure(tried, code, error);
        AssertValidationFailure(validation, code, validationError);
        AssertIdentifierException(() => objectId.ParseValue(text), code, error);
    }

    [Test]
    public void MongoCanonicalVector_ExposesTimestampBytesFormattingAndRoundTrip()
    {
        IObjectId objectId = new ObjectId();

        ObjectIdValue value = objectId.ParseValue("507F1F77BCF86CD799439011");
        IdentifierTimestampResult timestamp = value.Timestamp();

        Assert.That(value.ToBytes(), Is.EqualTo(new byte[] { 0x50, 0x7F, 0x1F, 0x77, 0xBC, 0xF8, 0x6C, 0xD7, 0x99, 0x43, 0x90, 0x11 }));
        Assert.That(value.UnixSeconds(), Is.EqualTo(1350508407));
        Assert.That(value.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(1350508407)));
        Assert.That(timestamp.Ok, Is.True);
        Assert.That(timestamp.Code, Is.EqualTo(IdentifierErrorCode.None));
        Assert.That(timestamp.UnixSeconds, Is.EqualTo(1350508407));
        Assert.That(timestamp.UnixMilliseconds, Is.EqualTo(1350508407000));
        Assert.That(objectId.Format(value), Is.EqualTo("507f1f77bcf86cd799439011"));
        Assert.That(objectId.FormatUpper(value), Is.EqualTo("507F1F77BCF86CD799439011"));
        ObjectIdParseResult roundTrip = objectId.Parse(objectId.Format(value));
        Assert.That(roundTrip.Value, Is.Not.Null);
        if (roundTrip.Value == null)
        {
            throw new InvalidOperationException(roundTrip.Error);
        }

        Assert.That(roundTrip.Value.EqualsValue(value), Is.True);
    }

    [Test]
    public void Generation_AcceptsUnsignedTimestampBoundariesAndRejectsOutsideValues()
    {
        IObjectId objectId = new ObjectId();

        ObjectIdValue minimum = objectId.NewAtUnixSeconds(0);
        ObjectIdValue maximum = objectId.NewAtUnixSeconds(ObjectIdLimits.MaxUnixSeconds);
        ObjectIdValue minimumAt = objectId.NewAt(DateTimeOffset.UnixEpoch);
        ObjectIdValue maximumAt = objectId.NewAt(DateTimeOffset.FromUnixTimeSeconds(ObjectIdLimits.MaxUnixSeconds));

        Assert.That(minimum.UnixSeconds(), Is.EqualTo(0));
        Assert.That(maximum.UnixSeconds(), Is.EqualTo(ObjectIdLimits.MaxUnixSeconds));
        Assert.That(minimumAt.UnixSeconds(), Is.EqualTo(0));
        Assert.That(maximumAt.UnixSeconds(), Is.EqualTo(ObjectIdLimits.MaxUnixSeconds));
        Assert.That(maximum.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(ObjectIdLimits.MaxUnixSeconds)));
        AssertIdentifierException(() => objectId.NewAtUnixSeconds(-1), IdentifierErrorCode.TimestampOverflow, "ObjectId timestamp is outside the unsigned 32-bit range");
        AssertIdentifierException(() => objectId.NewAtUnixSeconds(ObjectIdLimits.MaxUnixSeconds + 1), IdentifierErrorCode.TimestampOverflow, "ObjectId timestamp is outside the unsigned 32-bit range");
        AssertIdentifierException(() => objectId.NewAt(DateTimeOffset.UnixEpoch.AddSeconds(-1)), IdentifierErrorCode.TimestampOverflow, "ObjectId timestamp is outside the unsigned 32-bit range");
        AssertIdentifierException(() => objectId.NewAt(DateTimeOffset.FromUnixTimeSeconds(ObjectIdLimits.MaxUnixSeconds + 1)), IdentifierErrorCode.TimestampOverflow, "ObjectId timestamp is outside the unsigned 32-bit range");
    }

    [Test]
    public void ConstructionCopyEqualityAndFormatting_CoverNullAndLengthBoundaries()
    {
        byte[] source = ExampleBytes();
        ObjectIdValue value = new ObjectIdValue(source);
        byte[] destination = new byte[13];
        destination[12] = 99;

        source[0] = 255;
        value.CopyTo(destination);
        destination[1] = 255;

        Assert.That(value.ToBytes(), Is.EqualTo(ExampleBytes()));
        Assert.That(destination[12], Is.EqualTo(99));
        Assert.That(value.EqualsValue(null), Is.False);
        Assert.That(value.EqualsBytes(ExampleBytes()), Is.True);
        Assert.That(value.EqualsBytes(new byte[12]), Is.False);
        Assert.That(value.EqualsBytes(new byte[11]), Is.False);
        Assert.That(value.EqualsBytes(null), Is.False);

        ArgumentNullException constructorNull = Assert.Throws<ArgumentNullException>(() => new ObjectIdValue(null));
        IdentifierException constructorShort = Assert.Throws<IdentifierException>(() => new ObjectIdValue(new byte[11]));
        IdentifierException constructorLong = Assert.Throws<IdentifierException>(() => new ObjectIdValue(new byte[13]));
        ArgumentNullException destinationNull = Assert.Throws<ArgumentNullException>(() => value.CopyTo(null));
        IdentifierException destinationLength = Assert.Throws<IdentifierException>(() => value.CopyTo(new byte[11]));
        ArgumentNullException formatNull = Assert.Throws<ArgumentNullException>(() => new ObjectId().Format(null));
        ArgumentNullException upperNull = Assert.Throws<ArgumentNullException>(() => new ObjectId().FormatUpper(null));

        Assert.That(constructorNull.ParamName, Is.EqualTo("bytes"));
        Assert.That(constructorShort.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(constructorLong.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(constructorShort.Message, Is.EqualTo("ObjectId byte array must contain exactly 12 bytes"));
        Assert.That(destinationNull.ParamName, Is.EqualTo("destination"));
        Assert.That(destinationLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(destinationLength.Message, Is.EqualTo("destination must have room for 12 bytes"));
        Assert.That(formatNull.ParamName, Is.EqualTo("value"));
        Assert.That(upperNull.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void PublicComparison_OrdersNullTimestampProcessAndCounterFields()
    {
        IObjectId objectId = new ObjectId();
        byte[] process = new byte[] { 1, 2, 3, 4, 5 };
        ObjectIdValue minimum = ObjectIdGenerator.FromParts(0, new byte[5], 0);
        ObjectIdValue counterHigh = ObjectIdGenerator.FromParts(0, new byte[5], 1);
        ObjectIdValue processHigh = ObjectIdGenerator.FromParts(0, process, 0);
        ObjectIdValue timestampHigh = ObjectIdGenerator.FromParts(1, new byte[5], 0);

        Assert.That(objectId.CompareValues(null, null), Is.EqualTo(0));
        Assert.That(objectId.CompareValues(null, minimum), Is.LessThan(0));
        Assert.That(objectId.CompareValues(minimum, null), Is.GreaterThan(0));
        Assert.That(objectId.CompareValues(minimum, counterHigh), Is.LessThan(0));
        Assert.That(objectId.CompareValues(counterHigh, processHigh), Is.LessThan(0));
        Assert.That(objectId.CompareValues(processHigh, timestampHigh), Is.LessThan(0));
        Assert.That(objectId.CompareValues(timestampHigh, processHigh), Is.GreaterThan(0));
        Assert.That(objectId.CompareValues(minimum, new ObjectIdValue(new byte[12])), Is.EqualTo(0));
        Assert.That(minimum.CompareTo(null), Is.GreaterThan(0));
        Assert.That(string.CompareOrdinal(minimum.ToHexString(), counterHigh.ToHexString()), Is.LessThan(0));
        Assert.That(string.CompareOrdinal(processHigh.ToHexString(), timestampHigh.ToHexString()), Is.LessThan(0));
    }

    [Test]
    public void Counter_RollsFromUnsignedTwentyFourBitMaximumToZeroAndContinues()
    {
        byte[] process = new byte[] { 1, 2, 3, 4, 5 };
        ObjectIdGenerator generator = ObjectIdGenerator.Create(process, ObjectIdLimits.MaxCounter - 1);

        ObjectIdValue beforeMaximum = generator.NextAtUnixSeconds(1600000000);
        ObjectIdValue maximum = generator.NextAtUnixSeconds(1600000000);
        ObjectIdValue rolled = generator.NextAtUnixSeconds(1600000000);
        ObjectIdValue continued = generator.NextAtUnixSeconds(1600000000);

        Assert.That(beforeMaximum.ToHexString(), Does.EndWith("fffffe"));
        Assert.That(maximum.ToHexString(), Does.EndWith("ffffff"));
        Assert.That(rolled.ToHexString(), Does.EndWith("000000"));
        Assert.That(continued.ToHexString(), Does.EndWith("000001"));
        Assert.That(beforeMaximum.ToBytes()[4..9], Is.EqualTo(process));
        Assert.That(maximum.ToBytes()[4..9], Is.EqualTo(process));
        Assert.That(rolled.ToBytes()[4..9], Is.EqualTo(process));
        Assert.That(continued.ToBytes()[4..9], Is.EqualTo(process));
    }

    static byte[] ExampleBytes()
    {
        return new byte[] { 0x5F, 0x5E, 0x10, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x01, 0x02, 0x03 };
    }

    static void AssertParseFailure(ObjectIdParseResult result, IdentifierErrorCode code, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Code, Is.EqualTo(code));
    }

    static void AssertValidationFailure(IdentifierValidationResult result, IdentifierErrorCode code, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Code, Is.EqualTo(code));
    }

    static void AssertIdentifierException(TestDelegate action, IdentifierErrorCode code, string message)
    {
        IdentifierException exception = Assert.Throws<IdentifierException>(action);
        Assert.That(exception.Code, Is.EqualTo(code));
        Assert.That(exception.Message, Is.EqualTo(message));
    }
}
