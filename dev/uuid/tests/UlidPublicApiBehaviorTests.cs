using System;
using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UlidPublicApiBehaviorTests
{
    [Test]
    public void UlidInterface_ExposesEveryPublicOperation()
    {
        IUlid ulid = new Ulid();
        UlidValue generated = ulid.New();
        UlidValue atTime = ulid.NewAt(DateTimeOffset.FromUnixTimeMilliseconds(1469922850259));
        UlidValue atMilliseconds = ulid.NewAtUnixMilliseconds(1469922850259);
        UlidValue monotonic = ulid.NewMonotonic();
        UlidValue fromBytes = ulid.FromUuidBytes(ExampleBytes());
        byte[] uuidBytes = ulid.ToUuidBytes(fromBytes);
        UlidParseResult parsed = ulid.Parse("01arz3ndektsv4rrffq69g5fav");
        UlidParseResult tried = ulid.TryParse("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        UlidValue parsedValue = ulid.ParseValue("01ARZ3NDEKTSV4RRFFQ69G5FAV");

        Assert.That(generated.ToCanonicalString(), Has.Length.EqualTo(26));
        Assert.That(atTime.UnixMilliseconds(), Is.EqualTo(1469922850259));
        Assert.That(atMilliseconds.UnixMilliseconds(), Is.EqualTo(1469922850259));
        Assert.That(monotonic.ToCanonicalString(), Has.Length.EqualTo(26));
        Assert.That(uuidBytes, Is.EqualTo(ExampleBytes()));
        Assert.That(parsed.Ok, Is.True);
        Assert.That(tried.Ok, Is.True);
        Assert.That(parsedValue.EqualsValue(parsed.Value), Is.True);
        Assert.That(ulid.Format(parsedValue), Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(ulid.Validate("01arz3ndektsv4rrffq69g5fav").NormalizedText, Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(ulid.CompareValues(parsedValue, parsedValue), Is.EqualTo(0));
    }

    [Test]
    public void UlidValueInterfaces_ExposeEveryValueOperation()
    {
        UlidValue concrete = new UlidValue(ExampleBytes());
        IUlidValue value = concrete;
        IIdentifierValue identifier = concrete;
        ITimestampedIdentifierValue timestamped = concrete;
        byte[] destination = new byte[16];

        value.CopyTo(destination);

        Assert.That(value.ToBytes(), Is.EqualTo(ExampleBytes()));
        Assert.That(value.ToUuidBytes(), Is.EqualTo(ExampleBytes()));
        Assert.That(destination, Is.EqualTo(ExampleBytes()));
        Assert.That(value.UnixMilliseconds(), Is.EqualTo(1469922850259));
        Assert.That(value.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1469922850259)));
        Assert.That(value.RandomnessBytes(), Is.EqualTo(RandomnessBytes()));
        Assert.That(value.EqualsValue(new UlidValue(ExampleBytes())), Is.True);
        Assert.That(value.EqualsBytes(ExampleBytes()), Is.True);
        Assert.That(value.CompareTo(new UlidValue(ExampleBytes())), Is.EqualTo(0));
        Assert.That(identifier.Kind(), Is.EqualTo(IdentifierKind.Ulid));
        Assert.That(identifier.ToCanonicalString(), Is.EqualTo("01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        Assert.That(timestamped.Timestamp().UnixMilliseconds, Is.EqualTo(1469922850259));
    }

    [TestCase(null, IdentifierErrorCode.NullInput, "ULID text is null")]
    [TestCase("short", IdentifierErrorCode.InvalidLength, "expected 26 Crockford Base32 characters but found 5")]
    [TestCase("01ARZ3NDEKTSV4RRFFQ69G5FAI", IdentifierErrorCode.InvalidCharacter, "invalid Crockford Base32 character at index 25")]
    [TestCase("80000000000000000000000000", IdentifierErrorCode.ValueOverflow, "ULID text overflows 128 bits")]
    public void ParseValidateAndThrowingParse_PreserveEveryFailure(string? text, IdentifierErrorCode code, string error)
    {
        IUlid ulid = new Ulid();

        UlidParseResult parsed = ulid.Parse(text);
        UlidParseResult tried = ulid.TryParse(text);
        IdentifierValidationResult validation = ulid.Validate(text);

        AssertParseFailure(parsed, code, error);
        AssertParseFailure(tried, code, error);
        AssertValidationFailure(validation, code, error);
        AssertIdentifierException(() => ulid.ParseValue(text), code, error);
    }

    [Test]
    public void Generation_AcceptsFortyEightBitBoundariesAndRejectsOutsideValues()
    {
        IUlid ulid = new Ulid();

        UlidValue minimum = ulid.NewAtUnixMilliseconds(0);
        UlidValue maximum = ulid.NewAtUnixMilliseconds(BigEndianTools.MaxUInt48);
        UlidValue maximumDate = ulid.NewAt(DateTimeOffset.MaxValue);

        Assert.That(minimum.UnixMilliseconds(), Is.EqualTo(0));
        Assert.That(maximum.UnixMilliseconds(), Is.EqualTo(BigEndianTools.MaxUInt48));
        Assert.That(maximumDate.UnixMilliseconds(), Is.EqualTo(DateTimeOffset.MaxValue.ToUnixTimeMilliseconds()));
        AssertIdentifierException(() => ulid.NewAtUnixMilliseconds(-1), IdentifierErrorCode.TimestampOverflow, "ULID timestamp is outside the 48-bit range");
        AssertIdentifierException(() => ulid.NewAtUnixMilliseconds(BigEndianTools.MaxUInt48 + 1), IdentifierErrorCode.TimestampOverflow, "ULID timestamp is outside the 48-bit range");
        AssertIdentifierException(() => ulid.NewAt(DateTimeOffset.UnixEpoch.AddMilliseconds(-1)), IdentifierErrorCode.TimestampOverflow, "ULID timestamp is outside the 48-bit range");
    }

    [Test]
    public void MaximumValue_RoundTripsAndReportsDateTimeOverflow()
    {
        IUlid ulid = new Ulid();
        UlidValue maximum = ulid.ParseValue("7ZZZZZZZZZZZZZZZZZZZZZZZZZ");

        IdentifierTimestampResult timestamp = maximum.Timestamp();

        Assert.That(maximum.ToBytes(), Is.EqualTo(MaximumBytes()));
        Assert.That(maximum.UnixMilliseconds(), Is.EqualTo(BigEndianTools.MaxUInt48));
        Assert.That(maximum.RandomnessBytes(), Is.EqualTo(MaximumRandomnessBytes()));
        Assert.That(ulid.Format(maximum), Is.EqualTo("7ZZZZZZZZZZZZZZZZZZZZZZZZZ"));
        AssertTimestampFailure(timestamp, IdentifierErrorCode.TimestampOverflow, "unix milliseconds is outside DateTimeOffset range");
        AssertIdentifierException(() => maximum.Time(), IdentifierErrorCode.TimestampOverflow, "unix milliseconds is outside DateTimeOffset range");
    }

    [Test]
    public void UuidByteConversion_CopiesBitsAndReportsNullAndLengthFailures()
    {
        IUlid ulid = new Ulid();
        byte[] source = ExampleBytes();

        UlidValue value = ulid.FromUuidBytes(source);
        source[0] = 255;
        byte[] firstCopy = ulid.ToUuidBytes(value);
        firstCopy[1] = 255;

        Assert.That(value.ToUuidBytes(), Is.EqualTo(ExampleBytes()));
        Assert.That(ulid.ToUuidBytes(value), Is.EqualTo(ExampleBytes()));

        ArgumentNullException nullBytes = Assert.Throws<ArgumentNullException>(() => ulid.FromUuidBytes(null));
        IdentifierException shortBytes = Assert.Throws<IdentifierException>(() => ulid.FromUuidBytes(new byte[15]));
        ArgumentNullException nullValue = Assert.Throws<ArgumentNullException>(() => ulid.ToUuidBytes(null));

        Assert.That(nullBytes.ParamName, Is.EqualTo("bytes"));
        Assert.That(shortBytes.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(shortBytes.Message, Is.EqualTo("ULID byte array must contain exactly 16 bytes"));
        Assert.That(nullValue.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void ConstructionCopyEqualityAndFormatting_CoverNullAndLengthBoundaries()
    {
        byte[] source = ExampleBytes();
        UlidValue value = new UlidValue(source);
        byte[] destination = new byte[17];
        destination[16] = 99;

        source[0] = 255;
        value.CopyTo(destination);
        destination[1] = 255;

        Assert.That(value.ToBytes(), Is.EqualTo(ExampleBytes()));
        Assert.That(destination[16], Is.EqualTo(99));
        Assert.That(value.EqualsValue(null), Is.False);
        Assert.That(value.EqualsBytes(ExampleBytes()), Is.True);
        Assert.That(value.EqualsBytes(new byte[16]), Is.False);
        Assert.That(value.EqualsBytes(new byte[15]), Is.False);
        Assert.That(value.EqualsBytes(null), Is.False);

        ArgumentNullException constructorNull = Assert.Throws<ArgumentNullException>(() => new UlidValue(null));
        IdentifierException constructorLength = Assert.Throws<IdentifierException>(() => new UlidValue(new byte[15]));
        ArgumentNullException destinationNull = Assert.Throws<ArgumentNullException>(() => value.CopyTo(null));
        IdentifierException destinationLength = Assert.Throws<IdentifierException>(() => value.CopyTo(new byte[15]));
        ArgumentNullException formatNull = Assert.Throws<ArgumentNullException>(() => new Ulid().Format(null));

        Assert.That(constructorNull.ParamName, Is.EqualTo("bytes"));
        Assert.That(constructorLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(constructorLength.Message, Is.EqualTo("ULID byte array must contain exactly 16 bytes"));
        Assert.That(destinationNull.ParamName, Is.EqualTo("destination"));
        Assert.That(destinationLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(destinationLength.Message, Is.EqualTo("destination must have room for 16 bytes"));
        Assert.That(formatNull.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void PublicComparison_OrdersNullAndValueCombinations()
    {
        IUlid ulid = new Ulid();
        UlidValue low = new UlidValue(new byte[16]);
        byte[] highBytes = new byte[16];
        highBytes[15] = 1;
        UlidValue high = new UlidValue(highBytes);

        Assert.That(ulid.CompareValues(null, null), Is.EqualTo(0));
        Assert.That(ulid.CompareValues(null, low), Is.LessThan(0));
        Assert.That(ulid.CompareValues(low, null), Is.GreaterThan(0));
        Assert.That(ulid.CompareValues(low, high), Is.LessThan(0));
        Assert.That(ulid.CompareValues(high, low), Is.GreaterThan(0));
        Assert.That(ulid.CompareValues(low, new UlidValue(new byte[16])), Is.EqualTo(0));
        Assert.That(low.CompareTo(null), Is.GreaterThan(0));
    }

    static byte[] ExampleBytes()
    {
        return new byte[] { 0x01, 0x56, 0x3E, 0x3A, 0xB5, 0xD3, 0xD6, 0x76, 0x4C, 0x61, 0xEF, 0xB9, 0x93, 0x02, 0xBD, 0x5B };
    }

    static byte[] RandomnessBytes()
    {
        return new byte[] { 0xD6, 0x76, 0x4C, 0x61, 0xEF, 0xB9, 0x93, 0x02, 0xBD, 0x5B };
    }

    static byte[] MaximumBytes()
    {
        byte[] bytes = new byte[16];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = 255;
            index = index + 1;
        }

        return bytes;
    }

    static byte[] MaximumRandomnessBytes()
    {
        byte[] bytes = new byte[10];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = 255;
            index = index + 1;
        }

        return bytes;
    }

    static void AssertParseFailure(UlidParseResult result, IdentifierErrorCode code, string error)
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

    static void AssertTimestampFailure(IdentifierTimestampResult result, IdentifierErrorCode code, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.UnixMilliseconds, Is.EqualTo(0));
        Assert.That(result.UnixSeconds, Is.EqualTo(0));
        Assert.That(result.Time, Is.EqualTo(DateTimeOffset.UnixEpoch));
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
