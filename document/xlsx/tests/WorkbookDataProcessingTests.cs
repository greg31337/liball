using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookDataProcessingTests
{
    [Test]
    public void ProjectRangeRows_ResolvesSharedStringsAndIncludesBlankCoordinates()
    {
        XlsxWorkbook workbook = CreateWorkbook("Data");
        IXlsxWorksheetPartOperations worksheetOperations = new XlsxWorksheetPartOperations();
        int sharedIndex = new XlsxSharedStringTablePartOperations().GetOrAddText(workbook.SharedStrings, "Name");
        worksheetOperations.SetSharedStringValue(workbook.Worksheets[0], "A1", (uint)sharedIndex);
        worksheetOperations.SetTextValue(workbook.Worksheets[0], "B1", "Amount");
        worksheetOperations.SetTextValue(workbook.Worksheets[0], "A2", "Widgets");
        worksheetOperations.SetNumberValue(workbook.Worksheets[0], "B2", 12.5);
        IXlsxWorkbookDataService service = new XlsxWorkbookDataService();

        XlsxWorkbookRangeMaterialization result = service.ProjectRangeRows(workbook, "Data", "A1:C2");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Reference, Is.EqualTo("A1:C2"));
            Assert.That(result.ColumnCount, Is.EqualTo(3));
            Assert.That(result.Rows.Count, Is.EqualTo(2));
            Assert.That(result.Rows[0][0].Kind, Is.EqualTo(XlsxCellValueKind.Text));
            Assert.That(result.Rows[0][0].Text, Is.EqualTo("Name"));
            Assert.That(result.Rows[1][1].Number, Is.EqualTo(12.5));
            Assert.That(result.Rows[1][2].Kind, Is.EqualTo(XlsxCellValueKind.Blank));
        });
    }

    [Test]
    public void DetectHeaderRow_FindsTextLabelsBeforeTypedData()
    {
        XlsxWorkbook workbook = CreateWorkbook("Sales");
        IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();
        operations.SetTextValue(workbook.Worksheets[0], "A2", "Product");
        operations.SetTextValue(workbook.Worksheets[0], "B2", "Quantity");
        operations.SetTextValue(workbook.Worksheets[0], "A3", "Pencils");
        operations.SetNumberValue(workbook.Worksheets[0], "B3", 20);

        XlsxHeaderRowDetectionResult result = new XlsxWorkbookDataService().DetectHeaderRow(workbook, "Sales", "A1:B4");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasHeaderRow, Is.True);
            Assert.That(result.HeaderRow, Is.EqualTo(2));
            Assert.That(result.Confidence, Is.EqualTo(1));
            Assert.That(result.Headers, Is.EqualTo(new[] { "Product", "Quantity" }));
        });
    }

    [Test]
    public void MaterializeSelectedRanges_HandlesQuotedSheetsAndRequestOrder()
    {
        XlsxWorkbook workbook = CreateWorkbook("First", "Data Sheet");
        IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();
        operations.SetNumberValue(workbook.Worksheets[0], "A1", 1);
        operations.SetNumberValue(workbook.Worksheets[1], "B2", 2);
        List<string> selections = new List<string> { "'Data Sheet'!B2:C2", "A1" };

        XlsxSelectedRangesMaterializationResult result = new XlsxWorkbookDataService().MaterializeSelectedRanges(workbook, selections);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Ranges.Count, Is.EqualTo(2));
            Assert.That(result.Ranges[0].SheetName, Is.EqualTo("Data Sheet"));
            Assert.That(result.Ranges[0].Rows[0][0].Number, Is.EqualTo(2));
            Assert.That(result.Ranges[0].Rows[0][1].Kind, Is.EqualTo(XlsxCellValueKind.Blank));
            Assert.That(result.Ranges[1].SheetName, Is.EqualTo("First"));
            Assert.That(result.Ranges[1].Rows[0][0].Number, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReplaceTemplateTokens_UpdatesTextSharedStringsAndFormulas()
    {
        XlsxWorkbook workbook = CreateWorkbook("Template");
        IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();
        int sharedIndex = new XlsxSharedStringTablePartOperations().GetOrAddText(workbook.SharedStrings, "Hello {{Name}}");
        operations.SetSharedStringValue(workbook.Worksheets[0], "A1", (uint)sharedIndex);
        operations.SetInlineStringValue(workbook.Worksheets[0], "A2", "Order {{Id}}");
        operations.SetFormulaCachedValue(workbook.Worksheets[0], "A3", "\"{{Name}}\"", "", "str");
        List<XlsxTemplateReplacement> replacements = new List<XlsxTemplateReplacement>();
        replacements.Add(Replacement("{{Name}}", "Ada"));
        replacements.Add(Replacement("{{Id}}", "42"));

        XlsxTemplateReplacementResult result = new XlsxWorkbookDataService().ReplaceTemplateTokens(workbook, replacements, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ReplacedCellCount, Is.EqualTo(2));
            Assert.That(result.ReplacedFormulaCount, Is.EqualTo(1));
            Assert.That(result.ReplacementCount, Is.EqualTo(3));
            Assert.That(workbook.Worksheets[0].GetCell("A1")!.Value, Is.EqualTo("Hello Ada"));
            Assert.That(workbook.Worksheets[0].GetCell("A2")!.Value, Is.EqualTo("Order 42"));
            Assert.That(workbook.Worksheets[0].GetCell("A3")!.Formula, Is.EqualTo("\"Ada\""));
        });
    }

    [Test]
    public void InventoryExternalContent_ReturnsTypedUniqueSources()
    {
        XlsxWorkbook workbook = CreateWorkbook("Data");
        XlsxExternalLinkPart link = new XlsxExternalLinkPart();
        link.Target = "file:///books/source.xlsx";
        workbook.ExternalLinks.Add(link);
        XlsxWorkbookConnectionInfo connection = new XlsxWorkbookConnectionInfo();
        connection.Id = 3;
        connection.HasId = true;
        connection.Name = "Warehouse";
        workbook.Connections.Connections.Add(connection);
        XlsxQueryTablePart query = new XlsxQueryTablePart();
        query.Name = "Warehouse Query";
        query.ConnectionId = 3;
        query.HasConnectionId = true;
        workbook.QueryTables.Add(query);
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = "rIdExternal";
        relationship.Target = "https://example.test/data.csv";
        relationship.TargetMode = "External";
        relationship.IsExternal = true;
        workbook.WorkbookRelationships.Relationships.Add(relationship);
        IXlsxWorksheetPartOperations worksheetOperations = new XlsxWorksheetPartOperations();
        worksheetOperations.AddExternalHyperlink(workbook.Worksheets[0], "A1", "https://example.test", "Example", "");
        worksheetOperations.SetFormulaCachedValue(workbook.Worksheets[0], "B1", "[Budget.xlsx]Sheet1!A1", "", "");

        XlsxExternalContentInventoryResult result = new XlsxWorkbookDataService().InventoryExternalContent(workbook);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(HasKind(result, XlsxExternalContentKind.ExternalLink), Is.True);
            Assert.That(HasKind(result, XlsxExternalContentKind.Connection), Is.True);
            Assert.That(HasKind(result, XlsxExternalContentKind.QueryTable), Is.True);
            Assert.That(HasKind(result, XlsxExternalContentKind.ExternalRelationship), Is.True);
            Assert.That(HasKind(result, XlsxExternalContentKind.ExternalHyperlink), Is.True);
            Assert.That(HasKind(result, XlsxExternalContentKind.ExternalFormulaReference), Is.True);
        });
    }

    [Test]
    public void FormulaQueries_ReturnDirectDependenciesAndTransitiveImpacts()
    {
        XlsxWorkbook workbook = CreateWorkbook("Inputs", "Results");
        IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();
        operations.SetNumberValue(workbook.Worksheets[0], "A1", 5);
        operations.SetFormulaCachedValue(workbook.Worksheets[1], "B1", "Inputs!A1*2", "", "");
        operations.SetFormulaCachedValue(workbook.Worksheets[1], "C1", "B1+1", "", "");
        IXlsxWorkbookDataService service = new XlsxWorkbookDataService();

        XlsxFormulaDependencyQueryResult dependency = service.QueryFormulaDependencies(workbook, "Results!B1");
        XlsxImpactedCellsResult impact = service.QueryImpactedCells(workbook, new List<string> { "Inputs!A1" });

        Assert.Multiple(() =>
        {
            Assert.That(dependency.IsValid, Is.True);
            Assert.That(dependency.HasNode, Is.True);
            Assert.That(dependency.Precedents, Is.EqualTo(new[] { "Inputs!A1" }));
            Assert.That(dependency.Dependents, Is.EqualTo(new[] { "Results!C1" }));
            Assert.That(impact.IsValid, Is.True);
            Assert.That(impact.DirectDependents, Is.EqualTo(new[] { "Results!B1" }));
            Assert.That(impact.ImpactedCells, Is.EqualTo(new[] { "Results!B1", "Results!C1" }));
        });
    }

    [Test]
    public void DataProcessing_InvalidRequestsReturnDiagnosticsWithoutTemplateMutation()
    {
        XlsxWorkbook workbook = CreateWorkbook("Data");
        IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();
        operations.SetTextValue(workbook.Worksheets[0], "A1", "{{Name}}");
        IXlsxWorkbookDataService service = new XlsxWorkbookDataService();
        XlsxTemplateReplacement invalidReplacement = Replacement("", "Ada");

        XlsxWorkbookRangeMaterialization missingSheet = service.ProjectRangeRows(workbook, "Missing", "A1");
        XlsxSelectedRangesMaterializationResult externalSelection = service.MaterializeSelectedRanges(workbook, new List<string> { "[Other.xlsx]Data!A1" });
        XlsxTemplateReplacementResult template = service.ReplaceTemplateTokens(workbook, new List<XlsxTemplateReplacement> { invalidReplacement }, true);
        XlsxFormulaDependencyQueryResult dependency = service.QueryFormulaDependencies(workbook, "A1");

        Assert.Multiple(() =>
        {
            Assert.That(missingSheet.IsValid, Is.False);
            Assert.That(externalSelection.IsValid, Is.False);
            Assert.That(template.IsValid, Is.False);
            Assert.That(dependency.IsValid, Is.False);
            Assert.That(HasDiagnostic(missingSheet.Validation, "workbookData.sheetNotFound"), Is.True);
            Assert.That(HasDiagnostic(externalSelection.Validation, "workbookData.selectedRangeUnsupported"), Is.True);
            Assert.That(HasDiagnostic(template.Validation, "templateReplacement.tokenMissing"), Is.True);
            Assert.That(HasDiagnostic(dependency.Validation, "formulaDependency.referenceInvalid"), Is.True);
            Assert.That(workbook.Worksheets[0].GetCell("A1")!.Value, Is.EqualTo("{{Name}}"));
        });
    }

    static XlsxWorkbook CreateWorkbook(params string[] sheetNames)
    {
        XlsxWorkbook workbook = new XlsxWorkbook();
        uint sheetId = 1;
        foreach (string sheetName in sheetNames)
        {
            XlsxWorkbookSheet sheet = new XlsxWorkbookSheet();
            sheet.Name = sheetName;
            sheet.SheetId = sheetId;
            workbook.WorkbookPart.Sheets.Add(sheet);
            workbook.Worksheets.Add(new XlsxWorksheetPart());
            sheetId = sheetId + 1;
        }
        return workbook;
    }

    static XlsxTemplateReplacement Replacement(string token, string replacement)
    {
        XlsxTemplateReplacement value = new XlsxTemplateReplacement();
        value.Token = token;
        value.Replacement = replacement;
        return value;
    }

    static bool HasKind(XlsxExternalContentInventoryResult result, XlsxExternalContentKind kind)
    {
        foreach (XlsxExternalContentItem item in result.Items)
        {
            if (item.Kind == kind) { return true; }
        }
        return false;
    }

    static bool HasDiagnostic(XlsxValidationResult validation, string code)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            if (diagnostic.Code == code) { return true; }
        }
        return false;
    }
}
