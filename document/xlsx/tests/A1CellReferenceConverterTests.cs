using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class A1CellReferenceConverterTests
{
    [Test]
    public void Parse_AcceptsReferencesAndNormalizesColumnCase()
    {
        AssertReference("A1", 1, 1, false, false, "A1");
        AssertReference("$a$1", 1, 1, true, true, "$A$1");
        AssertReference("BC12", 12, 55, false, false, "BC12");
        AssertReference("$XFD1048576", 1048576, 16384, false, true, "$XFD1048576");
        AssertReference("xfd$1048576", 1048576, 16384, true, false, "XFD$1048576");
    }

    [Test]
    public void ParseLeading_ConsumesOnlyCellReferencePrefix()
    {
        XlsxA1CellReferenceResult result = XlsxA1CellReferenceConverter.ParseLeading("$bc$12:D20".AsSpan());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Row, Is.EqualTo(12));
        Assert.That(result.Column, Is.EqualTo(55));
        Assert.That(result.IsAbsoluteColumn, Is.True);
        Assert.That(result.IsAbsoluteRow, Is.True);
        Assert.That(result.Text, Is.EqualTo("$BC$12"));
        Assert.That(result.CharactersConsumed, Is.EqualTo(6));
    }

    [Test]
    public void Parse_RejectsMissingPartsTrailingCharactersAndOutOfRangeReferences()
    {
        XlsxA1CellReferenceResult empty = XlsxA1CellReferenceConverter.Parse("");
        XlsxA1CellReferenceResult missingColumn = XlsxA1CellReferenceConverter.Parse("1");
        XlsxA1CellReferenceResult missingRow = XlsxA1CellReferenceConverter.Parse("A");
        XlsxA1CellReferenceResult trailing = XlsxA1CellReferenceConverter.Parse("A1B");
        XlsxA1CellReferenceResult zeroRow = XlsxA1CellReferenceConverter.Parse("A0");
        XlsxA1CellReferenceResult highRow = XlsxA1CellReferenceConverter.Parse("A1048577");
        XlsxA1CellReferenceResult highColumn = XlsxA1CellReferenceConverter.Parse("XFE1");

        Assert.That(empty.IsValid, Is.False);
        Assert.That(missingColumn.IsValid, Is.False);
        Assert.That(missingRow.IsValid, Is.False);
        Assert.That(trailing.IsValid, Is.False);
        Assert.That(zeroRow.IsValid, Is.False);
        Assert.That(highRow.IsValid, Is.False);
        Assert.That(highColumn.IsValid, Is.False);
        Assert.That(HasDiagnostic(empty, "a1.cellReference.empty"), Is.True);
        Assert.That(HasDiagnostic(missingColumn, "a1.cellReference.column"), Is.True);
        Assert.That(HasDiagnostic(missingRow, "a1.cellReference.row"), Is.True);
        Assert.That(HasDiagnostic(trailing, "a1.cellReference.invalid"), Is.True);
        Assert.That(HasDiagnostic(zeroRow, "a1.cellReference.rowRange"), Is.True);
        Assert.That(HasDiagnostic(highRow, "a1.cellReference.rowRange"), Is.True);
        Assert.That(HasDiagnostic(highColumn, "a1.cellReference.columnRange"), Is.True);
    }

    [Test]
    public void Format_WritesReferencesAndReportsInvalidBounds()
    {
        AssertFormat(1, 1, false, false, "A1");
        AssertFormat(12, 28, true, false, "AB$12");
        AssertFormat(12, 28, false, true, "$AB12");
        AssertFormat(1048576, 16384, true, true, "$XFD$1048576");

        XlsxA1CellReferenceResult zeroRow = XlsxA1CellReferenceConverter.Format(0, 1, false, false);
        XlsxA1CellReferenceResult highColumn = XlsxA1CellReferenceConverter.Format(1, 16385, false, false);

        Assert.That(zeroRow.IsValid, Is.False);
        Assert.That(highColumn.IsValid, Is.False);
        Assert.That(HasDiagnostic(zeroRow, "a1.cellReference.range"), Is.True);
        Assert.That(HasDiagnostic(highColumn, "a1.cellReference.range"), Is.True);
        Assert.That(XlsxXmlValueWriter.WriteCellReference(12, 28, false, true), Is.EqualTo("$AB12"));
        Assert.That(XlsxXmlValueWriter.WriteCellReference(0, 1, false, false), Is.EqualTo(""));
    }

    [Test]
    public void Format_WritesIntoSpanAndReportsSmallBuffers()
    {
        char[] buffer = new char[12];
        XlsxA1CellReferenceResult result = XlsxA1CellReferenceConverter.Format(1048576, 16384, true, true, buffer.AsSpan());
        char[] smallBuffer = new char[11];
        XlsxA1CellReferenceResult small = XlsxA1CellReferenceConverter.Format(1048576, 16384, true, true, smallBuffer.AsSpan());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Text, Is.EqualTo("$XFD$1048576"));
        Assert.That(result.CharactersWritten, Is.EqualTo(12));
        Assert.That(new string(buffer), Is.EqualTo("$XFD$1048576"));
        Assert.That(small.IsValid, Is.False);
        Assert.That(small.BufferTooSmall, Is.True);
        Assert.That(HasDiagnostic(small, "a1.cellReference.bufferTooSmall"), Is.True);
    }

    static void AssertReference(string text, int row, int column, bool absoluteRow, bool absoluteColumn, string normalized)
    {
        XlsxA1CellReferenceResult result = XlsxA1CellReferenceConverter.Parse(text);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Row, Is.EqualTo(row));
        Assert.That(result.Column, Is.EqualTo(column));
        Assert.That(result.IsAbsoluteRow, Is.EqualTo(absoluteRow));
        Assert.That(result.IsAbsoluteColumn, Is.EqualTo(absoluteColumn));
        Assert.That(result.Reference.Row, Is.EqualTo(row));
        Assert.That(result.Reference.Column, Is.EqualTo(column));
        Assert.That(result.Reference.IsAbsoluteRow, Is.EqualTo(absoluteRow));
        Assert.That(result.Reference.IsAbsoluteColumn, Is.EqualTo(absoluteColumn));
        Assert.That(result.Text, Is.EqualTo(normalized));
        Assert.That(result.Reference.Text, Is.EqualTo(normalized));
        Assert.That(result.CharactersConsumed, Is.EqualTo(text.Length));
    }

    static void AssertFormat(int row, int column, bool absoluteRow, bool absoluteColumn, string text)
    {
        XlsxA1CellReferenceResult result = XlsxA1CellReferenceConverter.Format(row, column, absoluteRow, absoluteColumn);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Row, Is.EqualTo(row));
        Assert.That(result.Column, Is.EqualTo(column));
        Assert.That(result.IsAbsoluteRow, Is.EqualTo(absoluteRow));
        Assert.That(result.IsAbsoluteColumn, Is.EqualTo(absoluteColumn));
        Assert.That(result.Text, Is.EqualTo(text));
        Assert.That(result.CharactersWritten, Is.EqualTo(text.Length));
    }

    static bool HasDiagnostic(XlsxA1CellReferenceResult result, string code)
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
