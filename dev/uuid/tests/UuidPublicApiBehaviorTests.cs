using System;
using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class UuidPublicApiBehaviorTests
{
    [Test]
    public void UuidInterface_ExposesEveryPublicOperation()
    {
        IUuid uuid = new Uuid();
        UuidParseResult parsed = uuid.Parse("00112233445566778899AABBCCDDEEFF");
        UuidParseResult tried = uuid.TryParse("URN:UUID:00112233-4455-6677-8899-AABBCCDDEEFF");
        UuidValue parsedValue = uuid.ParseValue("00112233-4455-6677-8899-aabbccddeeff");
        UuidValue version4 = uuid.NewV4();
        UuidValue version7 = uuid.NewV7AtUnixMilliseconds(1469922850259);
        UuidValue version7At = uuid.NewV7At(DateTimeOffset.FromUnixTimeMilliseconds(1469922850259));
        UuidValue currentVersion7 = uuid.NewV7();
        UuidValue monotonicVersion7 = uuid.NewMonotonicV7();
        UuidValue version3 = uuid.FromNameV3(UuidNamespaces.Dns, "www.example.com");
        UuidValue version5 = uuid.FromNameV5(UuidNamespaces.Dns, "www.example.com");
        UuidValue version1 = uuid.ParseValue("c232ab00-9414-11ec-b3c8-9f6bdeced846");
        UuidValue version6 = uuid.V1ToV6(version1);
        UuidValue restoredVersion1 = uuid.V6ToV1(version6);
        UuidValue guidValue = uuid.FromGuid(new Guid("00112233-4455-6677-8899-aabbccddeeff"));

        Assert.That(parsed.Ok, Is.True);
        Assert.That(tried.Ok, Is.True);
        Assert.That(parsedValue.EqualsValue(parsed.Value), Is.True);
        Assert.That(version4.Version(), Is.EqualTo(UuidVersion.Version4));
        Assert.That(version7.Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(version7At.UnixMilliseconds(), Is.EqualTo(1469922850259));
        Assert.That(currentVersion7.Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(monotonicVersion7.Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(version3.Version(), Is.EqualTo(UuidVersion.Version3));
        Assert.That(version5.Version(), Is.EqualTo(UuidVersion.Version5));
        Assert.That(restoredVersion1.EqualsValue(version1), Is.True);
        Assert.That(guidValue.ToCanonicalString(), Is.EqualTo("00112233-4455-6677-8899-aabbccddeeff"));
        Assert.That(uuid.TimestampValue(version7).UnixMilliseconds, Is.EqualTo(1469922850259));
        Assert.That(uuid.TimestampText(version7.ToCanonicalString()).UnixMilliseconds, Is.EqualTo(1469922850259));
        Assert.That(uuid.Validate(version4.ToCanonicalString()).Ok, Is.True);
        Assert.That(uuid.ValidateRfc9562Variant(version4.ToCanonicalString()).Ok, Is.True);
        Assert.That(uuid.ValidateVersion(version4.ToCanonicalString(), UuidVersion.Version4).Ok, Is.True);
        Assert.That(uuid.CompareValues(parsedValue, parsedValue), Is.EqualTo(0));
    }

    [Test]
    public void UuidValueInterfaces_ExposeFormattingInspectionAndComparisonOperations()
    {
        UuidValue concrete = new Uuid().NewV7AtUnixMilliseconds(1469922850259);
        IUuidValue uuidValue = concrete;
        IIdentifierValue identifierValue = concrete;
        ITimestampedIdentifierValue timestampedValue = concrete;
        byte[] destination = new byte[16];

        uuidValue.CopyTo(destination);

        Assert.That(uuidValue.ToBytes(), Is.EqualTo(destination));
        Assert.That(uuidValue.ToUpperString(), Is.EqualTo(concrete.ToCanonicalString().ToUpperInvariant()));
        Assert.That(uuidValue.ToCompactString(), Is.EqualTo(concrete.ToCanonicalString().Replace("-", "")));
        Assert.That(uuidValue.ToCompactUpperString(), Is.EqualTo(concrete.ToCompactString().ToUpperInvariant()));
        Assert.That(uuidValue.ToGuid().ToString(), Is.EqualTo(concrete.ToCanonicalString()));
        Assert.That(uuidValue.VersionNumber(), Is.EqualTo(7));
        Assert.That(uuidValue.Version(), Is.EqualTo(UuidVersion.Version7));
        Assert.That(uuidValue.Variant(), Is.EqualTo(UuidVariant.Rfc9562));
        Assert.That(uuidValue.HasRfc9562Variant(), Is.True);
        Assert.That(uuidValue.IsNil(), Is.False);
        Assert.That(uuidValue.IsMax(), Is.False);
        Assert.That(uuidValue.UnixMilliseconds(), Is.EqualTo(1469922850259));
        Assert.That(uuidValue.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1469922850259)));
        Assert.That(uuidValue.EqualsValue(new UuidValue(destination)), Is.True);
        Assert.That(uuidValue.EqualsBytes(destination), Is.True);
        Assert.That(uuidValue.CompareTo(new UuidValue(destination)), Is.EqualTo(0));
        Assert.That(identifierValue.Kind(), Is.EqualTo(IdentifierKind.Uuid));
        Assert.That(identifierValue.ToCanonicalString(), Is.EqualTo(concrete.ToCanonicalString()));
        Assert.That(timestampedValue.Timestamp().UnixMilliseconds, Is.EqualTo(1469922850259));
    }

    [TestCase(null, IdentifierErrorCode.NullInput, "UUID text is null")]
    [TestCase("bad", IdentifierErrorCode.InvalidLength, "UUID text length must be 32 compact hex characters, 36 canonical characters, or urn:uuid: plus canonical UUID")]
    [TestCase("00112233-4455-6677-8899-aabbccddeefg", IdentifierErrorCode.InvalidCharacter, "invalid UUID hex character at index 35")]
    public void PublicParsingFailures_PreserveNeutralResultsAndCodedExceptions(string? text, IdentifierErrorCode code, string error)
    {
        IUuid uuid = new Uuid();

        UuidParseResult parsed = uuid.Parse(text);
        UuidParseResult tried = uuid.TryParse(text);

        AssertParseFailure(parsed, code, error);
        AssertParseFailure(tried, code, error);
        AssertIdentifierException(() => uuid.ParseValue(text), code, error);
    }

    [TestCase(null, IdentifierErrorCode.NullInput, "UUID text is null")]
    [TestCase("bad", IdentifierErrorCode.InvalidLength, "UUID text length must be 32 compact hex characters, 36 canonical characters, or urn:uuid: plus canonical UUID")]
    [TestCase("00112233-4455-6677-8899-aabbccddeefg", IdentifierErrorCode.InvalidCharacter, "invalid UUID hex character at index 35")]
    public void StrictValidation_PropagatesEveryParseFailure(string? text, IdentifierErrorCode code, string error)
    {
        Uuid uuid = new Uuid();

        IdentifierValidationResult variant = uuid.ValidateRfc9562Variant(text);
        IdentifierValidationResult version = uuid.ValidateVersion(text, UuidVersion.Version4);

        AssertValidationFailure(variant, code, error);
        AssertValidationFailure(version, code, error);
    }

    [TestCase(1, UuidVersion.Version1)]
    [TestCase(3, UuidVersion.Version3)]
    [TestCase(4, UuidVersion.Version4)]
    [TestCase(5, UuidVersion.Version5)]
    [TestCase(6, UuidVersion.Version6)]
    [TestCase(7, UuidVersion.Version7)]
    [TestCase(8, UuidVersion.Version8)]
    public void VersionValidation_AcceptsEveryDefinedVersion(int versionNumber, UuidVersion version)
    {
        byte[] bytes = new byte[16];
        bytes[6] = (byte)(versionNumber << 4);
        bytes[8] = 128;
        UuidValue value = new UuidValue(bytes);

        IdentifierValidationResult result = new Uuid().ValidateVersion(value.ToCanonicalString(), version);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Code, Is.EqualTo(IdentifierErrorCode.None));
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Uuid));
        Assert.That(result.NormalizedText, Is.EqualTo(value.ToCanonicalString()));
    }

    [Test]
    public void VersionValidation_RejectsUnknownUndefinedMismatchedAndNonRfcVersions()
    {
        Uuid uuid = new Uuid();
        const string version7 = "00000000-0000-7000-8000-000000000000";

        IdentifierValidationResult unknown = uuid.ValidateVersion(version7, UuidVersion.Unknown);
        IdentifierValidationResult undefined = uuid.ValidateVersion(version7, (UuidVersion)2);
        IdentifierValidationResult outside = uuid.ValidateVersion(version7, (UuidVersion)15);
        IdentifierValidationResult mismatch = uuid.ValidateVersion(version7, UuidVersion.Version4);
        IdentifierValidationResult variant = uuid.ValidateVersion("00000000-0000-7000-0000-000000000000", UuidVersion.Version7);

        AssertValidationFailure(unknown, IdentifierErrorCode.InvalidUuidVersion, "expected UUID version must be known");
        AssertValidationFailure(undefined, IdentifierErrorCode.InvalidUuidVersion, "UUID version does not match expected version");
        AssertValidationFailure(outside, IdentifierErrorCode.InvalidUuidVersion, "UUID version does not match expected version");
        AssertValidationFailure(mismatch, IdentifierErrorCode.InvalidUuidVersion, "UUID version does not match expected version");
        AssertValidationFailure(variant, IdentifierErrorCode.InvalidUuidVariant, "UUID does not use RFC 9562 variant bits");
    }

    [Test]
    public void VersionSevenGeneration_AcceptsFortyEightBitBoundariesAndRejectsOutsideValues()
    {
        Uuid uuid = new Uuid();

        UuidValue minimum = uuid.NewV7AtUnixMilliseconds(0);
        UuidValue maximum = uuid.NewV7AtUnixMilliseconds(BigEndianTools.MaxUInt48);
        UuidValue maximumDate = uuid.NewV7At(DateTimeOffset.MaxValue);

        Assert.That(minimum.UnixMilliseconds(), Is.EqualTo(0));
        Assert.That(maximum.UnixMilliseconds(), Is.EqualTo(BigEndianTools.MaxUInt48));
        Assert.That(maximumDate.UnixMilliseconds(), Is.EqualTo(DateTimeOffset.MaxValue.ToUnixTimeMilliseconds()));
        AssertIdentifierException(() => uuid.NewV7AtUnixMilliseconds(-1), IdentifierErrorCode.TimestampOverflow, "UUIDv7 timestamp is outside the 48-bit range");
        AssertIdentifierException(() => uuid.NewV7AtUnixMilliseconds(BigEndianTools.MaxUInt48 + 1), IdentifierErrorCode.TimestampOverflow, "UUIDv7 timestamp is outside the 48-bit range");
        AssertIdentifierException(() => uuid.NewV7At(DateTimeOffset.UnixEpoch.AddMilliseconds(-1)), IdentifierErrorCode.TimestampOverflow, "UUIDv7 timestamp is outside the 48-bit range");
    }

    [Test]
    public void PublicNameGeneration_AcceptsEmptyNamesAndReportsNullParameterNames()
    {
        IUuid uuid = new Uuid();

        UuidValue emptyV3 = uuid.FromNameV3(UuidNamespaces.Dns, "");
        UuidValue emptyV5 = uuid.FromNameV5(UuidNamespaces.Dns, "");
        ArgumentNullException v3Namespace = Assert.Throws<ArgumentNullException>(() => uuid.FromNameV3(null, "name"));
        ArgumentNullException v3Name = Assert.Throws<ArgumentNullException>(() => uuid.FromNameV3(UuidNamespaces.Dns, null));
        ArgumentNullException v5Namespace = Assert.Throws<ArgumentNullException>(() => uuid.FromNameV5(null, "name"));
        ArgumentNullException v5Name = Assert.Throws<ArgumentNullException>(() => uuid.FromNameV5(UuidNamespaces.Dns, null));

        Assert.That(emptyV3.Version(), Is.EqualTo(UuidVersion.Version3));
        Assert.That(emptyV5.Version(), Is.EqualTo(UuidVersion.Version5));
        Assert.That(emptyV3.EqualsValue(emptyV5), Is.False);
        Assert.That(v3Namespace.ParamName, Is.EqualTo("namespaceId"));
        Assert.That(v3Name.ParamName, Is.EqualTo("name"));
        Assert.That(v5Namespace.ParamName, Is.EqualTo("namespaceId"));
        Assert.That(v5Name.ParamName, Is.EqualTo("name"));
    }

    [Test]
    public void PublicVersionConversion_ReportsEveryDocumentedFailure()
    {
        IUuid uuid = new Uuid();
        UuidValue version4 = uuid.ParseValue("00000000-0000-4000-8000-000000000000");
        UuidValue wrongVariantV1 = uuid.ParseValue("00000000-0000-1000-0000-000000000000");
        UuidValue wrongVariantV6 = uuid.ParseValue("00000000-0000-6000-0000-000000000000");

        ArgumentNullException v1Null = Assert.Throws<ArgumentNullException>(() => uuid.V1ToV6(null));
        ArgumentNullException v6Null = Assert.Throws<ArgumentNullException>(() => uuid.V6ToV1(null));

        Assert.That(v1Null.ParamName, Is.EqualTo("value"));
        Assert.That(v6Null.ParamName, Is.EqualTo("value"));
        AssertIdentifierException(() => uuid.V1ToV6(version4), IdentifierErrorCode.InvalidUuidVersion, "value must be a UUIDv1 value");
        AssertIdentifierException(() => uuid.V1ToV6(wrongVariantV1), IdentifierErrorCode.InvalidUuidVariant, "value must use RFC 9562 variant bits");
        AssertIdentifierException(() => uuid.V6ToV1(version4), IdentifierErrorCode.InvalidUuidVersion, "value must be a UUIDv6 value");
        AssertIdentifierException(() => uuid.V6ToV1(wrongVariantV6), IdentifierErrorCode.InvalidUuidVariant, "value must use RFC 9562 variant bits");
    }

    [Test]
    public void TimestampOperations_CoverNullInvalidUnsupportedVariantAndRangeFailures()
    {
        Uuid uuid = new Uuid();
        UuidValue version4 = uuid.ParseValue("00000000-0000-4000-8000-000000000000");
        UuidValue wrongVariant = uuid.ParseValue("00000000-0000-7000-0000-000000000000");
        UuidValue maximumV7 = uuid.ParseValue("ffffffff-ffff-7fff-bfff-ffffffffffff");

        IdentifierTimestampResult nullValue = uuid.TimestampValue(null);
        IdentifierTimestampResult nullText = uuid.TimestampText(null);
        IdentifierTimestampResult invalidText = uuid.TimestampText("bad");
        IdentifierTimestampResult unsupported = uuid.TimestampValue(version4);
        IdentifierTimestampResult invalidVariant = uuid.TimestampValue(wrongVariant);
        IdentifierTimestampResult overflow = uuid.TimestampValue(maximumV7);

        AssertTimestampFailure(nullValue, IdentifierErrorCode.NullInput, "UUID value is null");
        AssertTimestampFailure(nullText, IdentifierErrorCode.NullInput, "UUID text is null");
        AssertTimestampFailure(invalidText, IdentifierErrorCode.InvalidLength, "UUID text length must be 32 compact hex characters, 36 canonical characters, or urn:uuid: plus canonical UUID");
        AssertTimestampFailure(unsupported, IdentifierErrorCode.InvalidUuidVersion, "UUID version does not carry a supported timestamp");
        AssertTimestampFailure(invalidVariant, IdentifierErrorCode.InvalidUuidVariant, "UUID does not use RFC 9562 variant bits");
        AssertTimestampFailure(overflow, IdentifierErrorCode.TimestampOverflow, "unix milliseconds is outside DateTimeOffset range");
        Assert.That(maximumV7.UnixMilliseconds(), Is.EqualTo(BigEndianTools.MaxUInt48));
        AssertIdentifierException(() => version4.UnixMilliseconds(), IdentifierErrorCode.InvalidUuidVersion, "UUID version does not carry a supported timestamp");
        AssertIdentifierException(() => wrongVariant.UnixMilliseconds(), IdentifierErrorCode.InvalidUuidVariant, "UUID does not use RFC 9562 variant bits");
        AssertIdentifierException(() => version4.Time(), IdentifierErrorCode.InvalidUuidVersion, "UUID version does not carry a supported timestamp");
        AssertIdentifierException(() => wrongVariant.Time(), IdentifierErrorCode.InvalidUuidVariant, "UUID does not use RFC 9562 variant bits");
        AssertIdentifierException(() => maximumV7.Time(), IdentifierErrorCode.TimestampOverflow, "unix milliseconds is outside DateTimeOffset range");
    }

    [Test]
    public void ValueConstructionCopyAndEquality_ReportBoundariesAndDefendStoredBytes()
    {
        byte[] bytes = ExampleBytes();
        UuidValue value = new UuidValue(bytes);
        byte[] destination = new byte[17];
        destination[16] = 99;

        bytes[0] = 255;
        value.CopyTo(destination);
        destination[1] = 255;

        Assert.That(value.ToBytes(), Is.EqualTo(ExampleBytes()));
        Assert.That(destination[16], Is.EqualTo(99));
        Assert.That(value.EqualsValue(null), Is.False);
        Assert.That(value.EqualsBytes(ExampleBytes()), Is.True);
        Assert.That(value.EqualsBytes(new byte[16]), Is.False);
        Assert.That(value.EqualsBytes(new byte[15]), Is.False);
        Assert.That(value.EqualsBytes(null), Is.False);
        Assert.That(value.VersionNumber(), Is.EqualTo(6));

        ArgumentNullException constructorNull = Assert.Throws<ArgumentNullException>(() => new UuidValue(null));
        IdentifierException constructorLength = Assert.Throws<IdentifierException>(() => new UuidValue(new byte[15]));
        ArgumentNullException destinationNull = Assert.Throws<ArgumentNullException>(() => value.CopyTo(null));
        IdentifierException destinationLength = Assert.Throws<IdentifierException>(() => value.CopyTo(new byte[15]));

        Assert.That(constructorNull.ParamName, Is.EqualTo("bytes"));
        Assert.That(constructorLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(constructorLength.Message, Is.EqualTo("UUID byte array must contain exactly 16 bytes"));
        Assert.That(destinationNull.ParamName, Is.EqualTo("destination"));
        Assert.That(destinationLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(destinationLength.Message, Is.EqualTo("destination must have room for 16 bytes"));
    }

    [Test]
    public void GuidConversion_PreservesCanonicalTextAndNetworkByteOrder()
    {
        Guid guid = new Guid("00112233-4455-6677-8899-aabbccddeeff");

        UuidValue value = new Uuid().FromGuid(guid);

        Assert.That(value.ToBytes(), Is.EqualTo(ExampleBytes()));
        Assert.That(value.ToCanonicalString(), Is.EqualTo(guid.ToString()));
        Assert.That(value.ToGuid(), Is.EqualTo(guid));
    }

    [Test]
    public void PublicComparison_OrdersAllNullAndValueCombinations()
    {
        IUuid uuid = new Uuid();
        UuidValue low = new UuidValue(new byte[16]);
        byte[] highBytes = new byte[16];
        highBytes[15] = 1;
        UuidValue high = new UuidValue(highBytes);

        Assert.That(uuid.CompareValues(null, null), Is.EqualTo(0));
        Assert.That(uuid.CompareValues(null, low), Is.LessThan(0));
        Assert.That(uuid.CompareValues(low, null), Is.GreaterThan(0));
        Assert.That(uuid.CompareValues(low, high), Is.LessThan(0));
        Assert.That(uuid.CompareValues(high, low), Is.GreaterThan(0));
        Assert.That(uuid.CompareValues(low, new UuidValue(new byte[16])), Is.EqualTo(0));
        Assert.That(low.CompareTo(null), Is.GreaterThan(0));
    }

    static byte[] ExampleBytes()
    {
        return new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
    }

    static void AssertParseFailure(UuidParseResult result, IdentifierErrorCode code, string error)
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
