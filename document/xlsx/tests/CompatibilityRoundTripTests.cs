using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class CompatibilityRoundTripTests
{
    [TestCase("Microsoft Excel 2007", XlsxNamespaces.OfficeExcel, false)]
    [TestCase("Microsoft Excel for Microsoft 365", XlsxNamespaces.Office2013Spreadsheet, false)]
    [TestCase("Strict OOXML", XlsxNamespaces.Office2010Spreadsheet, true)]
    [TestCase("Transitional OOXML", XlsxNamespaces.Office2010Spreadsheet, false)]
    [TestCase("LibreOffice Calc", "urn:org:documentfoundation:names:experimental:calc:xmlns:loext:1.0", false)]
    [TestCase("Google Sheets", "urn:google:sheets:extension", false)]
    [TestCase("Apple Numbers", "urn:apple:numbers:extension", false)]
    public void ProducerFixtures_RoundTripConformanceExtensionsAndUnknownParts(string producer, string extensionNamespace, bool strict)
    {
        byte[] fixture = CreateFixture(producer, extensionNamespace, strict, XlsxPackageVariant.Xlsx);
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.SelectedSheets.Add("Sheet1");
        IXlsxWorkbookOperations operations = new XlsxWorkbookOperations();

        XlsxWorkbookOpenResult firstOpen = operations.OpenBytesWithOptions(fixture, options);
        XlsxWorkbookSaveResult save = operations.SaveBytes(firstOpen.Workbook!);
        XlsxWorkbookOpenResult secondOpen = operations.OpenBytesWithOptions(save.Bytes, options);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, options);

        Assert.That(firstOpen.IsValid, Is.True, Diagnostics(firstOpen.Validation));
        Assert.That(save.IsValid, Is.True, Diagnostics(save.Validation));
        Assert.That(secondOpen.IsValid, Is.True, Diagnostics(secondOpen.Validation));
        Assert.That(secondOpen.Workbook!.Conformance, Is.EqualTo(strict ? XlsxPackageConformance.Strict : XlsxPackageConformance.Transitional));
        Assert.That(secondOpen.Workbook.ExtendedProperties.Application, Is.EqualTo(producer));
        Assert.That(secondOpen.Workbook.Worksheets[0].GetCell("A1")!.InlineStringText, Is.EqualTo(producer));
        Assert.That(secondOpen.Workbook.UnknownParts.ContainsPart("/xl/compat/producer.xml"), Is.True);
        AssertEntryContains(package, XlsxPackagePaths.Workbook, extensionNamespace);
        AssertEntryContains(package, XlsxPackagePaths.Workbook, strict ? XlsxNamespaces.StrictOfficeDocumentRelationships : XlsxNamespaces.OfficeDocumentRelationships);
        AssertEntryContains(package, XlsxPackagePaths.WorkbookRelationships, RelationshipType(strict, "worksheet"));
        Assert.That(package.GetEntry("/xl/compat/producer.xml")!.GetText(), Does.Contain(producer));
    }

    [TestCase(XlsxPackageVariant.Xlsx, XlsxContentTypes.Workbook, false)]
    [TestCase(XlsxPackageVariant.Xlsm, XlsxContentTypes.MacroEnabledWorkbook, true)]
    [TestCase(XlsxPackageVariant.Xltx, XlsxContentTypes.TemplateWorkbook, false)]
    [TestCase(XlsxPackageVariant.Xltm, XlsxContentTypes.MacroEnabledTemplateWorkbook, true)]
    public void PackageVariantFixtures_RoundTripContentTypeAndMacroPayload(XlsxPackageVariant variant, string contentType, bool macroEnabled)
    {
        byte[] fixture = CreateFixture("Variant " + variant.ToString(), XlsxNamespaces.Office2013Spreadsheet, false, variant);
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.AllowMacroEnabledPackages = macroEnabled;
        options.SelectedSheets.Add("Sheet1");

        XlsxWorkbookOpenResult firstOpen = new XlsxWorkbookOperations().OpenBytesWithOptions(fixture, options);
        XlsxWorkbookSaveResult save = firstOpen.Workbook!.SaveBytes();
        XlsxWorkbookOpenResult secondOpen = XlsxWorkbook.OpenBytes(save.Bytes, options);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, options);

        Assert.That(firstOpen.IsValid, Is.True, Diagnostics(firstOpen.Validation));
        Assert.That(save.IsValid, Is.True, Diagnostics(save.Validation));
        Assert.That(secondOpen.IsValid, Is.True, Diagnostics(secondOpen.Validation));
        Assert.That(secondOpen.Workbook!.PackageVariant, Is.EqualTo(variant));
        AssertEntryContains(package, XlsxPackagePaths.ContentTypes, contentType);
        Assert.That(package.GetEntry(XlsxPackagePaths.VbaProject) != null, Is.EqualTo(macroEnabled));
    }

    static byte[] CreateFixture(string producer, string extensionNamespace, bool strict, XlsxPackageVariant variant)
    {
        bool macroEnabled = variant == XlsxPackageVariant.Xlsm || variant == XlsxPackageVariant.Xltm;
        string spreadsheetNamespace = strict ? XlsxNamespaces.StrictSpreadsheet : XlsxNamespaces.Spreadsheet;
        string relationshipNamespace = strict ? XlsxNamespaces.StrictOfficeDocumentRelationships : XlsxNamespaces.OfficeDocumentRelationships;
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateTextEntry(XlsxPackagePaths.ContentTypes, ContentTypesXml(variant, macroEnabled)));
        entries.Add(CreateTextEntry(XlsxPackagePaths.PackageRelationships, PackageRelationshipsXml(strict)));
        entries.Add(CreateTextEntry(XlsxPackagePaths.Workbook, WorkbookXml(spreadsheetNamespace, relationshipNamespace, extensionNamespace)));
        entries.Add(CreateTextEntry(XlsxPackagePaths.WorkbookRelationships, WorkbookRelationshipsXml(strict, macroEnabled)));
        entries.Add(CreateTextEntry(XlsxPackagePaths.Worksheet1, WorksheetXml(spreadsheetNamespace, producer)));
        entries.Add(CreateTextEntry(XlsxPackagePaths.ExtendedProperties, ExtendedPropertiesXml(producer)));
        entries.Add(CreateTextEntry("/xl/compat/producer.xml", "<producer xmlns=\"" + extensionNamespace + "\">" + producer + "</producer>"));
        if (macroEnabled) { entries.Add(CreateBinaryEntry(XlsxPackagePaths.VbaProject, new byte[] { 1, 2, 3, 4 })); }
        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True, Diagnostics(write.Validation));
        return write.Bytes;
    }

    static string ContentTypesXml(XlsxPackageVariant variant, bool macroEnabled)
    {
        string xml = "<Types xmlns=\"" + XlsxNamespaces.ContentTypes + "\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"" + XlsxContentTypes.Xml + "\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + WorkbookContentType(variant) + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/docProps/app.xml\" ContentType=\"" + XlsxContentTypes.ExtendedProperties + "\" />" +
            "<Override PartName=\"/xl/compat/producer.xml\" ContentType=\"application/vnd.example.compatibility+xml\" />";
        if (macroEnabled) { xml = xml + "<Override PartName=\"/xl/vbaProject.bin\" ContentType=\"" + XlsxContentTypes.VbaProject + "\" />"; }
        return xml + "</Types>";
    }

    static string PackageRelationshipsXml(bool strict)
    {
        return "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            Relationship("rIdWorkbook", RelationshipType(strict, "officeDocument"), "xl/workbook.xml") +
            Relationship("rIdApp", RelationshipType(strict, "extended-properties"), "docProps/app.xml") +
            "</Relationships>";
    }

    static string WorkbookRelationshipsXml(bool strict, bool macroEnabled)
    {
        string xml = "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            Relationship("rIdSheet1", RelationshipType(strict, "worksheet"), "worksheets/sheet1.xml");
        if (macroEnabled) { xml = xml + Relationship("rIdVba", XlsxRelationshipTypes.VbaProject, "vbaProject.bin"); }
        return xml + "</Relationships>";
    }

    static string WorkbookXml(string spreadsheetNamespace, string relationshipNamespace, string extensionNamespace)
    {
        return "<workbook xmlns=\"" + spreadsheetNamespace + "\" xmlns:r=\"" + relationshipNamespace + "\" xmlns:mc=\"" + XlsxNamespaces.MarkupCompatibility + "\" xmlns:vendor=\"" + extensionNamespace + "\" mc:Ignorable=\"vendor\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" /></sheets>" +
            "<extLst><ext uri=\"{A90D8B32-59F4-40E3-8A97-8AB4017CCB9A}\"><vendor:compat value=\"1\" /></ext></extLst>" +
            "</workbook>";
    }

    static string WorksheetXml(string spreadsheetNamespace, string producer)
    {
        return "<worksheet xmlns=\"" + spreadsheetNamespace + "\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>" + producer + "</t></is></c></row></sheetData></worksheet>";
    }

    static string ExtendedPropertiesXml(string producer)
    {
        return "<Properties xmlns=\"" + XlsxNamespaces.ExtendedProperties + "\"><Application>" + producer + "</Application><AppVersion>1.0</AppVersion></Properties>";
    }

    static string RelationshipType(bool strict, string suffix)
    {
        string prefix = strict ? "http://purl.oclc.org/ooxml/officeDocument/relationships/" : "http://schemas.openxmlformats.org/officeDocument/2006/relationships/";
        return prefix + suffix;
    }

    static string Relationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" />";
    }

    static string WorkbookContentType(XlsxPackageVariant variant)
    {
        if (variant == XlsxPackageVariant.Xlsm) { return XlsxContentTypes.MacroEnabledWorkbook; }
        if (variant == XlsxPackageVariant.Xltx) { return XlsxContentTypes.TemplateWorkbook; }
        if (variant == XlsxPackageVariant.Xltm) { return XlsxContentTypes.MacroEnabledTemplateWorkbook; }
        return XlsxContentTypes.Workbook;
    }

    static XlsxPackageEntry CreateTextEntry(string path, string text)
    {
        return CreateBinaryEntry(path, Encoding.UTF8.GetBytes(text));
    }

    static XlsxPackageEntry CreateBinaryEntry(string path, byte[] data)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = data;
        return entry;
    }

    static void AssertEntryContains(XlsxZipPackageReadResult package, string path, string expected)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Contain(expected));
    }

    static string Diagnostics(XlsxValidationResult validation)
    {
        StringBuilder builder = new StringBuilder();
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics) { builder.Append(diagnostic.Code); builder.Append(": "); builder.Append(diagnostic.Message); builder.Append("\n"); }
        return builder.ToString();
    }
}
