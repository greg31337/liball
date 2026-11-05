using System;
using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class KsuidPublicApiBehaviorTests
{
    [Test]
    public void KsuidInterface_ExposesEveryPublicOperation()
    {
        IKsuid ksuid = new Ksuid();
        long unixSeconds = KsuidLimits.EpochUnixSeconds + 123456;
        KsuidValue generated = ksuid.New();
        KsuidValue atTime = ksuid.NewAt(DateTimeOffset.FromUnixTimeSeconds(unixSeconds));
        KsuidValue atSeconds = ksuid.NewAtUnixSeconds(unixSeconds);
        KsuidParseResult parsed = ksuid.Parse("0ujtsYcgvSTl8PAuAdqWYSMnLOv");
        KsuidParseResult tried = ksuid.TryParse("0ujtsYcgvSTl8PAuAdqWYSMnLOv");
        KsuidValue parsedValue = ksuid.ParseValue("0ujtsYcgvSTl8PAuAdqWYSMnLOv");

        Assert.That(generated.ToCanonicalString(), Has.Length.EqualTo(27));
        Assert.That(atTime.UnixSeconds(), Is.EqualTo(unixSeconds));
        Assert.That(atSeconds.UnixSeconds(), Is.EqualTo(unixSeconds));
        Assert.That(parsed.Ok, Is.True);
        Assert.That(tried.Ok, Is.True);
        Assert.That(parsedValue.EqualsValue(parsed.Value), Is.True);
        Assert.That(ksuid.Format(parsedValue), Is.EqualTo("0ujtsYcgvSTl8PAuAdqWYSMnLOv"));
        Assert.That(ksuid.Validate("0ujtsYcgvSTl8PAuAdqWYSMnLOv").Ok, Is.True);
        Assert.That(ksuid.CompareValues(parsedValue, parsedValue), Is.EqualTo(0));
    }

    [Test]
    public void KsuidValueInterfaces_ExposeEveryValueOperation()
    {
        KsuidValue concrete = new KsuidValue(ExampleBytes());
        IKsuidValue value = concrete;
        IIdentifierValue identifier = concrete;
        ITimestampedIdentifierValue timestamped = concrete;
        byte[] destination = new byte[20];

        value.CopyTo(destination);

        Assert.That(value.ToBytes(), Is.EqualTo(ExampleBytes()));
        Assert.That(destination, Is.EqualTo(ExampleBytes()));
        Assert.That(value.AdjustedSeconds(), Is.EqualTo(123456));
        Assert.That(value.UnixSeconds(), Is.EqualTo(KsuidLimits.EpochUnixSeconds + 123456));
        Assert.That(value.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(KsuidLimits.EpochUnixSeconds + 123456)));
        Assert.That(value.PayloadBytes(), Is.EqualTo(PayloadBytes()));
        Assert.That(value.EqualsValue(new KsuidValue(ExampleBytes())), Is.True);
        Assert.That(value.EqualsBytes(ExampleBytes()), Is.True);
        Assert.That(value.CompareTo(new KsuidValue(ExampleBytes())), Is.EqualTo(0));
        Assert.That(identifier.Kind(), Is.EqualTo(IdentifierKind.Ksuid));
        Assert.That(identifier.ToCanonicalString(), Is.EqualTo(concrete.ToCanonicalString()));
        Assert.That(timestamped.Timestamp().UnixSeconds, Is.EqualTo(KsuidLimits.EpochUnixSeconds + 123456));
    }

    [TestCase(null, IdentifierErrorCode.NullInput, "KSUID text is null")]
    [TestCase("short", IdentifierErrorCode.InvalidLength, "expected 27 Base62 characters but found 5")]
    [TestCase("00000000000000000000000000_", IdentifierErrorCode.InvalidCharacter, "invalid Base62 character at index 26")]
    [TestCase("zzzzzzzzzzzzzzzzzzzzzzzzzzz", IdentifierErrorCode.ValueOverflow, "KSUID text overflows 160 bits")]
    public void ParseValidateAndThrowingParse_PreserveEveryFailure(string? text, IdentifierErrorCode code, string error)
    {
        IKsuid ksuid = new Ksuid();

        KsuidParseResult parsed = ksuid.Parse(text);
        KsuidParseResult tried = ksuid.TryParse(text);
        IdentifierValidationResult validation = ksuid.Validate(text);

        AssertParseFailure(parsed, code, error);
        AssertParseFailure(tried, code, error);
        AssertValidationFailure(validation, code, error);
        AssertIdentifierException(() => ksuid.ParseValue(text), code, error);
    }

    [Test]
    public void Limits_DefineExactUnsignedTimestampRange()
    {
        Assert.That(KsuidLimits.EpochUnixSeconds, Is.EqualTo(1400000000));
        Assert.That(KsuidLimits.MaxAdjustedSeconds, Is.EqualTo(4294967295));
        Assert.That(KsuidLimits.MaxUnixSeconds, Is.EqualTo(5694967295));
        Assert.That(KsuidLimits.EpochUnixSeconds + KsuidLimits.MaxAdjustedSeconds, Is.EqualTo(KsuidLimits.MaxUnixSeconds));
    }

    [Test]
    public void Generation_AcceptsExactTimestampBoundariesAndRejectsOutsideValues()
    {
        IKsuid ksuid = new Ksuid();

        KsuidValue minimum = ksuid.NewAtUnixSeconds(KsuidLimits.EpochUnixSeconds);
        KsuidValue maximum = ksuid.NewAtUnixSeconds(KsuidLimits.MaxUnixSeconds);
        KsuidValue minimumAt = ksuid.NewAt(DateTimeOffset.FromUnixTimeSeconds(KsuidLimits.EpochUnixSeconds));
        KsuidValue maximumAt = ksuid.NewAt(DateTimeOffset.FromUnixTimeSeconds(KsuidLimits.MaxUnixSeconds));

        Assert.That(minimum.AdjustedSeconds(), Is.EqualTo(0));
        Assert.That(maximum.AdjustedSeconds(), Is.EqualTo(KsuidLimits.MaxAdjustedSeconds));
        Assert.That(minimumAt.UnixSeconds(), Is.EqualTo(KsuidLimits.EpochUnixSeconds));
        Assert.That(maximumAt.UnixSeconds(), Is.EqualTo(KsuidLimits.MaxUnixSeconds));
        AssertIdentifierException(() => ksuid.NewAtUnixSeconds(KsuidLimits.EpochUnixSeconds - 1), IdentifierErrorCode.TimestampOverflow, "KSUID unix seconds is outside adjusted epoch range");
        AssertIdentifierException(() => ksuid.NewAtUnixSeconds(KsuidLimits.MaxUnixSeconds + 1), IdentifierErrorCode.TimestampOverflow, "KSUID unix seconds is outside adjusted epoch range");
        AssertIdentifierException(() => ksuid.NewAt(DateTimeOffset.FromUnixTimeSeconds(KsuidLimits.EpochUnixSeconds - 1)), IdentifierErrorCode.TimestampOverflow, "KSUID unix seconds is outside adjusted epoch range");
        AssertIdentifierException(() => ksuid.NewAt(DateTimeOffset.FromUnixTimeSeconds(KsuidLimits.MaxUnixSeconds + 1)), IdentifierErrorCode.TimestampOverflow, "KSUID unix seconds is outside adjusted epoch range");
    }

    [Test]
    public void MinimumAndMaximumValues_RoundTripAtTextAndTimestampBoundaries()
    {
        IKsuid ksuid = new Ksuid();
        KsuidValue minimum = new KsuidValue(new byte[20]);
        KsuidValue maximum = new KsuidValue(MaximumBytes());
        string maximumText = maximum.ToCanonicalString();

        KsuidValue parsedMinimum = ksuid.ParseValue("000000000000000000000000000");
        KsuidValue parsedMaximum = ksuid.ParseValue(maximumText);
        IdentifierTimestampResult minimumTimestamp = parsedMinimum.Timestamp();
        IdentifierTimestampResult maximumTimestamp = parsedMaximum.Timestamp();

        Assert.That(parsedMinimum.EqualsValue(minimum), Is.True);
        Assert.That(parsedMaximum.EqualsValue(maximum), Is.True);
        Assert.That(maximumText, Is.EqualTo("aWgEPTl1tmebfsQzFP4bxwgy80V"));
        Assert.That(minimumTimestamp.Ok, Is.True);
        Assert.That(minimumTimestamp.UnixSeconds, Is.EqualTo(KsuidLimits.EpochUnixSeconds));
        Assert.That(minimumTimestamp.UnixMilliseconds, Is.EqualTo(KsuidLimits.EpochUnixSeconds * 1000));
        Assert.That(maximumTimestamp.Ok, Is.True);
        Assert.That(maximumTimestamp.UnixSeconds, Is.EqualTo(KsuidLimits.MaxUnixSeconds));
        Assert.That(maximumTimestamp.UnixMilliseconds, Is.EqualTo(KsuidLimits.MaxUnixSeconds * 1000));
        Assert.That(parsedMinimum.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(KsuidLimits.EpochUnixSeconds)));
        Assert.That(parsedMaximum.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(KsuidLimits.MaxUnixSeconds)));
    }

    [Test]
    public void ConstructionCopyEqualityAndFormatting_CoverNullAndLengthBoundaries()
    {
        byte[] source = ExampleBytes();
        KsuidValue value = new KsuidValue(source);
        byte[] destination = new byte[21];
        destination[20] = 99;

        source[0] = 255;
        value.CopyTo(destination);
        destination[1] = 255;

        Assert.That(value.ToBytes(), Is.EqualTo(ExampleBytes()));
        Assert.That(destination[20], Is.EqualTo(99));
        Assert.That(value.EqualsValue(null), Is.False);
        Assert.That(value.EqualsBytes(ExampleBytes()), Is.True);
        Assert.That(value.EqualsBytes(new byte[20]), Is.False);
        Assert.That(value.EqualsBytes(new byte[19]), Is.False);
        Assert.That(value.EqualsBytes(null), Is.False);

        ArgumentNullException constructorNull = Assert.Throws<ArgumentNullException>(() => new KsuidValue(null));
        IdentifierException constructorLength = Assert.Throws<IdentifierException>(() => new KsuidValue(new byte[19]));
        ArgumentNullException destinationNull = Assert.Throws<ArgumentNullException>(() => value.CopyTo(null));
        IdentifierException destinationLength = Assert.Throws<IdentifierException>(() => value.CopyTo(new byte[19]));
        ArgumentNullException formatNull = Assert.Throws<ArgumentNullException>(() => new Ksuid().Format(null));

        Assert.That(constructorNull.ParamName, Is.EqualTo("bytes"));
        Assert.That(constructorLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(constructorLength.Message, Is.EqualTo("KSUID byte array must contain exactly 20 bytes"));
        Assert.That(destinationNull.ParamName, Is.EqualTo("destination"));
        Assert.That(destinationLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(destinationLength.Message, Is.EqualTo("destination must have room for 20 bytes"));
        Assert.That(formatNull.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void PayloadBytes_AreDefensiveAcrossMinimumMaximumAndArbitraryValues()
    {
        KsuidValue minimum = new KsuidValue(new byte[20]);
        KsuidValue maximum = new KsuidValue(MaximumBytes());
        KsuidValue arbitrary = new KsuidValue(ExampleBytes());

        byte[] minimumPayload = minimum.PayloadBytes();
        byte[] maximumPayload = maximum.PayloadBytes();
        byte[] arbitraryPayload = arbitrary.PayloadBytes();
        minimumPayload[0] = 1;
        maximumPayload[0] = 0;
        arbitraryPayload[0] = 0;

        Assert.That(minimum.PayloadBytes(), Is.EqualTo(new byte[16]));
        Assert.That(maximum.PayloadBytes(), Is.EqualTo(MaximumPayloadBytes()));
        Assert.That(arbitrary.PayloadBytes(), Is.EqualTo(PayloadBytes()));
    }

    [Test]
    public void PublicComparison_OrdersNullTimestampAndPayloadBoundaries()
    {
        IKsuid ksuid = new Ksuid();
        KsuidValue minimum = new KsuidValue(new byte[20]);
        byte[] payloadHighBytes = new byte[20];
        payloadHighBytes[19] = 1;
        KsuidValue payloadHigh = new KsuidValue(payloadHighBytes);
        byte[] timestampHighBytes = new byte[20];
        BigEndianTools.WriteUInt32(timestampHighBytes, 0, 1);
        KsuidValue timestampHigh = new KsuidValue(timestampHighBytes);

        Assert.That(ksuid.CompareValues(null, null), Is.EqualTo(0));
        Assert.That(ksuid.CompareValues(null, minimum), Is.LessThan(0));
        Assert.That(ksuid.CompareValues(minimum, null), Is.GreaterThan(0));
        Assert.That(ksuid.CompareValues(minimum, payloadHigh), Is.LessThan(0));
        Assert.That(ksuid.CompareValues(payloadHigh, timestampHigh), Is.LessThan(0));
        Assert.That(ksuid.CompareValues(timestampHigh, payloadHigh), Is.GreaterThan(0));
        Assert.That(ksuid.CompareValues(minimum, new KsuidValue(new byte[20])), Is.EqualTo(0));
        Assert.That(minimum.CompareTo(null), Is.GreaterThan(0));
        Assert.That(string.CompareOrdinal(minimum.ToCanonicalString(), payloadHigh.ToCanonicalString()), Is.LessThan(0));
        Assert.That(string.CompareOrdinal(payloadHigh.ToCanonicalString(), timestampHigh.ToCanonicalString()), Is.LessThan(0));
    }

    static byte[] ExampleBytes()
    {
        return new byte[] { 0x00, 0x01, 0xE2, 0x40, 0x1D, 0xDE, 0xAA, 0xD7, 0x13, 0xF8, 0xA5, 0x9E, 0x5C, 0xA5, 0x7C, 0x2B, 0x47, 0x72, 0x6F, 0xC7 };
    }

    static byte[] PayloadBytes()
    {
        return new byte[] { 0x1D, 0xDE, 0xAA, 0xD7, 0x13, 0xF8, 0xA5, 0x9E, 0x5C, 0xA5, 0x7C, 0x2B, 0x47, 0x72, 0x6F, 0xC7 };
    }

    static byte[] MaximumBytes()
    {
        byte[] bytes = new byte[20];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = 255;
            index = index + 1;
        }

        return bytes;
    }

    static byte[] MaximumPayloadBytes()
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

    static void AssertParseFailure(KsuidParseResult result, IdentifierErrorCode code, string error)
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
