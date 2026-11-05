using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetRangeSortTests
{
    [Test]
    public void SortRowsByColumn_OrdersNumericKeysAndMovesRangeCellsTogether()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        AddRow(part, 1, "third", 3d, "c");
        AddRow(part, 2, "first", 1d, "a");
        AddRow(part, 3, "second", 2d, "b");
        part.GetCell("A2")!.Style = 9u;
        part.GetCell("A2")!.HasStyle = true;
        part.SetTextValue("D1", "outside");

        XlsxWorksheetRangeResult result = part.GetRange("A1:C3").SortRowsByColumn(2, true);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.SortedRowCount, Is.EqualTo(3));
        Assert.That(part.GetCell("A1")!.Value, Is.EqualTo("first"));
        Assert.That(part.GetCell("B1")!.Value, Is.EqualTo("1"));
        Assert.That(part.GetCell("C1")!.Value, Is.EqualTo("a"));
        Assert.That(part.GetCell("A1")!.Style, Is.EqualTo(9u));
        Assert.That(part.GetCell("A2")!.Value, Is.EqualTo("second"));
        Assert.That(part.GetCell("B2")!.Value, Is.EqualTo("2"));
        Assert.That(part.GetCell("A3")!.Value, Is.EqualTo("third"));
        Assert.That(part.GetCell("B3")!.Value, Is.EqualTo("3"));
        Assert.That(part.GetCell("D1")!.Value, Is.EqualTo("outside"));
    }

    [Test]
    public void SortRowsByRelativeColumn_OrdersTextDescendingAndKeepsBlanksLast()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetTextValue("A1", "row1");
        part.SetTextValue("B1", "beta");
        part.SetTextValue("A2", "row2");
        part.SetTextValue("A3", "row3");
        part.SetTextValue("B3", "alpha");

        XlsxWorksheetRangeResult result = part.GetRange("A1:B3").SortRowsByRelativeColumn(2, false);

        Assert.That(result.IsValid, Is.True);
        Assert.That(part.GetCell("A1")!.Value, Is.EqualTo("row1"));
        Assert.That(part.GetCell("B1")!.Value, Is.EqualTo("beta"));
        Assert.That(part.GetCell("A2")!.Value, Is.EqualTo("row3"));
        Assert.That(part.GetCell("B2")!.Value, Is.EqualTo("alpha"));
        Assert.That(part.GetCell("A3")!.Value, Is.EqualTo("row2"));
        Assert.That(part.GetCell("B3")!.Value, Is.EqualTo(""));
    }

    [Test]
    public void SortByColumn_ReportsInvalidSortColumns()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetTextValue("A1", "x");

        XlsxWorksheetRangeResult outside = part.GetRange("A1:B2").SortByColumn(3, true);
        XlsxWorksheetRangeResult badColumn = part.GetRange("A1:B2").SortRowsByColumn(0, true);
        XlsxWorksheetRangeResult badRelative = part.GetRange("A1:B2").SortRowsByRelativeColumn(3, true);
        XlsxWorksheetRangeResult badRange = part.GetRange("bad").SortRowsByColumn(1, true);

        Assert.That(outside.IsValid, Is.False);
        Assert.That(badColumn.IsValid, Is.False);
        Assert.That(badRelative.IsValid, Is.False);
        Assert.That(badRange.IsValid, Is.False);
        Assert.That(HasDiagnostic(outside, "worksheetRange.sortColumnOutsideRange"), Is.True);
        Assert.That(HasDiagnostic(badColumn, "worksheetRange.sortColumnRange"), Is.True);
        Assert.That(HasDiagnostic(badRelative, "worksheetRange.sortColumnOutsideRange"), Is.True);
        Assert.That(HasDiagnostic(badRange, "worksheetRange.referenceInvalid"), Is.True);
    }

    static void AddRow(XlsxWorksheetPart part, int row, string label, double key, string value)
    {
        part.SetTextValue("A" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), label);
        part.SetNumberValue("B" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), key);
        part.SetTextValue("C" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), value);
    }

    static bool HasDiagnostic(XlsxWorksheetRangeResult result, string code)
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
