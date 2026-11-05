using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetTableApiTests
{
    [Test]
    public void AddTable_CreatesColumnsHeaderCellsReferencesAndWorksheetXml()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("A1", "Region");
        worksheet.SetTextValue("B1", "Amount");

        XlsxWorksheetTableResult result = worksheet.AddTable("A1:B3", "Sales");
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(worksheetWrite.Xml);
        XlsxTablePartWriteResult tableWrite = new XlsxTablePartWriter().WriteString(result.Table);
        XlsxTablePart tableRoundTrip = new XlsxTablePartReader().ReadString(tableWrite.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(worksheet.Tables.Count, Is.EqualTo(1));
        Assert.That(worksheet.TableReferences.Count, Is.EqualTo(1));
        Assert.That(result.Table!.Id, Is.EqualTo(1));
        Assert.That(result.Table.Name, Is.EqualTo("Sales"));
        Assert.That(result.Table.DisplayName, Is.EqualTo("Sales"));
        Assert.That(result.Table.Reference, Is.EqualTo("A1:B3"));
        Assert.That(result.Table.Columns.Count, Is.EqualTo(2));
        Assert.That(result.Table.Columns[0].Name, Is.EqualTo("Region"));
        Assert.That(result.Table.Columns[1].Name, Is.EqualTo("Amount"));
        Assert.That(worksheet.GetCell("A1")!.Value, Is.EqualTo("Region"));
        Assert.That(worksheet.GetCell("B1")!.Value, Is.EqualTo("Amount"));
        Assert.That(worksheetWrite.IsValid, Is.True);
        Assert.That(worksheetWrite.Xml, Does.Contain("<tableParts count=\"1\">"));
        Assert.That(worksheetWrite.Xml, Does.Contain("<tablePart r:id=\"rIdTable1\" />"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.TableReferences.Count, Is.EqualTo(1));
        Assert.That(roundTrip.TableReferences[0].RelationshipId, Is.EqualTo("rIdTable1"));
        Assert.That(tableWrite.IsValid, Is.True);
        Assert.That(tableRoundTrip.IsValid, Is.True);
        Assert.That(tableRoundTrip.Columns.Count, Is.EqualTo(2));
    }

    [Test]
    public void ResizeRenameAndColumnManagement_UpdateTableDefinitionAndHeaderCells()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("A1", "Region");
        worksheet.SetTextValue("B1", "Amount");
        worksheet.AddTable("A1:B3", "Sales");

        XlsxWorksheetTableResult resize = worksheet.ResizeTable("Sales", "A1:C4");
        XlsxWorksheetTableResult renameTable = worksheet.RenameTable("Sales", "Orders");
        XlsxWorksheetTableResult renameColumn = worksheet.RenameTableColumn("Orders", "Column3", "Qty");
        XlsxWorksheetTableResult addColumn = worksheet.AddTableColumn("Orders", "Price");
        XlsxWorksheetTableResult deleteColumn = worksheet.DeleteTableColumn("Orders", "Amount");
        XlsxTablePart? table = worksheet.GetTable("Orders");

        Assert.That(resize.IsValid, Is.True);
        Assert.That(resize.AddedColumnCount, Is.EqualTo(1));
        Assert.That(renameTable.IsValid, Is.True);
        Assert.That(renameColumn.IsValid, Is.True);
        Assert.That(addColumn.IsValid, Is.True);
        Assert.That(addColumn.AddedColumnCount, Is.EqualTo(1));
        Assert.That(deleteColumn.IsValid, Is.True);
        Assert.That(deleteColumn.RemovedColumnCount, Is.EqualTo(1));
        Assert.That(table!.Name, Is.EqualTo("Orders"));
        Assert.That(table.Reference, Is.EqualTo("A1:C4"));
        Assert.That(table.Columns.Count, Is.EqualTo(3));
        Assert.That(table.Columns[0].Name, Is.EqualTo("Region"));
        Assert.That(table.Columns[1].Name, Is.EqualTo("Qty"));
        Assert.That(table.Columns[2].Name, Is.EqualTo("Price"));
        Assert.That(worksheet.GetCell("A1")!.Value, Is.EqualTo("Region"));
        Assert.That(worksheet.GetCell("B1")!.Value, Is.EqualTo("Qty"));
        Assert.That(worksheet.GetCell("C1")!.Value, Is.EqualTo("Price"));
    }

    [Test]
    public void HeaderTotalsFormulasAndStyles_AreManaged()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("A1", "Region");
        worksheet.SetTextValue("B1", "Amount");
        worksheet.AddTable("A1:B4", "Sales");

        XlsxWorksheetTableResult calculated = worksheet.SetTableColumnFormula("Sales", "Amount", "A2*2");
        XlsxWorksheetTableResult totals = worksheet.SetTableTotalsRow("Sales", true);
        XlsxWorksheetTableResult label = worksheet.SetTableColumnTotalsRowLabel("Sales", "Region", "Total");
        XlsxWorksheetTableResult function = worksheet.SetTableColumnTotalsRowFunction("Sales", "Amount", "sum");
        XlsxWorksheetTableResult totalFormula = worksheet.SetTableColumnTotalsRowFormula("Sales", "Amount", "SUBTOTAL(109,[Amount])");
        XlsxWorksheetTableResult style = worksheet.SetTableStyleWithStripeOptions("Sales", "TableStyleMedium9", false, true);
        XlsxTablePart? table = worksheet.GetTable("Sales");

        Assert.That(calculated.IsValid, Is.True);
        Assert.That(calculated.FormulaCellCount, Is.EqualTo(3));
        Assert.That(totals.IsValid, Is.True);
        Assert.That(label.IsValid, Is.True);
        Assert.That(function.IsValid, Is.True);
        Assert.That(totalFormula.IsValid, Is.True);
        Assert.That(style.IsValid, Is.True);
        Assert.That(table!.Reference, Is.EqualTo("A1:B5"));
        Assert.That(table.TotalsRowShown, Is.True);
        Assert.That(table.Columns[1].CalculatedColumnFormula, Is.EqualTo("A2*2"));
        Assert.That(table.Columns[1].TotalsRowFunction, Is.EqualTo("sum"));
        Assert.That(table.Columns[1].TotalsRowFormula, Is.EqualTo("SUBTOTAL(109,[Amount])"));
        Assert.That(worksheet.GetCell("B2")!.Formula, Is.EqualTo("A2*2"));
        Assert.That(worksheet.GetCell("B4")!.Formula, Is.EqualTo("A2*2"));
        Assert.That(worksheet.GetCell("A5")!.Value, Is.EqualTo("Total"));
        Assert.That(worksheet.GetCell("B5")!.Formula, Is.EqualTo("SUBTOTAL(109,[Amount])"));
        Assert.That(table.HasStyleInfo, Is.True);
        Assert.That(table.StyleInfo.Name, Is.EqualTo("TableStyleMedium9"));
        Assert.That(table.StyleInfo.ShowRowStripes, Is.False);
        Assert.That(table.StyleInfo.ShowColumnStripes, Is.True);
    }

    [Test]
    public void RowAndColumnEdits_UpdateTableReferencesAndStoredFormulas()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.AddTable("A1:B4", "Sales");
        worksheet.SetTableColumnFormula("Sales", "Column2", "SUM(A2:A4)");

        XlsxWorksheetRowEditResult rowInsert = worksheet.InsertRows(2, 1);
        XlsxWorksheetColumnEditResult columnInsert = worksheet.InsertColumns(1, 1);
        XlsxTablePart? table = worksheet.GetTable("Sales");

        Assert.That(rowInsert.IsValid, Is.True);
        Assert.That(columnInsert.IsValid, Is.True);
        Assert.That(table!.Reference, Is.EqualTo("B1:C5"));
        Assert.That(table.Columns[1].CalculatedColumnFormula, Is.EqualTo("SUM(B3:B5)"));
        Assert.That(rowInsert.StructuralReferenceUpdateCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(columnInsert.StructuralReferenceUpdateCount, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void TableApi_ReportsInvalidNamesRangesDuplicatesAndMissingColumns()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetTableResult invalidName = worksheet.AddTable("A1:B2", "A1");
        XlsxWorksheetTableResult invalidRange = worksheet.AddTable("XFE1:XFE2", "BadRange");
        XlsxWorksheetTableResult valid = worksheet.AddTable("A1:B2", "Sales");
        XlsxWorksheetTableResult duplicateName = worksheet.AddTable("D1:E2", "Sales");
        XlsxWorksheetTableResult overlap = worksheet.AddTable("B1:C2", "Other");
        XlsxWorksheetTableResult duplicateColumn = worksheet.AddTableColumn("Sales", "Column2");
        XlsxWorksheetTableResult missingColumn = worksheet.RenameTableColumn("Sales", "Missing", "NewName");
        XlsxWorksheetTableResult missingTable = worksheet.SetTableStyle("Missing", "TableStyleMedium4");

        Assert.That(invalidName.IsValid, Is.False);
        Assert.That(invalidRange.IsValid, Is.False);
        Assert.That(valid.IsValid, Is.True);
        Assert.That(duplicateName.IsValid, Is.False);
        Assert.That(overlap.IsValid, Is.False);
        Assert.That(duplicateColumn.IsValid, Is.False);
        Assert.That(missingColumn.IsValid, Is.False);
        Assert.That(missingTable.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidName, "worksheetTable.nameCellReference"), Is.True);
        Assert.That(HasDiagnostic(invalidRange, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(duplicateName, "worksheetTable.nameDuplicate"), Is.True);
        Assert.That(HasDiagnostic(overlap, "worksheetTable.rangeOverlap"), Is.True);
        Assert.That(HasDiagnostic(duplicateColumn, "worksheetTable.columnNameDuplicate"), Is.True);
        Assert.That(HasDiagnostic(missingColumn, "worksheetTable.columnNotFound"), Is.True);
        Assert.That(HasDiagnostic(missingTable, "worksheetTable.notFound"), Is.True);
    }

    static bool HasDiagnostic(XlsxWorksheetTableResult result, string code)
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
