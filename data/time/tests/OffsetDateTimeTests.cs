using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class OffsetDateTimeTests
{
    [Test]
    public void Create_StoresLocalDateTimeAndOffset()
    {
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 2, 29, 12, 34, 56, 789, 123, 4);
        Offset offset = Offset.FromSeconds(19845);
        OffsetDateTime offsetDateTime = OffsetDateTime.Create(localDateTime, offset);

        Assert.That(offsetDateTime.GetLocalDateTime(), Is.SameAs(localDateTime));
        Assert.That(offsetDateTime.GetOffset(), Is.SameAs(offset));
        Assert.That(offsetDateTime.GetDate(), Is.SameAs(localDateTime.GetDate()));
        Assert.That(offsetDateTime.GetTime(), Is.SameAs(localDateTime.GetTime()));
    }

    [Test]
    public void Create_RequiresLocalDateTimeAndOffset()
    {
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 1, 1, 0, 0, 0);
        Offset offset = Offset.Zero();

        ArgumentNullException localException = Assert.Throws<ArgumentNullException>(() => OffsetDateTime.Create(null!, offset))!;
        ArgumentNullException offsetException = Assert.Throws<ArgumentNullException>(() => OffsetDateTime.Create(localDateTime, null!))!;

        Assert.That(localException.ParamName, Is.EqualTo("localDateTime"));
        Assert.That(offsetException.ParamName, Is.EqualTo("offset"));
    }

    [Test]
    public void CreateFromFields_StoresLocalFieldsAndOffset()
    {
        Offset offset = Offset.FromHours(-8);
        OffsetDateTime offsetDateTime = OffsetDateTime.Create(2024, 2, 29, 12, 34, 56, 789, 123, 4, offset);
        LocalDateTime localDateTime = offsetDateTime.GetLocalDateTime();

        Assert.That(localDateTime.GetYear(), Is.EqualTo(2024));
        Assert.That(localDateTime.GetMonth(), Is.EqualTo(2));
        Assert.That(localDateTime.GetDay(), Is.EqualTo(29));
        Assert.That(localDateTime.GetHour(), Is.EqualTo(12));
        Assert.That(localDateTime.GetMinute(), Is.EqualTo(34));
        Assert.That(localDateTime.GetSecond(), Is.EqualTo(56));
        Assert.That(localDateTime.GetMillisecond(), Is.EqualTo(789));
        Assert.That(localDateTime.GetMicrosecond(), Is.EqualTo(123));
        Assert.That(localDateTime.GetTickRemainder(), Is.EqualTo(4));
        Assert.That(offsetDateTime.GetOffset(), Is.SameAs(offset));
    }

    [Test]
    public void CreateFromFields_RequiresOffset()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => OffsetDateTime.Create(2024, 1, 1, 0, 0, 0, null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("offset"));
    }

    [Test]
    public void CreateFromFields_RejectsInvalidLocalDateTimeFields()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OffsetDateTime.Create(2023, 2, 29, 0, 0, 0, Offset.Zero()));
        Assert.Throws<ArgumentOutOfRangeException>(() => OffsetDateTime.Create(2024, 1, 1, 24, 0, 0, Offset.Zero()));
    }

    [Test]
    public void FromDateTimeOffset_PreservesLocalDateTimeAndOffset()
    {
        DateTimeOffset value = new DateTimeOffset(2024, 2, 29, 23, 45, 1, TimeSpan.FromHours(5)).AddTicks(2345678L);
        OffsetDateTime offsetDateTime = OffsetDateTime.FromDateTimeOffset(value);
        LocalDateTime localDateTime = offsetDateTime.GetLocalDateTime();

        Assert.That(localDateTime.ToDateTimeUnspecified(), Is.EqualTo(value.DateTime));
        Assert.That(offsetDateTime.GetOffset().GetTotalSeconds(), Is.EqualTo((int)value.Offset.TotalSeconds));
    }

    [Test]
    public void FromDateTimeOffset_PreservesRangeEdgeValues()
    {
        OffsetDateTime min = OffsetDateTime.FromDateTimeOffset(DateTimeOffset.MinValue);
        OffsetDateTime max = OffsetDateTime.FromDateTimeOffset(DateTimeOffset.MaxValue);

        Assert.That(min.GetLocalDateTime().ToDateTimeUnspecified(), Is.EqualTo(DateTime.MinValue));
        Assert.That(min.GetOffset().EqualsValue(Offset.Zero()), Is.True);
        Assert.That(max.GetLocalDateTime().ToDateTimeUnspecified(), Is.EqualTo(DateTime.MaxValue));
        Assert.That(max.GetOffset().EqualsValue(Offset.Zero()), Is.True);
    }

    [Test]
    public void FromInstant_AppliesOffsetToLocalDateTime()
    {
        OffsetDateTime offsetDateTime = OffsetDateTime.FromInstant(Instant.UnixEpoch(), Offset.FromMinutes(90));

        Assert.That(offsetDateTime.GetLocalDateTime().ToIsoString(), Is.EqualTo("1970-01-01T01:30:00"));
        Assert.That(offsetDateTime.GetOffset().GetTotalSeconds(), Is.EqualTo(5400));
        Assert.That(offsetDateTime.GetInstant().EqualsValue(Instant.UnixEpoch()), Is.True);
    }

    [TestCase(621355968000000000L, 0)]
    [TestCase(621355968000000000L, 5400)]
    [TestCase(621355968000000000L, -19800)]
    [TestCase(621355967999999999L, 0)]
    [TestCase(621355968001234567L, 1)]
    public void FromInstant_RoundTripsRepresentativeInstantsAndOffsets(long utcTicks, int offsetSeconds)
    {
        Instant instant = Instant.FromUtcTicks(utcTicks);
        OffsetDateTime offsetDateTime = OffsetDateTime.FromInstant(instant, Offset.FromSeconds(offsetSeconds));

        Assert.That(offsetDateTime.GetInstant().EqualsValue(instant), Is.True);
        Assert.That(offsetDateTime.GetOffset().GetTotalSeconds(), Is.EqualTo(offsetSeconds));
    }

    [Test]
    public void FromInstant_RequiresInstantAndOffset()
    {
        Instant instant = Instant.UnixEpoch();
        Offset offset = Offset.Zero();

        ArgumentNullException instantException = Assert.Throws<ArgumentNullException>(() => OffsetDateTime.FromInstant(null!, offset))!;
        ArgumentNullException offsetException = Assert.Throws<ArgumentNullException>(() => OffsetDateTime.FromInstant(instant, null!))!;

        Assert.That(instantException.ParamName, Is.EqualTo("instant"));
        Assert.That(offsetException.ParamName, Is.EqualTo("offset"));
    }

    [Test]
    public void FromInstant_RejectsLocalResultsOutsideSupportedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OffsetDateTime.FromInstant(Instant.FromUtcTicks(TimeConstants.MinUtcTicks), Offset.FromSeconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => OffsetDateTime.FromInstant(Instant.FromUtcTicks(TimeConstants.MaxUtcTicks), Offset.FromSeconds(1)));
    }

    [Test]
    public void GetInstant_ConvertsLocalDateTimeAndOffsetToInstant()
    {
        OffsetDateTime positiveOffset = OffsetDateTime.Create(1970, 1, 1, 1, 30, 0, Offset.FromMinutes(90));
        OffsetDateTime negativeOffset = OffsetDateTime.Create(1969, 12, 31, 18, 30, 0, Offset.FromMinutes(-330));

        Assert.That(positiveOffset.GetInstant().EqualsValue(Instant.UnixEpoch()), Is.True);
        Assert.That(negativeOffset.GetInstant().EqualsValue(Instant.UnixEpoch()), Is.True);
    }

    [Test]
    public void GetInstant_RejectsUtcResultsOutsideSupportedRange()
    {
        Assert.Throws<InvalidOperationException>(() => OffsetDateTime.Create(1, 1, 1, 0, 0, 0, Offset.FromSeconds(1)).GetInstant());
        Assert.Throws<InvalidOperationException>(() => OffsetDateTime.Create(9999, 12, 31, 23, 59, 59, 999, 999, 9, Offset.FromSeconds(-1)).GetInstant());
    }

    [Test]
    public void AddDuration_PreservesOffsetAndMovesInstantExactly()
    {
        OffsetDateTime source = OffsetDateTime.Create(1970, 1, 1, 1, 0, 0, Offset.FromHours(1));
        OffsetDateTime result = source.AddDuration(Duration.FromHours(1));

        Assert.That(result.GetOffset().EqualsValue(source.GetOffset()), Is.True);
        Assert.That(result.GetLocalDateTime().ToIsoString(), Is.EqualTo("1970-01-01T02:00:00"));
        Assert.That(result.GetInstant().EqualsValue(Instant.FromUnixSeconds(3600L)), Is.True);
    }

    [Test]
    public void AddDuration_RejectsInstantOverflowAtRangeEdges()
    {
        OffsetDateTime min = OffsetDateTime.FromInstant(Instant.FromUtcTicks(TimeConstants.MinUtcTicks), Offset.Zero());
        OffsetDateTime max = OffsetDateTime.FromInstant(Instant.FromUtcTicks(TimeConstants.MaxUtcTicks), Offset.Zero());

        Assert.Throws<ArgumentOutOfRangeException>(() => min.SubtractDuration(Duration.FromTicks(1L)));
        Assert.Throws<ArgumentOutOfRangeException>(() => max.AddDuration(Duration.FromTicks(1L)));
    }

    [Test]
    public void SubtractDuration_PreservesOffsetAndMovesInstantExactly()
    {
        OffsetDateTime source = OffsetDateTime.Create(1970, 1, 1, 1, 0, 0, Offset.FromHours(1));
        OffsetDateTime result = source.SubtractDuration(Duration.FromHours(1));

        Assert.That(result.GetOffset().EqualsValue(source.GetOffset()), Is.True);
        Assert.That(result.GetLocalDateTime().ToIsoString(), Is.EqualTo("1970-01-01T00:00:00"));
        Assert.That(result.GetInstant().EqualsValue(Instant.FromUnixSeconds(-3600L)), Is.True);
    }

    [Test]
    public void DurationArithmetic_RequiresDuration()
    {
        OffsetDateTime source = OffsetDateTime.Create(1970, 1, 1, 0, 0, 0, Offset.Zero());

        ArgumentNullException addException = Assert.Throws<ArgumentNullException>(() => source.AddDuration(null!))!;
        ArgumentNullException subtractException = Assert.Throws<ArgumentNullException>(() => source.SubtractDuration(null!))!;

        Assert.That(addException.ParamName, Is.EqualTo("duration"));
        Assert.That(subtractException.ParamName, Is.EqualTo("duration"));
    }

    [Test]
    public void AddPeriod_PreservesOffsetAndUsesLocalCalendarArithmetic()
    {
        OffsetDateTime source = OffsetDateTime.Create(2024, 1, 31, 23, 30, 0, Offset.FromHours(2));
        Period period = Period.Create(0L, 1L, 0L, 0L, 2L, 0L, 0L, 0L, 0L, 0L);
        OffsetDateTime result = source.AddPeriod(period);

        Assert.That(result.GetOffset().EqualsValue(source.GetOffset()), Is.True);
        Assert.That(result.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-01T01:30:00"));
    }

    [Test]
    public void ArithmeticSemantics_DurationUsesInstantAndPeriodUsesLocalCalendar()
    {
        OffsetDateTime source = OffsetDateTime.Create(2024, 1, 31, 23, 0, 0, Offset.FromHours(2));
        OffsetDateTime durationResult = source.AddDuration(Duration.FromDays(31L));
        OffsetDateTime periodResult = source.AddPeriod(Period.FromMonths(1L));

        Assert.That(durationResult.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-02T23:00:00"));
        Assert.That(periodResult.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-02-29T23:00:00"));
        Assert.That(durationResult.GetOffset().EqualsValue(source.GetOffset()), Is.True);
        Assert.That(periodResult.GetOffset().EqualsValue(source.GetOffset()), Is.True);
    }

    [Test]
    public void SubtractPeriod_PreservesOffsetAndUsesLocalCalendarArithmetic()
    {
        OffsetDateTime source = OffsetDateTime.Create(2024, 3, 1, 1, 30, 0, Offset.FromHours(2));
        Period period = Period.Create(0L, 1L, 0L, 0L, 2L, 0L, 0L, 0L, 0L, 0L);
        OffsetDateTime result = source.SubtractPeriod(period);

        Assert.That(result.GetOffset().EqualsValue(source.GetOffset()), Is.True);
        Assert.That(result.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-01-31T23:30:00"));
    }

    [Test]
    public void PeriodArithmetic_RequiresPeriod()
    {
        OffsetDateTime source = OffsetDateTime.Create(1970, 1, 1, 0, 0, 0, Offset.Zero());

        ArgumentNullException addException = Assert.Throws<ArgumentNullException>(() => source.AddPeriod(null!))!;
        ArgumentNullException subtractException = Assert.Throws<ArgumentNullException>(() => source.SubtractPeriod(null!))!;

        Assert.That(addException.ParamName, Is.EqualTo("period"));
        Assert.That(subtractException.ParamName, Is.EqualTo("period"));
    }

    [Test]
    public void CompareInstant_ComparesTimelineInstants()
    {
        OffsetDateTime utc = OffsetDateTime.Create(1970, 1, 1, 0, 0, 0, Offset.Zero());
        OffsetDateTime sameInstant = OffsetDateTime.Create(1970, 1, 1, 1, 0, 0, Offset.FromHours(1));
        OffsetDateTime laterInstant = OffsetDateTime.Create(1970, 1, 1, 1, 0, 0, Offset.Zero());

        Assert.That(utc.CompareInstant(sameInstant), Is.EqualTo(0));
        Assert.That(utc.CompareInstant(laterInstant), Is.EqualTo(-1));
        Assert.That(laterInstant.CompareInstant(utc), Is.EqualTo(1));
    }

    [Test]
    public void CompareLocal_ComparesLocalDateTimeOnly()
    {
        OffsetDateTime earlyLocal = OffsetDateTime.Create(1970, 1, 1, 0, 0, 0, Offset.FromHours(-1));
        OffsetDateTime sameLocal = OffsetDateTime.Create(1970, 1, 1, 0, 0, 0, Offset.FromHours(1));
        OffsetDateTime laterLocal = OffsetDateTime.Create(1970, 1, 1, 1, 0, 0, Offset.Zero());

        Assert.That(earlyLocal.CompareLocal(sameLocal), Is.EqualTo(0));
        Assert.That(earlyLocal.CompareLocal(laterLocal), Is.EqualTo(-1));
        Assert.That(laterLocal.CompareLocal(earlyLocal), Is.EqualTo(1));
    }

    [Test]
    public void ComparisonModes_CanOrderTheSameValuesDifferently()
    {
        OffsetDateTime localLaterInstantEarlier = OffsetDateTime.Create(2024, 1, 1, 10, 0, 0, Offset.FromHours(2));
        OffsetDateTime localEarlierInstantLater = OffsetDateTime.Create(2024, 1, 1, 9, 0, 0, Offset.Zero());

        Assert.That(localLaterInstantEarlier.CompareLocal(localEarlierInstantLater), Is.EqualTo(1));
        Assert.That(localLaterInstantEarlier.CompareInstant(localEarlierInstantLater), Is.EqualTo(-1));
    }

    [Test]
    public void Comparison_RequiresOtherOffsetDateTime()
    {
        OffsetDateTime source = OffsetDateTime.Create(1970, 1, 1, 0, 0, 0, Offset.Zero());

        ArgumentNullException instantException = Assert.Throws<ArgumentNullException>(() => source.CompareInstant(null!))!;
        ArgumentNullException localException = Assert.Throws<ArgumentNullException>(() => source.CompareLocal(null!))!;

        Assert.That(instantException.ParamName, Is.EqualTo("other"));
        Assert.That(localException.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void EqualsValue_ComparesLocalDateTimeAndOffsetAndHandlesNull()
    {
        OffsetDateTime source = OffsetDateTime.Create(1970, 1, 1, 0, 0, 0, Offset.Zero());
        OffsetDateTime same = OffsetDateTime.Create(1970, 1, 1, 0, 0, 0, Offset.Zero());
        OffsetDateTime sameInstantDifferentLocal = OffsetDateTime.Create(1970, 1, 1, 1, 0, 0, Offset.FromHours(1));

        Assert.That(source.EqualsValue(same), Is.True);
        Assert.That(source.EqualsValue(sameInstantDifferentLocal), Is.False);
        Assert.That(source.EqualsValue(null), Is.False);
    }

    [TestCase("1970-01-01T00:00:00Z", "1970-01-01T00:00:00", 0)]
    [TestCase("1970-01-01T01:30:00+01:30", "1970-01-01T01:30:00", 5400)]
    [TestCase("1969-12-31T18:30:00-05:30", "1969-12-31T18:30:00", -19800)]
    [TestCase("1970-01-01T00:00:00.1234567+00:00:01", "1970-01-01T00:00:00.1234567", 1)]
    public void Parse_ReadsIsoOffsetDateTimeText(string text, string expectedLocalText, int expectedOffsetSeconds)
    {
        OffsetDateTime offsetDateTime = OffsetDateTime.Parse(text);

        Assert.That(offsetDateTime.GetLocalDateTime().ToIsoString(), Is.EqualTo(expectedLocalText));
        Assert.That(offsetDateTime.GetOffset().GetTotalSeconds(), Is.EqualTo(expectedOffsetSeconds));
    }

    [Test]
    public void Parse_RequiresNonNullText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => OffsetDateTime.Parse(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("text"));
    }

    [TestCase("")]
    [TestCase("1970-01-01T00:00:00")]
    [TestCase("1970-01-01t00:00:00Z")]
    [TestCase("1970-01-01T00:00Z")]
    [TestCase("1970-01-01T00:00:00z")]
    [TestCase("1970-01-01T00:00:00+0100")]
    [TestCase("1970-01-01T00:00:00+18:00:01")]
    public void Parse_RejectsMalformedOffsetDateTimeText(string text)
    {
        Assert.Throws<FormatException>(() => OffsetDateTime.Parse(text));
    }

    [TestCase(1970, 1, 1, 0, 0, 0, 0, "1970-01-01T00:00:00Z")]
    [TestCase(1970, 1, 1, 1, 30, 0, 5400, "1970-01-01T01:30:00+01:30")]
    [TestCase(1969, 12, 31, 18, 30, 0, -19800, "1969-12-31T18:30:00-05:30")]
    [TestCase(1970, 1, 1, 0, 0, 1, 1, "1970-01-01T00:00:01+00:00:01")]
    public void ToIsoString_FormatsLocalDateTimeAndOffset(int year, int month, int day, int hour, int minute, int second, int offsetSeconds, string expected)
    {
        OffsetDateTime offsetDateTime = OffsetDateTime.Create(year, month, day, hour, minute, second, Offset.FromSeconds(offsetSeconds));

        Assert.That(offsetDateTime.ToIsoString(), Is.EqualTo(expected));
    }

    [TestCase("1970-01-01T00:00:00Z")]
    [TestCase("1970-01-01T01:30:00+01:30")]
    [TestCase("1970-01-01T00:00:00.1234567+00:00:01")]
    public void Parse_RoundTripsFormattedOffsetDateTimeText(string text)
    {
        OffsetDateTime original = OffsetDateTime.Parse(text);
        OffsetDateTime parsed = OffsetDateTime.Parse(original.ToIsoString());

        Assert.That(parsed.EqualsValue(original), Is.True);
    }

    [TestCase("1970-01-01T00:00:00+00:00")]
    [TestCase("1970-01-01T00:00:00-00:00")]
    [TestCase("1970-01-01T00:00:00+00:00:00")]
    public void ToIsoString_CanonicalizesZeroOffsetToZ(string text)
    {
        Assert.That(OffsetDateTime.Parse(text).ToIsoString(), Is.EqualTo("1970-01-01T00:00:00Z"));
    }

    [Test]
    public void ToDateTimeOffset_ReturnsEquivalentDateTimeOffsetForMinuteOffset()
    {
        OffsetDateTime offsetDateTime = OffsetDateTime.Create(2024, 2, 29, 23, 45, 1, 234, 567, 8, Offset.FromMinutes(330));
        DateTimeOffset value = offsetDateTime.ToDateTimeOffset();

        Assert.That(value.DateTime, Is.EqualTo(offsetDateTime.GetLocalDateTime().ToDateTimeUnspecified()));
        Assert.That(value.Offset, Is.EqualTo(offsetDateTime.GetOffset().ToTimeSpan()));
    }

    [Test]
    public void DateTimeOffsetInterop_RoundTripsRepresentativeValues()
    {
        DateTimeOffset[] values = new DateTimeOffset[]
        {
            DateTimeOffset.MinValue,
            new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 2, 29, 23, 45, 1, TimeSpan.FromHours(5)).AddTicks(2345678L),
            new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.Zero).AddTicks(9999999L)
        };

        foreach (DateTimeOffset value in values)
        {
            OffsetDateTime offsetDateTime = OffsetDateTime.FromDateTimeOffset(value);
            DateTimeOffset roundTrip = offsetDateTime.ToDateTimeOffset();

            Assert.That(roundTrip, Is.EqualTo(value));
        }
    }

    [Test]
    public void ToDateTimeOffset_RejectsSecondPrecisionOffsets()
    {
        OffsetDateTime offsetDateTime = OffsetDateTime.Create(2024, 1, 1, 0, 0, 0, Offset.FromSeconds(1));

        Assert.Throws<InvalidOperationException>(() => offsetDateTime.ToDateTimeOffset());
    }

    [Test]
    public void ToDateTimeOffset_RejectsOffsetsOutsideDateTimeOffsetRange()
    {
        OffsetDateTime positive = OffsetDateTime.Create(2024, 1, 1, 15, 0, 0, Offset.FromHours(15));
        OffsetDateTime negative = OffsetDateTime.Create(2024, 1, 1, 0, 0, 0, Offset.FromHours(-15));

        Assert.Throws<InvalidOperationException>(() => positive.ToDateTimeOffset());
        Assert.Throws<InvalidOperationException>(() => negative.ToDateTimeOffset());
    }

    [Test]
    public void ToDateTimeOffset_RejectsUtcResultsOutsideSupportedRange()
    {
        OffsetDateTime offsetDateTime = OffsetDateTime.Create(1, 1, 1, 0, 0, 0, Offset.FromMinutes(1));

        Assert.Throws<InvalidOperationException>(() => offsetDateTime.ToDateTimeOffset());
    }
}
