using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetRowEditTests
{
    [Test]
    public void InsertRows_ShiftsRowsCellsAndRowMetadataDown()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetTextValue("A1", "top");
        part.SetTextValue("A3", "middle");
        part.SetNumberValue("B5", 5d);
        part.GetRow(3)!.Height = 18d;
        part.GetRow(3)!.HasHeight = true;
        part.GetCell("A3")!.Style = 4u;
        part.GetCell("A3")!.HasStyle = true;

        XlsxWorksheetRowEditResult result = part.InsertRows(3, 2);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.MovedRowCount, Is.EqualTo(2));
        Assert.That(result.UpdatedCellReferenceCount, Is.EqualTo(2));
        Assert.That(part.GetCell("A1")!.Value, Is.EqualTo("top"));
        Assert.That(part.GetCell("A3"), Is.Null);
        Assert.That(part.GetCell("A5")!.Value, Is.EqualTo("middle"));
        Assert.That(part.GetCell("A5")!.Reference, Is.EqualTo("A5"));
        Assert.That(part.GetCell("A5")!.Style, Is.EqualTo(4u));
        Assert.That(part.GetCell("B7")!.Value, Is.EqualTo("5"));
        Assert.That(part.GetCell("B7")!.Reference, Is.EqualTo("B7"));
        Assert.That(part.GetRow(5)!.Height, Is.EqualTo(18d).Within(0.0000001d));
        Assert.That(part.Rows[0].RowIndex, Is.EqualTo(1));
        Assert.That(part.Rows[1].RowIndex, Is.EqualTo(5));
        Assert.That(part.Rows[2].RowIndex, Is.EqualTo(7));
    }

    [Test]
    public void DeleteRows_RemovesDeletedRowsAndShiftsFollowingRowsUp()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetTextValue("A1", "top");
        part.SetTextValue("A3", "delete");
        part.SetTextValue("A5", "move");
        part.SetNumberValue("C6", 6d);
        part.GetRow(5)!.Hidden = true;
        part.GetRow(5)!.HasHidden = true;

        XlsxWorksheetRowEditResult result = part.DeleteRows(2, 2);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.RemovedRowCount, Is.EqualTo(1));
        Assert.That(result.MovedRowCount, Is.EqualTo(2));
        Assert.That(result.UpdatedCellReferenceCount, Is.EqualTo(2));
        Assert.That(part.GetCell("A1")!.Value, Is.EqualTo("top"));
        Assert.That(part.GetCell("A3")!.Value, Is.EqualTo("move"));
        Assert.That(part.GetCell("A3")!.Reference, Is.EqualTo("A3"));
        Assert.That(part.GetCell("C4")!.Value, Is.EqualTo("6"));
        Assert.That(part.GetCell("C4")!.Reference, Is.EqualTo("C4"));
        Assert.That(part.GetCell("A5"), Is.Null);
        Assert.That(part.GetRow(3)!.Hidden, Is.True);
        Assert.That(part.Rows.Count, Is.EqualTo(3));
        Assert.That(part.Rows[0].RowIndex, Is.EqualTo(1));
        Assert.That(part.Rows[1].RowIndex, Is.EqualTo(3));
        Assert.That(part.Rows[2].RowIndex, Is.EqualTo(4));
    }

    [Test]
    public void RowEdits_PreserveAbsoluteCellReferenceMarkers()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        row.RowIndex = 4u;
        cell.Reference = "$B$4";
        cell.Value = "x";
        row.Cells.Add(cell);
        part.Rows.Add(row);

        XlsxWorksheetRowEditResult insert = part.InsertRows(2, 1);
        XlsxWorksheetRowEditResult delete = part.DeleteRows(1, 2);

        Assert.That(insert.IsValid, Is.True);
        Assert.That(delete.IsValid, Is.True);
        Assert.That(part.GetCell("$B$3")!.Value, Is.EqualTo("x"));
        Assert.That(part.GetCell("$B$3")!.Reference, Is.EqualTo("$B$3"));
    }

    [Test]
    public void RowEdits_ReportInvalidInputsWithoutMovingCells()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetTextValue("A1048576", "last");
        XlsxWorksheetPart nullRows = new XlsxWorksheetPart();
        nullRows.Rows = null!;
        XlsxWorksheetPart nullRow = new XlsxWorksheetPart();
        nullRow.Rows.Add(null!);

        XlsxWorksheetRowEditResult badRow = part.InsertRows(0, 1);
        XlsxWorksheetRowEditResult badCount = part.DeleteRows(1, 0);
        XlsxWorksheetRowEditResult badRange = part.DeleteRows(1048576, 2);
        XlsxWorksheetRowEditResult shiftOutside = part.InsertRows(1, 1);
        XlsxWorksheetRowEditResult rowsNull = nullRows.InsertRows(1, 1);
        XlsxWorksheetRowEditResult rowNull = nullRow.DeleteRows(1, 1);

        Assert.That(badRow.IsValid, Is.False);
        Assert.That(badCount.IsValid, Is.False);
        Assert.That(badRange.IsValid, Is.False);
        Assert.That(shiftOutside.IsValid, Is.False);
        Assert.That(rowsNull.IsValid, Is.False);
        Assert.That(rowNull.IsValid, Is.False);
        Assert.That(HasDiagnostic(badRow, "worksheetRows.rowRange"), Is.True);
        Assert.That(HasDiagnostic(badCount, "worksheetRows.countRange"), Is.True);
        Assert.That(HasDiagnostic(badRange, "worksheetRows.rangeOutsideWorksheet"), Is.True);
        Assert.That(HasDiagnostic(shiftOutside, "worksheetRows.shiftOutsideWorksheet"), Is.True);
        Assert.That(HasDiagnostic(rowsNull, "worksheetRows.rowsNull"), Is.True);
        Assert.That(HasDiagnostic(rowNull, "worksheetRows.rowNull"), Is.True);
        Assert.That(part.GetCell("A1048576")!.Value, Is.EqualTo("last"));
    }

    static bool HasDiagnostic(XlsxWorksheetRowEditResult result, string code)
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
