using System.Globalization;
using System.Numerics;

namespace odf.Tests;

public class FormulaTemporalModelTests
{
    [Test]
    public void AppliesProlepticGregorianLeapYearRules()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FormulaTemporalModel.IsLeapYear(
                new BigInteger(1900)), Is.False);
            Assert.That(FormulaTemporalModel.IsLeapYear(
                new BigInteger(2000)), Is.True);
            Assert.That(FormulaTemporalModel.IsLeapYear(
                new BigInteger(2100)), Is.False);
            Assert.That(FormulaTemporalModel.IsLeapYear(
                new BigInteger(2400)), Is.True);
            Assert.That(FormulaTemporalModel.DaysBetweenDates(
                "1900-02-28",
                "1900-03-01"), Is.EqualTo(BigInteger.One));
            Assert.That(FormulaTemporalModel.DaysBetweenDates(
                "2000-02-28",
                "2000-03-01"), Is.EqualTo(new BigInteger(2)));
            Assert.That(FormulaTemporalModel.DaysBetweenDates(
                "-0001-12-31",
                "0001-01-01"), Is.EqualTo(BigInteger.One));
        });
    }

    [Test]
    public void UsesTheConfiguredNullDateForDateSerials()
    {
        OdfSpreadsheetSettings defaultSettings = new OdfSpreadsheetSettings();
        OdfSpreadsheetSettings alternateSettings = new OdfSpreadsheetSettings();
        alternateSettings.HasNullDate = true;
        alternateSettings.NullDate = "1904-01-01";

        OdfFormulaResult zero = FormulaTemporalModel.CreateTemporal(
            "1899-12-30",
            defaultSettings);
        OdfFormulaResult beforeMarch = FormulaTemporalModel.CreateTemporal(
            "1900-02-28",
            defaultSettings);
        OdfFormulaResult afterFebruary = FormulaTemporalModel.CreateTemporal(
            "1900-03-01",
            defaultSettings);
        OdfFormulaResult alternateZero = FormulaTemporalModel.CreateTemporal(
            "1904-01-01",
            alternateSettings);

        Assert.Multiple(() =>
        {
            Assert.That(zero.Kind, Is.EqualTo(OdfValueKind.Date));
            Assert.That(zero.NumericValue, Is.Zero);
            Assert.That(beforeMarch.NumericValue, Is.EqualTo(60));
            Assert.That(afterFebruary.NumericValue, Is.EqualTo(61));
            Assert.That(alternateZero.NumericValue, Is.Zero);
        });
    }

    [Test]
    public void NormalizesExplicitTimeZonesToTheSameSerialInstant()
    {
        OdfSpreadsheetSettings settings = new OdfSpreadsheetSettings();
        settings.NullDate = "2024-01-01";

        OdfFormulaResult offset = FormulaTemporalModel.CreateTemporal(
            "2024-01-02T00:30:00+02:00",
            settings);
        OdfFormulaResult utc = FormulaTemporalModel.CreateTemporal(
            "2024-01-01T22:30:00Z",
            settings);
        OdfFormulaResult time = FormulaTemporalModel.CreateTemporal(
            "00:30:00+02:00",
            settings);

        Assert.Multiple(() =>
        {
            Assert.That(offset.Kind, Is.EqualTo(OdfValueKind.DateTime));
            Assert.That(offset.NumericValue, Is.EqualTo(0.9375).Within(1e-12));
            Assert.That(utc.NumericValue,
                Is.EqualTo(offset.NumericValue).Within(1e-12));
            Assert.That(time.Kind, Is.EqualTo(OdfValueKind.Time));
            Assert.That(time.NumericValue, Is.EqualTo(0.9375).Within(1e-12));
        });
    }

    [Test]
    public void ConvertsSerialsBackToDatesTimesAndZones()
    {
        OdfSpreadsheetSettings settings = new OdfSpreadsheetSettings();
        settings.NullDate = "1904-01-01";

        OdfFormulaResult dateTime = FormulaTemporalModel.TemporalFromSerial(
            60.5,
            OdfValueKind.DateTime,
            settings,
            330,
            true);
        OdfFormulaResult time = FormulaTemporalModel.TemporalFromSerial(
            0.9375,
            OdfValueKind.Time,
            settings,
            120,
            true);
        OdfFormulaResult reread = FormulaTemporalModel.CreateTemporal(
            dateTime.StringValue,
            settings);
        OdfFormulaResult roundedBoundary =
            FormulaTemporalModel.TemporalFromSerial(
                60.9999999999999999,
                OdfValueKind.DateTime,
                settings,
                0,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(dateTime.StringValue,
                Is.EqualTo("1904-03-01T17:30:00+05:30"));
            Assert.That(time.StringValue, Is.EqualTo("00:30:00+02:00"));
            Assert.That(reread.NumericValue, Is.EqualTo(60.5).Within(1e-12));
            Assert.That(roundedBoundary.StringValue,
                Is.EqualTo("1904-03-02T00:00:00"));
        });
    }

    [Test]
    public void ConvertsDayTimeDurationsWithoutApproximatingMonthsOrYears()
    {
        string lexical = "P2DT3H4M5.5S";
        double expected = 2 + (3 * 3600 + 4 * 60 + 5.5) / 86400.0;
        OdfFormulaResult duration = FormulaTemporalModel.CreateDuration(
            lexical);
        OdfFormulaResult negative = FormulaTemporalModel.CreateDuration(
            "-PT30M");
        OdfFormulaResult calendarDuration =
            FormulaTemporalModel.CreateDuration("P1M");
        OdfFormulaResult formatted =
            FormulaTemporalModel.DurationFromSerial(duration.NumericValue);

        Assert.Multiple(() =>
        {
            Assert.That(duration.Kind, Is.EqualTo(OdfValueKind.Duration));
            Assert.That(duration.NumericValue,
                Is.EqualTo(expected).Within(1e-12));
            Assert.That(negative.NumericValue,
                Is.EqualTo(-30.0 / 1440.0).Within(1e-12));
            Assert.That(calendarDuration.Error,
                Is.EqualTo(OdfFormulaError.Value));
            Assert.That(formatted.StringValue, Is.EqualTo(lexical));
        });
    }

    [Test]
    [NonParallelizable]
    public void TemporalFormattingIsIndependentOfCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            OdfFormulaResult duration =
                FormulaTemporalModel.DurationFromSerial(1.5 / 86400.0);
            OdfSpreadsheetSettings settings = new OdfSpreadsheetSettings();
            settings.NullDate = "2024-01-01";
            OdfFormulaResult dateTime =
                FormulaTemporalModel.TemporalFromSerial(
                    0.0000173611111111111,
                    OdfValueKind.DateTime,
                    settings,
                    0,
                    false);

            Assert.Multiple(() =>
            {
                Assert.That(duration.StringValue, Is.EqualTo("PT1.5S"));
                Assert.That(dateTime.StringValue,
                    Is.EqualTo("2024-01-01T00:00:01.5"));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
