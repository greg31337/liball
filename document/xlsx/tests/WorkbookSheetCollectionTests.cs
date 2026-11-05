using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookSheetCollectionTests
{
    [Test]
    public void AddSheet_CreatesSheetMetadataRelationshipAndWorksheetPackagePart()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();

        XlsxWorkbookSheetOperationResult addResult = workbook.AddSheet("Data");
        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saveResult.Bytes, new XlsxReaderOptions());

        Assert.That(addResult.IsValid, Is.True);
        Assert.That(addResult.Sheet!.Name, Is.EqualTo("Data"));
        Assert.That(addResult.Sheet.SheetId, Is.EqualTo(2));
        Assert.That(addResult.Sheet.RelationshipId, Is.EqualTo("rId2"));
        Assert.That(workbook.WorkbookRelationships.GetRelationship("rId2"), Is.Not.Null);
        Assert.That(workbook.WorkbookRelationships.GetRelationship("rId2")!.Target, Is.EqualTo("worksheets/sheet2.xml"));
        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(package.GetEntry("/xl/worksheets/sheet2.xml"), Is.Not.Null);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook!.WorkbookPart.Sheets.Count, Is.EqualTo(2));
        Assert.That(openResult.Workbook.GetSheet("Data")!.RelationshipId, Is.EqualTo("rId2"));
    }

    [Test]
    public void RenameHideAndReorderSheet_UpdateWorkbookSheetCollection()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.AddSheet("Data");
        workbook.AddSheet("Archive");

        XlsxValidationResult renameResult = workbook.RenameSheet("Data", "Inputs");
        XlsxValidationResult hideResult = workbook.HideSheet("Inputs", "veryHidden");
        XlsxValidationResult reorderResult = workbook.ReorderSheet("Archive", 0);
        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes);

        Assert.That(renameResult.IsValid, Is.True);
        Assert.That(hideResult.IsValid, Is.True);
        Assert.That(reorderResult.IsValid, Is.True);
        Assert.That(workbook.GetSheet("Data"), Is.Null);
        Assert.That(workbook.GetSheet("Inputs")!.State, Is.EqualTo("veryHidden"));
        Assert.That(workbook.GetSheetAt(0)!.Name, Is.EqualTo("Archive"));
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook!.GetSheetAt(0)!.Name, Is.EqualTo("Archive"));
        Assert.That(openResult.Workbook.GetSheet("Inputs")!.State, Is.EqualTo("veryHidden"));
    }

    [Test]
    public void RemoveSheet_RemovesSheetAndWorkbookRelationship()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.AddSheet("Data");

        XlsxValidationResult removeResult = workbook.RemoveSheet("Sheet1");
        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes);

        Assert.That(removeResult.IsValid, Is.True);
        Assert.That(workbook.GetSheet("Sheet1"), Is.Null);
        Assert.That(workbook.WorkbookRelationships.GetRelationship("rId1"), Is.Null);
        Assert.That(workbook.WorkbookPart.Sheets.Count, Is.EqualTo(1));
        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook!.WorkbookPart.Sheets.Count, Is.EqualTo(1));
        Assert.That(openResult.Workbook.GetSheet("Data"), Is.Not.Null);
    }

    [Test]
    public void SheetOperations_ReportInvalidNamesStatesIndexesAndLastSheetRemoval()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();

        XlsxWorkbookSheetOperationResult duplicateAdd = workbook.AddSheet("Sheet1");
        XlsxWorkbookSheetOperationResult invalidAdd = workbook.AddSheet("Bad/Name");
        XlsxValidationResult missingRename = workbook.RenameSheet("Missing", "New");
        XlsxValidationResult duplicateRename = workbook.RenameSheet("Sheet1", "Sheet1");
        XlsxValidationResult badRename = workbook.RenameSheet("Sheet1", "Bad:Name");
        XlsxValidationResult badHide = workbook.HideSheet("Sheet1", "invisible");
        XlsxValidationResult badReorder = workbook.ReorderSheet("Sheet1", 3);
        XlsxValidationResult removeLast = workbook.RemoveSheet("Sheet1");

        Assert.That(duplicateAdd.IsValid, Is.False);
        Assert.That(HasDiagnostic(duplicateAdd.Validation, "workbook.sheet.nameDuplicate"), Is.True);
        Assert.That(invalidAdd.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidAdd.Validation, "workbook.sheet.nameInvalid"), Is.True);
        Assert.That(missingRename.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRename, "workbook.sheet.notFound"), Is.True);
        Assert.That(duplicateRename.IsValid, Is.True);
        Assert.That(badRename.IsValid, Is.False);
        Assert.That(HasDiagnostic(badRename, "workbook.sheet.nameInvalid"), Is.True);
        Assert.That(badHide.IsValid, Is.False);
        Assert.That(HasDiagnostic(badHide, "workbook.sheet.stateInvalid"), Is.True);
        Assert.That(badReorder.IsValid, Is.False);
        Assert.That(HasDiagnostic(badReorder, "workbook.sheet.indexOutOfRange"), Is.True);
        Assert.That(removeLast.IsValid, Is.False);
        Assert.That(HasDiagnostic(removeLast, "workbook.sheet.removeLast"), Is.True);
    }

    [Test]
    public void ValidateSheetNames_ReportsDuplicatesInvalidAndMissingNames()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.WorkbookPart.Sheets.Add(XlsxWorkbook.CreateSheet("sheet1", 2, "rId2"));
        workbook.WorkbookPart.Sheets.Add(XlsxWorkbook.CreateSheet("Bad*Name", 3, "rId3"));
        workbook.WorkbookPart.Sheets.Add(XlsxWorkbook.CreateSheet("", 4, "rId4"));

        XlsxValidationResult result = workbook.ValidateSheetNames();

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "workbook.sheet.nameDuplicate"), Is.True);
        Assert.That(HasDiagnostic(result, "workbook.sheet.nameInvalid"), Is.True);
        Assert.That(HasDiagnostic(result, "workbook.sheet.nameMissing"), Is.True);
    }

    static bool HasDiagnostic(XlsxValidationResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
