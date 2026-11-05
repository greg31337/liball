namespace cookies.Tests;

public class DecimalSecondsParserTests
{
    [TestCase("0", 0L, false)]
    [TestCase("123", 123L, false)]
    [TestCase("-123", -123L, true)]
    [TestCase("-0", 0L, true)]
    [TestCase("00042", 42L, false)]
    public void MaxAgeParserAcceptsSignedDecimalSeconds(string text, long expected, bool negative)
    {
        DecimalSecondsParseResult result = DecimalSecondsParser.ParseMaxAgeSeconds(text.AsSpan());

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsNegative, Is.EqualTo(negative));
        Assert.That(result.Saturated, Is.False);
        Assert.That(result.Value, Is.EqualTo(expected));
    }

    [Test]
    public void MaxAgeParserAcceptsExactInt64Bounds()
    {
        DecimalSecondsParseResult maximum = DecimalSecondsParser.ParseMaxAgeSeconds("9223372036854775807".AsSpan());
        DecimalSecondsParseResult minimum = DecimalSecondsParser.ParseMaxAgeSeconds("-9223372036854775808".AsSpan());

        Assert.That(maximum.Success, Is.True);
        Assert.That(maximum.Saturated, Is.False);
        Assert.That(maximum.Value, Is.EqualTo(long.MaxValue));
        Assert.That(minimum.Success, Is.True);
        Assert.That(minimum.Saturated, Is.False);
        Assert.That(minimum.Value, Is.EqualTo(long.MinValue));
    }

    [TestCase("9223372036854775808", long.MaxValue, false)]
    [TestCase("999999999999999999999999999999999999999", long.MaxValue, false)]
    [TestCase("-9223372036854775809", long.MinValue, true)]
    [TestCase("-999999999999999999999999999999999999999", long.MinValue, true)]
    public void MaxAgeParserSaturatesValuesOutsideInt64(string text, long expected, bool negative)
    {
        DecimalSecondsParseResult result = DecimalSecondsParser.ParseMaxAgeSeconds(text.AsSpan());

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsNegative, Is.EqualTo(negative));
        Assert.That(result.Saturated, Is.True);
        Assert.That(result.Value, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("-")]
    [TestCase("+1")]
    [TestCase(" 1")]
    [TestCase("1 ")]
    [TestCase("1.0")]
    [TestCase("12x")]
    [TestCase("999999999999999999999999x")]
    public void MaxAgeParserRejectsAnythingOtherThanOptionalMinusAndDigits(string text)
    {
        DecimalSecondsParseResult result = DecimalSecondsParser.ParseMaxAgeSeconds(text.AsSpan());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Saturated, Is.False);
        Assert.That(result.Value, Is.Zero);
    }

    [TestCase("0", 0, false)]
    [TestCase("42", 42, false)]
    [TestCase("2147483647", int.MaxValue, false)]
    [TestCase("2147483648", int.MaxValue, true)]
    [TestCase("999999999999999999999999", int.MaxValue, true)]
    public void NumericLimitParserSaturatesAtInt32Maximum(string text, int expected, bool saturated)
    {
        NumericLimitParseResult result = DecimalSecondsParser.ParseNumericLimit(text.AsSpan());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Saturated, Is.EqualTo(saturated));
        Assert.That(result.Value, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("-1")]
    [TestCase("+1")]
    [TestCase("1 ")]
    [TestCase("999999999999999999999x")]
    public void NumericLimitParserRejectsNonDigitsEvenAfterOverflow(string text)
    {
        NumericLimitParseResult result = DecimalSecondsParser.ParseNumericLimit(text.AsSpan());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Saturated, Is.False);
        Assert.That(result.Value, Is.Zero);
    }

    [Test]
    public void DateArithmeticAddsWholeSecondsUsingUtcTicks()
    {
        DateTimeOffset origin = new DateTimeOffset(2029, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));
        DateTimeSaturationResult positive = DecimalSecondsParser.AddSecondsSaturated(origin, 30);
        DateTimeSaturationResult negative = DecimalSecondsParser.AddSecondsSaturated(origin, -30);

        Assert.That(positive.Saturated, Is.False);
        Assert.That(positive.Value, Is.EqualTo(origin.ToUniversalTime().AddSeconds(30)));
        Assert.That(positive.Value.Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(negative.Saturated, Is.False);
        Assert.That(negative.Value, Is.EqualTo(origin.ToUniversalTime().AddSeconds(-30)));
    }

    [Test]
    public void DateArithmeticAllowsExactDateTimeOffsetBounds()
    {
        DateTimeOffset nearMaximum = DateTimeOffset.MaxValue.AddSeconds(-5);
        DateTimeOffset nearMinimum = DateTimeOffset.MinValue.AddSeconds(5);
        DateTimeSaturationResult maximum = DecimalSecondsParser.AddSecondsSaturated(nearMaximum, 5);
        DateTimeSaturationResult minimum = DecimalSecondsParser.AddSecondsSaturated(nearMinimum, -5);

        Assert.That(maximum.Saturated, Is.False);
        Assert.That(maximum.Value, Is.EqualTo(DateTimeOffset.MaxValue));
        Assert.That(minimum.Saturated, Is.False);
        Assert.That(minimum.Value, Is.EqualTo(DateTimeOffset.MinValue));
    }

    [Test]
    public void DateArithmeticSaturatesOutsideDateTimeOffsetBounds()
    {
        DateTimeOffset nearMaximum = DateTimeOffset.MaxValue.AddSeconds(-5);
        DateTimeOffset nearMinimum = DateTimeOffset.MinValue.AddSeconds(5);
        DateTimeSaturationResult maximum = DecimalSecondsParser.AddSecondsSaturated(nearMaximum, 6);
        DateTimeSaturationResult minimum = DecimalSecondsParser.AddSecondsSaturated(nearMinimum, -6);
        DateTimeSaturationResult hugeMaximum = DecimalSecondsParser.AddSecondsSaturated(DateTimeOffset.UnixEpoch, long.MaxValue);
        DateTimeSaturationResult hugeMinimum = DecimalSecondsParser.AddSecondsSaturated(DateTimeOffset.UnixEpoch, long.MinValue);

        Assert.That(maximum.Saturated, Is.True);
        Assert.That(maximum.Value, Is.EqualTo(DateTimeOffset.MaxValue));
        Assert.That(minimum.Saturated, Is.True);
        Assert.That(minimum.Value, Is.EqualTo(DateTimeOffset.MinValue));
        Assert.That(hugeMaximum.Saturated, Is.True);
        Assert.That(hugeMaximum.Value, Is.EqualTo(DateTimeOffset.MaxValue));
        Assert.That(hugeMinimum.Saturated, Is.True);
        Assert.That(hugeMinimum.Value, Is.EqualTo(DateTimeOffset.MinValue));
    }
}
