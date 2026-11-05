using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class InstantTests
{
    [Test]
    public void UnixEpoch_ReturnsUnixEpochUtcTicks()
    {
        Assert.That(Instant.UnixEpoch().GetUtcTicks(), Is.EqualTo(TimeConstants.UnixEpochUtcTicks));
    }

    [TestCase(0L)]
    [TestCase(621355968000000000L)]
    [TestCase(3155378975999999999L)]
    public void FromUtcTicks_StoresSupportedUtcTicks(long utcTicks)
    {
        Assert.That(Instant.FromUtcTicks(utcTicks).GetUtcTicks(), Is.EqualTo(utcTicks));
    }

    [TestCase(-1L)]
    [TestCase(3155378976000000000L)]
    public void FromUtcTicks_RejectsTicksOutsideDateTimeRange(long utcTicks)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Instant.FromUtcTicks(utcTicks))!;

        Assert.That(exception.ParamName, Is.EqualTo("utcTicks"));
    }

    [TestCase(0L, 621355968000000000L)]
    [TestCase(1L, 621355968010000000L)]
    [TestCase(-1L, 621355967990000000L)]
    public void FromUnixSeconds_ConvertsSecondsSinceUnixEpoch(long unixSeconds, long expectedUtcTicks)
    {
        Assert.That(Instant.FromUnixSeconds(unixSeconds).GetUtcTicks(), Is.EqualTo(expectedUtcTicks));
    }

    [TestCase(1500L, 621355968015000000L)]
    [TestCase(-1500L, 621355967985000000L)]
    public void FromUnixMilliseconds_ConvertsMillisecondsSinceUnixEpoch(long unixMilliseconds, long expectedUtcTicks)
    {
        Assert.That(Instant.FromUnixMilliseconds(unixMilliseconds).GetUtcTicks(), Is.EqualTo(expectedUtcTicks));
    }

    [TestCase(1500001L, 621355968015000010L)]
    [TestCase(-1500001L, 621355967984999990L)]
    public void FromUnixMicroseconds_ConvertsMicrosecondsSinceUnixEpoch(long unixMicroseconds, long expectedUtcTicks)
    {
        Assert.That(Instant.FromUnixMicroseconds(unixMicroseconds).GetUtcTicks(), Is.EqualTo(expectedUtcTicks));
    }

    [Test]
    public void UnixFactories_ThrowForArithmeticOverflow()
    {
        Assert.Throws<OverflowException>(() => Instant.FromUnixSeconds(long.MaxValue));
        Assert.Throws<OverflowException>(() => Instant.FromUnixMilliseconds(long.MaxValue));
        Assert.Throws<OverflowException>(() => Instant.FromUnixMicroseconds(long.MaxValue));
    }

    [Test]
    public void UnixFactories_RejectValuesOutsideDateTimeRangeAfterConversion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Instant.FromUnixSeconds(253402300800L));
        Assert.Throws<ArgumentOutOfRangeException>(() => Instant.FromUnixSeconds(-62135596801L));
    }

    [Test]
    public void FromDateTimeOffset_ConvertsToUtcTicks()
    {
        DateTimeOffset value = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));
        Instant instant = Instant.FromDateTimeOffset(value);

        Assert.That(instant.GetUtcTicks(), Is.EqualTo(value.UtcDateTime.Ticks));
    }

    [Test]
    public void FromUtcDateTime_AcceptsUtcDateTime()
    {
        DateTime value = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(6789L);
        Instant instant = Instant.FromUtcDateTime(value);

        Assert.That(instant.GetUtcTicks(), Is.EqualTo(value.Ticks));
    }

    [Test]
    public void FromUtcDateTime_RejectsNonUtcDateTimeKinds()
    {
        ArgumentException localException = Assert.Throws<ArgumentException>(() => Instant.FromUtcDateTime(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local)))!;
        ArgumentException unspecifiedException = Assert.Throws<ArgumentException>(() => Instant.FromUtcDateTime(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)))!;

        Assert.That(localException.ParamName, Is.EqualTo("dateTime"));
        Assert.That(unspecifiedException.ParamName, Is.EqualTo("dateTime"));
    }

    [Test]
    public void Now_ReturnsInstantInsideSupportedUtcRange()
    {
        long ticks = Instant.Now().GetUtcTicks();

        Assert.That(ticks, Is.GreaterThanOrEqualTo(TimeConstants.MinUtcTicks));
        Assert.That(ticks, Is.LessThanOrEqualTo(TimeConstants.MaxUtcTicks));
    }

    [TestCase(621355968000000000L, 0L, 0L, 0L)]
    [TestCase(621355968010000000L, 1L, 1000L, 1000000L)]
    [TestCase(621355967999999999L, -1L, -1L, -1L)]
    [TestCase(621355967990000000L, -1L, -1000L, -1000000L)]
    public void UnixAccessors_ReturnFloorBasedUnixUnits(long utcTicks, long seconds, long milliseconds, long microseconds)
    {
        Instant instant = Instant.FromUtcTicks(utcTicks);

        Assert.That(instant.GetUnixSeconds(), Is.EqualTo(seconds));
        Assert.That(instant.GetUnixMilliseconds(), Is.EqualTo(milliseconds));
        Assert.That(instant.GetUnixMicroseconds(), Is.EqualTo(microseconds));
    }

    [Test]
    public void UnixAccessors_MatchDateTimeOffsetForEpochPreEpochAndRangeEdges()
    {
        long[] utcTicksValues = new long[]
        {
            TimeConstants.MinUtcTicks,
            TimeConstants.UnixEpochUtcTicks - 1L,
            TimeConstants.UnixEpochUtcTicks,
            TimeConstants.UnixEpochUtcTicks + 123456789L,
            TimeConstants.MaxUtcTicks
        };

        foreach (long utcTicks in utcTicksValues)
        {
            Instant instant = Instant.FromUtcTicks(utcTicks);
            DateTimeOffset dateTimeOffset = instant.ToDateTimeOffset();

            Assert.That(instant.GetUnixSeconds(), Is.EqualTo(dateTimeOffset.ToUnixTimeSeconds()));
            Assert.That(instant.GetUnixMilliseconds(), Is.EqualTo(dateTimeOffset.ToUnixTimeMilliseconds()));
        }
    }

    [Test]
    public void PreEpochInstant_RoundTripsThroughTextAndUnixAccessors()
    {
        Instant instant = Instant.FromUtcTicks(TimeConstants.UnixEpochUtcTicks - 1L);
        string text = instant.ToIsoString();
        Instant parsed = Instant.Parse(text);

        Assert.That(instant.GetUnixSeconds(), Is.EqualTo(-1L));
        Assert.That(instant.GetUnixMilliseconds(), Is.EqualTo(-1L));
        Assert.That(instant.GetUnixMicroseconds(), Is.EqualTo(-1L));
        Assert.That(text, Is.EqualTo("1969-12-31T23:59:59.9999999Z"));
        Assert.That(parsed.EqualsValue(instant), Is.True);
    }

    [Test]
    public void AddDuration_ReturnsInstantOffsetByDurationTicks()
    {
        Instant instant = Instant.UnixEpoch().AddDuration(Duration.FromSeconds(5L));

        Assert.That(instant.GetUtcTicks(), Is.EqualTo(TimeConstants.UnixEpochUtcTicks + 50000000L));
    }

    [Test]
    public void AddDuration_RequiresDuration()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Instant.UnixEpoch().AddDuration(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("duration"));
    }

    [Test]
    public void AddDuration_RejectsResultsOutsideSupportedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Instant.FromUtcTicks(TimeConstants.MaxUtcTicks).AddDuration(Duration.FromTicks(1L)));
    }

    [Test]
    public void SubtractDuration_ReturnsInstantOffsetByNegativeDurationTicks()
    {
        Instant instant = Instant.UnixEpoch().SubtractDuration(Duration.FromSeconds(5L));

        Assert.That(instant.GetUtcTicks(), Is.EqualTo(TimeConstants.UnixEpochUtcTicks - 50000000L));
    }

    [Test]
    public void SubtractDuration_RequiresDuration()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Instant.UnixEpoch().SubtractDuration(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("duration"));
    }

    [Test]
    public void SubtractDuration_RejectsResultsOutsideSupportedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Instant.FromUtcTicks(TimeConstants.MinUtcTicks).SubtractDuration(Duration.FromTicks(1L)));
    }

    [Test]
    public void DurationArithmetic_AllowsMovesExactlyToSupportedRangeEdges()
    {
        Instant min = Instant.FromUtcTicks(TimeConstants.MinUtcTicks);
        Instant max = Instant.FromUtcTicks(TimeConstants.MaxUtcTicks);
        Duration fullRange = Duration.FromTicks(TimeConstants.MaxUtcTicks);

        Assert.That(min.AddDuration(fullRange).EqualsValue(max), Is.True);
        Assert.That(max.SubtractDuration(fullRange).EqualsValue(min), Is.True);
        Assert.That(min.DurationUntil(max).EqualsValue(fullRange), Is.True);
        Assert.That(max.DurationUntil(min).EqualsValue(fullRange.Negate()), Is.True);
    }

    [Test]
    public void DurationUntil_ReturnsDifferenceToOtherInstant()
    {
        Duration duration = Instant.UnixEpoch().DurationUntil(Instant.FromUnixSeconds(5L));

        Assert.That(duration.GetTicks(), Is.EqualTo(50000000L));
    }

    [Test]
    public void DurationUntil_RequiresOtherInstant()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Instant.UnixEpoch().DurationUntil(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void CompareTo_OrdersByUtcTicks()
    {
        Assert.That(Instant.FromUnixSeconds(-1L).CompareTo(Instant.UnixEpoch()), Is.EqualTo(-1));
        Assert.That(Instant.UnixEpoch().CompareTo(Instant.FromUnixSeconds(0L)), Is.EqualTo(0));
        Assert.That(Instant.FromUnixSeconds(1L).CompareTo(Instant.UnixEpoch()), Is.EqualTo(1));
    }

    [Test]
    public void CompareTo_RequiresOtherInstant()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Instant.UnixEpoch().CompareTo(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void EqualsValue_ComparesUtcTicksAndHandlesNull()
    {
        Assert.That(Instant.UnixEpoch().EqualsValue(Instant.FromUnixSeconds(0L)), Is.True);
        Assert.That(Instant.UnixEpoch().EqualsValue(Instant.FromUnixSeconds(1L)), Is.False);
        Assert.That(Instant.UnixEpoch().EqualsValue(null), Is.False);
    }

    [Test]
    public void ToDateTimeOffset_ReturnsUtcOffsetDateTime()
    {
        Instant instant = Instant.FromUnixSeconds(1L);
        DateTimeOffset dateTimeOffset = instant.ToDateTimeOffset();

        Assert.That(dateTimeOffset.Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(dateTimeOffset.UtcTicks, Is.EqualTo(instant.GetUtcTicks()));
    }

    [Test]
    public void ToUtcDateTime_ReturnsUtcDateTime()
    {
        Instant instant = Instant.FromUnixSeconds(1L);
        DateTime dateTime = instant.ToUtcDateTime();

        Assert.That(dateTime.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(dateTime.Ticks, Is.EqualTo(instant.GetUtcTicks()));
    }

    [Test]
    public void DotNetInterop_RoundTripsRangeEdgesAndOffsetValues()
    {
        DateTime minDateTime = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        DateTime maxDateTime = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        DateTimeOffset offsetValue = new DateTimeOffset(2024, 2, 29, 23, 45, 1, TimeSpan.FromHours(5)).AddTicks(2345678L);

        Instant minInstant = Instant.FromUtcDateTime(minDateTime);
        Instant maxInstant = Instant.FromUtcDateTime(maxDateTime);
        Instant offsetInstant = Instant.FromDateTimeOffset(offsetValue);

        Assert.That(minInstant.ToUtcDateTime(), Is.EqualTo(minDateTime));
        Assert.That(maxInstant.ToUtcDateTime(), Is.EqualTo(maxDateTime));
        Assert.That(minInstant.ToDateTimeOffset(), Is.EqualTo(new DateTimeOffset(minDateTime)));
        Assert.That(maxInstant.ToDateTimeOffset(), Is.EqualTo(new DateTimeOffset(maxDateTime)));
        Assert.That(offsetInstant.ToDateTimeOffset().UtcTicks, Is.EqualTo(offsetValue.UtcTicks));
        Assert.That(offsetInstant.ToUtcDateTime().Ticks, Is.EqualTo(offsetValue.UtcDateTime.Ticks));
    }

    [TestCase(0L, "0001-01-01T00:00:00Z")]
    [TestCase(621355968000000000L, "1970-01-01T00:00:00Z")]
    [TestCase(621355968010000000L, "1970-01-01T00:00:01Z")]
    [TestCase(621355968001234567L, "1970-01-01T00:00:00.1234567Z")]
    [TestCase(3155378975999999999L, "9999-12-31T23:59:59.9999999Z")]
    public void ToIsoString_FormatsCanonicalUtcInstant(long utcTicks, string expected)
    {
        Assert.That(Instant.FromUtcTicks(utcTicks).ToIsoString(), Is.EqualTo(expected));
    }

    [TestCase("1970-01-01T00:00:00Z", 621355968000000000L)]
    [TestCase("1970-01-01T00:00:00.1234567Z", 621355968001234567L)]
    [TestCase("1970-01-01T01:30:00+01:30", 621355968000000000L)]
    [TestCase("1969-12-31T18:30:00-05:30", 621355968000000000L)]
    [TestCase("1970-01-01T00:00:30+00:00:30", 621355968000000000L)]
    [TestCase("1969-12-31T23:59:30-00:00:30", 621355968000000000L)]
    public void Parse_ReadsIsoInstantText(string text, long expectedUtcTicks)
    {
        Assert.That(Instant.Parse(text).GetUtcTicks(), Is.EqualTo(expectedUtcTicks));
    }

    [TestCase("0001-01-01T00:00:00Z", 0L)]
    [TestCase("0001-01-01T18:00:00+18:00", 0L)]
    [TestCase("9999-12-31T23:59:59.9999999Z", 3155378975999999999L)]
    [TestCase("9999-12-31T05:59:59.9999999-18:00", 3155378975999999999L)]
    public void Parse_ReadsSupportedRangeEdgesWithUtcOffsets(string text, long expectedUtcTicks)
    {
        Assert.That(Instant.Parse(text).GetUtcTicks(), Is.EqualTo(expectedUtcTicks));
    }

    [Test]
    public void Parse_RequiresNonNullText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Instant.Parse(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("text"));
    }

    [TestCase("")]
    [TestCase("1970-01-01T00:00:00")]
    [TestCase("1970-01-01t00:00:00Z")]
    [TestCase("1970-01-01T00:00Z")]
    [TestCase("1970-01-01T00:00:00z")]
    [TestCase("1970-01-01T00:00:00+0100")]
    [TestCase("1970-01-01T00:00:00+01")]
    [TestCase("1970-01-01T00:00:00+01:60")]
    [TestCase("1970-01-01T00:00:00+00:00:60")]
    [TestCase("1970-01-01T00:00:00+18:00:01")]
    [TestCase("1970-01-01T00:00:00+19:00")]
    [TestCase("1970-01-01T00:00:00+24:00")]
    [TestCase("1970-01-01T00:00:00+0A:00")]
    [TestCase("1970-01-01T00:00:00+01:00:00:")]
    public void Parse_RejectsMalformedOrUnsupportedInstantText(string text)
    {
        Assert.Throws<FormatException>(() => Instant.Parse(text));
    }

    [Test]
    public void Parse_RejectsUtcResultsOutsideSupportedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Instant.Parse("0001-01-01T00:00:00+00:00:01"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Instant.Parse("9999-12-31T23:59:59.9999999-00:00:01"));
    }

    [TestCase(0L)]
    [TestCase(621355968000000000L)]
    [TestCase(621355968001234567L)]
    [TestCase(3155378975999999999L)]
    public void Parse_RoundTripsFormattedInstantText(long utcTicks)
    {
        Instant instant = Instant.FromUtcTicks(utcTicks);
        Instant parsed = Instant.Parse(instant.ToIsoString());

        Assert.That(parsed.EqualsValue(instant), Is.True);
    }
}
