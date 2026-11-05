using System;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class DateSerialConverterTests
{
    [Test]
    public void FromSerial_Converts1900DateSystemAroundExcelLeapYearBug()
    {
        XlsxDateSerialConversionResult serial1 = XlsxDateSerialConverter.FromSerial(1d, false);
        XlsxDateSerialConversionResult serial59 = XlsxDateSerialConverter.FromSerial(59d, false);
        XlsxDateSerialConversionResult serial60 = XlsxDateSerialConverter.FromSerial(60d, false);
        XlsxDateSerialConversionResult serial61 = XlsxDateSerialConverter.FromSerial(61d, false);

        Assert.That(serial1.IsValid, Is.True);
        Assert.That(serial1.DateTimeValue, Is.EqualTo(new DateTime(1900, 1, 1)));
        Assert.That(serial59.DateTimeValue, Is.EqualTo(new DateTime(1900, 2, 28)));
        Assert.That(serial60.IsValid, Is.True);
        Assert.That(serial60.IsExcel1900LeapDay, Is.True);
        Assert.That(serial60.DateTimeValue, Is.EqualTo(new DateTime(1900, 2, 28)));
        Assert.That(serial61.DateTimeValue, Is.EqualTo(new DateTime(1900, 3, 1)));
    }

    [Test]
    public void ToSerial_Converts1900DateSystemWithLeapYearCompatibilityGap()
    {
        XlsxDateSerialConversionResult feb28 = XlsxDateSerialConverter.ToSerial(new DateTime(1900, 2, 28), false);
        XlsxDateSerialConversionResult mar1 = XlsxDateSerialConverter.ToSerial(new DateTime(1900, 3, 1), false);
        XlsxDateSerialConversionResult noon = XlsxDateSerialConverter.ToSerial(new DateTime(1900, 1, 2, 12, 0, 0), false);

        Assert.That(feb28.IsValid, Is.True);
        Assert.That(feb28.SerialValue, Is.EqualTo(59d));
        Assert.That(mar1.IsValid, Is.True);
        Assert.That(mar1.SerialValue, Is.EqualTo(61d));
        Assert.That(noon.IsValid, Is.True);
        Assert.That(noon.SerialValue, Is.EqualTo(2.5d).Within(0.0000000001d));
        Assert.That(noon.HasTimeFraction, Is.True);
    }

    [Test]
    public void FromSerialAndToSerial_Convert1904DateSystem()
    {
        XlsxDateSerialConversionResult baseDate = XlsxDateSerialConverter.FromSerial(0d, true);
        XlsxDateSerialConversionResult fractional = XlsxDateSerialConverter.FromSerial(1.25d, true);
        XlsxDateSerialConversionResult serial = XlsxDateSerialConverter.ToSerial(new DateTime(1904, 1, 2, 6, 0, 0), true);

        Assert.That(baseDate.IsValid, Is.True);
        Assert.That(baseDate.Is1904DateSystem, Is.True);
        Assert.That(baseDate.DateTimeValue, Is.EqualTo(new DateTime(1904, 1, 1)));
        Assert.That(fractional.DateTimeValue, Is.EqualTo(new DateTime(1904, 1, 2, 6, 0, 0)));
        Assert.That(fractional.HasTimeFraction, Is.True);
        Assert.That(serial.IsValid, Is.True);
        Assert.That(serial.SerialValue, Is.EqualTo(1.25d).Within(0.0000000001d));
    }

    [Test]
    public void CreateExcel1900LeapDay_CreatesSerializableLeapDayMarker()
    {
        XlsxDateSerialConversionResult leapNoon = XlsxDateSerialConverter.CreateExcel1900LeapDay(0.5d);

        Assert.That(leapNoon.IsValid, Is.True);
        Assert.That(leapNoon.IsExcel1900LeapDay, Is.True);
        Assert.That(leapNoon.SerialValue, Is.EqualTo(60.5d).Within(0.0000000001d));
        Assert.That(leapNoon.DateTimeValue, Is.EqualTo(new DateTime(1900, 2, 28, 12, 0, 0)));
        Assert.That(leapNoon.HasTimeFraction, Is.True);
    }

    [Test]
    public void DateSerialConverter_ReportsInvalidInputs()
    {
        XlsxDateSerialConversionResult negative = XlsxDateSerialConverter.FromSerial(-1d, false);
        XlsxDateSerialConversionResult notFinite = XlsxDateSerialConverter.FromSerial(double.NaN, false);
        XlsxDateSerialConversionResult tooLarge = XlsxDateSerialConverter.FromSerial(5000000d, false);
        XlsxDateSerialConversionResult before1904 = XlsxDateSerialConverter.ToSerial(new DateTime(1903, 12, 31), true);
        XlsxDateSerialConversionResult badLeapFraction = XlsxDateSerialConverter.CreateExcel1900LeapDay(1d);

        Assert.That(negative.IsValid, Is.False);
        Assert.That(HasDiagnostic(negative, "dateSerial.range"), Is.True);
        Assert.That(notFinite.IsValid, Is.False);
        Assert.That(HasDiagnostic(notFinite, "dateSerial.invalid"), Is.True);
        Assert.That(tooLarge.IsValid, Is.False);
        Assert.That(HasDiagnostic(tooLarge, "dateSerial.range"), Is.True);
        Assert.That(before1904.IsValid, Is.False);
        Assert.That(HasDiagnostic(before1904, "dateSerial.dateRange"), Is.True);
        Assert.That(badLeapFraction.IsValid, Is.False);
        Assert.That(HasDiagnostic(badLeapFraction, "dateSerial.timeFractionRange"), Is.True);
    }

    static bool HasDiagnostic(XlsxDateSerialConversionResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
