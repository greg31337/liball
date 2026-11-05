using System.Globalization;
using iso20022;

namespace iso20022.Tests;

public class DurationAndPartialDateLexicalTests
{
    [TestCase("2026", PartialDateKind.Year, 2026, 0, 0, false, 0, false)]
    [TestCase("-0001Z", PartialDateKind.Year, -1, 0, 0, true, 0, false)]
    [TestCase("2026-08+05:30", PartialDateKind.YearMonth, 2026, 8, 0, true, 330, false)]
    [TestCase("--05", PartialDateKind.Month, 0, 5, 0, false, 0, false)]
    [TestCase("---31-00:00", PartialDateKind.Day, 0, 0, 31, true, 0, true)]
    [TestCase("--02-29Z", PartialDateKind.MonthDay, 0, 2, 29, true, 0, false)]
    public void PartialDateParserPreservesShapeAndTimezone(string text, PartialDateKind kind, int year, int month, int day, bool hasTimezone, int offsetMinutes, bool negativeZero)
    {
        IsoPartialDateValue? result = PartialDateLexical.ParsePartialDate(text.AsSpan());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result?.Kind, Is.EqualTo(kind));
            Assert.That(result?.Year, Is.EqualTo(year));
            Assert.That(result?.Month, Is.EqualTo(month));
            Assert.That(result?.Day, Is.EqualTo(day));
            Assert.That(result?.HasTimezone, Is.EqualTo(hasTimezone));
            Assert.That(result?.TimezoneOffsetMinutes, Is.EqualTo(offsetMinutes));
            Assert.That(result?.IsNegativeTimezoneZero, Is.EqualTo(negativeZero));
            Assert.That(result?.LexicalValue, Is.EqualTo(text));
            Assert.That(PartialDateLexical.FormatPartialDate(result!), Is.EqualTo(text));
        });
    }

    [TestCase("0000")]
    [TestCase("2026-00")]
    [TestCase("--00")]
    [TestCase("--13")]
    [TestCase("--05--")]
    [TestCase("---00")]
    [TestCase("---32")]
    [TestCase("--02-30")]
    [TestCase("--2-29")]
    [TestCase("2026-08+14:01")]
    public void PartialDateParserRejectsInvalidShapesAndValues(string text)
    {
        Assert.That(PartialDateLexical.ParsePartialDate(text.AsSpan()), Is.Null);
    }

    [TestCase("P1Y2M3DT4H5M6.700S")]
    [TestCase("-P0Y")]
    [TestCase("PT0S")]
    [TestCase("P3D")]
    [TestCase("PT25H61M70S")]
    [TestCase("P2147483647Y")]
    public void DurationParserPreservesComponentsAndPresence(string text)
    {
        IsoDurationValue? result = DurationLexical.ParseDuration(text.AsSpan());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result?.LexicalValue, Is.EqualTo(text));
            Assert.That(DurationLexical.FormatDuration(result!), Is.EqualTo(text));
        });
    }

    [Test]
    public void DurationParserRetainsFractionalPrecisionAndZeroComponentChoice()
    {
        IsoDurationValue? fractional = DurationLexical.ParseDuration("PT6.0700S".AsSpan());
        IsoDurationValue? zeroYears = DurationLexical.ParseDuration("P0Y".AsSpan());

        Assert.That(fractional, Is.Not.Null);
        Assert.That(zeroYears, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(fractional?.Seconds, Is.EqualTo(6));
            Assert.That(fractional?.FractionalSecondDigits, Is.EqualTo("0700"));
            Assert.That(fractional?.HasSeconds, Is.True);
            Assert.That(zeroYears?.Years, Is.Zero);
            Assert.That(zeroYears?.HasYears, Is.True);
            Assert.That(zeroYears?.HasSeconds, Is.False);
        });
    }

    [TestCase("")]
    [TestCase("P")]
    [TestCase("-P")]
    [TestCase("P1")]
    [TestCase("P1S")]
    [TestCase("PT")]
    [TestCase("P1YT")]
    [TestCase("P1D2Y")]
    [TestCase("PT1S2M")]
    [TestCase("P1Y1Y")]
    [TestCase("PT1.2H")]
    [TestCase("PT1.S")]
    [TestCase("+P1D")]
    [TestCase("P2147483648Y")]
    public void DurationParserRejectsMalformedOrderAndOverflow(string text)
    {
        Assert.That(DurationLexical.ParseDuration(text.AsSpan()), Is.Null);
    }

    [Test]
    public void EmptyDurationModelFormatsAsCanonicalZero()
    {
        Assert.That(DurationLexical.FormatDuration(new IsoDurationValue()), Is.EqualTo("PT0S"));
    }

    [Test]
    [NonParallelizable]
    public void DateTimeFamilyFormattingIsCultureInvariant()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            IsoDateValue? date = DateTimeLexical.ParseDate("2026-08-31+05:30".AsSpan());
            IsoDurationValue? duration = DurationLexical.ParseDuration("P1Y2M3DT4H5M6.70S".AsSpan());

            Assert.That(date, Is.Not.Null);
            Assert.That(duration, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(DateTimeLexical.FormatDate(date!), Is.EqualTo("2026-08-31+05:30"));
                Assert.That(DurationLexical.FormatDuration(duration!), Is.EqualTo("P1Y2M3DT4H5M6.70S"));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
