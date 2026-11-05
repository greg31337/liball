using System.Globalization;
using fake;

namespace fake.Tests;

[TestFixture]
public class DateSurfaceCompletionTests
{
    [Test]
    public void ConvenienceValuesAndQuartersAreBoundedAndDeterministic()
    {
        DateProvider first = Fake.Create(6301).Date();
        DateProvider second = Fake.Create(6301).Date();
        HashSet<int> quarters = new HashSet<int>();

        int index = 0;
        while (index < 512)
        {
            TimeOnly time = first.Time();
            TimeSpan duration = first.Duration();
            int quarter = first.Quarter();
            Assert.That(time, Is.EqualTo(second.Time()));
            Assert.That(duration, Is.EqualTo(second.Duration()));
            Assert.That(quarter, Is.EqualTo(second.Quarter()));
            Assert.That(time.Ticks, Is.InRange(TimeOnly.MinValue.Ticks, TimeOnly.MaxValue.Ticks));
            Assert.That(duration, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
            Assert.That(duration, Is.LessThan(TimeSpan.FromDays(7)));
            Assert.That(quarter, Is.InRange(1, 4));
            quarters.Add(quarter);
            index++;
        }

        Assert.That(quarters, Is.EquivalentTo(new int[] { 1, 2, 3, 4 }));
        Assert.Multiple(() =>
        {
            Assert.That(first.QuarterFromDate(new DateOnly(2024, 1, 31)), Is.EqualTo(1));
            Assert.That(first.QuarterFromDate(new DateOnly(2024, 4, 1)), Is.EqualTo(2));
            Assert.That(first.QuarterFromDate(new DateOnly(2024, 9, 30)), Is.EqualTo(3));
            Assert.That(first.QuarterFromDate(new DateOnly(2024, 12, 31)), Is.EqualTo(4));
        });
    }

    [Test]
    public void RequestedPrecisionGenerationAlignsTicksInsideBothBoundaryKinds()
    {
        DateProvider date = Fake.Create(6302).Date();
        TemporalPrecision[] precisions = new TemporalPrecision[]
        {
            TemporalPrecision.Seconds,
            TemporalPrecision.Milliseconds,
            TemporalPrecision.Microseconds,
            TemporalPrecision.Ticks
        };
        long[] units = new long[]
        {
            TimeSpan.TicksPerSecond,
            TimeSpan.TicksPerMillisecond,
            10L,
            1L
        };
        DateTimeOffset origin = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        int precisionIndex = 0;
        while (precisionIndex < precisions.Length)
        {
            DateTimeOffset full = date.AnytimeFromPrecision(precisions[precisionIndex]);
            Assert.That(full.UtcTicks % units[precisionIndex], Is.Zero);

            int sample = 0;
            while (sample < 128)
            {
                DateTimeOffset value = date.BetweenFromMinimumAndMaximumAndPrecision(
                    origin.AddTicks(1L),
                    origin.AddSeconds(5),
                    precisions[precisionIndex]);
                Assert.That(value.UtcTicks, Is.InRange(origin.UtcTicks + 1L, origin.AddSeconds(5).UtcTicks - 1L));
                Assert.That(value.UtcTicks % units[precisionIndex], Is.Zero);
                sample++;
            }
            precisionIndex++;
        }

        DateTimeOffset inclusive = date.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
            origin.AddSeconds(1),
            origin.AddSeconds(1),
            DateBoundary.Inclusive,
            TemporalPrecision.Seconds);
        Assert.That(inclusive, Is.EqualTo(origin.AddSeconds(1)));
        Assert.That(
            () => date.BetweenFromMinimumAndMaximumAndPrecision(
                origin.AddTicks(1L),
                origin.AddTicks(TimeSpan.TicksPerSecond),
                TemporalPrecision.Seconds),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void LocaleTextUsesTheConfiguredCultureAndCapturedReference()
    {
        DateTimeOffset reference = new DateTimeOffset(2024, 12, 31, 13, 5, 6, TimeSpan.FromHours(5.5));
        DateOnly calendarDate = new DateOnly(2024, 12, 31);
        TimeOnly clockTime = new TimeOnly(13, 5, 6);
        FakeOptions frenchOptions = FakeOptions.Create("fr-FR", 6303);
        frenchOptions.ReferenceTime = reference;
        DateProvider french = Fake.Create(frenchOptions).Date();
        frenchOptions.ReferenceTime = reference.AddYears(10);
        CultureInfo culture = CultureInfo.GetCultureInfo("fr-FR");

        Assert.Multiple(() =>
        {
            Assert.That(french.LocaleDateFromValue(calendarDate), Is.EqualTo(calendarDate.ToString("d", culture)));
            Assert.That(french.LocaleTimeFromValue(clockTime), Is.EqualTo(clockTime.ToString("T", culture)));
            Assert.That(french.LocaleDateTimeFromValue(reference), Is.EqualTo(reference.DateTime.ToString("G", culture) + " +05:30"));
            Assert.That(french.LocaleDate(), Is.EqualTo(calendarDate.ToString("d", culture)));
            Assert.That(french.LocaleTime(), Is.EqualTo(clockTime.ToString("T", culture)));
            Assert.That(french.LocaleDateTime(), Is.EqualTo(reference.DateTime.ToString("G", culture) + " +05:30"));
        });
    }

    [Test]
    public void RelativePhrasesUseCapturedInstantsAndLocaleLanguages()
    {
        DateTimeOffset reference = new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero);
        FakeOptions englishOptions = FakeOptions.Create("en-US", 6304);
        englishOptions.ReferenceTime = reference;
        DateProvider english = Fake.Create(englishOptions).Date();
        FakeOptions frenchOptions = FakeOptions.Create("fr-FR", 6304);
        frenchOptions.ReferenceTime = reference;
        DateProvider french = Fake.Create(frenchOptions).Date();
        FakeOptions japaneseOptions = FakeOptions.Create("ja-JP", 6304);
        japaneseOptions.ReferenceTime = reference;
        DateProvider japanese = Fake.Create(japaneseOptions).Date();

        Assert.Multiple(() =>
        {
            Assert.That(english.RelativePhraseFromValue(reference), Is.EqualTo("now"));
            Assert.That(english.RelativePhraseFromValue(reference.AddSeconds(-90)), Is.EqualTo("2 minutes ago"));
            Assert.That(english.RelativePhraseFromValue(reference.AddHours(3)), Is.EqualTo("in 3 hours"));
            Assert.That(french.RelativePhraseFromValue(reference.AddDays(14)), Is.EqualTo("dans 2 semaines"));
            Assert.That(japanese.RelativePhraseFromValue(reference.AddMinutes(-30)), Is.EqualTo("30分前"));
        });
    }

    [Test]
    public void DstTransitionsExposeExactGapAndOverlapInstants()
    {
        DateProvider date = Fake.Create(6305).Date();
        DaylightSavingTransition gap = date.DaylightSavingGapFromIanaIdAndYear(
            "America/New_York",
            2024);
        DaylightSavingTransition overlap = date.DaylightSavingOverlapFromIanaIdAndYear(
            "America/New_York",
            2024);

        Assert.Multiple(() =>
        {
            Assert.That(gap.Kind, Is.EqualTo(DaylightSavingTransitionKind.Gap));
            Assert.That(gap.LocalStart, Is.EqualTo(new DateTime(2024, 3, 10, 2, 0, 0, DateTimeKind.Unspecified)));
            Assert.That(gap.LocalEnd, Is.EqualTo(new DateTime(2024, 3, 10, 3, 0, 0, DateTimeKind.Unspecified)));
            Assert.That(gap.OffsetBefore, Is.EqualTo(TimeSpan.FromHours(-5)));
            Assert.That(gap.OffsetAfter, Is.EqualTo(TimeSpan.FromHours(-4)));
            Assert.That(gap.After.UtcTicks - gap.Before.UtcTicks, Is.EqualTo(1L));
            Assert.That(overlap.Kind, Is.EqualTo(DaylightSavingTransitionKind.Overlap));
            Assert.That(overlap.LocalStart, Is.EqualTo(new DateTime(2024, 11, 3, 1, 0, 0, DateTimeKind.Unspecified)));
            Assert.That(overlap.LocalEnd, Is.EqualTo(new DateTime(2024, 11, 3, 2, 0, 0, DateTimeKind.Unspecified)));
            Assert.That(overlap.OffsetBefore, Is.EqualTo(TimeSpan.FromHours(-4)));
            Assert.That(overlap.OffsetAfter, Is.EqualTo(TimeSpan.FromHours(-5)));
            Assert.That(overlap.EarlierOccurrence.DateTime, Is.EqualTo(overlap.LaterOccurrence.DateTime));
            Assert.That(overlap.LaterOccurrence.UtcTicks - overlap.EarlierOccurrence.UtcTicks, Is.EqualTo(TimeSpan.TicksPerHour));
            Assert.That(overlap.After.UtcTicks - overlap.Before.UtcTicks, Is.EqualTo(1L));
        });
    }

    [Test]
    public void OffsetBoundaryValuesAreRepresentableAndReplayable()
    {
        DateProvider first = Fake.Create(6306).Date();
        DateProvider second = Fake.Create(6306).Date();

        DateTimeOffset maximum = first.MaximumOffsetValue();
        DateTimeOffset minimum = first.MinimumOffsetValue();

        Assert.Multiple(() =>
        {
            Assert.That(maximum, Is.EqualTo(second.MaximumOffsetValue()));
            Assert.That(minimum, Is.EqualTo(second.MinimumOffsetValue()));
            Assert.That(maximum.Offset, Is.EqualTo(TimeSpan.FromHours(14)));
            Assert.That(minimum.Offset, Is.EqualTo(TimeSpan.FromHours(-14)));
            Assert.That(maximum.UtcTicks, Is.InRange(DateTimeOffset.MinValue.UtcTicks, DateTimeOffset.MaxValue.UtcTicks));
            Assert.That(minimum.UtcTicks, Is.InRange(DateTimeOffset.MinValue.UtcTicks, DateTimeOffset.MaxValue.UtcTicks));
        });
    }

    [Test]
    public void InvalidTransitionAndOutputRequestsFailExplicitly()
    {
        DateProvider date = Fake.Create(6307).Date();
        Assert.Multiple(() =>
        {
            Assert.That(
                () => date.DaylightSavingGapFromIanaIdAndYear("Africa/Abidjan", 2024),
                Throws.TypeOf<FakeException>());
            Assert.That(
                () => date.DaylightSavingGapFromIanaIdAndYear("America/New_York", 9999),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });

        FakeOptions limitedOptions = FakeOptions.Create(6308);
        limitedOptions.MaximumOutputSize = 3;
        DateProvider limited = Fake.Create(limitedOptions).Date();
        Assert.That(
            () => limited.RelativePhraseFromValue(limitedOptions.ReferenceTime.AddHours(2)),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
