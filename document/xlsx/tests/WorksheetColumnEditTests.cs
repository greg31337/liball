using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetColumnEditTests
{
    [Test]
    public void InsertColumns_ShiftsCellsAndColumnMetadataRight()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetTextValue("A1", "left");
        part.SetTextValue("B1", "middle");
        part.SetNumberValue("D2", 4d);
        part.Columns.Add(CreateColumn(2, 3, 20d, 5u, true));

        XlsxWorksheetColumnEditResult result = part.InsertColumns(2, 2);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.MovedCellCount, Is.EqualTo(2));
        Assert.That(result.UpdatedCellReferenceCount, Is.EqualTo(2));
        Assert.That(result.MovedColumnCount, Is.EqualTo(2));
        Assert.That(part.GetCell("A1")!.Value, Is.EqualTo("left"));
        Assert.That(part.GetCell("B1"), Is.Null);
        Assert.That(part.GetCell("D1")!.Value, Is.EqualTo("middle"));
        Assert.That(part.GetCell("D1")!.Reference, Is.EqualTo("D1"));
        Assert.That(part.GetCell("F2")!.Value, Is.EqualTo("4"));
        Assert.That(part.GetCell("F2")!.Reference, Is.EqualTo("F2"));
        Assert.That(part.Columns.Count, Is.EqualTo(1));
        Assert.That(part.Columns[0].Min, Is.EqualTo(4));
        Assert.That(part.Columns[0].Max, Is.EqualTo(5));
        Assert.That(part.Columns[0].Width, Is.EqualTo(20d).Within(0.0000001d));
        Assert.That(part.Columns[0].Style, Is.EqualTo(5u));
        Assert.That(part.Columns[0].Hidden, Is.True);
    }

    [Test]
    public void DeleteColumns_RemovesDeletedCellsAndShiftsFollowingCellsLeft()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetTextValue("A1", "left");
        part.SetTextValue("B1", "delete");
        part.SetTextValue("D1", "move");
        part.SetNumberValue("E2", 8d);
        part.Columns.Add(CreateColumn(2, 3, 18d, 4u, true));
        part.Columns.Add(CreateColumn(5, 5, 11d, 0u, false));

        XlsxWorksheetColumnEditResult result = part.DeleteColumns(2, 2);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.RemovedCellCount, Is.EqualTo(1));
        Assert.That(result.MovedCellCount, Is.EqualTo(2));
        Assert.That(result.RemovedColumnCount, Is.EqualTo(2));
        Assert.That(result.MovedColumnCount, Is.EqualTo(1));
        Assert.That(part.GetCell("A1")!.Value, Is.EqualTo("left"));
        Assert.That(part.GetCell("B1")!.Value, Is.EqualTo("move"));
        Assert.That(part.GetCell("B1")!.Reference, Is.EqualTo("B1"));
        Assert.That(part.GetCell("C2")!.Value, Is.EqualTo("8"));
        Assert.That(part.GetCell("C2")!.Reference, Is.EqualTo("C2"));
        Assert.That(part.GetCell("D1"), Is.Null);
        Assert.That(part.Columns.Count, Is.EqualTo(1));
        Assert.That(part.Columns[0].Min, Is.EqualTo(3));
        Assert.That(part.Columns[0].Max, Is.EqualTo(3));
        Assert.That(part.Columns[0].Width, Is.EqualTo(11d).Within(0.0000001d));
    }

    [Test]
    public void ColumnEdits_PreserveAbsoluteCellReferenceMarkers()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        row.RowIndex = 4u;
        cell.Reference = "$D$4";
        cell.Value = "x";
        row.Cells.Add(cell);
        part.Rows.Add(row);

        XlsxWorksheetColumnEditResult insert = part.InsertColumns(2, 1);
        XlsxWorksheetColumnEditResult delete = part.DeleteColumns(1, 2);

        Assert.That(insert.IsValid, Is.True);
        Assert.That(delete.IsValid, Is.True);
        Assert.That(part.GetCell("$C$4")!.Value, Is.EqualTo("x"));
        Assert.That(part.GetCell("$C$4")!.Reference, Is.EqualTo("$C$4"));
    }

    [Test]
    public void ColumnEdits_ReportInvalidInputsWithoutMovingCells()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetTextValue("XFD1", "last");
        XlsxWorksheetPart nullRows = new XlsxWorksheetPart();
        nullRows.Rows = null!;
        XlsxWorksheetPart nullColumns = new XlsxWorksheetPart();
        nullColumns.Columns = null!;
        XlsxWorksheetPart nullColumn = new XlsxWorksheetPart();
        nullColumn.Columns.Add(null!);

        XlsxWorksheetColumnEditResult badColumn = part.InsertColumns(0, 1);
        XlsxWorksheetColumnEditResult badCount = part.DeleteColumns(1, 0);
        XlsxWorksheetColumnEditResult badRange = part.DeleteColumns(16384, 2);
        XlsxWorksheetColumnEditResult shiftOutside = part.InsertColumns(1, 1);
        XlsxWorksheetColumnEditResult rowsNull = nullRows.InsertColumns(1, 1);
        XlsxWorksheetColumnEditResult columnsNull = nullColumns.InsertColumns(1, 1);
        XlsxWorksheetColumnEditResult columnNull = nullColumn.DeleteColumns(1, 1);

        Assert.That(badColumn.IsValid, Is.False);
        Assert.That(badCount.IsValid, Is.False);
        Assert.That(badRange.IsValid, Is.False);
        Assert.That(shiftOutside.IsValid, Is.False);
        Assert.That(rowsNull.IsValid, Is.False);
        Assert.That(columnsNull.IsValid, Is.False);
        Assert.That(columnNull.IsValid, Is.False);
        Assert.That(HasDiagnostic(badColumn, "worksheetColumns.columnRange"), Is.True);
        Assert.That(HasDiagnostic(badCount, "worksheetColumns.countRange"), Is.True);
        Assert.That(HasDiagnostic(badRange, "worksheetColumns.rangeOutsideWorksheet"), Is.True);
        Assert.That(HasDiagnostic(shiftOutside, "worksheetColumns.shiftOutsideWorksheet"), Is.True);
        Assert.That(HasDiagnostic(rowsNull, "worksheetColumns.rowsNull"), Is.True);
        Assert.That(HasDiagnostic(columnsNull, "worksheetColumns.columnsNull"), Is.True);
        Assert.That(HasDiagnostic(columnNull, "columnMetadataStore.columnNull"), Is.True);
        Assert.That(part.GetCell("XFD1")!.Value, Is.EqualTo("last"));
    }

    static XlsxWorksheetColumn CreateColumn(uint min, uint max, double width, uint style, bool hidden)
    {
        XlsxWorksheetColumn column = new XlsxWorksheetColumn();
        column.Min = min;
        column.Max = max;
        column.Width = width;
        column.HasWidth = true;
        if (style > 0)
        {
            column.Style = style;
            column.HasStyle = true;
        }

        column.Hidden = hidden;
        column.HasHidden = true;
        return column;
    }

    static bool HasDiagnostic(XlsxWorksheetColumnEditResult result, string code)
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
