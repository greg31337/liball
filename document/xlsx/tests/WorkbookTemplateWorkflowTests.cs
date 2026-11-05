using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookTemplateWorkflowTests
{
    [Test]
    public void OpenTemplate_EditNamedRangeAndTable_SaveAsWorkbookPackage()
    {
        IXlsxWorkbookTemplateOperations operations = new XlsxWorkbookTemplateOperations();
        XlsxWorkbookTemplateOpenResult openResult = operations.OpenBytes(CreateTemplatePackage());
        XlsxWorkbookTemplate template = openResult.Template!;

        XlsxWorkbookTemplateEditResult namedRangeResult = operations.SetNamedRange(template, "InputCell", "Sheet1!$C$3");
        XlsxWorkbookTemplateEditResult tableResult = operations.ResizeTable(template, "SalesTable", "A1:C5");
        XlsxWorkbookSaveResult saveResult = operations.SaveAsBytes(template);
        XlsxWorkbookOpenResult savedOpen = XlsxWorkbook.OpenBytes(saveResult.Bytes);
        XlsxZipPackageReadResult savedPackage = new XlsxZipPackageReader().ReadBytes(saveResult.Bytes, new XlsxReaderOptions());
        XlsxContentTypesPart contentTypes = new XlsxContentTypesReader().ReadBytes(savedPackage.GetEntry(XlsxPackagePaths.ContentTypes)!.Data);
        XlsxTablePart table = new XlsxTablePartReader().ReadBytes(savedPackage.GetEntry("/xl/tables/table1.xml")!.Data);

        Assert.That(openResult.IsValid, Is.True);
        Assert.That(template.Workbook.PackageVariant, Is.EqualTo(XlsxPackageVariant.Xltx));
        Assert.That(namedRangeResult.IsValid, Is.True);
        Assert.That(tableResult.IsValid, Is.True);
        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(savedOpen.IsValid, Is.True);
        Assert.That(contentTypes.GetOverride(XlsxPackagePaths.Workbook)!.ContentType, Is.EqualTo(XlsxContentTypes.Workbook));
        Assert.That(savedOpen.Workbook!.WorkbookPart.GetDefinedName("InputCell")!.Text, Is.EqualTo("Sheet1!$C$3"));
        Assert.That(table.IsValid, Is.True);
        Assert.That(table.Reference, Is.EqualTo("A1:C5"));
        Assert.That(table.Name, Is.EqualTo("SalesTable"));
    }

    [Test]
    public void SetLocalNamedRange_CreatesScopedDefinedName()
    {
        IXlsxWorkbookTemplateOperations operations = new XlsxWorkbookTemplateOperations();
        XlsxWorkbookTemplateOpenResult openResult = operations.OpenBytes(CreateTemplatePackage());
        XlsxWorkbookTemplate template = openResult.Template!;

        XlsxWorkbookTemplateEditResult editResult = operations.SetLocalNamedRange(template, "LocalInput", 0, "Sheet1!$B$2");
        XlsxWorkbookSaveResult saveResult = operations.SaveAsBytes(template);
        XlsxWorkbookOpenResult savedOpen = XlsxWorkbook.OpenBytes(saveResult.Bytes);
        XlsxWorkbookDefinedName? definedName = savedOpen.Workbook!.WorkbookPart.GetLocalDefinedName("LocalInput", 0);

        Assert.That(openResult.IsValid, Is.True);
        Assert.That(editResult.IsValid, Is.True);
        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(definedName, Is.Not.Null);
        Assert.That(definedName!.Text, Is.EqualTo("Sheet1!$B$2"));
        Assert.That(definedName.HasLocalSheetId, Is.True);
    }

    [Test]
    public void TemplateFacade_OpensAndSavesFilesAndExposesDefinitionHelpers()
    {
        string inputPath = Path.GetTempFileName();
        string outputPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(inputPath, CreateTemplatePackage());
            IXlsxWorkbookTemplateOperations operations = new XlsxWorkbookTemplateOperations();
            XlsxWorkbookTemplateOpenResult openResult = operations.OpenFile(inputPath);
            XlsxWorkbookTemplate template = openResult.Template!;
            XlsxWorkbookDefinedName name = operations.GetOrCreateDefinedName(template, "Created", false, 0);
            XlsxWorkbookTemplateEditResult lookupResult = new XlsxWorkbookTemplateEditResult();
            XlsxUnknownPart? table = operations.FindTableUnknownPart(template, "SalesTable", lookupResult);
            XlsxWorkbookSaveResult saveResult = operations.SaveAsFile(template, outputPath);

            Assert.That(openResult.IsValid, Is.True);
            Assert.That(name.Name, Is.EqualTo("Created"));
            Assert.That(table, Is.Not.Null);
            Assert.That(table!.Path, Is.EqualTo("/xl/tables/table1.xml"));
            Assert.That(lookupResult.IsValid, Is.True);
            Assert.That(saveResult.IsValid, Is.True);
            Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(0));
        }
        finally
        {
            File.Delete(inputPath);
            File.Delete(outputPath);
        }
    }

    [Test]
    public void TemplateWorkflow_ReportsInvalidNamedRangeAndMissingTable()
    {
        XlsxWorkbookTemplateOpenResult openResult = XlsxWorkbookTemplate.OpenBytes(CreateTemplatePackage());

        XlsxWorkbookTemplateEditResult missingName = openResult.Template!.SetNamedRange("", "Sheet1!$A$1");
        XlsxWorkbookTemplateEditResult missingReference = openResult.Template.SetNamedRange("InputCell", "");
        XlsxWorkbookTemplateEditResult missingTable = openResult.Template.ResizeTable("Missing", "A1:B2");
        XlsxWorkbookTemplateEditResult invalidTableRange = openResult.Template.ResizeTable("SalesTable", "bad");

        Assert.That(missingName.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingName, "template.namedRange.nameMissing"), Is.True);
        Assert.That(missingReference.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingReference, "template.namedRange.referenceMissing"), Is.True);
        Assert.That(missingTable.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingTable, "template.table.notFound"), Is.True);
        Assert.That(invalidTableRange.IsValid, Is.False);
        Assert.That(invalidTableRange.Validation.Diagnostics.Count, Is.GreaterThan(0));
    }

    static byte[] CreateTemplatePackage()
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                AddEntry(archive, XlsxPackagePaths.ContentTypes, CreateContentTypesXml());
                AddEntry(archive, XlsxPackagePaths.PackageRelationships, CreatePackageRelationshipsXml());
                AddEntry(archive, XlsxPackagePaths.Workbook, CreateWorkbookXml());
                AddEntry(archive, XlsxPackagePaths.WorkbookRelationships, CreateWorkbookRelationshipsXml());
                AddEntry(archive, XlsxPackagePaths.Worksheet1, CreateWorksheetXml());
                AddEntry(archive, "/xl/worksheets/_rels/sheet1.xml.rels", CreateWorksheetRelationshipsXml());
                AddEntry(archive, "/xl/tables/table1.xml", CreateTableXml());
            }

            return stream.ToArray();
        }
    }

    static void AddEntry(ZipArchive archive, string path, string text)
    {
        string entryName = path;
        if (entryName.StartsWith("/"))
        {
            entryName = entryName.Substring(1);
        }

        ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using (Stream entryStream = entry.Open())
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            entryStream.Write(data, 0, data.Length);
        }
    }

    static string CreateContentTypesXml()
    {
        return "<Types xmlns=\"" + XlsxNamespaces.ContentTypes + "\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Override PartName=\"" + XlsxPackagePaths.Workbook + "\" ContentType=\"" + XlsxContentTypes.TemplateWorkbook + "\" />" +
            "<Override PartName=\"" + XlsxPackagePaths.Worksheet1 + "\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/tables/table1.xml\" ContentType=\"" + XlsxContentTypes.Table + "\" />" +
            "</Types>";
    }

    static string CreatePackageRelationshipsXml()
    {
        return "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rId1\" Type=\"" + XlsxRelationshipTypes.OfficeDocument + "\" Target=\"xl/workbook.xml\" />" +
            "</Relationships>";
    }

    static string CreateWorkbookXml()
    {
        return "<workbook xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" /></sheets>" +
            "<definedNames><definedName name=\"InputCell\">Sheet1!$A$1</definedName></definedNames>" +
            "</workbook>";
    }

    static string CreateWorkbookRelationshipsXml()
    {
        return "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rId1\" Type=\"" + XlsxRelationshipTypes.Worksheet + "\" Target=\"worksheets/sheet1.xml\" />" +
            "</Relationships>";
    }

    static string CreateWorksheetXml()
    {
        return "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\">" +
            "<sheetData><row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>Region</t></is></c><c r=\"B1\" t=\"inlineStr\"><is><t>Amount</t></is></c></row></sheetData>" +
            "<tableParts count=\"1\"><tablePart r:id=\"rId1\" /></tableParts>" +
            "</worksheet>";
    }

    static string CreateWorksheetRelationshipsXml()
    {
        return "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rId1\" Type=\"" + XlsxRelationshipTypes.Table + "\" Target=\"../tables/table1.xml\" />" +
            "</Relationships>";
    }

    static string CreateTableXml()
    {
        return "<table xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" id=\"1\" name=\"SalesTable\" displayName=\"SalesTable\" ref=\"A1:B2\" headerRowCount=\"1\">" +
            "<autoFilter ref=\"A1:B2\" />" +
            "<tableColumns count=\"2\"><tableColumn id=\"1\" name=\"Region\" /><tableColumn id=\"2\" name=\"Amount\" /></tableColumns>" +
            "<tableStyleInfo name=\"TableStyleMedium2\" showRowStripes=\"1\" />" +
            "</table>";
    }

    static bool HasDiagnostic(XlsxWorkbookTemplateEditResult result, string code)
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
