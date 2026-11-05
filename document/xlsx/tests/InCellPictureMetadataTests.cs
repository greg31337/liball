using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class InCellPictureMetadataTests
{
    [Test]
    public void InCellPictureMetadataReaderWriter_PreservesRawXmlAndReadsRoot()
    {
        string xml = RichValueXml();

        XlsxInCellPictureMetadataPart? part = new XlsxInCellPictureMetadataPartReader().ReadString("/xl/richData/rdrichvalue.xml", XlsxContentTypes.RichValue, xml);
        XlsxInCellPictureMetadataPartWriteResult write = new XlsxInCellPictureMetadataPartWriter().WriteString(part);

        Assert.That(part!.IsValid, Is.True);
        Assert.That(part.PartPath, Is.EqualTo("/xl/richData/rdrichvalue.xml"));
        Assert.That(part.ContentType, Is.EqualTo(XlsxContentTypes.RichValue));
        Assert.That(part.IsRichDataPart, Is.True);
        Assert.That(part.RootElementName, Is.EqualTo("rvData"));
        Assert.That(part.RawXml, Is.EqualTo(xml));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Is.EqualTo(xml));
    }

    [Test]
    public void WorkbookOpenSave_PreservesInCellPictureAndImageFunctionMetadata()
    {
        byte[] bytes = CreateInCellPicturePackage();

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);
        XlsxWorkbookSaveResult saved = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saved.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.ListInCellPictureMetadataParts().Count, Is.EqualTo(4));
        Assert.That(FindMetadataPart(open.Workbook.InCellPictureMetadataParts, XlsxPackagePaths.SheetMetadata)!.IsSheetMetadata, Is.True);
        Assert.That(FindMetadataPart(open.Workbook.InCellPictureMetadataParts, "/xl/richData/rdrichvalue.xml")!.RootElementName, Is.EqualTo("rvData"));
        Assert.That(FindMetadataPart(open.Workbook.InCellPictureMetadataParts, "/xl/richData/rdRichValueTypes.xml")!.RootElementName, Is.EqualTo("rvTypesInfo"));
        Assert.That(FindMetadataPart(open.Workbook.InCellPictureMetadataParts, "/xl/richData/richValueRel.xml")!.RootElementName, Is.EqualTo("richValueRels"));
        Assert.That(saved.IsValid, Is.True);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.SheetMetadata);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.RichValue);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.RichValueTypes);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.RichValueRelationship);
        AssertEntryText(package, XlsxPackagePaths.Worksheet1, "_xlfn.IMAGE");
        AssertEntryText(package, "/xl/richData/richValueRel.xml", "r:id=\"rIdImage1\"");
        AssertEntryText(package, "/xl/richData/_rels/richValueRel.xml.rels", "Target=\"../media/image1.png\"");
        Assert.That(package.GetEntry("/xl/media/image1.png")!.Data, Is.EqualTo(PngBytes()));
    }

    [Test]
    public void InCellPictureMetadataWriter_ReportsInvalidPart()
    {
        XlsxInCellPictureMetadataPart invalid = new XlsxInCellPictureMetadataPart();
        invalid.Data = null!;

        XlsxInCellPictureMetadataPartWriteResult write = new XlsxInCellPictureMetadataPartWriter().WriteString(invalid);

        Assert.That(write.IsValid, Is.False);
        Assert.That(HasDiagnostic(write.Validation, "inCellPictureMetadataWriter.partPathMissing"), Is.True);
        Assert.That(HasDiagnostic(write.Validation, "inCellPictureMetadataWriter.dataNull"), Is.True);
    }

    static byte[] CreateInCellPicturePackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"" + XlsxContentTypes.Xml + "\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/metadata.xml\" ContentType=\"" + XlsxContentTypes.SheetMetadata + "\" />" +
            "<Override PartName=\"/xl/richData/rdrichvalue.xml\" ContentType=\"" + XlsxContentTypes.RichValue + "\" />" +
            "<Override PartName=\"/xl/richData/rdRichValueTypes.xml\" ContentType=\"" + XlsxContentTypes.RichValueTypes + "\" />" +
            "<Override PartName=\"/xl/richData/richValueRel.xml\" ContentType=\"" + XlsxContentTypes.RichValueRelationship + "\" />" +
            "<Override PartName=\"/xl/media/image1.png\" ContentType=\"image/png\" />" +
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
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"str\"><f>_xlfn.IMAGE(&quot;https://example.test/image.png&quot;,&quot;Logo&quot;)</f><v /></c></row></sheetData></worksheet>"));
        entries.Add(CreateEntry(XlsxPackagePaths.SheetMetadata, SheetMetadataXml()));
        entries.Add(CreateEntry("/xl/richData/rdrichvalue.xml", RichValueXml()));
        entries.Add(CreateEntry("/xl/richData/rdRichValueTypes.xml", RichValueTypesXml()));
        entries.Add(CreateEntry("/xl/richData/richValueRel.xml", RichValueRelXml()));
        entries.Add(CreateEntry("/xl/richData/_rels/richValueRel.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdImage1", XlsxRelationshipTypes.Image, "../media/image1.png") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/media/image1.png", PngBytes()));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static string SheetMetadataXml()
    {
        return "<metadata xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><metadataTypes count=\"1\"><metadataType name=\"XLDAPR\" minSupportedVersion=\"120000\" /></metadataTypes></metadata>";
    }

    static string RichValueXml()
    {
        return "<rvData xmlns=\"http://schemas.microsoft.com/office/spreadsheetml/2017/richdata\" count=\"1\"><rv s=\"0\"><v>0</v></rv></rvData>";
    }

    static string RichValueTypesXml()
    {
        return "<rvTypesInfo xmlns=\"http://schemas.microsoft.com/office/spreadsheetml/2017/richdata\"><global><key name=\"_localImagePath\" /></global></rvTypesInfo>";
    }

    static string RichValueRelXml()
    {
        return "<richValueRels xmlns=\"http://schemas.microsoft.com/office/spreadsheetml/2022/richvaluerel\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><rel r:id=\"rIdImage1\" /></richValueRels>";
    }

    static byte[] PngBytes()
    {
        return new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0 };
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

    static XlsxInCellPictureMetadataPart? FindMetadataPart(List<XlsxInCellPictureMetadataPart> parts, string path)
    {
        foreach (XlsxInCellPictureMetadataPart part in parts)
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
