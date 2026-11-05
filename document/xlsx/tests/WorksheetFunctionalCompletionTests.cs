using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetFunctionalCompletionTests
{
    [Test]
    public void StructuralGridOperations_KeepStoredDimensionSynchronized()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();
        Assert.That(operations.SetTextValue(worksheet, "A1", "first").IsValid, Is.True);
        Assert.That(operations.SetNumberValue(worksheet, "D5", 5d).IsValid, Is.True);
        Assert.That(operations.ApplyCalculatedDimension(worksheet).Reference, Is.EqualTo("A1:D5"));

        XlsxWorksheetRowEditResult insertedRows = operations.InsertRows(worksheet, 3, 2);
        XlsxWorksheetColumnEditResult insertedColumns = operations.InsertColumns(worksheet, 2, 1);

        Assert.That(insertedRows.IsValid, Is.True, Diagnostics(insertedRows.Validation));
        Assert.That(insertedColumns.IsValid, Is.True, Diagnostics(insertedColumns.Validation));
        Assert.That(worksheet.Dimension.Reference, Is.EqualTo("A1:E7"));
        Assert.That(operations.GetCell(worksheet, "E7")!.Value, Is.EqualTo("5"));

        XlsxWorksheetRowEditResult deletedRows = operations.DeleteRows(worksheet, 1, 1);
        XlsxWorksheetColumnEditResult deletedColumns = operations.DeleteColumns(worksheet, 1, 2);

        Assert.That(deletedRows.IsValid, Is.True, Diagnostics(deletedRows.Validation));
        Assert.That(deletedColumns.IsValid, Is.True, Diagnostics(deletedColumns.Validation));
        Assert.That(worksheet.HasDimension, Is.True);
        Assert.That(worksheet.Dimension.Reference, Is.EqualTo("C6"));
        Assert.That(operations.GetCellAt(worksheet, 6, 3)!.Value, Is.EqualTo("5"));
        Assert.That(operations.GetDimensionRange(worksheet).Reference, Is.EqualTo("C6"));
    }

    [Test]
    public void DimensionOperations_HandleCallerClearedDimensionModels()
    {
        IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();
        XlsxWorksheetPart populated = new XlsxWorksheetPart();
        Assert.That(operations.SetTextValue(populated, "B2", "value").IsValid, Is.True);
        populated.Dimension = null!;

        XlsxWorksheetUsedRangeResult populatedResult = operations.ApplyCalculatedDimension(populated);

        Assert.That(populatedResult.IsValid, Is.True, Diagnostics(populatedResult.Validation));
        Assert.That(populated.HasDimension, Is.True);
        Assert.That(populated.Dimension, Is.Not.Null);
        Assert.That(populated.Dimension.Reference, Is.EqualTo("B2"));

        XlsxWorksheetPart empty = new XlsxWorksheetPart();
        empty.HasDimension = true;
        empty.Dimension = null!;

        XlsxWorksheetUsedRangeResult emptyResult = operations.ApplyCalculatedDimension(empty);

        Assert.That(emptyResult.IsValid, Is.True, Diagnostics(emptyResult.Validation));
        Assert.That(emptyResult.HasRange, Is.False);
        Assert.That(empty.HasDimension, Is.False);
        Assert.That(empty.Dimension, Is.Null);
    }

    static string Diagnostics(XlsxValidationResult validation)
    {
        string text = "";
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            text = text + diagnostic.Code + ": " + diagnostic.Message + "\n";
        }

        return text;
    }
}
