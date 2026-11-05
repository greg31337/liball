using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookFunctionalCompletionTests
{
    [Test]
    public void ReorderAndRemoveSheet_RemapScopedNamesAndWorkbookViewsAcrossSaveReopen()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.AddSheet("Input");
        workbook.AddSheet("Archive");
        workbook.WorkbookPart.DefinedNames.Add(DefinedName("Global", "1", 0, false));
        workbook.WorkbookPart.DefinedNames.Add(DefinedName("SheetOneLocal", "'Sheet1'!$A$1", 0, true));
        workbook.WorkbookPart.DefinedNames.Add(DefinedName("InputLocal", "'Input'!$A$1", 1, true));
        workbook.WorkbookPart.DefinedNames.Add(DefinedName("ArchiveLocal", "'Archive'!$A$1", 2, true));
        XlsxWorkbookView view = WorkbookView(2, 1);
        workbook.WorkbookPart.WorkbookViews.Add(view);

        XlsxValidationResult reorder = workbook.ReorderSheet("Archive", 0);

        Assert.That(reorder.IsValid, Is.True);
        Assert.That(workbook.GetSheetAt(0)!.Name, Is.EqualTo("Archive"));
        Assert.That(FindDefinedName(workbook, "ArchiveLocal")!.LocalSheetId, Is.EqualTo(0));
        Assert.That(FindDefinedName(workbook, "SheetOneLocal")!.LocalSheetId, Is.EqualTo(1));
        Assert.That(FindDefinedName(workbook, "InputLocal")!.LocalSheetId, Is.EqualTo(2));
        Assert.That(view.ActiveTab, Is.EqualTo(0));
        Assert.That(view.FirstSheet, Is.EqualTo(2));

        XlsxValidationResult remove = workbook.RemoveSheet("Sheet1");
        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxWorkbookOpenResult reopened = XlsxWorkbook.OpenBytes(save.Bytes);

        Assert.That(remove.IsValid, Is.True);
        Assert.That(FindDefinedName(workbook, "SheetOneLocal"), Is.Null);
        Assert.That(FindDefinedName(workbook, "ArchiveLocal")!.LocalSheetId, Is.EqualTo(0));
        Assert.That(FindDefinedName(workbook, "InputLocal")!.LocalSheetId, Is.EqualTo(1));
        Assert.That(FindDefinedName(workbook, "Global")!.HasLocalSheetId, Is.False);
        Assert.That(view.ActiveTab, Is.EqualTo(0));
        Assert.That(view.FirstSheet, Is.EqualTo(1));
        Assert.That(save.IsValid, Is.True, Diagnostics(save.Validation));
        Assert.That(reopened.IsValid, Is.True, Diagnostics(reopened.Validation));
        Assert.That(reopened.Workbook!.GetSheetAt(0)!.Name, Is.EqualTo("Archive"));
        Assert.That(reopened.Workbook.GetSheetAt(1)!.Name, Is.EqualTo("Input"));
        Assert.That(FindDefinedName(reopened.Workbook, "ArchiveLocal")!.LocalSheetId, Is.EqualTo(0));
        Assert.That(FindDefinedName(reopened.Workbook, "InputLocal")!.LocalSheetId, Is.EqualTo(1));
        Assert.That(reopened.Workbook.WorkbookPart.WorkbookViews[0].ActiveTab, Is.EqualTo(0));
        Assert.That(reopened.Workbook.WorkbookPart.WorkbookViews[0].FirstSheet, Is.EqualTo(1));
    }

    [Test]
    public void RemoveOpenedSheet_RemovesTypedAndPhysicalSheetAggregates()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.AddSheet("Data");
        string relationshipPath = XlsxPackagePath.GetRelationshipPartPath("/xl/worksheets/sheet2.xml");
        workbook.UnknownParts.AddPart(relationshipPath, XlsxContentTypes.Relationships, Encoding.UTF8.GetBytes(
            "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\"><Relationship Id=\"rIdExternal\" Type=\"" + XlsxRelationshipTypes.Hyperlink + "\" Target=\"https://example.invalid/\" TargetMode=\"External\" /></Relationships>"));
        XlsxWorkbookSaveResult initialSave = workbook.SaveBytes();
        XlsxReaderOptions openOptions = new XlsxReaderOptions();
        openOptions.ValuesOnly = true;
        XlsxWorkbookOpenResult opened = XlsxWorkbook.OpenBytes(initialSave.Bytes, openOptions);

        Assert.That(opened.IsValid, Is.True, Diagnostics(opened.Validation));
        Assert.That(ContainsWorksheet(opened.Workbook!, "/xl/worksheets/sheet2.xml"), Is.True);
        Assert.That(ContainsRelationshipPart(opened.Workbook!, relationshipPath), Is.True);
        Assert.That(opened.Workbook!.GetPackagePart("/xl/worksheets/sheet2.xml"), Is.Not.Null);

        XlsxValidationResult remove = opened.Workbook.RemoveSheet("Data");
        XlsxWorkbookSaveResult save = opened.Workbook.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());
        XlsxWorkbookOpenResult reopened = XlsxWorkbook.OpenBytes(save.Bytes);

        Assert.That(remove.IsValid, Is.True);
        Assert.That(ContainsWorksheet(opened.Workbook, "/xl/worksheets/sheet2.xml"), Is.False);
        Assert.That(ContainsRelationshipPart(opened.Workbook, relationshipPath), Is.False);
        Assert.That(opened.Workbook.GetPackagePart("/xl/worksheets/sheet2.xml"), Is.Null);
        Assert.That(opened.Workbook.GetPackagePart(relationshipPath), Is.Null);
        Assert.That(save.IsValid, Is.True, Diagnostics(save.Validation));
        Assert.That(package.GetEntry("/xl/worksheets/sheet2.xml"), Is.Null);
        Assert.That(package.GetEntry(relationshipPath), Is.Null);
        Assert.That(reopened.IsValid, Is.True, Diagnostics(reopened.Validation));
        Assert.That(reopened.Workbook!.WorkbookPart.Sheets, Has.Count.EqualTo(1));
        Assert.That(reopened.Workbook.GetSheet("Data"), Is.Null);
    }

    [Test]
    public void HideSheet_PreservesVisibleSheetAndMovesActiveWorkbookView()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.AddSheet("Data");
        XlsxWorkbookView view = WorkbookView(1, 1);
        workbook.WorkbookPart.WorkbookViews.Add(view);

        XlsxValidationResult hideData = workbook.HideSheet("Data", "veryHidden");

        Assert.That(hideData.IsValid, Is.True);
        Assert.That(view.ActiveTab, Is.EqualTo(0));
        Assert.That(view.FirstSheet, Is.EqualTo(0));
        Assert.That(workbook.GetSheet("Sheet1")!.State, Is.Empty);

        XlsxValidationResult showData = workbook.HideSheet("Data", "visible");
        XlsxValidationResult hideSheetOne = workbook.HideSheet("Sheet1", "hidden");
        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxWorkbookOpenResult reopened = XlsxWorkbook.OpenBytes(save.Bytes);

        Assert.That(showData.IsValid, Is.True);
        Assert.That(hideSheetOne.IsValid, Is.True);
        Assert.That(view.ActiveTab, Is.EqualTo(1));
        Assert.That(view.FirstSheet, Is.EqualTo(1));
        Assert.That(save.IsValid, Is.True, Diagnostics(save.Validation));
        Assert.That(reopened.IsValid, Is.True, Diagnostics(reopened.Validation));
        Assert.That(reopened.Workbook!.GetSheet("Sheet1")!.State, Is.EqualTo("hidden"));
        Assert.That(reopened.Workbook.GetSheet("Data")!.State, Is.Not.EqualTo("hidden"));
        Assert.That(reopened.Workbook.GetSheet("Data")!.State, Is.Not.EqualTo("veryHidden"));
        Assert.That(reopened.Workbook.WorkbookPart.WorkbookViews[0].ActiveTab, Is.EqualTo(1));
    }

    [Test]
    public void RemoveSheet_CleansChartsheetMacrosheetAndDialogsheetAggregates()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorkbookSheet chart = AddNonWorksheetSheet(workbook, "Chart", 2, "rIdChart", XlsxSheetType.Chartsheet, "chartsheets/sheet1.xml", "/xl/chartsheets/sheet1.xml");
        XlsxWorkbookSheet macro = AddNonWorksheetSheet(workbook, "Macro", 3, "rIdMacro", XlsxSheetType.Macrosheet, "macrosheets/sheet1.xml", "/xl/macrosheets/sheet1.xml");
        XlsxWorkbookSheet dialog = AddNonWorksheetSheet(workbook, "Dialog", 4, "rIdDialog", XlsxSheetType.Dialogsheet, "dialogSheets/sheet1.xml", "/xl/dialogSheets/sheet1.xml");
        XlsxChartsheetPart chartsheet = new XlsxChartsheetPart();
        chartsheet.PartPath = chart.TargetPath;
        workbook.Chartsheets.Add(chartsheet);
        workbook.Macrosheets.Add(macro);
        workbook.Dialogsheets.Add(dialog);

        XlsxValidationResult removeChart = workbook.RemoveSheet("Chart");
        XlsxValidationResult removeMacro = workbook.RemoveSheet("Macro");
        XlsxValidationResult removeDialog = workbook.RemoveSheet("Dialog");

        Assert.That(removeChart.IsValid, Is.True);
        Assert.That(removeMacro.IsValid, Is.True);
        Assert.That(removeDialog.IsValid, Is.True);
        Assert.That(workbook.WorkbookPart.Sheets, Has.Count.EqualTo(1));
        Assert.That(workbook.Chartsheets, Is.Empty);
        Assert.That(workbook.Macrosheets, Is.Empty);
        Assert.That(workbook.Dialogsheets, Is.Empty);
        Assert.That(workbook.WorkbookRelationships.GetRelationship("rIdChart"), Is.Null);
        Assert.That(workbook.WorkbookRelationships.GetRelationship("rIdMacro"), Is.Null);
        Assert.That(workbook.WorkbookRelationships.GetRelationship("rIdDialog"), Is.Null);
    }

    static XlsxWorkbookDefinedName DefinedName(string name, string text, int localSheetId, bool local)
    {
        XlsxWorkbookDefinedName definedName = new XlsxWorkbookDefinedName();
        definedName.Name = name;
        definedName.Text = text;
        definedName.LocalSheetId = localSheetId;
        definedName.HasLocalSheetId = local;
        return definedName;
    }

    static XlsxWorkbookSheet AddNonWorksheetSheet(XlsxWorkbook workbook, string name, uint sheetId, string relationshipId, XlsxSheetType sheetType, string target, string targetPath)
    {
        XlsxWorkbookSheet sheet = XlsxWorkbook.CreateSheet(name, sheetId, relationshipId, sheetType);
        sheet.TargetPath = targetPath;
        sheet.HasRelationshipMetadata = true;
        workbook.WorkbookPart.Sheets.Add(sheet);
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = relationshipId;
        relationship.Type = SheetRelationshipType(sheetType);
        relationship.Target = target;
        relationship.TargetPath = targetPath;
        workbook.WorkbookRelationships.Relationships.Add(relationship);
        return sheet;
    }

    static string SheetRelationshipType(XlsxSheetType sheetType)
    {
        if (sheetType == XlsxSheetType.Chartsheet)
        {
            return XlsxRelationshipTypes.Chartsheet;
        }
        if (sheetType == XlsxSheetType.Macrosheet)
        {
            return XlsxRelationshipTypes.Macrosheet;
        }
        return XlsxRelationshipTypes.Dialogsheet;
    }

    static XlsxWorkbookView WorkbookView(uint activeTab, uint firstSheet)
    {
        XlsxWorkbookView view = new XlsxWorkbookView();
        view.ActiveTab = activeTab;
        view.HasActiveTab = true;
        view.FirstSheet = firstSheet;
        view.HasFirstSheet = true;
        return view;
    }

    static XlsxWorkbookDefinedName? FindDefinedName(XlsxWorkbook workbook, string name)
    {
        foreach (XlsxWorkbookDefinedName definedName in workbook.WorkbookPart.DefinedNames)
        {
            if (definedName.Name == name)
            {
                return definedName;
            }
        }
        return null;
    }

    static bool ContainsWorksheet(XlsxWorkbook workbook, string partPath)
    {
        foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
        {
            if (worksheet.PartPath == partPath)
            {
                return true;
            }
        }
        return false;
    }

    static bool ContainsRelationshipPart(XlsxWorkbook workbook, string partPath)
    {
        foreach (XlsxRelationshipsPart relationships in workbook.RelationshipParts)
        {
            if (relationships.PartPath == partPath)
            {
                return true;
            }
        }
        return false;
    }

    static bool HasDiagnostic(XlsxValidationResult validation, string code)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }
        return false;
    }

    static string Diagnostics(XlsxValidationResult validation)
    {
        string result = "";
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            result = result + diagnostic.Code + ": " + diagnostic.Message + System.Environment.NewLine;
        }
        return result;
    }
}
