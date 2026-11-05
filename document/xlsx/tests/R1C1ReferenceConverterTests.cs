using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class R1C1ReferenceConverterTests
{
    [Test]
    public void Parse_AcceptsAbsoluteRelativeAndCurrentAxisReferences()
    {
        AssertReference("R1C1", 1, 1, true, true, false, false, "R1C1");
        AssertReference("r1048576c16384", 1048576, 16384, true, true, false, false, "R1048576C16384");
        AssertReference("R[2]C[-1]", 2, -1, true, true, true, true, "R[2]C[-1]");
        AssertReference("RC", 0, 0, false, false, true, true, "RC");
        AssertReference("R[-1]C", -1, 0, true, false, true, true, "R[-1]C");
        AssertReference("RC[3]", 0, 3, false, true, true, true, "RC[3]");
    }

    [Test]
    public void ParseLeading_ConsumesOnlyR1C1Prefix()
    {
        XlsxR1C1ReferenceResult result = XlsxR1C1ReferenceConverter.ParseLeading("R[2]C[-1]+R1C1".AsSpan());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Row, Is.EqualTo(2));
        Assert.That(result.Column, Is.EqualTo(-1));
        Assert.That(result.IsRelativeRow, Is.True);
        Assert.That(result.IsRelativeColumn, Is.True);
        Assert.That(result.Text, Is.EqualTo("R[2]C[-1]"));
        Assert.That(result.CharactersConsumed, Is.EqualTo(9));
    }

    [Test]
    public void Parse_RejectsMissingMarkersInvalidBracketsTrailingCharactersAndOutOfRangeValues()
    {
        XlsxR1C1ReferenceResult empty = XlsxR1C1ReferenceConverter.Parse("");
        XlsxR1C1ReferenceResult missingRowMarker = XlsxR1C1ReferenceConverter.Parse("A1");
        XlsxR1C1ReferenceResult missingColumnMarker = XlsxR1C1ReferenceConverter.Parse("R1");
        XlsxR1C1ReferenceResult emptyBracket = XlsxR1C1ReferenceConverter.Parse("R[]C");
        XlsxR1C1ReferenceResult openBracket = XlsxR1C1ReferenceConverter.Parse("R[1C1");
        XlsxR1C1ReferenceResult trailing = XlsxR1C1ReferenceConverter.Parse("R1C1:R2C2");
        XlsxR1C1ReferenceResult zeroRow = XlsxR1C1ReferenceConverter.Parse("R0C1");
        XlsxR1C1ReferenceResult highRow = XlsxR1C1ReferenceConverter.Parse("R1048577C1");
        XlsxR1C1ReferenceResult zeroColumn = XlsxR1C1ReferenceConverter.Parse("R1C0");
        XlsxR1C1ReferenceResult highColumn = XlsxR1C1ReferenceConverter.Parse("R1C16385");
        XlsxR1C1ReferenceResult highRelativeColumn = XlsxR1C1ReferenceConverter.Parse("RC[-16385]");

        Assert.That(empty.IsValid, Is.False);
        Assert.That(missingRowMarker.IsValid, Is.False);
        Assert.That(missingColumnMarker.IsValid, Is.False);
        Assert.That(emptyBracket.IsValid, Is.False);
        Assert.That(openBracket.IsValid, Is.False);
        Assert.That(trailing.IsValid, Is.False);
        Assert.That(zeroRow.IsValid, Is.False);
        Assert.That(highRow.IsValid, Is.False);
        Assert.That(zeroColumn.IsValid, Is.False);
        Assert.That(highColumn.IsValid, Is.False);
        Assert.That(highRelativeColumn.IsValid, Is.False);
        Assert.That(HasDiagnostic(empty, "r1c1.reference.empty"), Is.True);
        Assert.That(HasDiagnostic(missingRowMarker, "r1c1.reference.rowMarker"), Is.True);
        Assert.That(HasDiagnostic(missingColumnMarker, "r1c1.reference.columnMarker"), Is.True);
        Assert.That(HasDiagnostic(emptyBracket, "r1c1.reference.row"), Is.True);
        Assert.That(HasDiagnostic(openBracket, "r1c1.reference.invalid"), Is.True);
        Assert.That(HasDiagnostic(trailing, "r1c1.reference.invalid"), Is.True);
        Assert.That(HasDiagnostic(zeroRow, "r1c1.reference.rowRange"), Is.True);
        Assert.That(HasDiagnostic(highRow, "r1c1.reference.rowRange"), Is.True);
        Assert.That(HasDiagnostic(zeroColumn, "r1c1.reference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(highColumn, "r1c1.reference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(highRelativeColumn, "r1c1.reference.columnRange"), Is.True);
    }

    [Test]
    public void Format_WritesAbsoluteRelativeAndCurrentAxisReferences()
    {
        AssertFormat(CreateReference(1, 1, true, true, false, false), "R1C1");
        AssertFormat(CreateReference(1048576, 16384, true, true, false, false), "R1048576C16384");
        AssertFormat(CreateReference(2, -1, true, true, true, true), "R[2]C[-1]");
        AssertFormat(CreateReference(0, 0, false, false, true, true), "RC");
        AssertFormat(CreateReference(-1, 0, true, false, true, true), "R[-1]C");
        AssertFormat(CreateReference(0, 3, false, true, true, true), "RC[3]");

        XlsxR1C1ReferenceResult invalid = XlsxR1C1ReferenceConverter.Format(CreateReference(0, 1, true, true, false, false));

        Assert.That(invalid.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalid, "r1c1.reference.range"), Is.True);
    }

    [Test]
    public void Format_WritesIntoSpanAndReportsSmallBuffers()
    {
        XlsxR1C1ReferenceValue reference = CreateReference(1048576, 16384, true, true, false, false);
        char[] buffer = new char[14];
        XlsxR1C1ReferenceResult result = XlsxR1C1ReferenceConverter.Format(reference, buffer.AsSpan());
        char[] smallBuffer = new char[13];
        XlsxR1C1ReferenceResult small = XlsxR1C1ReferenceConverter.Format(reference, smallBuffer.AsSpan());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Text, Is.EqualTo("R1048576C16384"));
        Assert.That(result.CharactersWritten, Is.EqualTo(14));
        Assert.That(new string(buffer), Is.EqualTo("R1048576C16384"));
        Assert.That(small.IsValid, Is.False);
        Assert.That(small.BufferTooSmall, Is.True);
        Assert.That(HasDiagnostic(small, "r1c1.reference.bufferTooSmall"), Is.True);
    }

    static void AssertReference(string text, int row, int column, bool hasRow, bool hasColumn, bool relativeRow, bool relativeColumn, string normalized)
    {
        XlsxR1C1ReferenceResult result = XlsxR1C1ReferenceConverter.Parse(text);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Row, Is.EqualTo(row));
        Assert.That(result.Column, Is.EqualTo(column));
        Assert.That(result.HasRow, Is.EqualTo(hasRow));
        Assert.That(result.HasColumn, Is.EqualTo(hasColumn));
        Assert.That(result.IsRelativeRow, Is.EqualTo(relativeRow));
        Assert.That(result.IsRelativeColumn, Is.EqualTo(relativeColumn));
        Assert.That(result.Reference.Row, Is.EqualTo(row));
        Assert.That(result.Reference.Column, Is.EqualTo(column));
        Assert.That(result.Reference.HasRow, Is.EqualTo(hasRow));
        Assert.That(result.Reference.HasColumn, Is.EqualTo(hasColumn));
        Assert.That(result.Reference.IsRelativeRow, Is.EqualTo(relativeRow));
        Assert.That(result.Reference.IsRelativeColumn, Is.EqualTo(relativeColumn));
        Assert.That(result.Text, Is.EqualTo(normalized));
        Assert.That(result.Reference.Text, Is.EqualTo(normalized));
        Assert.That(result.CharactersConsumed, Is.EqualTo(text.Length));
    }

    static void AssertFormat(XlsxR1C1ReferenceValue reference, string text)
    {
        XlsxR1C1ReferenceResult result = XlsxR1C1ReferenceConverter.Format(reference);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Text, Is.EqualTo(text));
        Assert.That(result.Reference.Text, Is.EqualTo(text));
        Assert.That(result.CharactersWritten, Is.EqualTo(text.Length));
    }

    static XlsxR1C1ReferenceValue CreateReference(int row, int column, bool hasRow, bool hasColumn, bool relativeRow, bool relativeColumn)
    {
        XlsxR1C1ReferenceValue reference = new XlsxR1C1ReferenceValue();
        reference.Row = row;
        reference.Column = column;
        reference.HasRow = hasRow;
        reference.HasColumn = hasColumn;
        reference.IsRelativeRow = relativeRow;
        reference.IsRelativeColumn = relativeColumn;
        return reference;
    }

    static bool HasDiagnostic(XlsxR1C1ReferenceResult result, string code)
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
