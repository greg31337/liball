using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class A1ColumnNameConverterTests
{
    [Test]
    public void Parse_AcceptsCaseInsensitiveColumnNamesAndNormalizesText()
    {
        AssertColumn("A", 1, "A");
        AssertColumn("z", 26, "Z");
        AssertColumn("AA", 27, "AA");
        AssertColumn("az", 52, "AZ");
        AssertColumn("BA", 53, "BA");
        AssertColumn("ZZ", 702, "ZZ");
        AssertColumn("AAA", 703, "AAA");
        AssertColumn("xfd", 16384, "XFD");
    }

    [Test]
    public void ParseLeading_ConsumesOnlyColumnPrefix()
    {
        XlsxA1ColumnNameResult result = XlsxA1ColumnNameConverter.ParseLeading("bc12".AsSpan());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Column, Is.EqualTo(55));
        Assert.That(result.Text, Is.EqualTo("BC"));
        Assert.That(result.CharactersConsumed, Is.EqualTo(2));
    }

    [Test]
    public void Parse_RejectsEmptyInvalidTrailingAndOutOfRangeNames()
    {
        XlsxA1ColumnNameResult empty = XlsxA1ColumnNameConverter.Parse("");
        XlsxA1ColumnNameResult invalidStart = XlsxA1ColumnNameConverter.Parse("1A");
        XlsxA1ColumnNameResult trailing = XlsxA1ColumnNameConverter.Parse("A1");
        XlsxA1ColumnNameResult outOfRange = XlsxA1ColumnNameConverter.Parse("XFE");

        Assert.That(empty.IsValid, Is.False);
        Assert.That(invalidStart.IsValid, Is.False);
        Assert.That(trailing.IsValid, Is.False);
        Assert.That(outOfRange.IsValid, Is.False);
        Assert.That(HasDiagnostic(empty, "a1.columnName.empty"), Is.True);
        Assert.That(HasDiagnostic(invalidStart, "a1.columnName.invalid"), Is.True);
        Assert.That(HasDiagnostic(trailing, "a1.columnName.invalid"), Is.True);
        Assert.That(HasDiagnostic(outOfRange, "a1.columnName.range"), Is.True);
    }

    [Test]
    public void Format_WritesColumnNamesAndReportsInvalidIndexes()
    {
        AssertFormat(1, "A");
        AssertFormat(26, "Z");
        AssertFormat(27, "AA");
        AssertFormat(702, "ZZ");
        AssertFormat(703, "AAA");
        AssertFormat(16384, "XFD");

        XlsxA1ColumnNameResult zero = XlsxA1ColumnNameConverter.Format(0);
        XlsxA1ColumnNameResult pastLimit = XlsxA1ColumnNameConverter.Format(16385);

        Assert.That(zero.IsValid, Is.False);
        Assert.That(pastLimit.IsValid, Is.False);
        Assert.That(HasDiagnostic(zero, "a1.columnName.range"), Is.True);
        Assert.That(HasDiagnostic(pastLimit, "a1.columnName.range"), Is.True);
        Assert.That(XlsxXmlValueWriter.WriteColumnName(16384), Is.EqualTo("XFD"));
        Assert.That(XlsxXmlValueWriter.WriteColumnName(16385), Is.EqualTo(""));
    }

    [Test]
    public void Format_WritesIntoSpanAndReportsSmallBuffers()
    {
        char[] buffer = new char[3];
        XlsxA1ColumnNameResult result = XlsxA1ColumnNameConverter.Format(16384, buffer.AsSpan());
        char[] smallBuffer = new char[2];
        XlsxA1ColumnNameResult small = XlsxA1ColumnNameConverter.Format(16384, smallBuffer.AsSpan());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Text, Is.EqualTo("XFD"));
        Assert.That(result.CharactersWritten, Is.EqualTo(3));
        Assert.That(new string(buffer), Is.EqualTo("XFD"));
        Assert.That(small.IsValid, Is.False);
        Assert.That(small.BufferTooSmall, Is.True);
        Assert.That(HasDiagnostic(small, "a1.columnName.bufferTooSmall"), Is.True);
    }

    static void AssertColumn(string text, int column, string normalized)
    {
        XlsxA1ColumnNameResult result = XlsxA1ColumnNameConverter.Parse(text);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Column, Is.EqualTo(column));
        Assert.That(result.Text, Is.EqualTo(normalized));
        Assert.That(result.CharactersConsumed, Is.EqualTo(text.Length));
    }

    static void AssertFormat(int column, string text)
    {
        XlsxA1ColumnNameResult result = XlsxA1ColumnNameConverter.Format(column);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Column, Is.EqualTo(column));
        Assert.That(result.Text, Is.EqualTo(text));
        Assert.That(result.CharactersWritten, Is.EqualTo(text.Length));
    }

    static bool HasDiagnostic(XlsxA1ColumnNameResult result, string code)
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
