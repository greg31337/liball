using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ExternalLinkTests
{
    [Test]
    public void ExternalLinkReaderWriter_ReadsExternalBookSheetsAndRelationship()
    {
        string xml = ExternalLinkXml();
        XlsxRelationshipsPart relationships = ReadRelationships(
            "/xl/externalLinks/externalLink1.xml",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            ExternalRelationship("rIdExternalBook1", XlsxRelationshipTypes.ExternalLinkPath, "file:///C:/Data/source.xlsx") +
            "</Relationships>");

        XlsxExternalLinkPart part = new XlsxExternalLinkPartReader().ReadStringWithRelationships("/xl/externalLinks/externalLink1.xml", xml, relationships);
        XlsxExternalLinkPart detached = new XlsxExternalLinkPartReader().ReadString("/xl/externalLinks/externalLink1.xml", xml);
        new XlsxExternalLinkPartOperations().ResolveExternalBookRelationship(detached, relationships);
        XlsxExternalLinkPart generated = new XlsxExternalLinkPart();
        generated.RelationshipId = "rIdExternalBook1";
        generated.SheetNames.Add("Sheet1");
        generated.SheetNames.Add("Data");
        XlsxExternalLinkPartWriteResult write = new XlsxExternalLinkPartWriter().WriteString(generated);
        XlsxExternalLinkPart readBack = new XlsxExternalLinkPartReader().ReadString("/xl/externalLinks/externalLink1.xml", write.Xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.RawXml, Is.EqualTo(xml));
        Assert.That(part.RelationshipId, Is.EqualTo("rIdExternalBook1"));
        Assert.That(part.SheetNames.Count, Is.EqualTo(2));
        Assert.That(part.HasRelationship, Is.True);
        Assert.That(detached.HasRelationship, Is.True);
        Assert.That(part.Target, Is.EqualTo("file:///C:/Data/source.xlsx"));
        Assert.That(part.TargetMode, Is.EqualTo("External"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<externalBook r:id=\"rIdExternalBook1\">"));
        Assert.That(write.Xml, Does.Contain("<sheetName val=\"Data\" />"));
        Assert.That(readBack.SheetNames.Count, Is.EqualTo(2));
    }

    [Test]
    public void WorkbookOpenSave_ListsAndRemovesExternalLinks()
    {
        byte[] bytes = CreateExternalLinkPackage();
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);
        XlsxWorkbookSaveResult preserved = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult preservedPackage = new XlsxZipPackageReader().ReadBytes(preserved.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.ListExternalLinks().Count, Is.EqualTo(1));
        Assert.That(open.Workbook.WorkbookPart.ExternalReferences.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.ExternalLinks[0].Target, Is.EqualTo("file:///C:/Data/source.xlsx"));
        Assert.That(preserved.IsValid, Is.True);
        AssertEntryText(preservedPackage, XlsxPackagePaths.Workbook, "<externalReferences>");
        AssertEntryText(preservedPackage, XlsxPackagePaths.WorkbookRelationships, "Type=\"" + XlsxRelationshipTypes.ExternalLink + "\"");
        AssertEntryText(preservedPackage, "/xl/externalLinks/externalLink1.xml", "<sheetName val=\"Sheet1\"");
        AssertEntryText(preservedPackage, "/xl/externalLinks/_rels/externalLink1.xml.rels", "TargetMode=\"External\"");

        XlsxValidationResult remove = open.Workbook.RemoveExternalLinks();
        XlsxWorkbookSaveResult removed = open.Workbook.SaveBytes();
        XlsxZipPackageReadResult removedPackage = new XlsxZipPackageReader().ReadBytes(removed.Bytes, new XlsxReaderOptions());

        Assert.That(remove.IsValid, Is.True);
        Assert.That(open.Workbook.ListExternalLinks().Count, Is.EqualTo(0));
        Assert.That(open.Workbook.WorkbookPart.ExternalReferences.Count, Is.EqualTo(0));
        Assert.That(removed.IsValid, Is.True);
        Assert.That(removedPackage.GetEntry("/xl/externalLinks/externalLink1.xml"), Is.Null);
        Assert.That(removedPackage.GetEntry("/xl/externalLinks/_rels/externalLink1.xml.rels"), Is.Null);
        AssertEntryTextDoesNotContain(removedPackage, XlsxPackagePaths.Workbook, "externalReferences");
        AssertEntryTextDoesNotContain(removedPackage, XlsxPackagePaths.WorkbookRelationships, XlsxRelationshipTypes.ExternalLink);
        AssertEntryTextDoesNotContain(removedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.ExternalLink);
    }

    [Test]
    public void ExternalLinkWriter_ReportsInvalidPart()
    {
        XlsxExternalLinkPart invalid = new XlsxExternalLinkPart();
        invalid.SheetNames = null!;

        XlsxExternalLinkPartWriteResult write = new XlsxExternalLinkPartWriter().WriteString(invalid);

        Assert.That(write.IsValid, Is.False);
        Assert.That(HasDiagnostic(write, "externalLinkWriter.relationshipIdMissing"), Is.True);
        Assert.That(HasDiagnostic(write, "externalLinkWriter.sheetNamesNull"), Is.True);
    }

    static byte[] CreateExternalLinkPackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/externalLinks/externalLink1.xml\" ContentType=\"" + XlsxContentTypes.ExternalLink + "\" />" +
            "</Types>"));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.OfficeDocument, "xl/workbook.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook,
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" /></sheets><externalReferences><externalReference r:id=\"rIdExternalLink1\" /></externalReferences></workbook>"));
        entries.Add(CreateEntry(XlsxPackagePaths.WorkbookRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSheet1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            Relationship("rIdExternalLink1", XlsxRelationshipTypes.ExternalLink, "externalLinks/externalLink1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1,
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData /></worksheet>"));
        entries.Add(CreateEntry("/xl/externalLinks/externalLink1.xml", ExternalLinkXml()));
        entries.Add(CreateEntry("/xl/externalLinks/_rels/externalLink1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            ExternalRelationship("rIdExternalBook1", XlsxRelationshipTypes.ExternalLinkPath, "file:///C:/Data/source.xlsx") +
            "</Relationships>"));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static string ExternalLinkXml()
    {
        return "<externalLink xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><externalBook r:id=\"rIdExternalBook1\"><sheetNames><sheetName val=\"Sheet1\" /><sheetName val=\"Data\" /></sheetNames></externalBook></externalLink>";
    }

    static XlsxPackageEntry CreateEntry(string path, string text)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = Encoding.UTF8.GetBytes(text);
        entry.UncompressedBytes = entry.Data.Length;
        return entry;
    }

    static XlsxRelationshipsPart ReadRelationships(string sourcePartPath, string xml)
    {
        return new XlsxPartRelationshipsReader().ReadString(sourcePartPath, xml);
    }

    static string Relationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" />";
    }

    static string ExternalRelationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" TargetMode=\"External\" />";
    }

    static void AssertEntryText(XlsxZipPackageReadResult package, string path, string expected)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Contain(expected));
    }

    static void AssertEntryTextDoesNotContain(XlsxZipPackageReadResult package, string path, string text)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Not.Contain(text));
    }

    static bool HasDiagnostic(XlsxExternalLinkPartWriteResult result, string code)
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
