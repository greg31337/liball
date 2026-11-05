using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class CustomXmlPartTests
{
    [Test]
    public void CustomXmlReaderWriter_PreservesBytesAndReadsRootMetadata()
    {
        string xml = CustomXmlItemXml();

        XlsxCustomXmlPart? part = new XlsxCustomXmlPartReader().ReadString("/customXml/item1.xml", XlsxContentTypes.Xml, xml);
        XlsxCustomXmlPartWriteResult write = new XlsxCustomXmlPartWriter().WriteString(part);

        Assert.That(part!.IsValid, Is.True);
        Assert.That(part.PartPath, Is.EqualTo("/customXml/item1.xml"));
        Assert.That(part.ContentType, Is.EqualTo(XlsxContentTypes.Xml));
        Assert.That(part.RootElementName, Is.EqualTo("customer"));
        Assert.That(part.RootNamespaceUri, Is.EqualTo("urn:example:customer"));
        Assert.That(part.RawXml, Is.EqualTo(xml));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Is.EqualTo(xml));
    }

    [Test]
    public void WorkbookOpenSave_ListsPreservesAndRemovesCustomXmlParts()
    {
        byte[] bytes = CreateCustomXmlPackage();
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);
        XlsxWorkbookSaveResult preserved = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult preservedPackage = new XlsxZipPackageReader().ReadBytes(preserved.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.ListCustomXmlParts().Count, Is.EqualTo(2));
        Assert.That(open.Workbook.CustomXmlParts[0].RootElementName, Is.EqualTo("customer"));
        Assert.That(open.Workbook.CustomXmlParts[1].IsPropertiesPart, Is.True);
        Assert.That(preserved.IsValid, Is.True);
        AssertEntryText(preservedPackage, XlsxPackagePaths.PackageRelationships, "Type=\"" + XlsxRelationshipTypes.CustomXml + "\"");
        AssertEntryText(preservedPackage, "/customXml/item1.xml", "<customer");
        AssertEntryText(preservedPackage, "/customXml/itemProps1.xml", "datastoreItem");
        AssertEntryText(preservedPackage, "/customXml/_rels/item1.xml.rels", XlsxRelationshipTypes.CustomXmlProperties);
        AssertEntryText(preservedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.CustomXmlProperties);

        XlsxValidationResult remove = open.Workbook.RemoveCustomXmlParts();
        XlsxWorkbookSaveResult removed = open.Workbook.SaveBytes();
        XlsxZipPackageReadResult removedPackage = new XlsxZipPackageReader().ReadBytes(removed.Bytes, new XlsxReaderOptions());

        Assert.That(remove.IsValid, Is.True);
        Assert.That(open.Workbook.ListCustomXmlParts().Count, Is.EqualTo(0));
        Assert.That(removed.IsValid, Is.True);
        Assert.That(removedPackage.GetEntry("/customXml/item1.xml"), Is.Null);
        Assert.That(removedPackage.GetEntry("/customXml/itemProps1.xml"), Is.Null);
        Assert.That(removedPackage.GetEntry("/customXml/_rels/item1.xml.rels"), Is.Null);
        AssertEntryTextDoesNotContain(removedPackage, XlsxPackagePaths.PackageRelationships, XlsxRelationshipTypes.CustomXml);
        AssertEntryTextDoesNotContain(removedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.CustomXmlProperties);
    }

    [Test]
    public void CustomXmlWriter_ReportsInvalidPart()
    {
        XlsxCustomXmlPart invalid = new XlsxCustomXmlPart();
        invalid.Data = null!;

        XlsxCustomXmlPartWriteResult write = new XlsxCustomXmlPartWriter().WriteString(invalid);

        Assert.That(write.IsValid, Is.False);
        Assert.That(HasDiagnostic(write.Validation, "customXmlWriter.partPathMissing"), Is.True);
        Assert.That(HasDiagnostic(write.Validation, "customXmlWriter.dataNull"), Is.True);
    }

    static byte[] CreateCustomXmlPackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"" + XlsxContentTypes.Xml + "\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/customXml/itemProps1.xml\" ContentType=\"" + XlsxContentTypes.CustomXmlProperties + "\" />" +
            "</Types>"));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.OfficeDocument, "xl/workbook.xml") +
            Relationship("rIdCustomXml1", XlsxRelationshipTypes.CustomXml, "customXml/item1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook,
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" /></sheets></workbook>"));
        entries.Add(CreateEntry(XlsxPackagePaths.WorkbookRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSheet1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1,
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData /></worksheet>"));
        entries.Add(CreateEntry("/customXml/item1.xml", CustomXmlItemXml()));
        entries.Add(CreateEntry("/customXml/itemProps1.xml", CustomXmlPropertiesXml()));
        entries.Add(CreateEntry("/customXml/_rels/item1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdProps", XlsxRelationshipTypes.CustomXmlProperties, "itemProps1.xml") +
            "</Relationships>"));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static string CustomXmlItemXml()
    {
        return "<customer xmlns=\"urn:example:customer\"><name>Ada</name><tier>gold</tier></customer>";
    }

    static string CustomXmlPropertiesXml()
    {
        return "<ds:datastoreItem ds:itemID=\"{11111111-1111-1111-1111-111111111111}\" xmlns:ds=\"http://schemas.openxmlformats.org/officeDocument/2006/customXml\"><ds:schemaRefs /></ds:datastoreItem>";
    }

    static XlsxPackageEntry CreateEntry(string path, string text)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = Encoding.UTF8.GetBytes(text);
        entry.UncompressedBytes = entry.Data.Length;
        return entry;
    }

    static string Relationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" />";
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
