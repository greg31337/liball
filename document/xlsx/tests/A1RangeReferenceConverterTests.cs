using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class A1RangeReferenceConverterTests
{
    [Test]
    public void Parse_AcceptsSingleCellAndTwoCellRanges()
    {
        XlsxA1RangeReferenceResult single = XlsxA1RangeReferenceConverter.Parse("a1");
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse("$a$1:xfd$1048576");

        Assert.That(single.IsValid, Is.True);
        Assert.That(single.HasLast, Is.False);
        Assert.That(single.Text, Is.EqualTo("A1"));
        Assert.That(single.First.Row, Is.EqualTo(1));
        Assert.That(single.First.Column, Is.EqualTo(1));
        Assert.That(single.Last.Row, Is.EqualTo(1));
        Assert.That(single.Last.Column, Is.EqualTo(1));
        Assert.That(range.IsValid, Is.True);
        Assert.That(range.HasLast, Is.True);
        Assert.That(range.Text, Is.EqualTo("$A$1:XFD$1048576"));
        Assert.That(range.First.IsAbsoluteColumn, Is.True);
        Assert.That(range.First.IsAbsoluteRow, Is.True);
        Assert.That(range.Last.Row, Is.EqualTo(1048576));
        Assert.That(range.Last.Column, Is.EqualTo(16384));
        Assert.That(range.Last.IsAbsoluteRow, Is.True);
    }

    [Test]
    public void ParseLeading_ConsumesOnlyRangePrefix()
    {
        XlsxA1RangeReferenceResult result = XlsxA1RangeReferenceConverter.ParseLeading("A1:B2 rest".AsSpan());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Text, Is.EqualTo("A1:B2"));
        Assert.That(result.CharactersConsumed, Is.EqualTo(5));
        Assert.That(result.HasLast, Is.True);
    }

    [Test]
    public void Parse_RejectsEmptyMissingEndpointTrailingCharactersAndOutOfRangeReferences()
    {
        XlsxA1RangeReferenceResult empty = XlsxA1RangeReferenceConverter.Parse("");
        XlsxA1RangeReferenceResult missingLast = XlsxA1RangeReferenceConverter.Parse("A1:");
        XlsxA1RangeReferenceResult trailing = XlsxA1RangeReferenceConverter.Parse("A1:B2:C3");
        XlsxA1RangeReferenceResult highRow = XlsxA1RangeReferenceConverter.Parse("A1:A1048577");
        XlsxA1RangeReferenceResult highColumn = XlsxA1RangeReferenceConverter.Parse("A1:XFE2");

        Assert.That(empty.IsValid, Is.False);
        Assert.That(missingLast.IsValid, Is.False);
        Assert.That(trailing.IsValid, Is.False);
        Assert.That(highRow.IsValid, Is.False);
        Assert.That(highColumn.IsValid, Is.False);
        Assert.That(HasDiagnostic(empty, "a1.rangeReference.empty"), Is.True);
        Assert.That(HasDiagnostic(missingLast, "a1.cellReference.empty"), Is.True);
        Assert.That(HasDiagnostic(trailing, "a1.rangeReference.invalid"), Is.True);
        Assert.That(HasDiagnostic(highRow, "a1.cellReference.rowRange"), Is.True);
        Assert.That(HasDiagnostic(highColumn, "a1.cellReference.columnRange"), Is.True);
    }

    [Test]
    public void Format_WritesSingleCellAndTwoCellRanges()
    {
        XlsxA1RangeReferenceValue single = CreateRange(1, 1, false, false, 0, 0, false, false, false);
        XlsxA1RangeReferenceValue range = CreateRange(1, 1, true, true, 1048576, 16384, true, false, true);

        XlsxA1RangeReferenceResult singleResult = XlsxA1RangeReferenceConverter.Format(single);
        XlsxA1RangeReferenceResult rangeResult = XlsxA1RangeReferenceConverter.Format(range);

        Assert.That(singleResult.IsValid, Is.True);
        Assert.That(singleResult.Text, Is.EqualTo("A1"));
        Assert.That(singleResult.HasLast, Is.False);
        Assert.That(rangeResult.IsValid, Is.True);
        Assert.That(rangeResult.Text, Is.EqualTo("$A$1:XFD$1048576"));
        Assert.That(rangeResult.HasLast, Is.True);
        Assert.That(XlsxA1RangeReferenceConverter.FormatString(range), Is.EqualTo("$A$1:XFD$1048576"));
    }

    [Test]
    public void Format_ReportsInvalidValuesAndSmallBuffers()
    {
        XlsxA1RangeReferenceValue invalid = CreateRange(0, 1, false, false, 0, 0, false, false, false);
        XlsxA1RangeReferenceValue range = CreateRange(1, 1, false, false, 2, 2, false, false, true);
        char[] buffer = new char[5];
        XlsxA1RangeReferenceResult written = XlsxA1RangeReferenceConverter.Format(range, buffer.AsSpan());
        char[] smallBuffer = new char[4];
        XlsxA1RangeReferenceResult small = XlsxA1RangeReferenceConverter.Format(range, smallBuffer.AsSpan());
        XlsxA1RangeReferenceResult invalidResult = XlsxA1RangeReferenceConverter.Format(invalid);

        Assert.That(written.IsValid, Is.True);
        Assert.That(written.Text, Is.EqualTo("A1:B2"));
        Assert.That(written.CharactersWritten, Is.EqualTo(5));
        Assert.That(new string(buffer), Is.EqualTo("A1:B2"));
        Assert.That(small.IsValid, Is.False);
        Assert.That(small.BufferTooSmall, Is.True);
        Assert.That(HasDiagnostic(small, "a1.rangeReference.bufferTooSmall"), Is.True);
        Assert.That(invalidResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidResult, "a1.rangeReference.range"), Is.True);
    }

    static XlsxA1RangeReferenceValue CreateRange(int firstRow, int firstColumn, bool firstAbsoluteRow, bool firstAbsoluteColumn, int lastRow, int lastColumn, bool lastAbsoluteRow, bool lastAbsoluteColumn, bool hasLast)
    {
        XlsxA1RangeReferenceValue range = new XlsxA1RangeReferenceValue();
        range.First.Row = firstRow;
        range.First.Column = firstColumn;
        range.First.IsAbsoluteRow = firstAbsoluteRow;
        range.First.IsAbsoluteColumn = firstAbsoluteColumn;
        range.HasLast = hasLast;
        if (hasLast)
        {
            range.Last.Row = lastRow;
            range.Last.Column = lastColumn;
            range.Last.IsAbsoluteRow = lastAbsoluteRow;
            range.Last.IsAbsoluteColumn = lastAbsoluteColumn;
        }

        return range;
    }

    static bool HasDiagnostic(XlsxA1RangeReferenceResult result, string code)
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
