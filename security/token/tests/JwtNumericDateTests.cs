using token;

namespace token.Tests;

public class JwtNumericDateTests
{
    [TestCase("0", 0L)]
    [TestCase("1", 10000000L)]
    [TestCase("-1", -10000000L)]
    [TestCase("1.25", 12500000L)]
    [TestCase("-0.0000001", -1L)]
    [TestCase("1.23456789", 12345678L)]
    [TestCase("-1.23456789", -12345678L)]
    [TestCase("1.5e2", 1500000000L)]
    [TestCase("15E-1", 15000000L)]
    [TestCase("1e-100000", 0L)]
    public void ToDateTimeOffsetConvertsSecondsAndFractionsToTicks(
        string numberText,
        long expectedTicks)
    {
        JwtNumericDateResult result = JwtNumericDate.ToDateTimeOffset(numberText);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.DateTime,
                Is.EqualTo(DateTimeOffset.UnixEpoch.AddTicks(expectedTicks)));
            Assert.That(result.Seconds,
                Is.EqualTo((decimal)expectedTicks / TimeSpan.TicksPerSecond));
            Assert.That(result.NumberText, Is.EqualTo(numberText));
        });
    }

    [Test]
    public void ToDateTimeOffsetSupportsTheCompleteDateTimeOffsetRange()
    {
        JwtNumericDateResult minimum = JwtNumericDate.ToDateTimeOffset("-62135596800");
        JwtNumericDateResult maximum = JwtNumericDate.ToDateTimeOffset(
            "253402300799.9999999");

        Assert.Multiple(() =>
        {
            Assert.That(minimum.Success, Is.True);
            Assert.That(minimum.DateTime, Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(maximum.Success, Is.True);
            Assert.That(maximum.DateTime, Is.EqualTo(DateTimeOffset.MaxValue));
        });
    }

    [TestCase("-62135596800.00000001")]
    [TestCase("253402300799.99999991")]
    [TestCase("253402300800")]
    [TestCase("-62135596801")]
    [TestCase("1e100000")]
    [TestCase("-1e100000")]
    public void ToDateTimeOffsetRejectsValuesOutsideTheSupportedRange(string numberText)
    {
        AssertFailure(
            JwtNumericDate.ToDateTimeOffset(numberText),
            "numeric_date_out_of_range");
    }

    [TestCase(0, "0")]
    [TestCase(1, "0.0000001")]
    [TestCase(-1, "-0.0000001")]
    [TestCase(10000000, "1")]
    [TestCase(12345678, "1.2345678")]
    [TestCase(-12345678, "-1.2345678")]
    public void FromDateTimeOffsetProducesCanonicalNumberText(
        long ticks,
        string expected)
    {
        DateTimeOffset value = DateTimeOffset.UnixEpoch.AddTicks(ticks);

        JwtNumericDateResult result = JwtNumericDate.FromDateTimeOffset(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DateTime, Is.EqualTo(value));
            Assert.That(result.Seconds,
                Is.EqualTo((decimal)ticks / TimeSpan.TicksPerSecond));
            Assert.That(result.NumberText, Is.EqualTo(expected));
        });
    }

    [Test]
    public void FromDateTimeOffsetNormalizesOffsetsToUtc()
    {
        DateTimeOffset offsetValue = new DateTimeOffset(
            2024,
            5,
            6,
            12,
            34,
            56,
            TimeSpan.FromHours(2)).AddTicks(7654321);

        JwtNumericDateResult result = JwtNumericDate.FromDateTimeOffset(offsetValue);
        JwtNumericDateResult reparsed = JwtNumericDate.ToDateTimeOffset(result.NumberText);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DateTime.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(result.DateTime, Is.EqualTo(offsetValue.ToUniversalTime()));
            Assert.That(reparsed.Success, Is.True);
            Assert.That(reparsed.DateTime, Is.EqualTo(offsetValue.ToUniversalTime()));
        });
    }

    [Test]
    public void DecimalAndJsonValueOverloadsUseTheSameConversion()
    {
        JsonValue json = new JsonValue();
        json.Kind = TokenJsonValueKind.Number;
        json.NumberText = "123.0000001";

        JwtNumericDateResult fromDecimal = JwtNumericDate.ToDateTimeOffset(123.0000001m);
        JwtNumericDateResult fromJson = JwtNumericDate.ToDateTimeOffset(json);
        JsonValue written = JwtNumericDate.ToJsonValue(fromJson.DateTime);

        Assert.Multiple(() =>
        {
            Assert.That(fromDecimal.Success, Is.True);
            Assert.That(fromJson.Success, Is.True);
            Assert.That(fromDecimal.DateTime, Is.EqualTo(fromJson.DateTime));
            Assert.That(written.Kind, Is.EqualTo(TokenJsonValueKind.Number));
            Assert.That(written.NumberText, Is.EqualTo("123.0000001"));
        });
    }

    [TestCase("")]
    [TestCase(" 1")]
    [TestCase("1 ")]
    [TestCase("01")]
    [TestCase("+1")]
    [TestCase("1.")]
    [TestCase(".1")]
    [TestCase("NaN")]
    [TestCase("Infinity")]
    [TestCase("\"1\"")]
    [TestCase("null")]
    public void ToDateTimeOffsetRejectsValuesThatAreNotStrictJsonNumbers(string numberText)
    {
        AssertFailure(
            JwtNumericDate.ToDateTimeOffset(numberText),
            "invalid_numeric_date");
    }

    [Test]
    public void JsonValueOverloadRejectsNonNumberAndIncompleteValues()
    {
        JsonValue text = new JsonValue();
        text.Kind = TokenJsonValueKind.String;
        text.StringValue = "1";
        JsonValue missing = new JsonValue();
        missing.Kind = TokenJsonValueKind.Number;

        Assert.Multiple(() =>
        {
            AssertFailure(
                JwtNumericDate.ToDateTimeOffset(text),
                "invalid_numeric_date");
            AssertFailure(
                JwtNumericDate.ToDateTimeOffset(missing),
                "invalid_numeric_date");
        });
    }

    [Test]
    public void TokenClockUsesInjectedTimeOnceAndNormalizesItToUtc()
    {
        DateTimeOffset supplied = new DateTimeOffset(
            2025,
            1,
            2,
            3,
            4,
            5,
            TimeSpan.FromHours(-7));
        int calls = 0;
        TokenClock clock = new TokenClock(() =>
        {
            calls += 1;
            return supplied;
        });

        JwtNumericDateResult result = JwtNumericDate.FromClock(clock);

        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(result.Success, Is.True);
            Assert.That(result.DateTime, Is.EqualTo(supplied.ToUniversalTime()));
            Assert.That(result.DateTime.Offset, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public void DefaultTokenClockReturnsCurrentUtcTime()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow;
        DateTimeOffset actual = TokenClock.CreateSystem().GetUtcNow();
        DateTimeOffset after = DateTimeOffset.UtcNow;

        Assert.Multiple(() =>
        {
            Assert.That(actual.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(actual, Is.GreaterThanOrEqualTo(before));
            Assert.That(actual, Is.LessThanOrEqualTo(after));
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => JwtNumericDate.ToDateTimeOffset((string)null),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtNumericDate.ToDateTimeOffset((JsonValue)null),
                Throws.ArgumentNullException);
            Assert.That(
                () => JwtNumericDate.FromClock(null),
                Throws.ArgumentNullException);
            Assert.That(
                () => new TokenClock(null),
                Throws.ArgumentNullException);
        });
    }

    static void AssertFailure(JwtNumericDateResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.NumberText, Is.Null);
    }
}
