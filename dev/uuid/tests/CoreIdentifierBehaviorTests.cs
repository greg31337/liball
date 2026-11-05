using System;
using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class CoreIdentifierBehaviorTests
{
    [Test]
    public void NullText_ReturnsCompleteFailureResultsAndCodedExceptions()
    {
        IIdentifier identifier = new Identifier();

        IdentifierValidationResult validation = identifier.Validate(null);
        IdentifierParseResult parse = identifier.Parse(null);
        IdentifierTimestampResult timestamp = identifier.TimestampText(null);

        AssertValidationFailure(validation, IdentifierErrorCode.NullInput, "identifier text is null");
        AssertParseFailure(parse, IdentifierErrorCode.NullInput, "identifier text is null");
        AssertTimestampFailure(timestamp, IdentifierErrorCode.NullInput, "identifier text is null");
        Assert.That(identifier.ClassifyKind(null), Is.EqualTo(IdentifierKind.Unknown));
        AssertIdentifierException(() => identifier.NormalizeText(null), IdentifierErrorCode.NullInput, "identifier text is null");
        AssertIdentifierException(() => identifier.CompareText(null, "1"), IdentifierErrorCode.NullInput, "identifier text is null");
        AssertIdentifierException(() => identifier.CompareText("1", null), IdentifierErrorCode.NullInput, "identifier text is null");
    }

    [Test]
    public void NullValues_ThrowDocumentedArgumentNullExceptions()
    {
        IIdentifier identifier = new Identifier();
        IIdentifierValue value = ParseValue("1");

        ArgumentNullException normalize = Assert.Throws<ArgumentNullException>(() => identifier.NormalizeValue(null));
        ArgumentNullException timestamp = Assert.Throws<ArgumentNullException>(() => identifier.TimestampValue(null));
        ArgumentNullException left = Assert.Throws<ArgumentNullException>(() => identifier.CompareValues(null, value));
        ArgumentNullException right = Assert.Throws<ArgumentNullException>(() => identifier.CompareValues(value, null));

        Assert.That(normalize.ParamName, Is.EqualTo("value"));
        Assert.That(timestamp.ParamName, Is.EqualTo("value"));
        Assert.That(left.ParamName, Is.EqualTo("left"));
        Assert.That(right.ParamName, Is.EqualTo("right"));
    }

    [TestCase("550e8400-e29b-41d4-a716-446655440000", IdentifierKind.Uuid, "550e8400-e29b-41d4-a716-446655440000")]
    [TestCase("01arz3ndektsv4rrffq69g5fav", IdentifierKind.Ulid, "01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    [TestCase("0ujtsYcgvSTl8PAuAdqWYSMnLOv", IdentifierKind.Ksuid, "0ujtsYcgvSTl8PAuAdqWYSMnLOv")]
    [TestCase("4194308096", IdentifierKind.Snowflake, "4194308096")]
    [TestCase("507F1F77BCF86CD799439011", IdentifierKind.ObjectId, "507f1f77bcf86cd799439011")]
    [TestCase("V1StGXR8_Z5jdHi6B-myT", IdentifierKind.NanoId, "V1StGXR8_Z5jdHi6B-myT")]
    public void CoreSuccessOperations_AgreeForEverySupportedKind(string text, IdentifierKind kind, string normalizedText)
    {
        IIdentifier identifier = new Identifier();

        IdentifierValidationResult validation = identifier.Validate(text);
        IdentifierValidationResult classification = identifier.Classify(text);
        IdentifierParseResult parse = identifier.Parse(text);

        Assert.That(validation.Ok, Is.True);
        Assert.That(validation.Code, Is.EqualTo(IdentifierErrorCode.None));
        Assert.That(validation.Kind, Is.EqualTo(kind));
        Assert.That(validation.NormalizedText, Is.EqualTo(normalizedText));
        Assert.That(classification.Ok, Is.True);
        Assert.That(classification.Kind, Is.EqualTo(kind));
        Assert.That(classification.NormalizedText, Is.EqualTo(normalizedText));
        Assert.That(identifier.ClassifyKind(text), Is.EqualTo(kind));
        Assert.That(identifier.NormalizeText(text), Is.EqualTo(normalizedText));
        Assert.That(parse.Ok, Is.True);
        Assert.That(parse.Code, Is.EqualTo(IdentifierErrorCode.None));
        Assert.That(parse.Kind, Is.EqualTo(kind));
        Assert.That(parse.NormalizedText, Is.EqualTo(normalizedText));
        Assert.That(parse.Value, Is.Not.Null);
        Assert.That(identifier.NormalizeValue(parse.Value), Is.EqualTo(normalizedText));
    }

    [Test]
    public void Classification_UsesPrecedenceAndFallsThroughFailedCandidates()
    {
        Identifier identifier = new Identifier();

        IdentifierValidationResult uuidBeforeSnowflake = identifier.Classify("00000000000000000000000000000000");
        IdentifierValidationResult ulidBeforeSnowflake = identifier.Classify("00000000000000000000000000");
        IdentifierValidationResult ksuidBeforeSnowflake = identifier.Classify("000000000000000000000000000");
        IdentifierValidationResult objectIdBeforeSnowflake = identifier.Classify("000000000000000000000000");
        IdentifierValidationResult snowflakeBeforeNanoId = identifier.Classify("000000000000000000000");
        IdentifierValidationResult nanoIdAfterOverflowingSnowflake = identifier.Classify("999999999999999999999");

        Assert.That(uuidBeforeSnowflake.Kind, Is.EqualTo(IdentifierKind.Uuid));
        Assert.That(ulidBeforeSnowflake.Kind, Is.EqualTo(IdentifierKind.Ulid));
        Assert.That(ksuidBeforeSnowflake.Kind, Is.EqualTo(IdentifierKind.Ksuid));
        Assert.That(objectIdBeforeSnowflake.Kind, Is.EqualTo(IdentifierKind.ObjectId));
        Assert.That(snowflakeBeforeNanoId.Kind, Is.EqualTo(IdentifierKind.Snowflake));
        Assert.That(snowflakeBeforeNanoId.NormalizedText, Is.EqualTo("0"));
        Assert.That(nanoIdAfterOverflowingSnowflake.Kind, Is.EqualTo(IdentifierKind.NanoId));
        Assert.That(nanoIdAfterOverflowingSnowflake.NormalizedText, Is.EqualTo("999999999999999999999"));
    }

    [Test]
    public void SnowflakeSignedBoundary_IsPreservedByRequestedValidationAndParsing()
    {
        Identifier identifier = new Identifier();
        const string maximum = "9223372036854775807";
        const string overflow = "9223372036854775808";

        IdentifierValidationResult maximumValidation = identifier.ValidateKind(maximum, IdentifierKind.Snowflake);
        IdentifierParseResult maximumParse = identifier.Parse(maximum);
        IdentifierValidationResult overflowValidation = identifier.ValidateKind(overflow, IdentifierKind.Snowflake);
        IdentifierParseResult overflowParse = identifier.Parse(overflow);

        Assert.That(maximumValidation.Ok, Is.True);
        Assert.That(maximumValidation.Kind, Is.EqualTo(IdentifierKind.Snowflake));
        Assert.That(maximumValidation.NormalizedText, Is.EqualTo(maximum));
        Assert.That(maximumParse.Ok, Is.True);
        Assert.That(maximumParse.Kind, Is.EqualTo(IdentifierKind.Snowflake));
        AssertValidationFailure(overflowValidation, IdentifierErrorCode.ValueOverflow, "Snowflake decimal value exceeds 63-bit range");
        AssertParseFailure(overflowParse, IdentifierErrorCode.UnsupportedKind, "identifier kind is unknown");
    }

    [TestCase("00000000-0000-4000-8000-000000000001", "00000000-0000-4000-8000-000000000002")]
    [TestCase("01ARZ3NDEKTSV4RRFFQ69G5FAV", "01ARZ3NDEKTSV4RRFFQ69G5FAW")]
    [TestCase("000000000000000000000000000", "000000000000000000000000001")]
    [TestCase("9", "10")]
    [TestCase("000000000000000000000001", "000000000000000000000002")]
    [TestCase("V1StGXR8_Z5jdHi6B-myT", "V1StGXR8_Z5jdHi6B-myU")]
    public void CompareText_OrdersEveryKindAndHandlesEquality(string earlier, string later)
    {
        Identifier identifier = new Identifier();

        Assert.That(identifier.CompareText(earlier, later), Is.LessThan(0));
        Assert.That(identifier.CompareText(later, earlier), Is.GreaterThan(0));
        Assert.That(identifier.CompareText(earlier, earlier), Is.EqualTo(0));
    }

    [Test]
    public void CompareValues_OrdersEveryAdjacentKindAndUnknownConsumerKinds()
    {
        Identifier identifier = new Identifier();
        IIdentifierValue unknown = new ConsumerIdentifierValue(IdentifierKind.Unknown, "consumer-value");
        IIdentifierValue uuid = ParseValue("550e8400-e29b-41d4-a716-446655440000");
        IIdentifierValue ulid = ParseValue("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        IIdentifierValue ksuid = ParseValue("0ujtsYcgvSTl8PAuAdqWYSMnLOv");
        IIdentifierValue snowflake = ParseValue("4194308096");
        IIdentifierValue objectId = ParseValue("507f1f77bcf86cd799439011");
        IIdentifierValue nanoId = ParseValue("V1StGXR8_Z5jdHi6B-myT");

        AssertCrossKindOrder(identifier, unknown, uuid);
        AssertCrossKindOrder(identifier, uuid, ulid);
        AssertCrossKindOrder(identifier, ulid, ksuid);
        AssertCrossKindOrder(identifier, ksuid, snowflake);
        AssertCrossKindOrder(identifier, snowflake, objectId);
        AssertCrossKindOrder(identifier, objectId, nanoId);
    }

    [Test]
    public void CompareValues_RejectsInvalidConsumerSnowflakeTextOnEitherSide()
    {
        Identifier identifier = new Identifier();
        IIdentifierValue invalid = new ConsumerIdentifierValue(IdentifierKind.Snowflake, "x");
        IIdentifierValue valid = new ConsumerIdentifierValue(IdentifierKind.Snowflake, "1");

        AssertIdentifierException(() => identifier.CompareValues(invalid, valid), IdentifierErrorCode.InvalidCharacter, "invalid decimal character at index 0");
        AssertIdentifierException(() => identifier.CompareValues(valid, invalid), IdentifierErrorCode.InvalidCharacter, "invalid decimal character at index 0");
    }

    [Test]
    public void TimestampValue_UsesTimestampContractBeforeCanonicalTextFallback()
    {
        Identifier identifier = new Identifier();
        IdentifierTimestampResult supplied = IdentifierTimestampResult.Success(IdentifierKind.Ksuid, 123000, 123, DateTimeOffset.FromUnixTimeSeconds(123));
        IIdentifierValue timestamped = new ConsumerTimestampedValue("not an identifier", supplied);

        IdentifierTimestampResult result = identifier.TimestampValue(timestamped);

        Assert.That(result, Is.SameAs(supplied));
    }

    [Test]
    public void TimestampValue_FallsBackToCanonicalTextForOtherConsumerValues()
    {
        Identifier identifier = new Identifier();
        UuidValue uuid = new Uuid().NewV7AtUnixMilliseconds(1469922850259);
        IIdentifierValue valid = new ConsumerIdentifierValue(IdentifierKind.Unknown, uuid.ToCanonicalString());
        IIdentifierValue invalid = new ConsumerIdentifierValue(IdentifierKind.Unknown, "not an identifier");
        IIdentifierValue nanoId = ParseValue("V1StGXR8_Z5jdHi6B-myT");

        IdentifierTimestampResult validResult = identifier.TimestampValue(valid);
        IdentifierTimestampResult invalidResult = identifier.TimestampValue(invalid);
        IdentifierTimestampResult nanoIdResult = identifier.TimestampValue(nanoId);

        Assert.That(validResult.Ok, Is.True);
        Assert.That(validResult.Kind, Is.EqualTo(IdentifierKind.Uuid));
        Assert.That(validResult.UnixMilliseconds, Is.EqualTo(1469922850259));
        AssertTimestampFailure(invalidResult, IdentifierErrorCode.UnsupportedKind, "identifier kind is unknown");
        AssertTimestampFailure(nanoIdResult, IdentifierErrorCode.UnsupportedKind, "identifier kind does not carry a timestamp");
    }

    [Test]
    public void IdentifierException_PreservesItsPublicCodeAndMessage()
    {
        IdentifierException exception = new IdentifierException(IdentifierErrorCode.ValueOverflow, "value is too large");

        Assert.That(exception, Is.InstanceOf<System.Exception>());
        Assert.That(exception.Code, Is.EqualTo(IdentifierErrorCode.ValueOverflow));
        Assert.That(exception.Message, Is.EqualTo("value is too large"));
    }

    [Test]
    public void UnsupportedText_ReturnsNeutralResultsAndCodedExceptions()
    {
        Identifier identifier = new Identifier();

        IdentifierValidationResult validation = identifier.Classify("not an identifier");
        IdentifierParseResult parse = identifier.Parse("not an identifier");
        IdentifierTimestampResult timestamp = identifier.TimestampText("not an identifier");

        AssertValidationFailure(validation, IdentifierErrorCode.UnsupportedKind, "identifier kind is unknown");
        AssertParseFailure(parse, IdentifierErrorCode.UnsupportedKind, "identifier kind is unknown");
        AssertTimestampFailure(timestamp, IdentifierErrorCode.UnsupportedKind, "identifier kind is unknown");
        AssertIdentifierException(() => identifier.NormalizeText("not an identifier"), IdentifierErrorCode.UnsupportedKind, "identifier kind is unknown");
        AssertIdentifierException(() => identifier.CompareText("not an identifier", "1"), IdentifierErrorCode.UnsupportedKind, "identifier kind is unknown");
        AssertIdentifierException(() => identifier.CompareText("1", "not an identifier"), IdentifierErrorCode.UnsupportedKind, "identifier kind is unknown");
    }

    static IIdentifierValue ParseValue(string text)
    {
        IdentifierParseResult result = new Identifier().Parse(text);
        if (!result.Ok || result.Value == null)
        {
            throw new InvalidOperationException(result.Error);
        }

        return result.Value;
    }

    static void AssertCrossKindOrder(Identifier identifier, IIdentifierValue earlier, IIdentifierValue later)
    {
        Assert.That(identifier.CompareValues(earlier, later), Is.LessThan(0));
        Assert.That(identifier.CompareValues(later, earlier), Is.GreaterThan(0));
    }

    static void AssertIdentifierException(TestDelegate action, IdentifierErrorCode code, string message)
    {
        IdentifierException exception = Assert.Throws<IdentifierException>(action);
        Assert.That(exception.Code, Is.EqualTo(code));
        Assert.That(exception.Message, Is.EqualTo(message));
    }

    static void AssertValidationFailure(IdentifierValidationResult result, IdentifierErrorCode code, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Code, Is.EqualTo(code));
    }

    static void AssertParseFailure(IdentifierParseResult result, IdentifierErrorCode code, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Value, Is.Null);
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

    class ConsumerIdentifierValue : IIdentifierValue
    {
        IdentifierKind IdentifierKind;
        string Text;

        public ConsumerIdentifierValue(IdentifierKind kind, string text)
        {
            IdentifierKind = kind;
            Text = text;
        }

        public IdentifierKind Kind()
        {
            return IdentifierKind;
        }

        public string ToCanonicalString()
        {
            return Text;
        }
    }

    class ConsumerTimestampedValue : IIdentifierValue, ITimestampedIdentifierValue
    {
        string Text;
        IdentifierTimestampResult Result;

        public ConsumerTimestampedValue(string text, IdentifierTimestampResult result)
        {
            Text = text;
            Result = result;
        }

        public IdentifierKind Kind()
        {
            return IdentifierKind.Ksuid;
        }

        public string ToCanonicalString()
        {
            return Text;
        }

        public IdentifierTimestampResult Timestamp()
        {
            return Result;
        }
    }
}
