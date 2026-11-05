using System.Globalization;
using fake;

namespace fake.Tests;

[TestFixture]
public class DateSurfaceComprehensiveTests
{
    [Test]
    public void PrecisionGenerationCoversEveryPrecisionAndBoundaryDeterministically()
    {
        DateProvider first = Fake.Create(6401).Date();
        DateProvider second = Fake.Create(6401).Date();
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
        DateTimeOffset origin = DateTimeOffset.UnixEpoch;

        int precisionIndex = 0;
        while (precisionIndex < precisions.Length)
        {
            TemporalPrecision precision = precisions[precisionIndex];
            long unit = units[precisionIndex];
            DateTimeOffset minimum = origin.AddTicks(1L);
            DateTimeOffset maximum = origin.AddTicks(unit * 5L);
            DateTimeOffset singleton = origin.AddTicks(unit * 8L);

            DateTimeOffset firstAnytime = first.AnytimeFromPrecision(precision);
            DateTimeOffset secondAnytime = second.AnytimeFromPrecision(precision);
            Assert.That(firstAnytime, Is.EqualTo(secondAnytime));
            Assert.That(firstAnytime.UtcTicks % unit, Is.Zero);

            int sample = 0;
            while (sample < 64)
            {
                DateTimeOffset shortcut = first.BetweenFromMinimumAndMaximumAndPrecision(
                    minimum,
                    maximum,
                    precision);
                DateTimeOffset shortcutReplay = second.BetweenFromMinimumAndMaximumAndPrecision(
                    minimum,
                    maximum,
                    precision);
                DateTimeOffset exclusive = first.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
                    minimum,
                    maximum,
                    DateBoundary.Exclusive,
                    precision);
                DateTimeOffset exclusiveReplay = second.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
                    minimum,
                    maximum,
                    DateBoundary.Exclusive,
                    precision);
                DateTimeOffset inclusive = first.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
                    minimum,
                    maximum,
                    DateBoundary.Inclusive,
                    precision);
                DateTimeOffset inclusiveReplay = second.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
                    minimum,
                    maximum,
                    DateBoundary.Inclusive,
                    precision);

                Assert.That(shortcut, Is.EqualTo(shortcutReplay));
                Assert.That(exclusive, Is.EqualTo(exclusiveReplay));
                Assert.That(inclusive, Is.EqualTo(inclusiveReplay));
                Assert.That(shortcut.UtcTicks, Is.InRange(minimum.UtcTicks, maximum.UtcTicks - 1L));
                Assert.That(exclusive.UtcTicks, Is.InRange(minimum.UtcTicks, maximum.UtcTicks - 1L));
                Assert.That(inclusive.UtcTicks, Is.InRange(minimum.UtcTicks, maximum.UtcTicks));
                Assert.That(shortcut.UtcTicks % unit, Is.Zero);
                Assert.That(exclusive.UtcTicks % unit, Is.Zero);
                Assert.That(inclusive.UtcTicks % unit, Is.Zero);
                sample++;
            }

            Assert.That(
                first.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
                    singleton,
                    singleton,
                    DateBoundary.Inclusive,
                    precision),
                Is.EqualTo(singleton));
            Assert.That(
                second.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
                    singleton,
                    singleton,
                    DateBoundary.Inclusive,
                    precision),
                Is.EqualTo(singleton));
            precisionIndex++;
        }
    }

    [Test]
    public void InvalidPrecisionRangesAndEnumsFailBeforeConsumingEntropy()
    {
        DateProvider actual = Fake.Create(6402).Date();
        DateProvider expected = Fake.Create(6402).Date();
        DateTimeOffset origin = DateTimeOffset.UnixEpoch;

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    actual.BetweenFromMinimumAndMaximumAndPrecision(
                        origin,
                        origin,
                        TemporalPrecision.Seconds)).ParamName,
                Is.EqualTo("maximum"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    actual.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
                        origin.AddSeconds(2),
                        origin.AddSeconds(1),
                        DateBoundary.Inclusive,
                        TemporalPrecision.Seconds)).ParamName,
                Is.EqualTo("maximum"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    actual.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
                        origin,
                        origin.AddSeconds(1),
                        (DateBoundary)99,
                        TemporalPrecision.Seconds)).ParamName,
                Is.EqualTo("boundary"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    actual.BetweenFromMinimumAndMaximumAndPrecision(
                        origin,
                        origin.AddSeconds(1),
                        (TemporalPrecision)99)).ParamName,
                Is.EqualTo("precision"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    actual.BetweenFromMinimumAndMaximumAndPrecision(
                        origin.AddTicks(1L),
                        origin.AddSeconds(1),
                        TemporalPrecision.Seconds)).ParamName,
                Is.EqualTo("maximum"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    actual.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
                        origin.AddTicks(1L),
                        origin.AddTicks(1L),
                        DateBoundary.Inclusive,
                        TemporalPrecision.Seconds)).ParamName,
                Is.EqualTo("maximum"));
        });

        Assert.That(
            actual.AnytimeFromPrecision(TemporalPrecision.Microseconds),
            Is.EqualTo(expected.AnytimeFromPrecision(TemporalPrecision.Microseconds)));
    }

    [Test]
    public void LeapDaysEndOfMonthAndFiscalTransitionsMapToCorrectQuarters()
    {
        FakeOptions options = FakeOptions.Create("en-CA", 6403);
        options.ReferenceTime = new DateTimeOffset(2024, 2, 29, 23, 59, 58, TimeSpan.FromHours(-5));
        DateProvider date = Fake.Create(options).Date();
        FiscalPeriod fiscal = date.FiscalPeriodFromFiscalYearAndQuarterAndFiscalYearStartMonth(
            2023,
            4,
            4);
        CultureInfo culture = CultureInfo.GetCultureInfo("en-CA");

        Assert.Multiple(() =>
        {
            Assert.That(date.QuarterFromDate(new DateOnly(2024, 2, 29)), Is.EqualTo(1));
            Assert.That(date.QuarterFromDate(new DateOnly(2024, 3, 31)), Is.EqualTo(1));
            Assert.That(date.QuarterFromDate(new DateOnly(2024, 4, 1)), Is.EqualTo(2));
            Assert.That(date.QuarterFromDate(new DateOnly(2024, 6, 30)), Is.EqualTo(2));
            Assert.That(date.QuarterFromDate(new DateOnly(2024, 7, 1)), Is.EqualTo(3));
            Assert.That(date.QuarterFromDate(new DateOnly(2024, 9, 30)), Is.EqualTo(3));
            Assert.That(date.QuarterFromDate(new DateOnly(2024, 10, 1)), Is.EqualTo(4));
            Assert.That(date.QuarterFromDate(new DateOnly(2024, 12, 31)), Is.EqualTo(4));
            Assert.That(date.LocaleDate(), Is.EqualTo(new DateOnly(2024, 2, 29).ToString("d", culture)));
            Assert.That(fiscal.Quarter, Is.EqualTo(4));
            Assert.That(fiscal.Start, Is.EqualTo(new DateOnly(2024, 1, 1)));
            Assert.That(fiscal.End, Is.EqualTo(new DateOnly(2024, 3, 31)));
        });
    }

    [Test]
    public void EveryBundledLocaleFormatsDateTimeWithItsRuntimeCulture()
    {
        string[] locales = new string[]
        {
            "ar-EG", "ar-SA", "cs-CZ", "da-DK", "de-AT", "de-CH", "de-DE",
            "el-GR", "en-AU", "en-CA", "en-GB", "en-US", "es-ES", "es-MX",
            "fa-IR", "fi-FI", "fr-CA", "fr-FR", "he-IL", "hi-IN", "id-ID",
            "it-IT", "ja-JP", "ko-KR", "nb-NO", "nl-NL", "pl-PL", "pt-BR",
            "pt-PT", "ru-RU", "sv-SE", "th-TH", "tr-TR", "uk-UA", "vi-VN",
            "zh-CN", "zh-HK", "zh-TW"
        };
        DateTimeOffset value = new DateTimeOffset(
            2024,
            8,
            19,
            16,
            7,
            8,
            TimeSpan.FromMinutes(330));
        DateOnly dateValue = DateOnly.FromDateTime(value.DateTime);
        TimeOnly timeValue = TimeOnly.FromDateTime(value.DateTime);

        int index = 0;
        while (index < locales.Length)
        {
            string locale = locales[index];
            FakeOptions options = FakeOptions.Create(locale, 6404);
            options.ReferenceTime = value;
            DateProvider date = Fake.Create(options).Date();
            CultureInfo culture = CultureInfo.GetCultureInfo(locale);

            Assert.That(date.LocaleDateFromValue(dateValue), Is.EqualTo(dateValue.ToString("d", culture)), locale);
            Assert.That(date.LocaleTimeFromValue(timeValue), Is.EqualTo(timeValue.ToString("T", culture)), locale);
            Assert.That(
                date.LocaleDateTimeFromValue(value),
                Is.EqualTo(value.DateTime.ToString("G", culture) + " +05:30"),
                locale);
            Assert.That(date.LocaleDate(), Is.EqualTo(date.LocaleDateFromValue(dateValue)), locale);
            Assert.That(date.LocaleTime(), Is.EqualTo(date.LocaleTimeFromValue(timeValue)), locale);
            Assert.That(date.LocaleDateTime(), Is.EqualTo(date.LocaleDateTimeFromValue(value)), locale);
            index++;
        }

    }

    [Test]
    public void EveryBundledLocaleLanguageHasAnExplicitNowPhrase()
    {
        string[] locales = new string[]
        {
            "ar-EG", "ar-SA", "cs-CZ", "da-DK", "de-AT", "de-CH", "de-DE",
            "el-GR", "en-AU", "en-CA", "en-GB", "en-US", "es-ES", "es-MX",
            "fa-IR", "fi-FI", "fr-CA", "fr-FR", "he-IL", "hi-IN", "id-ID",
            "it-IT", "ja-JP", "ko-KR", "nb-NO", "nl-NL", "pl-PL", "pt-BR",
            "pt-PT", "ru-RU", "sv-SE", "th-TH", "tr-TR", "uk-UA", "vi-VN",
            "zh-CN", "zh-HK", "zh-TW"
        };
        string[] expected = new string[]
        {
            "الآن", "الآن", "nyní", "nu", "jetzt", "jetzt", "jetzt",
            "τώρα", "now", "now", "now", "now", "ahora", "ahora",
            "اکنون", "nyt", "maintenant", "maintenant", "עכשיו", "अभी", "sekarang",
            "adesso", "今", "지금", "nå", "nu", "teraz", "agora",
            "agora", "сейчас", "nu", "ตอนนี้", "şimdi", "зараз", "bây giờ",
            "现在", "现在", "现在"
        };
        DateTimeOffset reference = new DateTimeOffset(2032, 6, 7, 8, 9, 10, TimeSpan.Zero);

        int index = 0;
        while (index < locales.Length)
        {
            FakeOptions options = FakeOptions.Create(locales[index], 6405);
            options.ReferenceTime = reference;
            DateProvider date = Fake.Create(options).Date();
            Assert.That(date.RelativePhraseFromValue(reference), Is.EqualTo(expected[index]), locales[index]);
            index++;
        }
    }

    [Test]
    public void RelativePhraseThresholdsCoverEveryUnitInBothDirections()
    {
        FakeOptions options = FakeOptions.Create("en-US", 6406);
        DateTimeOffset reference = new DateTimeOffset(2032, 6, 7, 8, 9, 10, TimeSpan.Zero);
        options.ReferenceTime = reference;
        DateProvider date = Fake.Create(options).Date();

        Assert.Multiple(() =>
        {
            Assert.That(date.RelativePhraseFromValue(reference.AddSeconds(59)), Is.EqualTo("in 59 seconds"));
            Assert.That(date.RelativePhraseFromValue(reference.AddMinutes(1)), Is.EqualTo("in 1 minute"));
            Assert.That(date.RelativePhraseFromValue(reference.AddMinutes(-90)), Is.EqualTo("2 hours ago"));
            Assert.That(date.RelativePhraseFromValue(reference.AddHours(23)), Is.EqualTo("in 23 hours"));
            Assert.That(date.RelativePhraseFromValue(reference.AddDays(-1)), Is.EqualTo("1 day ago"));
            Assert.That(date.RelativePhraseFromValue(reference.AddDays(7)), Is.EqualTo("in 1 week"));
            Assert.That(date.RelativePhraseFromValue(reference.AddDays(-30)), Is.EqualTo("1 month ago"));
            Assert.That(date.RelativePhraseFromValue(reference.AddDays(365)), Is.EqualTo("in 1 year"));
            Assert.That(
                date.RelativePhraseFromValue(reference.ToOffset(TimeSpan.FromHours(12))),
                Is.EqualTo("now"));
        });
    }

    [Test]
    public void ReferenceFormattingAndPhrasesUseTheCapturedClockSnapshot()
    {
        FakeOptions options = FakeOptions.Create("de-DE", 6407);
        DateTimeOffset captured = new DateTimeOffset(2024, 2, 29, 23, 59, 58, TimeSpan.FromHours(1));
        options.ReferenceTime = captured;
        DateProvider date = Fake.Create(options).Date();
        options.ReferenceTime = captured.AddYears(20);
        CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");

        Assert.Multiple(() =>
        {
            Assert.That(date.LocaleDate(), Is.EqualTo(DateOnly.FromDateTime(captured.DateTime).ToString("d", culture)));
            Assert.That(date.LocaleTime(), Is.EqualTo(TimeOnly.FromDateTime(captured.DateTime).ToString("T", culture)));
            Assert.That(date.LocaleDateTime(), Is.EqualTo(captured.DateTime.ToString("G", culture) + " +01:00"));
            Assert.That(date.RelativePhraseFromValue(captured), Is.EqualTo("jetzt"));
            Assert.That(date.RelativePhraseFromValue(captured.AddHours(2)), Is.EqualTo("in 2 Stunden"));
        });
    }

    [Test]
    public void LordHoweTransitionsExposeHalfHourGapAndOverlapBoundaries()
    {
        DateProvider date = Fake.Create(6408).Date();
        DaylightSavingTransition gap = date.DaylightSavingGapFromIanaIdAndYear(
            "Australia/Lord_Howe",
            2024);
        DaylightSavingTransition overlap = date.DaylightSavingOverlapFromIanaIdAndYear(
            "Australia/Lord_Howe",
            2024);

        Assert.Multiple(() =>
        {
            Assert.That(gap.IanaId, Is.EqualTo("Australia/Lord_Howe"));
            Assert.That(gap.Year, Is.EqualTo(2024));
            Assert.That(gap.LocalStart, Is.EqualTo(new DateTime(2024, 10, 6, 2, 0, 0, DateTimeKind.Unspecified)));
            Assert.That(gap.LocalEnd, Is.EqualTo(new DateTime(2024, 10, 6, 2, 30, 0, DateTimeKind.Unspecified)));
            Assert.That(gap.LocalEnd - gap.LocalStart, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(gap.OffsetBefore, Is.EqualTo(TimeSpan.FromMinutes(630)));
            Assert.That(gap.OffsetAfter, Is.EqualTo(TimeSpan.FromHours(11)));
            Assert.That(gap.EarlierOccurrence, Is.EqualTo(gap.Before));
            Assert.That(gap.LaterOccurrence, Is.EqualTo(gap.After));
            Assert.That(gap.After.UtcTicks - gap.Before.UtcTicks, Is.EqualTo(1L));
            Assert.That(overlap.LocalStart, Is.EqualTo(new DateTime(2024, 4, 7, 1, 30, 0, DateTimeKind.Unspecified)));
            Assert.That(overlap.LocalEnd, Is.EqualTo(new DateTime(2024, 4, 7, 2, 0, 0, DateTimeKind.Unspecified)));
            Assert.That(overlap.LocalEnd - overlap.LocalStart, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(overlap.OffsetBefore, Is.EqualTo(TimeSpan.FromHours(11)));
            Assert.That(overlap.OffsetAfter, Is.EqualTo(TimeSpan.FromMinutes(630)));
            Assert.That(overlap.EarlierOccurrence.DateTime, Is.EqualTo(overlap.LaterOccurrence.DateTime));
            Assert.That(overlap.LaterOccurrence.UtcTicks - overlap.EarlierOccurrence.UtcTicks, Is.EqualTo(TimeSpan.FromMinutes(30).Ticks));
            Assert.That(overlap.After.UtcTicks - overlap.Before.UtcTicks, Is.EqualTo(1L));
        });
    }

    [Test]
    public void TransitionFailuresCoverAbsentRulesIdentifiersAndYearBounds()
    {
        DateProvider date = Fake.Create(6409).Date();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => date.DaylightSavingGapFromIanaIdAndYear("Africa/Abidjan", 2024),
                Throws.TypeOf<FakeException>());
            Assert.That(
                () => date.DaylightSavingOverlapFromIanaIdAndYear("Africa/Abidjan", 2024),
                Throws.TypeOf<FakeException>());
            Assert.That(
                Assert.Throws<ArgumentException>(() =>
                    date.DaylightSavingGapFromIanaIdAndYear("america/new_york", 2024)).ParamName,
                Is.EqualTo("ianaId"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    date.DaylightSavingOverlapFromIanaIdAndYear("America/New_York", 0)).ParamName,
                Is.EqualTo("year"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    date.DaylightSavingOverlapFromIanaIdAndYear("America/New_York", 9999)).ParamName,
                Is.EqualTo("year"));
        });
    }

    [Test]
    public void OffsetExtremeGeneratorsCoverBothBoundsAndReplayEveryValue()
    {
        DateProvider first = Fake.Create(6410).Date();
        DateProvider second = Fake.Create(6410).Date();

        int index = 0;
        while (index < 256)
        {
            DateTimeOffset positive = first.MaximumOffsetValue();
            DateTimeOffset negative = first.MinimumOffsetValue();
            Assert.That(positive, Is.EqualTo(second.MaximumOffsetValue()));
            Assert.That(negative, Is.EqualTo(second.MinimumOffsetValue()));
            Assert.That(positive.Offset, Is.EqualTo(TimeSpan.FromHours(14)));
            Assert.That(negative.Offset, Is.EqualTo(TimeSpan.FromHours(-14)));
            Assert.That(first.Rfc3339FromValue(positive), Does.EndWith("+14:00"));
            Assert.That(first.Rfc3339FromValue(negative), Does.EndWith("-14:00"));
            Assert.That(positive.UtcTicks, Is.InRange(DateTimeOffset.MinValue.UtcTicks, DateTimeOffset.MaxValue.UtcTicks));
            Assert.That(negative.UtcTicks, Is.InRange(DateTimeOffset.MinValue.UtcTicks, DateTimeOffset.MaxValue.UtcTicks));
            index++;
        }
    }

    [Test]
    public void EveryEntropyConsumingDateCompletionOperationReplaysAsOneStream()
    {
        DateProvider first = Fake.Create(6411).Date();
        DateProvider second = Fake.Create(6411).Date();
        DateTimeOffset minimum = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset maximum = minimum.AddDays(1);

        int index = 0;
        while (index < 128)
        {
            Assert.That(first.Time(), Is.EqualTo(second.Time()));
            Assert.That(first.Duration(), Is.EqualTo(second.Duration()));
            Assert.That(first.AnytimeFromPrecision(TemporalPrecision.Milliseconds), Is.EqualTo(second.AnytimeFromPrecision(TemporalPrecision.Milliseconds)));
            Assert.That(
                first.BetweenFromMinimumAndMaximumAndPrecision(minimum, maximum, TemporalPrecision.Microseconds),
                Is.EqualTo(second.BetweenFromMinimumAndMaximumAndPrecision(minimum, maximum, TemporalPrecision.Microseconds)));
            Assert.That(
                first.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(minimum, maximum, DateBoundary.Inclusive, TemporalPrecision.Seconds),
                Is.EqualTo(second.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(minimum, maximum, DateBoundary.Inclusive, TemporalPrecision.Seconds)));
            Assert.That(first.Quarter(), Is.EqualTo(second.Quarter()));
            Assert.That(first.MaximumOffsetValue(), Is.EqualTo(second.MaximumOffsetValue()));
            Assert.That(first.MinimumOffsetValue(), Is.EqualTo(second.MinimumOffsetValue()));
            index++;
        }
    }

    [Test]
    public void LocaleOutputLimitsRejectEveryFormattedSurface()
    {
        FakeOptions options = FakeOptions.Create("en-US", 6412);
        options.MaximumOutputSize = 1;
        DateProvider date = Fake.Create(options).Date();

        Assert.Multiple(() =>
        {
            Assert.That(() => date.LocaleDate(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => date.LocaleTime(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => date.LocaleDateTime(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => date.LocaleDateFromValue(new DateOnly(2024, 2, 29)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => date.LocaleTimeFromValue(new TimeOnly(23, 59, 59)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => date.LocaleDateTimeFromValue(DateTimeOffset.UnixEpoch),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => date.RelativePhraseFromValue(options.ReferenceTime),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }
}
