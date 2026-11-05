using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class SecurityHardeningTests
{
    [Test]
    public void ZipPackageReader_RejectsTraversalAndBombLikePackages()
    {
        byte[] traversalZip = CreateRawZip(new RawZipEntry("../evil.xml", "<evil />"));
        XlsxZipPackageReadResult traversal = new XlsxZipPackageReader().ReadBytes(traversalZip, new XlsxReaderOptions());

        byte[] bombZip = CreateRawZip(new RawZipEntry("xl/worksheets/sheet1.xml", new string('A', 4096)));
        XlsxReaderOptions bombOptions = new XlsxReaderOptions();
        bombOptions.MaxCompressedBytes = 256;
        bombOptions.MaxUncompressedBytes = 256;
        XlsxZipPackageReadResult bomb = new XlsxZipPackageReader().ReadBytes(bombZip, bombOptions);

        Assert.That(traversal.IsValid, Is.False);
        Assert.That(HasDiagnostic(traversal.Validation, "path.parentSegment"), Is.True);
        Assert.That(bomb.IsValid, Is.False);
        Assert.That(HasDiagnostic(bomb.Validation, "zip.maxUncompressedBytes"), Is.True);
    }

    [Test]
    public void XmlSafety_BlocksExternalEntitiesAndXmlBombsBeforeExpansion()
    {
        string externalEntityXml = "<!DOCTYPE worksheet [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]>" +
            "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><sheetData><row><c><v>&xxe;</v></c></row></sheetData></worksheet>";
        string entityExpansionXml = "<!DOCTYPE worksheet [" +
            "<!ENTITY a \"1234567890\">" +
            "<!ENTITY b \"&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;\">" +
            "]><worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><sheetData><row><c><v>&b;</v></c></row></sheetData></worksheet>";

        XmlException? externalEntity = Assert.Throws<XmlException>(() => XlsxXmlReaderHelpers.ValidateStringLimits(externalEntityXml, new XlsxReaderOptions(), XlsxPackagePaths.Worksheet1));
        XmlException? entityExpansion = Assert.Throws<XmlException>(() => XlsxXmlReaderHelpers.ValidateStringLimits(entityExpansionXml, new XlsxReaderOptions(), XlsxPackagePaths.Worksheet1));

        Assert.That(externalEntity!.Message, Does.Contain("DTD"));
        Assert.That(entityExpansion!.Message, Does.Contain("DTD"));
    }

    [Test]
    public void WorkbookOpen_BlocksHostileXmlBeforeEntityResolution()
    {
        XmlException? exception = Assert.Throws<XmlException>(() => XlsxWorkbook.OpenBytes(CreateHostileXmlWorkbookPackage()));

        Assert.That(exception!.Message, Does.Contain("DTD"));
    }

    [Test]
    public void Relationships_RejectMaliciousInternalTargets()
    {
        string xml = "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            Relationship("rIdExternal", XlsxRelationshipTypes.Worksheet, "https://example.invalid/payload.xml") +
            Relationship("rIdTraversal", XlsxRelationshipTypes.Worksheet, "../../evil.xml") +
            "</Relationships>";

        XlsxRelationshipsPart relationships = new XlsxPartRelationshipsReader().ReadString(XlsxPackagePaths.Workbook, xml);

        Assert.That(relationships.IsValid, Is.False);
        Assert.That(HasDiagnostic(relationships.Validation, "relationship.target.externalModeRequired"), Is.True);
        Assert.That(HasDiagnostic(relationships.Validation, "path.traversal"), Is.True);
    }

    [Test]
    public void WorkbookValidator_ReportsExcessiveLoadedDimensions()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.PartPath = XlsxPackagePaths.Worksheet1;
        worksheet.SetNumberValue("C3", 1);
        workbook.Worksheets.Add(worksheet);

        XlsxWorkbookValidationOptions options = new XlsxWorkbookValidationOptions();
        options.ReaderOptions.MaxRows = 2;
        options.ReaderOptions.MaxColumns = 2;
        XlsxValidationResult result = workbook.ValidateWithOptions(options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "workbookValidator.rowLimit"), Is.True);
        Assert.That(HasDiagnostic(result, "workbookValidator.columnLimit"), Is.True);
    }

    [Test]
    public void Sanitize_StripsActiveContentAndUnsafeUnknownParts()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.PackageVariant = XlsxPackageVariant.Xlsm;
        workbook.HasVbaProject = true;
        workbook.VbaProject.Data = new byte[] { 0xD0, 0xCF, 0x11, 0xE0 };
        workbook.WorkbookRelationships.Relationships.Add(CreateRelationship("rIdVba", XlsxRelationshipTypes.VbaProject, "vbaProject.bin"));
        workbook.WorkbookRelationships.Relationships.Add(CreateRelationship("rIdExternal", XlsxRelationshipTypes.ExternalLink, "externalLinks/externalLink1.xml"));
        workbook.PackageRelationships.Relationships.Add(CreateRelationship("rIdCustomXml", XlsxRelationshipTypes.CustomXml, "customXml/item1.xml"));
        workbook.ListExternalLinks().Add(new XlsxExternalLinkPart());
        workbook.ListCustomXmlParts().Add(new XlsxCustomXmlPart());
        workbook.ListEmbeddedObjects().Add(CreateEmbeddedObject("/xl/embeddings/oleObject1.bin", XlsxContentTypes.OleObject));
        workbook.UnknownParts.AddPart(XlsxPackagePaths.VbaProject, XlsxContentTypes.VbaProject, new byte[] { 1 });
        workbook.UnknownParts.AddPart("/xl/externalLinks/externalLink1.xml", XlsxContentTypes.ExternalLink, Encoding.UTF8.GetBytes("<externalLink />"));
        workbook.UnknownParts.AddPart("/customXml/item1.xml", XlsxContentTypes.Xml, Encoding.UTF8.GetBytes("<custom />"));
        workbook.UnknownParts.AddPart("/xl/embeddings/oleObject1.bin", XlsxContentTypes.OleObject, new byte[] { 2 });
        workbook.UnknownParts.AddPart("/xl/unknown/active.bin", "application/vnd.example.active", new byte[] { 3 });

        XlsxWorkbookSanitizeResult sanitize = workbook.Sanitize();
        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.That(sanitize.IsValid, Is.True);
        Assert.That(sanitize.MacrosRemoved, Is.GreaterThan(0));
        Assert.That(sanitize.ExternalLinksRemoved, Is.GreaterThan(0));
        Assert.That(sanitize.CustomXmlPartsRemoved, Is.GreaterThan(0));
        Assert.That(sanitize.OleObjectsRemoved, Is.GreaterThan(0));
        Assert.That(sanitize.UnknownPartsRemoved, Is.GreaterThan(0));
        Assert.That(workbook.PackageVariant, Is.EqualTo(XlsxPackageVariant.Xlsx));
        Assert.That(workbook.HasVbaProject, Is.False);
        Assert.That(workbook.ListExternalLinks().Count, Is.EqualTo(0));
        Assert.That(workbook.ListCustomXmlParts().Count, Is.EqualTo(0));
        Assert.That(workbook.ListEmbeddedObjects().Count, Is.EqualTo(0));
        Assert.That(save.IsValid, Is.True);
        Assert.That(package.GetEntry(XlsxPackagePaths.VbaProject), Is.Null);
        Assert.That(package.GetEntry("/xl/externalLinks/externalLink1.xml"), Is.Null);
        Assert.That(package.GetEntry("/customXml/item1.xml"), Is.Null);
        Assert.That(package.GetEntry("/xl/embeddings/oleObject1.bin"), Is.Null);
        Assert.That(package.GetEntry("/xl/unknown/active.bin"), Is.Null);
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.MacroEnabledWorkbook);
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.WorkbookRelationships, XlsxRelationshipTypes.VbaProject);
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.WorkbookRelationships, XlsxRelationshipTypes.ExternalLink);
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.PackageRelationships, XlsxRelationshipTypes.CustomXml);
    }

    static byte[] CreateHostileXmlWorkbookPackage()
    {
        string hostileWorkbookXml = "<!DOCTYPE workbook [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]>" +
            "<workbook xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\">" +
            "<sheets><sheet name=\"&xxe;\" sheetId=\"1\" r:id=\"rIdSheet1\" /></sheets></workbook>";
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"" + XlsxNamespaces.ContentTypes + "\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"" + XlsxContentTypes.Xml + "\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "</Types>"));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships,
            "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            Relationship("rIdWorkbook", XlsxRelationshipTypes.OfficeDocument, "xl/workbook.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook, hostileWorkbookXml));
        entries.Add(CreateEntry(XlsxPackagePaths.WorkbookRelationships,
            "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            Relationship("rIdSheet1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1,
            "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><sheetData /></worksheet>"));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static XlsxEmbeddedObjectPart CreateEmbeddedObject(string partPath, string contentType)
    {
        XlsxEmbeddedObjectPart part = new XlsxEmbeddedObjectPart();
        part.PartPath = partPath;
        part.ContentType = contentType;
        part.Data = new byte[] { 1, 2, 3 };
        part.IsOleObject = true;
        return part;
    }

    static XlsxRelationship CreateRelationship(string id, string type, string target)
    {
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = id;
        relationship.Type = type;
        relationship.Target = target;
        return relationship;
    }

    static XlsxPackageEntry CreateEntry(string path, string text)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = Encoding.UTF8.GetBytes(text);
        entry.UncompressedBytes = entry.Data.Length;
        return entry;
    }

    static byte[] CreateRawZip(params RawZipEntry[] entries)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                foreach (RawZipEntry rawEntry in entries)
                {
                    ZipArchiveEntry entry = archive.CreateEntry(rawEntry.Name, CompressionLevel.SmallestSize);
                    using (Stream entryStream = entry.Open())
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(rawEntry.Text);
                        entryStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }

            return stream.ToArray();
        }
    }

    static string Relationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" />";
    }

    static void AssertEntryTextDoesNotContain(XlsxZipPackageReadResult package, string path, string unexpected)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Not.Contain(unexpected));
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

    class RawZipEntry
    {
        public string Name;
        public string Text;

        public RawZipEntry(string name, string text)
        {
            Name = name;
            Text = text;
        }
    }
}
