using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class XmlValueParserWriterTests
{
    [Test]
    public void ParseBoolean_AcceptsSpreadsheetLexicalValuesAndWriterUsesNumericForm()
    {
        XlsxXmlValueParseResult trueResult = XlsxXmlValueParser.ParseBoolean(" true ");
        XlsxXmlValueParseResult oneResult = XlsxXmlValueParser.ParseBoolean("1");
        XlsxXmlValueParseResult falseResult = XlsxXmlValueParser.ParseBoolean("0");
        XlsxXmlValueParseResult invalidResult = XlsxXmlValueParser.ParseBoolean("yes");

        Assert.That(trueResult.IsValid, Is.True);
        Assert.That(trueResult.BooleanValue, Is.True);
        Assert.That(oneResult.IsValid, Is.True);
        Assert.That(oneResult.BooleanValue, Is.True);
        Assert.That(falseResult.IsValid, Is.True);
        Assert.That(falseResult.BooleanValue, Is.False);
        Assert.That(invalidResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidResult, "xml.boolean.invalid"), Is.True);
        Assert.That(XlsxXmlValueWriter.WriteBoolean(true), Is.EqualTo("1"));
        Assert.That(XlsxXmlValueWriter.WriteBoolean(false), Is.EqualTo("0"));
    }

    [Test]
    public void ParseIntegerAndUnsignedInteger_ValidateBoundsAndStableWriting()
    {
        XlsxXmlValueParseResult integerResult = XlsxXmlValueParser.ParseInteger("-2147483648");
        XlsxXmlValueParseResult integerOverflow = XlsxXmlValueParser.ParseInteger("2147483648");
        XlsxXmlValueParseResult unsignedResult = XlsxXmlValueParser.ParseUnsignedInteger("4294967295");
        XlsxXmlValueParseResult unsignedOverflow = XlsxXmlValueParser.ParseUnsignedInteger("4294967296");
        XlsxXmlValueParseResult unsignedInvalid = XlsxXmlValueParser.ParseUnsignedInteger("-1");

        Assert.That(integerResult.IsValid, Is.True);
        Assert.That(integerResult.IntValue, Is.EqualTo(int.MinValue));
        Assert.That(integerOverflow.IsValid, Is.False);
        Assert.That(HasDiagnostic(integerOverflow, "xml.integer.overflow"), Is.True);
        Assert.That(unsignedResult.IsValid, Is.True);
        Assert.That(unsignedResult.UnsignedIntValue, Is.EqualTo(uint.MaxValue));
        Assert.That(unsignedOverflow.IsValid, Is.False);
        Assert.That(HasDiagnostic(unsignedOverflow, "xml.unsignedInteger.overflow"), Is.True);
        Assert.That(unsignedInvalid.IsValid, Is.False);
        Assert.That(HasDiagnostic(unsignedInvalid, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(XlsxXmlValueWriter.WriteInteger(-42), Is.EqualTo("-42"));
        Assert.That(XlsxXmlValueWriter.WriteUnsignedInteger(42), Is.EqualTo("42"));
    }

    [Test]
    public void ParseDouble_AcceptsDecimalsAndExponentsAndRejectsInvalidValues()
    {
        XlsxXmlValueParseResult decimalResult = XlsxXmlValueParser.ParseDouble("12.5");
        XlsxXmlValueParseResult exponentResult = XlsxXmlValueParser.ParseDouble("-1.25E2");
        XlsxXmlValueParseResult invalidResult = XlsxXmlValueParser.ParseDouble("1.2.3");
        XlsxXmlValueParseResult overflowResult = XlsxXmlValueParser.ParseDouble("1e400");

        Assert.That(decimalResult.IsValid, Is.True);
        Assert.That(decimalResult.DoubleValue, Is.EqualTo(12.5d).Within(0.000000001d));
        Assert.That(exponentResult.IsValid, Is.True);
        Assert.That(exponentResult.DoubleValue, Is.EqualTo(-125d).Within(0.000000001d));
        Assert.That(invalidResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidResult, "xml.double.invalid"), Is.True);
        Assert.That(overflowResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(overflowResult, "xml.double.overflow"), Is.True);
        Assert.That(XlsxXmlValueParser.ParseDouble(XlsxXmlValueWriter.WriteDouble(0.1d)).IsValid, Is.True);
    }

    [Test]
    public void ParseDateAndDateTime_ValidateIsoFormsAndWriteStableText()
    {
        XlsxXmlValueParseResult dateResult = XlsxXmlValueParser.ParseDate("2026-08-09");
        XlsxXmlValueParseResult dateTimeResult = XlsxXmlValueParser.ParseDateTime("2026-08-09T12:34:56Z");
        XlsxXmlValueParseResult invalidDate = XlsxXmlValueParser.ParseDate("2026-02-29");
        XlsxXmlValueParseResult invalidDateTime = XlsxXmlValueParser.ParseDateTime("2026-08-09T25:00:00");

        Assert.That(dateResult.IsValid, Is.True);
        Assert.That(dateResult.DateTimeValue.Year, Is.EqualTo(2026));
        Assert.That(dateResult.DateTimeValue.Month, Is.EqualTo(8));
        Assert.That(dateResult.DateTimeValue.Day, Is.EqualTo(9));
        Assert.That(dateTimeResult.IsValid, Is.True);
        Assert.That(dateTimeResult.DateTimeValue.Kind, Is.EqualTo(System.DateTimeKind.Utc));
        Assert.That(invalidDate.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidDate, "xml.date.range"), Is.True);
        Assert.That(invalidDateTime.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidDateTime, "xml.dateTime.range"), Is.True);
        Assert.That(XlsxXmlValueWriter.WriteDate(dateResult.DateTimeValue), Is.EqualTo("2026-08-09"));
        Assert.That(XlsxXmlValueWriter.WriteDateTime(dateTimeResult.DateTimeValue), Is.EqualTo("2026-08-09T12:34:56Z"));
    }

    [Test]
    public void ParseColor_NormalizesHexRgbAndRejectsMalformedValues()
    {
        XlsxXmlValueParseResult rgbResult = XlsxXmlValueParser.ParseColor("ff00aa11");
        XlsxXmlValueParseResult hashResult = XlsxXmlValueParser.ParseColor("#00aa11");
        XlsxXmlValueParseResult invalidResult = XlsxXmlValueParser.ParseColor("not-a-color");

        Assert.That(rgbResult.IsValid, Is.True);
        Assert.That(rgbResult.ColorValue.HasRgb, Is.True);
        Assert.That(rgbResult.ColorValue.Rgb, Is.EqualTo("FF00AA11"));
        Assert.That(hashResult.IsValid, Is.True);
        Assert.That(hashResult.ColorValue.Rgb, Is.EqualTo("00AA11"));
        Assert.That(invalidResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidResult, "xml.color.invalid"), Is.True);
        Assert.That(XlsxXmlValueWriter.WriteColor(rgbResult.ColorValue), Is.EqualTo("FF00AA11"));
    }

    [Test]
    public void ParseCellReference_HandlesAbsoluteMarkersBoundsAndFormatting()
    {
        XlsxXmlValueParseResult referenceResult = XlsxXmlValueParser.ParseCellReference("$xfd$1048576");
        XlsxXmlValueParseResult invalidColumn = XlsxXmlValueParser.ParseCellReference("XFE1");
        XlsxXmlValueParseResult invalidRow = XlsxXmlValueParser.ParseCellReference("A1048577");

        Assert.That(referenceResult.IsValid, Is.True);
        Assert.That(referenceResult.CellReferenceValue.Column, Is.EqualTo(16384));
        Assert.That(referenceResult.CellReferenceValue.Row, Is.EqualTo(1048576));
        Assert.That(referenceResult.CellReferenceValue.IsAbsoluteColumn, Is.True);
        Assert.That(referenceResult.CellReferenceValue.IsAbsoluteRow, Is.True);
        Assert.That(referenceResult.TextValue, Is.EqualTo("$XFD$1048576"));
        Assert.That(invalidColumn.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidColumn, "xml.cellReference.columnRange"), Is.True);
        Assert.That(invalidRow.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRow, "xml.cellReference.rowRange"), Is.True);
        Assert.That(XlsxXmlValueWriter.WriteCellReference(12, 28, false, true), Is.EqualTo("$AB12"));
        Assert.That(XlsxXmlValueWriter.WriteColumnName(16384), Is.EqualTo("XFD"));
    }

    [Test]
    public void ParseRelationshipId_UsesXmlNameSubsetAndWriterRejectsInvalidIds()
    {
        XlsxXmlValueParseResult validResult = XlsxXmlValueParser.ParseRelationshipId("rId42");
        XlsxXmlValueParseResult invalidStart = XlsxXmlValueParser.ParseRelationshipId("1rel");
        XlsxXmlValueParseResult invalidCharacter = XlsxXmlValueParser.ParseRelationshipId("rel:1");

        Assert.That(validResult.IsValid, Is.True);
        Assert.That(validResult.TextValue, Is.EqualTo("rId42"));
        Assert.That(invalidStart.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidStart, "xml.relationshipId.invalid"), Is.True);
        Assert.That(invalidCharacter.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidCharacter, "xml.relationshipId.invalid"), Is.True);
        Assert.That(XlsxXmlValueWriter.WriteRelationshipId("rId42"), Is.EqualTo("rId42"));
        Assert.That(XlsxXmlValueWriter.WriteRelationshipId("1rel"), Is.EqualTo(""));
    }

    static bool HasDiagnostic(XlsxXmlValueParseResult result, string code)
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
