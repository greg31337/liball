using System;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetCellAccessTests
{
    [Test]
    public void GetCell_UsesA1ReferencesAndImpliedRowColumnPositions()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow firstRow = new XlsxWorksheetRow();
        XlsxWorksheetRow secondRow = new XlsxWorksheetRow();
        secondRow.RowIndex = 4;
        firstRow.Cells.Add(CreateCell("", "implied-a1"));
        firstRow.Cells.Add(CreateCell("", "implied-b1"));
        secondRow.Cells.Add(CreateCell("", "implied-a4"));
        secondRow.Cells.Add(CreateCell("D4", "explicit-d4"));
        part.Rows.Add(firstRow);
        part.Rows.Add(secondRow);

        Assert.That(part.GetRow(1), Is.SameAs(firstRow));
        Assert.That(part.GetRow(4), Is.SameAs(secondRow));
        Assert.That(part.GetCell("A1")!.Value, Is.EqualTo("implied-a1"));
        Assert.That(part.GetCellAt(1, 2)!.Value, Is.EqualTo("implied-b1"));
        Assert.That(part.GetCell("A4")!.Value, Is.EqualTo("implied-a4"));
        Assert.That(part.GetCell("$D$4")!.Value, Is.EqualTo("explicit-d4"));
        Assert.That(part.ContainsCell("D4"), Is.True);
        Assert.That(part.GetCell("bad"), Is.Null);
        Assert.That(part.GetCellAt(0, 1), Is.Null);
    }

    [Test]
    public void GetOrCreateCell_CreatesRowsCellsAndKeepsAddressOrder()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetCellAccessResult c3 = part.GetOrCreateCellByReference("C3");
        XlsxWorksheetCellAccessResult a1 = part.GetOrCreateCell(1, 1);
        XlsxWorksheetCellAccessResult b1 = part.GetOrCreateCellByReference("B1");
        XlsxWorksheetCellAccessResult existing = part.GetOrCreateCellByReference("C3");

        Assert.That(c3.IsValid, Is.True);
        Assert.That(c3.CreatedRow, Is.True);
        Assert.That(c3.CreatedCell, Is.True);
        Assert.That(a1.IsValid, Is.True);
        Assert.That(b1.IsValid, Is.True);
        Assert.That(existing.IsValid, Is.True);
        Assert.That(existing.CreatedCell, Is.False);
        Assert.That(existing.Cell, Is.SameAs(c3.Cell));
        Assert.That(part.Rows.Count, Is.EqualTo(2));
        Assert.That(part.Rows[0].RowIndex, Is.EqualTo(1));
        Assert.That(part.Rows[0].Cells[0].Reference, Is.EqualTo("A1"));
        Assert.That(part.Rows[0].Cells[1].Reference, Is.EqualTo("B1"));
        Assert.That(part.Rows[1].RowIndex, Is.EqualTo(3));
        Assert.That(part.Rows[1].Cells[0].Reference, Is.EqualTo("C3"));
    }

    [Test]
    public void SetCell_ReplacesExistingAddressWithoutDuplicatingCell()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetCell first = CreateCell("B2", "first");
        XlsxWorksheetCell replacement = CreateCell("", "replacement");

        XlsxWorksheetCellAccessResult firstResult = part.SetCellByReference("B2", first);
        XlsxWorksheetCellAccessResult replacementResult = part.SetCell(2, 2, replacement);

        Assert.That(firstResult.IsValid, Is.True);
        Assert.That(replacementResult.IsValid, Is.True);
        Assert.That(replacementResult.Cell, Is.SameAs(replacement));
        Assert.That(replacement.Reference, Is.EqualTo("B2"));
        Assert.That(part.Rows.Count, Is.EqualTo(1));
        Assert.That(part.Rows[0].Cells.Count, Is.EqualTo(1));
        Assert.That(part.GetCell("B2")!.Value, Is.EqualTo("replacement"));
    }

    [Test]
    public void SetCellValueByReference_WritesTypedValuesAndRoundTrips()
    {
        DateTime date = new DateTime(2026, 8, 9, 10, 11, 12, DateTimeKind.Utc);
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        Assert.That(part.SetNumberValue("A1", 12.5d).IsValid, Is.True);
        Assert.That(part.SetTextValue("B1", "text").IsValid, Is.True);
        Assert.That(part.SetInlineStringValue("C1", "inline").IsValid, Is.True);
        Assert.That(part.SetBooleanValue("D1", true).IsValid, Is.True);
        Assert.That(part.SetSharedStringValue("E1", 4u).IsValid, Is.True);
        Assert.That(part.SetErrorValue("F1", "#N/A").IsValid, Is.True);
        Assert.That(part.SetDateTimeValue("G1", date).IsValid, Is.True);
        Assert.That(part.SetFormulaCachedValue("H1", "A1*2", "25", "n").IsValid, Is.True);

        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(roundTrip.IsValid, Is.True);
        AssertCell(roundTrip.GetCell("A1"), "n", "12.5", "", "");
        AssertCell(roundTrip.GetCell("B1"), "str", "text", "", "");
        AssertCell(roundTrip.GetCell("C1"), "inlineStr", "", "", "inline");
        AssertCell(roundTrip.GetCell("D1"), "b", "1", "", "");
        AssertCell(roundTrip.GetCell("E1"), "s", "4", "", "");
        AssertCell(roundTrip.GetCell("F1"), "e", "#N/A", "", "");
        AssertCell(roundTrip.GetCell("G1"), "d", XlsxXmlValueWriter.WriteDateTime(date), "", "");
        AssertCell(roundTrip.GetCell("H1"), "n", "25", "A1*2", "");
    }

    [Test]
    public void CellAccessMethods_ReportInvalidInputs()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetCellAccessResult invalidReference = part.GetOrCreateCellByReference("XFE1");
        XlsxWorksheetCellAccessResult invalidCoordinate = part.GetOrCreateCell(0, 0);
        XlsxWorksheetCellAccessResult? nullCell = part.SetCellByReference("A1", null!);
        XlsxWorksheetCellAccessResult? nullValue = part.SetCellValueByReference("A1", null!);
        part.Rows = null!;
        XlsxWorksheetCellAccessResult nullRows = part.GetOrCreateCellByReference("A1");

        Assert.That(invalidReference.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidReference, "a1.cellReference.columnRange"), Is.True);
        Assert.That(invalidCoordinate.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidCoordinate, "worksheetCell.rowRange"), Is.True);
        Assert.That(HasDiagnostic(invalidCoordinate, "worksheetCell.columnRange"), Is.True);
        Assert.That(nullCell.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullCell, "worksheetCell.cellNull"), Is.True);
        Assert.That(nullValue.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullValue, "cellValue.valueNull"), Is.True);
        Assert.That(nullRows.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullRows, "worksheetCell.rowsNull"), Is.True);
    }

    static XlsxWorksheetCell CreateCell(string reference, string value)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.Value = value;
        return cell;
    }

    static void AssertCell(XlsxWorksheetCell? cell, string type, string value, string formula, string inlineText)
    {
        Assert.That(cell, Is.Not.Null);
        Assert.That(cell!.Type, Is.EqualTo(type));
        Assert.That(cell.Value, Is.EqualTo(value));
        Assert.That(cell.Formula, Is.EqualTo(formula));
        Assert.That(cell.InlineStringText, Is.EqualTo(inlineText));
    }

    static bool HasDiagnostic(XlsxWorksheetCellAccessResult result, string code)
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
