using System.Globalization;
using iso20022;

namespace iso20022.Tests;

public class CalendarLexicalExhaustiveTests
{
    [Test]
    public void DatesRoundTripAcrossACompleteGregorianFourHundredYearCycle()
    {
        int year = 1600;
        while (year < 2000)
        {
            int month = 1;
            while (month <= 12)
            {
                int maximumDay = DateTime.DaysInMonth(year, month);
                int day = 1;
                while (day <= maximumDay)
                {
                    string text = year.ToString("D4", CultureInfo.InvariantCulture) + "-" + month.ToString("D2", CultureInfo.InvariantCulture) + "-" + day.ToString("D2", CultureInfo.InvariantCulture);
                    IsoDateValue? parsed = DateTimeLexical.ParseDate(text.AsSpan());
                    if (parsed == null)
                    {
                        Assert.Fail("Valid Gregorian date was rejected: " + text);
                    }

                    if (DateTimeLexical.FormatDate(parsed!) != text)
                    {
                        Assert.Fail("Gregorian date did not round trip: " + text);
                    }

                    day++;
                }

                string invalidText = year.ToString("D4", CultureInfo.InvariantCulture) + "-" + month.ToString("D2", CultureInfo.InvariantCulture) + "-" + (maximumDay + 1).ToString("D2", CultureInfo.InvariantCulture);
                Assert.That(DateTimeLexical.ParseDate(invalidText.AsSpan()), Is.Null, invalidText);
                month++;
            }

            year++;
        }
    }

    [Test]
    public void TimesRoundTripEveryWholeSecondIncludingLeapSeconds()
    {
        int hour = 0;
        while (hour < 24)
        {
            int minute = 0;
            while (minute < 60)
            {
                int second = 0;
                while (second <= 60)
                {
                    string text = hour.ToString("D2", CultureInfo.InvariantCulture) + ":" + minute.ToString("D2", CultureInfo.InvariantCulture) + ":" + second.ToString("D2", CultureInfo.InvariantCulture);
                    IsoTimeValue? parsed = DateTimeLexical.ParseTime(text.AsSpan());
                    if (parsed == null)
                    {
                        Assert.Fail("Valid whole-second time was rejected: " + text);
                    }

                    if (DateTimeLexical.FormatTime(parsed!) != text)
                    {
                        Assert.Fail("Whole-second time did not round trip: " + text);
                    }

                    second++;
                }

                minute++;
            }

            hour++;
        }

        Assert.That(DateTimeLexical.ParseTime("24:00:00".AsSpan()), Is.Not.Null);
        Assert.That(DateTimeLexical.ParseTime("24:00:00.000000".AsSpan()), Is.Not.Null);
        Assert.That(DateTimeLexical.ParseTime("24:00:00.000001".AsSpan()), Is.Null);
    }

    [Test]
    public void EveryPermittedTimezoneMinuteOffsetRoundTrips()
    {
        int offset = -840;
        while (offset <= 840)
        {
            IsoTimeValue value = new IsoTimeValue
            {
                Hour = 12,
                Minute = 34,
                Second = 56,
                HasTimezone = true,
                TimezoneOffsetMinutes = offset
            };
            string? text = DateTimeLexical.FormatTime(value);
            if (text == null)
            {
                Assert.Fail("Permitted timezone offset was rejected: " + offset.ToString(CultureInfo.InvariantCulture));
            }

            IsoTimeValue? parsed = DateTimeLexical.ParseTime(text.AsSpan());
            if (parsed == null || parsed.TimezoneOffsetMinutes != offset)
            {
                Assert.Fail("Timezone offset did not round trip: " + offset.ToString(CultureInfo.InvariantCulture));
            }

            offset++;
        }
    }

    [Test]
    public void DateAndTimeFormattersRejectEveryInconsistentTimezoneState()
    {
        IsoDateValue absentWithOffset = new IsoDateValue { Year = 2026, Month = 8, Day = 31, TimezoneOffsetMinutes = 1 };
        IsoTimeValue utcWithOffset = new IsoTimeValue { Hour = 1, Minute = 2, Second = 3, HasTimezone = true, IsUtc = true, TimezoneOffsetMinutes = 1 };
        IsoTimeValue negativeZeroWithOffset = new IsoTimeValue { Hour = 1, Minute = 2, Second = 3, HasTimezone = true, IsNegativeTimezoneZero = true, TimezoneOffsetMinutes = -1 };
        IsoPartialDateValue excessiveOffset = new IsoPartialDateValue { Kind = PartialDateKind.Year, Year = 2026, HasTimezone = true, TimezoneOffsetMinutes = 841 };

        Assert.Multiple(() =>
        {
            Assert.That(DateTimeLexical.FormatDate(absentWithOffset), Is.Null);
            Assert.That(DateTimeLexical.FormatTime(utcWithOffset), Is.Null);
            Assert.That(DateTimeLexical.FormatTime(negativeZeroWithOffset), Is.Null);
            Assert.That(PartialDateLexical.FormatPartialDate(excessiveOffset), Is.Null);
            Assert.That(DateTimeLexical.FormatDate(null!), Is.Null);
            Assert.That(DateTimeLexical.FormatTime(null!), Is.Null);
            Assert.That(DateTimeLexical.FormatDateTime(null!), Is.Null);
            Assert.That(DurationLexical.FormatDuration(null!), Is.Null);
            Assert.That(PartialDateLexical.FormatPartialDate(null!), Is.Null);
        });
    }

