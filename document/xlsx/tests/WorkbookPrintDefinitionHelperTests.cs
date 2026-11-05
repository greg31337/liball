using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookPrintDefinitionHelperTests
{
    [Test]
    public void SetPrintAreaAndTitles_CreateUpdateAndRoundTripLocalDefinedNames()
    {
        XlsxWorkbookPart workbook = CreateMinimumWorkbook("Data Sheet");

        XlsxWorkbookPrintDefinitionResult createdArea = workbook.SetPrintArea(0, "Data Sheet", "$A$1:$C$5");
        XlsxWorkbookPrintDefinitionResult updatedArea = workbook.SetPrintArea(0, "Data Sheet", "$B$2:$D$8");
        XlsxWorkbookPrintDefinitionResult titles = workbook.SetPrintTitles(0, "Data Sheet", "$1:$2", "$A:$B");
        XlsxWorkbookPartWriteResult writeResult = new XlsxWorkbookPartWriter().WriteString(workbook);
        XlsxWorkbookPart roundTrip = new XlsxWorkbookPartReader().ReadString(writeResult.Xml);

        Assert.That(createdArea.IsValid, Is.True);
        Assert.That(createdArea.CreatedDefinedName, Is.True);
        Assert.That(updatedArea.IsValid, Is.True);
        Assert.That(updatedArea.CreatedDefinedName, Is.False);
        Assert.That(updatedArea.DefinedName!.Text, Is.EqualTo("'Data Sheet'!$B$2:$D$8"));
        Assert.That(titles.IsValid, Is.True);
        Assert.That(titles.DefinedName!.Text, Is.EqualTo("'Data Sheet'!$1:$2,'Data Sheet'!$A:$B"));
        Assert.That(workbook.DefinedNames.Count, Is.EqualTo(2));
        Assert.That(workbook.GetPrintArea(0)!.Hidden, Is.True);
        Assert.That(workbook.GetPrintTitles(0)!.Hidden, Is.True);
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.GetPrintArea(0)!.Text, Is.EqualTo("'Data Sheet'!$B$2:$D$8"));
        Assert.That(roundTrip.GetPrintTitles(0)!.Text, Is.EqualTo("'Data Sheet'!$1:$2,'Data Sheet'!$A:$B"));
    }

    [Test]
    public void SetPrintDefinitions_QuoteSheetNamesAndPreserveQualifiedReferences()
    {
        XlsxWorkbookPart workbook = CreateMinimumWorkbook("O'Brien Data");

        XlsxWorkbookPrintDefinitionResult quotedArea = workbook.SetPrintArea(0, "O'Brien Data", "$A$1:$B$2");
        XlsxWorkbookPrintDefinitionResult qualifiedArea = workbook.SetPrintArea(1, "Ignored", "External!$C$3:$D$4");
        XlsxWorkbookPrintDefinitionResult rowTitles = workbook.SetPrintTitles(0, "O'Brien Data", "$1:$1", "");
        XlsxWorkbookPrintDefinitionResult columnTitles = workbook.SetPrintTitles(1, "Simple", "", "$A:$A");

        Assert.That(quotedArea.IsValid, Is.True);
        Assert.That(quotedArea.DefinedName!.Text, Is.EqualTo("'O''Brien Data'!$A$1:$B$2"));
        Assert.That(qualifiedArea.IsValid, Is.True);
        Assert.That(qualifiedArea.DefinedName!.Text, Is.EqualTo("External!$C$3:$D$4"));
        Assert.That(rowTitles.IsValid, Is.True);
        Assert.That(rowTitles.DefinedName!.Text, Is.EqualTo("'O''Brien Data'!$1:$1"));
        Assert.That(columnTitles.IsValid, Is.True);
        Assert.That(columnTitles.DefinedName!.Text, Is.EqualTo("Simple!$A:$A"));
    }

    [Test]
    public void ClearPrintDefinitions_RemoveOnlyMatchingLocalScope()
    {
        XlsxWorkbookPart workbook = CreateMinimumWorkbook("Sheet1");
        workbook.SetPrintArea(0, "Sheet1", "$A$1");
        workbook.SetPrintArea(1, "Sheet1", "$B$2");
        workbook.SetPrintTitles(0, "Sheet1", "$1:$1", "");

        XlsxWorkbookPrintDefinitionResult removedArea = workbook.ClearPrintArea(0);
        XlsxWorkbookPrintDefinitionResult missingArea = workbook.ClearPrintArea(0);
        XlsxWorkbookPrintDefinitionResult removedTitles = workbook.ClearPrintTitles(0);

        Assert.That(removedArea.IsValid, Is.True);
        Assert.That(removedArea.RemovedDefinedName, Is.True);
        Assert.That(removedArea.HasDefinedName, Is.True);
        Assert.That(removedArea.DefinedName!.Text, Is.EqualTo("Sheet1!$A$1"));
        Assert.That(missingArea.IsValid, Is.True);
        Assert.That(missingArea.RemovedDefinedName, Is.False);
        Assert.That(missingArea.HasDefinedName, Is.False);
        Assert.That(removedTitles.IsValid, Is.True);
        Assert.That(removedTitles.RemovedDefinedName, Is.True);
        Assert.That(workbook.GetPrintArea(0), Is.Null);
        Assert.That(workbook.GetPrintArea(1)!.Text, Is.EqualTo("Sheet1!$B$2"));
        Assert.That(workbook.GetPrintTitles(0), Is.Null);
        Assert.That(workbook.DefinedNames.Count, Is.EqualTo(1));
    }

    [Test]
    public void PrintDefinitionHelpers_ReportInvalidInputs()
    {
        XlsxWorkbookPart nullDefinedNames = CreateMinimumWorkbook("Sheet1");
        XlsxWorkbookPart negativeSet = CreateMinimumWorkbook("Sheet1");
        XlsxWorkbookPart missingArea = CreateMinimumWorkbook("Sheet1");
        XlsxWorkbookPart missingTitles = CreateMinimumWorkbook("Sheet1");
        XlsxWorkbookPart negativeClear = CreateMinimumWorkbook("Sheet1");
        XlsxWorkbookPart nullClear = CreateMinimumWorkbook("Sheet1");

        nullDefinedNames.DefinedNames = null!;
        nullClear.DefinedNames = null!;

        XlsxWorkbookPrintDefinitionResult nullDefinedNamesResult = nullDefinedNames.SetPrintArea(0, "Sheet1", "$A$1");
        XlsxWorkbookPrintDefinitionResult negativeSetResult = negativeSet.SetPrintArea(-1, "Sheet1", "$A$1");
        XlsxWorkbookPrintDefinitionResult missingAreaResult = missingArea.SetPrintArea(0, "Sheet1", "");
        XlsxWorkbookPrintDefinitionResult missingTitlesResult = missingTitles.SetPrintTitles(0, "Sheet1", "", "");
        XlsxWorkbookPrintDefinitionResult negativeClearResult = negativeClear.ClearPrintArea(-1);
        XlsxWorkbookPrintDefinitionResult nullClearResult = nullClear.ClearPrintTitles(0);

        Assert.That(nullDefinedNamesResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullDefinedNamesResult, "workbook.definedNamesNull"), Is.True);
        Assert.That(negativeSetResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(negativeSetResult, "workbook.printDefinitionLocalSheetIdRange"), Is.True);
        Assert.That(missingAreaResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingAreaResult, "workbook.printAreaReferenceMissing"), Is.True);
        Assert.That(missingTitlesResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingTitlesResult, "workbook.printTitlesReferenceMissing"), Is.True);
        Assert.That(negativeClearResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(negativeClearResult, "workbook.printDefinitionLocalSheetIdRange"), Is.True);
        Assert.That(nullClearResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullClearResult, "workbook.definedNamesNull"), Is.True);
    }

    [Test]
    public void WorkbookPartFacade_ExercisesLookupValidationMutationAndReferenceUpdates()
    {
        XlsxWorkbookPart workbook = CreateMinimumWorkbook("Sheet1");
        XlsxWorkbookDefinedName global = new XlsxWorkbookDefinedName();
        global.Name = "GlobalName";
        global.Text = "Sheet1!$A$1";
        workbook.DefinedNames.Add(global);
        IXlsxWorkbookPartOperations operations = new XlsxWorkbookPartOperations();

        XlsxWorkbookPrintDefinitionResult area = operations.SetPrintArea(workbook, 0, "Sheet1", "$A$1:$B$2");
        XlsxWorkbookPrintDefinitionResult titles = operations.SetPrintTitles(workbook, 0, "Sheet1", "$1:$1", "$A:$A");
        XlsxWorkbookPrintDefinitionResult createResult = new XlsxWorkbookPrintDefinitionResult();
        XlsxWorkbookDefinedName local = operations.GetOrCreateLocalDefinedName(workbook, "LocalName", 0, createResult);
        local.Text = "Sheet1!$C$3";

        Assert.That(operations.GetSheetByName(workbook, "Sheet1"), Is.Not.Null);
        Assert.That(operations.GetDefinedName(workbook, "GlobalName"), Is.SameAs(global));
        Assert.That(operations.GetDefinedNameForSheet(workbook, "LocalName", 0), Is.SameAs(local));
        Assert.That(operations.GetGlobalDefinedName(workbook, "GlobalName"), Is.SameAs(global));
        Assert.That(operations.GetLocalDefinedName(workbook, "LocalName", 0), Is.SameAs(local));
        Assert.That(operations.GetPrintArea(workbook, 0), Is.SameAs(area.DefinedName));
        Assert.That(operations.GetPrintTitles(workbook, 0), Is.SameAs(titles.DefinedName));
        Assert.That(operations.ValidateLocalDefinedNameAccess(workbook, 0).IsValid, Is.True);
        Assert.That(operations.ValidatePrintDefinition(workbook, 0, "Sheet1", "$A$1", "missing").IsValid, Is.True);
        Assert.That(operations.ValidatePrintTitles(workbook, 0, "Sheet1", "$1:$1", "$A:$A").IsValid, Is.True);
        Assert.That(operations.UpdateDefinedNameReferencesForRowInsert(workbook, 0, 1, 1).IsValid, Is.True);
        Assert.That(operations.UpdateDefinedNameReferencesForRowDelete(workbook, 0, 2, 1).IsValid, Is.True);
        Assert.That(operations.UpdateDefinedNameReferencesForColumnInsert(workbook, 0, 1, 1).IsValid, Is.True);
        Assert.That(operations.UpdateDefinedNameReferencesForColumnDelete(workbook, 0, 2, 1).IsValid, Is.True);
        Assert.That(operations.UpdateDefinedNameReferences(workbook, 0, XlsxFormulaReferenceUpdateKind.RowInsert, 1, 1, 1).IsValid, Is.True);
        Assert.That(operations.ClearLocalDefinedName(workbook, "LocalName", 0).RemovedDefinedName, Is.True);
        Assert.That(operations.ClearPrintArea(workbook, 0).RemovedDefinedName, Is.True);
        Assert.That(operations.ClearPrintTitles(workbook, 0).RemovedDefinedName, Is.True);
    }

    static XlsxWorkbookPart CreateMinimumWorkbook(string sheetName)
    {
        XlsxWorkbookPart part = new XlsxWorkbookPart();
        XlsxWorkbookSheet sheet = new XlsxWorkbookSheet();
        sheet.Name = sheetName;
        sheet.SheetId = 1;
        sheet.RelationshipId = "rId1";
        part.Sheets.Add(sheet);
        return part;
    }

    static bool HasDiagnostic(XlsxWorkbookPrintDefinitionResult result, string code)
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
