using fake;

namespace fake.Tests;

[TestFixture]
public class DatePresentationTests
{
    [Test]
    public void DefaultEnglishCalendarNamesMapEveryMonthAndWeekday()
    {
        DateProvider date = Fake.Create(6001).Date();
        string[] months = new string[]
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };
        string[] abbreviatedMonths = new string[]
        {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        };
        string[] weekdays = new string[]
        {
            "Sunday", "Monday", "Tuesday", "Wednesday",
            "Thursday", "Friday", "Saturday"
        };
        string[] abbreviatedWeekdays = new string[]
        {
            "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"
        };

        int index = 0;
        while (index < months.Length)
        {
            Assert.That(date.MonthNameFromMonth(index + 1), Is.EqualTo(months[index]));
            Assert.That(date.MonthNameFromMonthAndStyle(index + 1, DateNameStyle.Abbreviated), Is.EqualTo(abbreviatedMonths[index]));
            index++;
        }
        index = 0;
        while (index < weekdays.Length)
        {
            Assert.That(date.WeekdayNameFromWeekday((DayOfWeek)index), Is.EqualTo(weekdays[index]));
            Assert.That(date.WeekdayNameFromWeekdayAndStyle((DayOfWeek)index, DateNameStyle.Abbreviated), Is.EqualTo(abbreviatedWeekdays[index]));
            index++;
        }
        Assert.That(date.DateOrder(), Is.EqualTo(DateOrder.MonthDayYear));
    }

    [Test]
    public void CalendarNamesAndOrderUseRegisteredLocaleFallbackData()
    {
        FakeOptions options = FakeOptions.Create("fr-BE", 6002);
        Fake value = Fake.Create(options);
        value.Locale().RegisterFromLocaleAndFields(
            "fr",
            new Dictionary<string, object>
            {
                ["date.month.wide"] = new string[]
                {
                    "janvier", "février", "mars", "avril", "mai", "juin",
                    "juillet", "août", "septembre", "octobre", "novembre", "décembre"
                },
                ["date.month.abbreviated"] = new string[]
                {
                    "janv.", "févr.", "mars", "avr.", "mai", "juin",
                    "juill.", "août", "sept.", "oct.", "nov.", "déc."
                },
                ["date.weekday.wide"] = new string[]
                {
                    "dimanche", "lundi", "mardi", "mercredi",
                    "jeudi", "vendredi", "samedi"
                },
                ["date.weekday.abbreviated"] = new string[]
                {
                    "dim.", "lun.", "mar.", "mer.", "jeu.", "ven.", "sam."
                },
                ["date.order"] = "dmy"
            });
        DateProvider date = value.Date();

        Assert.Multiple(() =>
        {
            Assert.That(date.MonthNameFromMonth(8), Is.EqualTo("août"));
            Assert.That(date.MonthNameFromMonthAndStyle(2, DateNameStyle.Abbreviated), Is.EqualTo("févr."));
            Assert.That(date.WeekdayNameFromWeekday(DayOfWeek.Monday), Is.EqualTo("lundi"));
            Assert.That(date.WeekdayNameFromWeekdayAndStyle(DayOfWeek.Saturday, DateNameStyle.Abbreviated), Is.EqualTo("sam."));
            Assert.That(date.DateOrder(), Is.EqualTo(DateOrder.DayMonthYear));
        });
    }

    [Test]
    public void RandomCalendarNamesAreCompleteAndDeterministic()
    {
        DateProvider first = Fake.Create(6003).Date();
        DateProvider second = Fake.Create(6003).Date();
        HashSet<string> months = new HashSet<string>();
        HashSet<string> weekdays = new HashSet<string>();

        int index = 0;
        while (index < 1024)
        {
            string firstMonth = first.MonthNameFromStyle(DateNameStyle.Abbreviated);
            string firstWeekday = first.WeekdayName();
            Assert.That(firstMonth, Is.EqualTo(second.MonthNameFromStyle(DateNameStyle.Abbreviated)));
            Assert.That(firstWeekday, Is.EqualTo(second.WeekdayName()));
            months.Add(firstMonth);
            weekdays.Add(firstWeekday);
            index++;
        }

        Assert.That(months.Count, Is.EqualTo(12));
        Assert.That(weekdays.Count, Is.EqualTo(7));
    }

    [Test]
    public void TruncationPreservesOffsetsAndDiscardsLowerPrecisionTicks()
    {
        DateProvider date = Fake.Create(6004).Date();
        DateTimeOffset value = new DateTimeOffset(
            2024,
            2,
            29,
            12,
            34,
            56,
            TimeSpan.FromHours(5.5)).AddTicks(1234567L);

        Assert.Multiple(() =>
        {
            Assert.That(date.TruncateFromValueAndPrecision(value, TemporalPrecision.Seconds).Ticks % TimeSpan.TicksPerSecond, Is.Zero);
            Assert.That(date.TruncateFromValueAndPrecision(value, TemporalPrecision.Milliseconds).Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(1230000L));
            Assert.That(date.TruncateFromValueAndPrecision(value, TemporalPrecision.Microseconds).Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(1234560L));
            Assert.That(date.TruncateFromValueAndPrecision(value, TemporalPrecision.Ticks), Is.EqualTo(value));
            Assert.That(date.TruncateFromValueAndPrecision(value, TemporalPrecision.Milliseconds).Offset, Is.EqualTo(value.Offset));
        });
    }

    [Test]
    public void ReferenceTruncationUsesTheCapturedClockSnapshot()
    {
        FakeOptions options = FakeOptions.Create(6005);
        DateTimeOffset reference = new DateTimeOffset(2031, 2, 3, 4, 5, 6, TimeSpan.FromHours(-7)).AddTicks(9876543L);
        options.ReferenceTime = reference;
        DateProvider date = Fake.Create(options).Date();
        options.ReferenceTime = reference.AddYears(20);

        Assert.That(
            date.Truncate(TemporalPrecision.Microseconds),
            Is.EqualTo(new DateTimeOffset(reference.Ticks - reference.Ticks % 10L, reference.Offset)));
    }

    [Test]
    public void Rfc3339FormatsEveryPrecisionAndOffsetExactly()
    {
        DateProvider date = Fake.Create(6006).Date();
        DateTimeOffset offsetValue = new DateTimeOffset(
            1985,
            4,
            12,
            23,
            20,
            50,
            TimeSpan.FromHours(-7.5)).AddTicks(1234567L);
        DateTimeOffset utcValue = offsetValue.ToUniversalTime();

        Assert.Multiple(() =>
        {
            Assert.That(date.Rfc3339FromValue(offsetValue), Is.EqualTo("1985-04-12T23:20:50-07:30"));
            Assert.That(date.Rfc3339FromValueAndPrecision(offsetValue, TemporalPrecision.Milliseconds), Is.EqualTo("1985-04-12T23:20:50.123-07:30"));
            Assert.That(date.Rfc3339FromValueAndPrecision(offsetValue, TemporalPrecision.Microseconds), Is.EqualTo("1985-04-12T23:20:50.123456-07:30"));
            Assert.That(date.Rfc3339FromValueAndPrecision(offsetValue, TemporalPrecision.Ticks), Is.EqualTo("1985-04-12T23:20:50.1234567-07:30"));
            Assert.That(date.Rfc3339FromValueAndPrecision(utcValue, TemporalPrecision.Ticks), Is.EqualTo("1985-04-13T06:50:50.1234567Z"));
        });
    }

    [Test]
    public void ReferenceRfc3339OverloadsUseTheCapturedClock()
    {
        FakeOptions options = FakeOptions.Create(6007);
        options.ReferenceTime = new DateTimeOffset(2040, 6, 7, 8, 9, 10, TimeSpan.Zero).AddTicks(7654321L);
        DateProvider date = Fake.Create(options).Date();

        Assert.That(date.Rfc3339(), Is.EqualTo("2040-06-07T08:09:10Z"));
        Assert.That(date.Rfc3339FromPrecision(TemporalPrecision.Milliseconds), Is.EqualTo("2040-06-07T08:09:10.765Z"));
    }

    [Test]
    public void UnixConversionsHandleEpochOffsetsAndPreEpochFlooring()
    {
        DateProvider date = Fake.Create(6008).Date();
        DateTimeOffset epoch = DateTimeOffset.UnixEpoch;
        DateTimeOffset equivalentOffset = epoch.ToOffset(TimeSpan.FromHours(9));
        DateTimeOffset beforeEpoch = epoch.AddTicks(-1L);

        Assert.Multiple(() =>
        {
            Assert.That(date.UnixSecondsFromValue(epoch), Is.Zero);
            Assert.That(date.UnixMillisecondsFromValue(epoch), Is.Zero);
            Assert.That(date.UnixSecondsFromValue(equivalentOffset), Is.Zero);
            Assert.That(date.UnixMillisecondsFromValue(equivalentOffset), Is.Zero);
            Assert.That(date.UnixSecondsFromValue(beforeEpoch), Is.EqualTo(-1L));
            Assert.That(date.UnixMillisecondsFromValue(beforeEpoch), Is.EqualTo(-1L));
            Assert.That(date.FromUnixSeconds(0L), Is.EqualTo(epoch));
            Assert.That(date.FromUnixMilliseconds(0L), Is.EqualTo(epoch));
        });
    }

    [Test]
    public void UnixConversionsRoundTripAcrossTheRuntimeRangeAtTheirPrecision()
    {
        DateProvider date = Fake.Create(6009).Date();
        DateTimeOffset[] values = new DateTimeOffset[]
        {
            DateTimeOffset.MinValue,
            new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.FromHours(-5)).AddTicks(9999L),
            DateTimeOffset.UnixEpoch,
            new DateTimeOffset(2500, 12, 31, 23, 59, 59, TimeSpan.FromHours(12)).AddTicks(9876543L),
            DateTimeOffset.MaxValue
        };

        int index = 0;
        while (index < values.Length)
        {
            long seconds = date.UnixSecondsFromValue(values[index]);
            long milliseconds = date.UnixMillisecondsFromValue(values[index]);
            Assert.That(date.UnixSecondsFromValue(date.FromUnixSeconds(seconds)), Is.EqualTo(seconds));
            Assert.That(date.UnixMillisecondsFromValue(date.FromUnixMilliseconds(milliseconds)), Is.EqualTo(milliseconds));
            index++;
        }
    }

    [Test]
    public void ReferenceUnixConversionsUseTheCapturedInstant()
    {
        FakeOptions options = FakeOptions.Create(6010);
        DateTimeOffset reference = DateTimeOffset.UnixEpoch.AddMilliseconds(123456789L).ToOffset(TimeSpan.FromHours(-4));
        options.ReferenceTime = reference;
        DateProvider date = Fake.Create(options).Date();
        options.ReferenceTime = reference.AddYears(10);

        Assert.That(date.UnixSeconds(), Is.EqualTo(reference.ToUnixTimeSeconds()));
        Assert.That(date.UnixMilliseconds(), Is.EqualTo(reference.ToUnixTimeMilliseconds()));
    }

    [Test]
    public void InvalidPresentationInputsFailBeforeDateEntropy()
    {
        Fake actualFake = Fake.Create(6011);
        Fake expectedFake = Fake.Create(6011);
        DateProvider actual = actualFake.Date();
        DateProvider expected = expectedFake.Date();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.MonthNameFromMonth(0)).ParamName, Is.EqualTo("month"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.MonthNameFromStyle((DateNameStyle)99)).ParamName, Is.EqualTo("style"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WeekdayNameFromWeekday((DayOfWeek)99)).ParamName, Is.EqualTo("weekday"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Rfc3339FromPrecision((TemporalPrecision)99)).ParamName, Is.EqualTo("precision"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.FromUnixSeconds(long.MaxValue)).ParamName, Is.EqualTo("seconds"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.FromUnixMilliseconds(long.MaxValue)).ParamName, Is.EqualTo("milliseconds"));
        });

        Assert.That(actual.Anytime(), Is.EqualTo(expected.Anytime()));
    }

    [Test]
    public void MalformedRegisteredCalendarDataFailsBeforeDateEntropy()
    {
        FakeOptions options = FakeOptions.Create("en-US", 6012);
        Fake actualFake = Fake.Create(options);
        Fake expectedFake = Fake.Create(options);
        actualFake.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object>
            {
                ["date.month.wide"] = new string[] { "only one" },
                ["date.order"] = "invalid"
            });

        Assert.That(() => actualFake.Date().MonthName(), Throws.TypeOf<FakeException>());
        Assert.That(() => actualFake.Date().DateOrder(), Throws.TypeOf<FakeException>());
        Assert.That(actualFake.Date().Anytime(), Is.EqualTo(expectedFake.Date().Anytime()));
    }

    [Test]
    public void PresentationOperationsDoNotPerturbOtherProviderStreams()
    {
        Fake actual = Fake.Create(6013);
        Fake expected = Fake.Create(6013);

        actual.Date().MonthName();
        actual.Date().WeekdayName();
        actual.Date().Rfc3339();
        actual.Date().UnixMilliseconds();

        Assert.That(actual.Identifier().Uuid4(), Is.EqualTo(expected.Identifier().Uuid4()));
    }
}
