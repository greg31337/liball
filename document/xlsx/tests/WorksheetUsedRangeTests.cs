using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetUsedRangeTests
{
    [Test]
    public void CalculateUsedRange_UsesExplicitAndImpliedAddressesAndIgnoresEmptyPlaceholders()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow firstRow = new XlsxWorksheetRow();
        firstRow.Cells.Add(CreateCell("A1", ""));
        firstRow.Cells.Add(CreateCell("", "implied-b1"));
        firstRow.Cells.Add(CreateFormulaCell("D3", "A1+B1"));
        XlsxWorksheetRow fourthRow = new XlsxWorksheetRow();
        fourthRow.RowIndex = 4;
        fourthRow.Cells.Add(CreateInlineCell("", "implied-a4"));
        fourthRow.Cells.Add(CreateCell("", ""));
        part.Rows.Add(firstRow);
        part.Rows.Add(fourthRow);

        XlsxWorksheetUsedRangeResult result = part.CalculateUsedRange();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasRange, Is.True);
        Assert.That(result.MinRow, Is.EqualTo(1));
        Assert.That(result.MinColumn, Is.EqualTo(1));
        Assert.That(result.MaxRow, Is.EqualTo(4));
        Assert.That(result.MaxColumn, Is.EqualTo(4));
        Assert.That(result.CellCount, Is.EqualTo(3));
        Assert.That(result.Reference, Is.EqualTo("A1:D4"));
    }

    [Test]
    public void CalculateUsedRange_IncludesStyledAndUnknownCells()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        XlsxWorksheetCell styled = CreateCell("C2", "");
        styled.HasStyle = true;
        styled.Style = 5;
        XlsxWorksheetCell unknown = CreateCell("F6", "");
        unknown.UnknownXml.Add("<ext />");
        row.Cells.Add(styled);
        row.Cells.Add(unknown);
        part.Rows.Add(row);

        XlsxWorksheetUsedRangeResult result = part.GetUsedRange();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reference, Is.EqualTo("C2:F6"));
        Assert.That(result.CellCount, Is.EqualTo(2));
    }

    [Test]
    public void ApplyCalculatedDimension_WritesDimensionReferenceAndRoundTrips()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        Assert.That(part.SetTextValue("A1", "start").IsValid, Is.True);
        Assert.That(part.SetNumberValue("C3", 3d).IsValid, Is.True);

        XlsxWorksheetUsedRangeResult result = part.ApplyCalculatedDimension();
        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reference, Is.EqualTo("A1:C3"));
        Assert.That(part.HasDimension, Is.True);
        Assert.That(part.Dimension.Reference, Is.EqualTo("A1:C3"));
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<dimension ref=\"A1:C3\" />"));
        Assert.That(roundTrip.HasDimension, Is.True);
        Assert.That(roundTrip.Dimension.Reference, Is.EqualTo("A1:C3"));
    }

    [Test]
    public void ApplyCalculatedDimension_ClearsDimensionForEmptyUsedRange()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.HasDimension = true;
        part.Dimension.Reference = "A1:C3";
        Assert.That(part.GetOrCreateCellByReference("A1").IsValid, Is.True);

        XlsxWorksheetUsedRangeResult result = part.ApplyCalculatedDimension();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasRange, Is.False);
        Assert.That(result.Reference, Is.EqualTo(""));
        Assert.That(part.HasDimension, Is.False);
        Assert.That(part.Dimension.Reference, Is.EqualTo(""));
    }

    [Test]
    public void GetDimensionRange_ParsesStoredSingleCellAndRangeDimensions()
    {
        XlsxWorksheetPart single = new XlsxWorksheetPart();
        single.HasDimension = true;
        single.Dimension.Reference = "C3";
        XlsxWorksheetPart range = new XlsxWorksheetPart();
        range.HasDimension = true;
        range.Dimension.Reference = "B2:D5";

        XlsxWorksheetUsedRangeResult singleResult = single.GetDimensionRange();
        XlsxWorksheetUsedRangeResult rangeResult = range.GetDimensionRange();

        Assert.That(singleResult.IsValid, Is.True);
        Assert.That(singleResult.Reference, Is.EqualTo("C3"));
        Assert.That(singleResult.MinRow, Is.EqualTo(3));
        Assert.That(singleResult.MaxRow, Is.EqualTo(3));
        Assert.That(rangeResult.IsValid, Is.True);
        Assert.That(rangeResult.Reference, Is.EqualTo("B2:D5"));
        Assert.That(rangeResult.MinColumn, Is.EqualTo(2));
        Assert.That(rangeResult.MaxColumn, Is.EqualTo(4));
    }

    [Test]
    public void CalculateUsedRange_ReportsInvalidReferencesAndNullRows()
    {
        XlsxWorksheetPart invalidReferencePart = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.Cells.Add(CreateCell("XFE1", "bad"));
        invalidReferencePart.Rows.Add(row);
        XlsxWorksheetPart nullRowsPart = new XlsxWorksheetPart();
        nullRowsPart.Rows = null!;
        XlsxWorksheetPart nullCellsPart = new XlsxWorksheetPart();
        XlsxWorksheetRow nullCellsRow = new XlsxWorksheetRow();
        nullCellsRow.Cells = null!;
        nullCellsPart.Rows.Add(nullCellsRow);

        XlsxWorksheetUsedRangeResult invalidReference = invalidReferencePart.CalculateUsedRange();
        XlsxWorksheetUsedRangeResult nullRows = nullRowsPart.CalculateUsedRange();
        XlsxWorksheetUsedRangeResult nullCells = nullCellsPart.CalculateUsedRange();

        Assert.That(invalidReference.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidReference, "a1.cellReference.columnRange"), Is.True);
        Assert.That(invalidReference.HasRange, Is.False);
        Assert.That(nullRows.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullRows, "worksheetUsedRange.rowsNull"), Is.True);
        Assert.That(nullCells.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullCells, "worksheetUsedRange.cellsNull"), Is.True);
    }

    static XlsxWorksheetCell CreateCell(string reference, string value)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.Value = value;
        return cell;
    }

    static XlsxWorksheetCell CreateFormulaCell(string reference, string formula)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.Formula = formula;
        return cell;
    }

    static XlsxWorksheetCell CreateInlineCell(string reference, string text)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.Type = "inlineStr";
        cell.InlineStringText = text;
        return cell;
    }

    static bool HasDiagnostic(XlsxWorksheetUsedRangeResult result, string code)
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
