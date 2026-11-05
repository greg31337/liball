using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class OleEmbeddedPackageTests
{
    [Test]
    public void EmbeddedObjectReaderWriter_PreservesPayloadAndClassifiesParts()
    {
        XlsxEmbeddedObjectPart ole = new XlsxEmbeddedObjectPartReader().ReadBytes("/xl/embeddings/oleObject1.bin", XlsxContentTypes.OleObject, OleBytes());
        XlsxEmbeddedObjectPart package = new XlsxEmbeddedObjectPartReader().ReadBytes("/xl/embeddings/embeddedPackage1.xlsx", XlsxContentTypes.EmbeddedSpreadsheetPackage, EmbeddedPackageBytes());
        XlsxEmbeddedObjectPartWriteResult oleWrite = new XlsxEmbeddedObjectPartWriter().WriteBytes(ole);
        XlsxEmbeddedObjectPartWriteResult packageWrite = new XlsxEmbeddedObjectPartWriter().WriteBytes(package);

        Assert.That(ole.IsValid, Is.True);
        Assert.That(ole.IsOleObject, Is.True);
        Assert.That(ole.IsEmbeddedPackage, Is.False);
        Assert.That(ole.Data, Is.EqualTo(OleBytes()));
        Assert.That(package.IsValid, Is.True);
        Assert.That(package.IsOleObject, Is.False);
        Assert.That(package.IsEmbeddedPackage, Is.True);
        Assert.That(package.Data, Is.EqualTo(EmbeddedPackageBytes()));
        Assert.That(oleWrite.IsValid, Is.True);
        Assert.That(oleWrite.Bytes, Is.EqualTo(OleBytes()));
        Assert.That(packageWrite.IsValid, Is.True);
        Assert.That(packageWrite.Bytes, Is.EqualTo(EmbeddedPackageBytes()));
    }

    [Test]
    public void WorkbookOpenSave_PreservesOleObjectsAndEmbeddedPackages()
    {
        byte[] bytes = CreateOlePackage();

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);
        XlsxWorkbookSaveResult saved = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saved.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.ListEmbeddedObjects().Count, Is.EqualTo(2));
        XlsxEmbeddedObjectPart ole = FindEmbeddedObject(open.Workbook.EmbeddedObjects, "/xl/embeddings/oleObject1.bin")!;
        XlsxEmbeddedObjectPart embeddedPackage = FindEmbeddedObject(open.Workbook.EmbeddedObjects, "/xl/embeddings/embeddedPackage1.xlsx")!;
        Assert.That(ole, Is.Not.Null);
        Assert.That(ole!.IsOleObject, Is.True);
        Assert.That(embeddedPackage, Is.Not.Null);
        Assert.That(embeddedPackage!.IsEmbeddedPackage, Is.True);
        Assert.That(saved.IsValid, Is.True);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.OleObject);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.EmbeddedSpreadsheetPackage);
        AssertEntryText(package, "/xl/worksheets/sheet1.xml", "<oleObjects>");
        AssertEntryText(package, "/xl/worksheets/_rels/sheet1.xml.rels", "Type=\"" + XlsxRelationshipTypes.OleObject + "\"");
        AssertEntryText(package, "/xl/worksheets/_rels/sheet1.xml.rels", "Type=\"" + XlsxRelationshipTypes.EmbeddedPackage + "\"");
        Assert.That(package.GetEntry("/xl/embeddings/oleObject1.bin")!.Data, Is.EqualTo(OleBytes()));
        Assert.That(package.GetEntry("/xl/embeddings/embeddedPackage1.xlsx")!.Data, Is.EqualTo(EmbeddedPackageBytes()));
    }

    [Test]
    public void EmbeddedObjectWriter_ReportsInvalidPart()
    {
        XlsxEmbeddedObjectPart invalid = new XlsxEmbeddedObjectPart();
        invalid.Data = null!;

        XlsxEmbeddedObjectPartWriteResult write = new XlsxEmbeddedObjectPartWriter().WriteBytes(invalid);

        Assert.That(write.IsValid, Is.False);
        Assert.That(HasDiagnostic(write.Validation, "embeddedObjectWriter.partPathMissing"), Is.True);
        Assert.That(HasDiagnostic(write.Validation, "embeddedObjectWriter.dataNull"), Is.True);
    }

    static byte[] CreateOlePackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"" + XlsxContentTypes.Xml + "\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/embeddings/oleObject1.bin\" ContentType=\"" + XlsxContentTypes.OleObject + "\" />" +
            "<Override PartName=\"/xl/embeddings/embeddedPackage1.xlsx\" ContentType=\"" + XlsxContentTypes.EmbeddedSpreadsheetPackage + "\" />" +
            "</Types>"));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.OfficeDocument, "xl/workbook.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook,
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" /></sheets></workbook>"));
        entries.Add(CreateEntry(XlsxPackagePaths.WorkbookRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSheet1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1,
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheetData /><oleObjects><oleObject progId=\"Package\" shapeId=\"1025\" r:id=\"rIdOleObject1\" /><oleObject progId=\"Excel.Sheet.12\" shapeId=\"1026\" r:id=\"rIdEmbeddedPackage1\" /></oleObjects></worksheet>"));
        entries.Add(CreateEntry("/xl/worksheets/_rels/sheet1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdOleObject1", XlsxRelationshipTypes.OleObject, "../embeddings/oleObject1.bin") +
            Relationship("rIdEmbeddedPackage1", XlsxRelationshipTypes.EmbeddedPackage, "../embeddings/embeddedPackage1.xlsx") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/embeddings/oleObject1.bin", OleBytes()));
        entries.Add(CreateEntry("/xl/embeddings/embeddedPackage1.xlsx", EmbeddedPackageBytes()));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static byte[] OleBytes()
    {
        return new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0x10, 0x20, 0x30, 0x40 };
    }

    static byte[] EmbeddedPackageBytes()
    {
        return new byte[] { 0x50, 0x4B, 0x03, 0x04, 0xAA, 0xBB, 0xCC, 0xDD };
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

    static XlsxEmbeddedObjectPart? FindEmbeddedObject(List<XlsxEmbeddedObjectPart> parts, string path)
    {
        foreach (XlsxEmbeddedObjectPart part in parts)
        {
            if (part.PartPath == path)
            {
                return part;
            }
        }

        return null;
    }

    static void AssertEntryText(XlsxZipPackageReadResult package, string path, string expected)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Contain(expected));
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
}
