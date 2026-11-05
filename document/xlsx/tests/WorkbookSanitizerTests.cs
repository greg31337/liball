using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookSanitizerTests
{
    [Test]
    public void Sanitize_RemovesActiveContentHiddenSheetsMetadataAndUnknownPartsFromPackage()
    {
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.AllowMacroEnabledPackages = true;
        options.SelectedSheets.Add("Sheet1");
        options.SelectedSheets.Add("Hidden");
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(CreateUnsafePackage(), options);

        XlsxWorkbookSanitizeResult sanitize = open.Workbook!.Sanitize();
        XlsxWorkbookSaveResult save = open.Workbook.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(sanitize.IsValid, Is.True);
        Assert.That(sanitize.MacrosRemoved, Is.GreaterThan(0));
        Assert.That(sanitize.ExternalLinksRemoved, Is.GreaterThan(0));
        Assert.That(sanitize.HiddenSheetsRemoved, Is.EqualTo(1));
        Assert.That(sanitize.CustomXmlPartsRemoved, Is.GreaterThan(0));
        Assert.That(sanitize.PersonalMetadataPartsRemoved, Is.EqualTo(3));
        Assert.That(sanitize.OleObjectsRemoved, Is.EqualTo(2));
        Assert.That(sanitize.UnknownPartsRemoved, Is.GreaterThan(0));
        Assert.That(open.Workbook.PackageVariant, Is.EqualTo(XlsxPackageVariant.Xlsx));
        Assert.That(open.Workbook.HasVbaProject, Is.False);
        Assert.That(open.Workbook.GetSheet("Hidden"), Is.Null);
        Assert.That(open.Workbook.HasCoreProperties, Is.False);
        Assert.That(open.Workbook.HasExtendedProperties, Is.False);
        Assert.That(open.Workbook.HasCustomProperties, Is.False);
        Assert.That(open.Workbook.ListExternalLinks().Count, Is.EqualTo(0));
        Assert.That(open.Workbook.ListCustomXmlParts().Count, Is.EqualTo(0));
        Assert.That(open.Workbook.ListEmbeddedObjects().Count, Is.EqualTo(0));
        Assert.That(save.IsValid, Is.True);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.Workbook);
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.MacroEnabledWorkbook);
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.Workbook, "Hidden");
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.WorkbookRelationships, XlsxRelationshipTypes.ExternalLink);
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.WorkbookRelationships, XlsxRelationshipTypes.VbaProject);
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.PackageRelationships, XlsxRelationshipTypes.CoreProperties);
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.PackageRelationships, XlsxRelationshipTypes.CustomXml);
        AssertEntryText(package, XlsxPackagePaths.Worksheet1, "<v>1</v>");
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.Worksheet1, "<oleObjects>");
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.Worksheet1, "legacyDrawing");
        AssertEntryTextDoesNotContain(package, "/xl/worksheets/_rels/sheet1.xml.rels", XlsxRelationshipTypes.Comments);
        AssertEntryTextDoesNotContain(package, "/xl/worksheets/_rels/sheet1.xml.rels", XlsxRelationshipTypes.VmlDrawing);
        AssertEntryTextDoesNotContain(package, "/xl/worksheets/_rels/sheet1.xml.rels", XlsxRelationshipTypes.OleObject);
        AssertEntryTextDoesNotContain(package, "/xl/worksheets/_rels/sheet1.xml.rels", XlsxRelationshipTypes.EmbeddedPackage);
        AssertEntryMissing(package, XlsxPackagePaths.VbaProject);
        AssertEntryMissing(package, "/xl/externalLinks/externalLink1.xml");
        AssertEntryMissing(package, "/xl/comments/comment1.xml");
        AssertEntryMissing(package, "/xl/drawings/vmlDrawing1.vml");
        AssertEntryMissing(package, "/xl/embeddings/oleObject1.bin");
        AssertEntryMissing(package, "/xl/embeddings/embeddedPackage1.xlsx");
        AssertEntryMissing(package, "/customXml/item1.xml");
        AssertEntryMissing(package, "/xl/unknown/future.bin");
        AssertEntryMissing(package, "/xl/worksheets/sheet2.xml");
        AssertEntryMissing(package, XlsxPackagePaths.CoreProperties);
        AssertEntryMissing(package, XlsxPackagePaths.ExtendedProperties);
        AssertEntryMissing(package, XlsxPackagePaths.CustomProperties);
    }

    [Test]
    public void Sanitize_HonorsDisabledOptionsAndUnhidesLastRemainingHiddenSheet()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.HideSheet("Sheet1", "hidden");
        workbook.CoreProperties.Creator = "Ada";
        workbook.HasCoreProperties = true;
        workbook.UnknownParts.AddPart("/xl/unknown/future.bin", "application/vnd.example.future", new byte[] { 1, 2, 3 });
        XlsxWorkbookSanitizeOptions options = new XlsxWorkbookSanitizeOptions();
        options.RemoveUnknownParts = false;
        options.RemovePersonalMetadata = true;
        options.RemoveHiddenSheets = true;
        options.RemoveMacros = false;
        options.RemoveExternalLinks = false;
        options.RemoveComments = false;
        options.RemoveCustomXmlParts = false;
        options.RemoveOleObjects = false;

        XlsxWorkbookSanitizeResult result = workbook.SanitizeWithOptions(options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HiddenSheetsRemoved, Is.EqualTo(0));
        Assert.That(result.HiddenSheetsUnhidden, Is.EqualTo(1));
        Assert.That(HasDiagnostic(result, "workbookSanitize.hiddenSheetRetained"), Is.True);
        Assert.That(result.PersonalMetadataPartsRemoved, Is.EqualTo(1));
        Assert.That(result.UnknownPartsRemoved, Is.EqualTo(0));
        Assert.That(workbook.GetSheet("Sheet1")!.State, Is.EqualTo(""));
        Assert.That(workbook.HasCoreProperties, Is.False);
        Assert.That(workbook.UnknownParts.ContainsPart("/xl/unknown/future.bin"), Is.True);
    }

    static byte[] CreateUnsafePackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"" + XlsxContentTypes.Xml + "\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.MacroEnabledWorkbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/comments/comment1.xml\" ContentType=\"" + XlsxContentTypes.Comments + "\" />" +
            "<Override PartName=\"/xl/drawings/vmlDrawing1.vml\" ContentType=\"" + XlsxContentTypes.VmlDrawing + "\" />" +
            "<Override PartName=\"/xl/externalLinks/externalLink1.xml\" ContentType=\"" + XlsxContentTypes.ExternalLink + "\" />" +
            "<Override PartName=\"/xl/vbaProject.bin\" ContentType=\"" + XlsxContentTypes.VbaProject + "\" />" +
            "<Override PartName=\"/xl/embeddings/oleObject1.bin\" ContentType=\"" + XlsxContentTypes.OleObject + "\" />" +
            "<Override PartName=\"/xl/embeddings/embeddedPackage1.xlsx\" ContentType=\"" + XlsxContentTypes.EmbeddedSpreadsheetPackage + "\" />" +
            "<Override PartName=\"/docProps/core.xml\" ContentType=\"" + XlsxContentTypes.CoreProperties + "\" />" +
            "<Override PartName=\"/docProps/app.xml\" ContentType=\"" + XlsxContentTypes.ExtendedProperties + "\" />" +
            "<Override PartName=\"/docProps/custom.xml\" ContentType=\"" + XlsxContentTypes.CustomProperties + "\" />" +
            "<Override PartName=\"/xl/unknown/future.bin\" ContentType=\"application/vnd.example.future\" />" +
            "</Types>"));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.OfficeDocument, "xl/workbook.xml") +
            Relationship("rIdCore", XlsxRelationshipTypes.CoreProperties, "docProps/core.xml") +
            Relationship("rIdApp", XlsxRelationshipTypes.ExtendedProperties, "docProps/app.xml") +
            Relationship("rIdCustom", XlsxRelationshipTypes.CustomProperties, "docProps/custom.xml") +
            Relationship("rIdCustomXml", XlsxRelationshipTypes.CustomXml, "customXml/item1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook,
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<externalReferences><externalReference r:id=\"rIdExternal\" /></externalReferences>" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" /><sheet name=\"Hidden\" sheetId=\"2\" state=\"veryHidden\" r:id=\"rIdSheet2\" /></sheets></workbook>"));
        entries.Add(CreateEntry(XlsxPackagePaths.WorkbookRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSheet1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            Relationship("rIdSheet2", XlsxRelationshipTypes.Worksheet, "worksheets/sheet2.xml") +
            Relationship("rIdExternal", XlsxRelationshipTypes.ExternalLink, "externalLinks/externalLink1.xml") +
            Relationship("rIdVba", XlsxRelationshipTypes.VbaProject, "vbaProject.bin") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1,
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheetData><row r=\"1\"><c r=\"A1\"><v>1</v></c></row></sheetData>" +
            "<legacyDrawing r:id=\"rIdVml\" />" +
            "<oleObjects><oleObject progId=\"Package\" shapeId=\"1025\" r:id=\"rIdOle\" /><oleObject progId=\"Excel.Sheet.12\" shapeId=\"1026\" r:id=\"rIdPackage\" /></oleObjects></worksheet>"));
        entries.Add(CreateEntry("/xl/worksheets/sheet2.xml",
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"str\"><v>secret</v></c></row></sheetData></worksheet>"));
        entries.Add(CreateEntry("/xl/worksheets/_rels/sheet1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdComments", XlsxRelationshipTypes.Comments, "../comments/comment1.xml") +
            Relationship("rIdVml", XlsxRelationshipTypes.VmlDrawing, "../drawings/vmlDrawing1.vml") +
            Relationship("rIdOle", XlsxRelationshipTypes.OleObject, "../embeddings/oleObject1.bin") +
            Relationship("rIdPackage", XlsxRelationshipTypes.EmbeddedPackage, "../embeddings/embeddedPackage1.xlsx") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/comments/comment1.xml",
            "<comments xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><authors><author>Ada</author></authors><commentList><comment ref=\"A1\" authorId=\"0\"><text><t>note</t></text></comment></commentList></comments>"));
        entries.Add(CreateEntry("/xl/drawings/vmlDrawing1.vml", "<xml xmlns:v=\"urn:schemas-microsoft-com:vml\" />"));
        entries.Add(CreateEntry("/xl/externalLinks/externalLink1.xml",
            "<externalLink xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><externalBook r:id=\"rIdExt\" /></externalLink>"));
        entries.Add(CreateEntry("/xl/externalLinks/_rels/externalLink1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rIdExt\" Type=\"" + XlsxRelationshipTypes.ExternalLinkPath + "\" Target=\"https://example.invalid/book.xlsx\" TargetMode=\"External\" /></Relationships>"));
        entries.Add(CreateEntry("/customXml/item1.xml", "<customer><name>Ada</name></customer>"));
        entries.Add(CreateEntry(XlsxPackagePaths.CoreProperties,
            "<cp:coreProperties xmlns:cp=\"" + XlsxNamespaces.CoreProperties + "\" xmlns:dc=\"" + XlsxNamespaces.DublinCore + "\"><dc:creator>Ada</dc:creator><cp:lastModifiedBy>Grace</cp:lastModifiedBy></cp:coreProperties>"));
        entries.Add(CreateEntry(XlsxPackagePaths.ExtendedProperties,
            "<Properties xmlns=\"" + XlsxNamespaces.ExtendedProperties + "\"><Manager>Ada</Manager><Company>Example</Company></Properties>"));
        entries.Add(CreateEntry(XlsxPackagePaths.CustomProperties,
            "<Properties xmlns=\"" + XlsxNamespaces.CustomProperties + "\" xmlns:vt=\"" + XlsxNamespaces.VariantTypes + "\"><property fmtid=\"{D5CDD505-2E9C-101B-9397-08002B2CF9AE}\" pid=\"2\" name=\"Owner\"><vt:lpwstr>Ada</vt:lpwstr></property></Properties>"));
        entries.Add(CreateEntry(XlsxPackagePaths.VbaProject, new byte[] { 1, 2, 3 }));
        entries.Add(CreateEntry("/xl/embeddings/oleObject1.bin", new byte[] { 4, 5, 6 }));
        entries.Add(CreateEntry("/xl/embeddings/embeddedPackage1.xlsx", new byte[] { 7, 8, 9 }));
        entries.Add(CreateEntry("/xl/unknown/future.bin", new byte[] { 10 }));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static XlsxPackageEntry CreateEntry(string path, string text)
    {
        return CreateEntry(path, Encoding.UTF8.GetBytes(text));
    }

    static XlsxPackageEntry CreateEntry(string path, byte[] data)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = data;
        entry.UncompressedBytes = entry.Data.Length;
        return entry;
    }

    static string Relationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" />";
    }

    static void AssertEntryMissing(XlsxZipPackageReadResult package, string path)
    {
        Assert.That(package.GetEntry(path), Is.Null);
    }

    static void AssertEntryText(XlsxZipPackageReadResult package, string path, string expected)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Contain(expected));
    }

    static void AssertEntryTextDoesNotContain(XlsxZipPackageReadResult package, string path, string unexpected)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Not.Contain(unexpected));
    }

    static bool HasDiagnostic(XlsxWorkbookSanitizeResult result, string code)
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
