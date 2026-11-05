using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class DigitalSignaturePreservationTests
{
    [Test]
    public void DigitalSignatureReaderWriter_PreservesSignatureXml()
    {
        string xml = SignatureXml();

        XlsxDigitalSignaturePart? part = new XlsxDigitalSignaturePartReader().ReadString("/_xmlsignatures/sig1.xml", XlsxContentTypes.DigitalSignatureXml, xml);
        XlsxDigitalSignaturePartWriteResult write = new XlsxDigitalSignaturePartWriter().WriteString(part);

        Assert.That(part!.IsValid, Is.True);
        Assert.That(part.IsSignatureXmlPart, Is.True);
        Assert.That(part.RootElementName, Is.EqualTo("Signature"));
        Assert.That(part.RootNamespaceUri, Is.EqualTo("http://www.w3.org/2000/09/xmldsig#"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Is.EqualTo(xml));
    }

    [Test]
    public void WorkbookOpenSave_PreservesDigitalSignaturePartsAndReportsWarning()
    {
        byte[] bytes = CreateSignedPackage(true);

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);
        XlsxWorkbookSaveResult saved = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saved.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.HasDigitalSignatures, Is.True);
        Assert.That(open.Workbook.ListDigitalSignatures().Count, Is.EqualTo(2));
        Assert.That(FindSignaturePart(open.Workbook.DigitalSignatures, XlsxPackagePaths.DigitalSignatureOrigin)!.IsOriginPart, Is.True);
        Assert.That(FindSignaturePart(open.Workbook.DigitalSignatures, "/_xmlsignatures/sig1.xml")!.RootElementName, Is.EqualTo("Signature"));
        Assert.That(HasDiagnostic(open.Validation, "workbook.open.digitalSignaturesPresent"), Is.True);
        Assert.That(saved.IsValid, Is.True);
        AssertEntryText(package, XlsxPackagePaths.PackageRelationships, XlsxRelationshipTypes.DigitalSignatureOrigin);
        AssertEntryText(package, XlsxPackagePath.GetRelationshipPartPath(XlsxPackagePaths.DigitalSignatureOrigin), XlsxRelationshipTypes.DigitalSignature);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.DigitalSignatureOrigin);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.DigitalSignatureXml);
        AssertEntryText(package, "/_xmlsignatures/sig1.xml", "<ds:Signature");
        Assert.That(package.GetEntry(XlsxPackagePaths.DigitalSignatureOrigin), Is.Not.Null);
    }

    [Test]
    public void WorkbookOpen_ReportsMissingDigitalSignatureOriginPart()
    {
        byte[] bytes = CreateSignedPackage(false);

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);

        Assert.That(open.IsValid, Is.False);
        Assert.That(HasDiagnostic(open.Validation, "digitalSignature.originPartMissing"), Is.True);
    }

    [Test]
    public void DigitalSignatureWriter_ReportsInvalidPart()
    {
        XlsxDigitalSignaturePart invalid = new XlsxDigitalSignaturePart();
        invalid.Data = null!;

        XlsxDigitalSignaturePartWriteResult write = new XlsxDigitalSignaturePartWriter().WriteBytes(invalid);

        Assert.That(write.IsValid, Is.False);
        Assert.That(HasDiagnostic(write.Validation, "digitalSignatureWriter.partPathMissing"), Is.True);
        Assert.That(HasDiagnostic(write.Validation, "digitalSignatureWriter.dataNull"), Is.True);
    }

    static byte[] CreateSignedPackage(bool includeOriginPart)
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"" + XlsxContentTypes.Xml + "\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/_xmlsignatures/origin.sigs\" ContentType=\"" + XlsxContentTypes.DigitalSignatureOrigin + "\" />" +
            "<Override PartName=\"/_xmlsignatures/sig1.xml\" ContentType=\"" + XlsxContentTypes.DigitalSignatureXml + "\" />" +
            "</Types>"));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.OfficeDocument, "xl/workbook.xml") +
            Relationship("rIdSignatureOrigin", XlsxRelationshipTypes.DigitalSignatureOrigin, "_xmlsignatures/origin.sigs") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook,
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" /></sheets></workbook>"));
        entries.Add(CreateEntry(XlsxPackagePaths.WorkbookRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSheet1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1,
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData /></worksheet>"));
        if (includeOriginPart)
        {
            entries.Add(CreateEntry(XlsxPackagePaths.DigitalSignatureOrigin, new byte[0]));
        }

        entries.Add(CreateEntry(XlsxPackagePath.GetRelationshipPartPath(XlsxPackagePaths.DigitalSignatureOrigin),
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSignature1", XlsxRelationshipTypes.DigitalSignature, "sig1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry("/_xmlsignatures/sig1.xml", SignatureXml()));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static string SignatureXml()
    {
        return "<ds:Signature xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\"><ds:SignedInfo /><ds:SignatureValue>abc</ds:SignatureValue></ds:Signature>";
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

    static XlsxDigitalSignaturePart? FindSignaturePart(List<XlsxDigitalSignaturePart> parts, string path)
    {
        foreach (XlsxDigitalSignaturePart part in parts)
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