    [Test]
    public void EveryGregorianMonthDayRoundTripsAndRejectsItsFirstOutOfRangeDay()
    {
        int month = 1;
        while (month <= 12)
        {
            int maximumDay = DateTime.DaysInMonth(2000, month);
            int day = 1;
            while (day <= maximumDay)
            {
                string text = "--" + month.ToString("D2", CultureInfo.InvariantCulture) + "-" + day.ToString("D2", CultureInfo.InvariantCulture);
                IsoPartialDateValue? parsed = PartialDateLexical.ParsePartialDate(text.AsSpan());
                Assert.That(parsed, Is.Not.Null, text);
                Assert.That(PartialDateLexical.FormatPartialDate(parsed!), Is.EqualTo(text));
                day++;
            }

            string invalidText = "--" + month.ToString("D2", CultureInfo.InvariantCulture) + "-" + (maximumDay + 1).ToString("D2", CultureInfo.InvariantCulture);
            Assert.That(PartialDateLexical.ParsePartialDate(invalidText.AsSpan()), Is.Null, invalidText);
            month++;
        }
    }

    [Test]
    public void EveryDurationComponentPresenceCombinationRoundTrips()
    {
        int mask = 0;
        while (mask < 64)
        {
            IsoDurationValue value = new IsoDurationValue();
            value.IsNegative = (mask & 1) != 0;
            value.HasYears = (mask & 1) != 0;
            value.Years = value.HasYears ? 1 : 0;
            value.HasMonths = (mask & 2) != 0;
            value.Months = value.HasMonths ? 2 : 0;
            value.HasDays = (mask & 4) != 0;
            value.Days = value.HasDays ? 3 : 0;
            value.HasHours = (mask & 8) != 0;
            value.Hours = value.HasHours ? 4 : 0;
            value.HasMinutes = (mask & 16) != 0;
            value.Minutes = value.HasMinutes ? 5 : 0;
            value.HasSeconds = (mask & 32) != 0;
            value.Seconds = value.HasSeconds ? 6 : 0;
            value.FractionalSecondDigits = value.HasSeconds ? "0700" : string.Empty;

            string? text = DurationLexical.FormatDuration(value);
            Assert.That(text, Is.Not.Null, "Mask " + mask.ToString(CultureInfo.InvariantCulture));
            IsoDurationValue? parsed = DurationLexical.ParseDuration(text!.AsSpan());
            Assert.That(parsed, Is.Not.Null, text);
            Assert.That(DurationLexical.FormatDuration(parsed!), Is.EqualTo(text));
            mask++;
        }
    }

    [Test]
    public void DurationFormatterRejectsNegativeComponentsAndMalformedFractions()
    {
        IsoDurationValue negativeComponent = new IsoDurationValue { Years = -1 };
        IsoDurationValue malformedFraction = new IsoDurationValue { HasSeconds = true, FractionalSecondDigits = "1A" };

        Assert.Multiple(() =>
        {
            Assert.That(DurationLexical.FormatDuration(negativeComponent), Is.Null);
            Assert.That(DurationLexical.FormatDuration(malformedFraction), Is.Null);
        });
    }

    [Test]
    [NonParallelizable]
    public void EveryPrimitiveFormatterRemainsInvariantAcrossRepresentativeCultures()
    {
        string[] cultureNames = new string[] { "fr-FR", "ar-SA", "th-TH", "tr-TR" };
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            foreach (string cultureName in cultureNames)
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultureName);
                CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
                IsoDecimalValue? number = DecimalLexical.ParseDecimal("1234.500".AsSpan());
                IsoDateTimeValue? dateTime = DateTimeLexical.ParseDateTime("2026-08-31T09:05:07.1200-07:00".AsSpan());
                IsoDurationValue? duration = DurationLexical.ParseDuration("P1Y2M3DT4H5M6.70S".AsSpan());

                Assert.That(DecimalLexical.FormatDecimal(number!), Is.EqualTo("1234.500"), cultureName);
                Assert.That(DateTimeLexical.FormatDateTime(dateTime!), Is.EqualTo("2026-08-31T09:05:07.1200-07:00"), cultureName);
                Assert.That(DurationLexical.FormatDuration(duration!), Is.EqualTo("P1Y2M3DT4H5M6.70S"), cultureName);
                Assert.That(BooleanLexical.FormatBoolean(true), Is.EqualTo("true"), cultureName);
                Assert.That(IntegerLexical.FormatInt64(-1234567890), Is.EqualTo("-1234567890"), cultureName);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
