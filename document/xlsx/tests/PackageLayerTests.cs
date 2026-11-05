using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PackageLayerTests
{
    [Test]
    public void PackagePaths_NormalizeZipNamesAndResolveRelationshipTargets()
    {
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath("xl/workbook.xml");
        XlsxPackagePathResult invalidBackslash = XlsxPackagePath.NormalizePartPath("xl\\workbook.xml");
        XlsxPackagePathResult worksheetTarget = XlsxPackagePath.ResolveRelationshipTarget("/xl/worksheets/sheet1.xml", "../styles.xml", false);
        XlsxPackagePathResult externalTarget = XlsxPackagePath.ResolveRelationshipTarget("/xl/workbook.xml", "https://example.com/book.xlsx", true);
        XlsxPackagePathResult traversalTarget = XlsxPackagePath.ResolveRelationshipTarget("/xl/workbook.xml", "../../evil.xml", false);

        Assert.That(normalized.IsValid, Is.True);
        Assert.That(normalized.Path, Is.EqualTo("/xl/workbook.xml"));
        Assert.That(XlsxPackagePath.GetZipEntryName(normalized.Path), Is.EqualTo("xl/workbook.xml"));
        Assert.That(invalidBackslash.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidBackslash.Validation, "path.backslash"), Is.True);
        Assert.That(worksheetTarget.IsValid, Is.True);
        Assert.That(worksheetTarget.Path, Is.EqualTo("/xl/styles.xml"));
        Assert.That(externalTarget.IsValid, Is.True);
        Assert.That(externalTarget.IsExternal, Is.True);
        Assert.That(externalTarget.ExternalTarget, Is.EqualTo("https://example.com/book.xlsx"));
        Assert.That(traversalTarget.IsValid, Is.False);
        Assert.That(HasDiagnostic(traversalTarget.Validation, "path.traversal"), Is.True);
    }

    [Test]
    public void ContentTypes_ReadWriteDefaultsOverridesAndUnknownMetadata()
    {
        string xml = "<Types xmlns=\"" + XlsxNamespaces.ContentTypes + "\" xmlns:x=\"urn:x\" x:root=\"1\">" +
            "<Default Extension=\"xml\" ContentType=\"" + XlsxContentTypes.Xml + "\" x:item=\"d\" />" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" x:item=\"o\" />" +
            "<x:future />" +
            "</Types>";

        XlsxContentTypesPart part = new XlsxContentTypesReader().ReadString(xml);
        XlsxContentTypesWriteResult write = new XlsxContentTypesWriter().WriteString(part);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.GetContentType("/xl/workbook.xml"), Is.EqualTo(XlsxContentTypes.Workbook));
        Assert.That(part.GetContentType("/xl/worksheets/sheet1.xml"), Is.EqualTo(XlsxContentTypes.Xml));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("x:root=\"1\""));
        Assert.That(write.Xml, Does.Contain("<x:future"));
    }

    [Test]
    public void Relationships_ReadWriteResolveExternalTargetsAndDetectDuplicates()
    {
        string xml = "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rIdTable\" Type=\"" + XlsxRelationshipTypes.Table + "\" Target=\"../tables/table1.xml\" />" +
            "<Relationship Id=\"rIdLink\" Type=\"" + XlsxRelationshipTypes.Hyperlink + "\" Target=\"https://example.com\" TargetMode=\"External\" />" +
            "</Relationships>";
        XlsxRelationshipsPart part = new XlsxPartRelationshipsReader().ReadString("/xl/worksheets/_rels/sheet1.xml.rels", xml);
        XlsxRelationshipsWriteResult write = new XlsxPartRelationshipsWriter().WriteString(part);

        XlsxRelationshipsPart duplicate = new XlsxRelationshipsPart();
        duplicate.SourcePartPath = XlsxPackagePaths.Workbook;
        duplicate.PartPath = XlsxPackagePaths.WorkbookRelationships;
        duplicate.Relationships.Add(CreateRelationship("rId1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml"));
        duplicate.Relationships.Add(CreateRelationship("rId1", XlsxRelationshipTypes.Styles, "styles.xml"));
        XlsxRelationshipsWriteResult duplicateWrite = new XlsxPartRelationshipsWriter().WriteString(duplicate);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.GetRelationship("rIdTable")!.TargetPath, Is.EqualTo("/xl/tables/table1.xml"));
        Assert.That(part.GetRelationship("rIdLink")!.IsExternal, Is.True);
        Assert.That(part.GetRelationship("rIdLink")!.ExternalTarget, Is.EqualTo("https://example.com"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("TargetMode=\"External\""));
        Assert.That(duplicateWrite.IsValid, Is.False);
        Assert.That(HasDiagnostic(duplicateWrite.Validation, "relationships.duplicateId"), Is.True);
    }

    [Test]
    public void ZipPackage_EnforcesLimitsRejectsDuplicatesAndWritesDeterministically()
    {
        List<XlsxPackageEntry> firstOrder = new List<XlsxPackageEntry>();
        firstOrder.Add(CreateEntry("/xl/b.xml", "B"));
        firstOrder.Add(CreateEntry("/xl/a.xml", "A"));

        List<XlsxPackageEntry> secondOrder = new List<XlsxPackageEntry>();
        secondOrder.Add(CreateEntry("/xl/a.xml", "A"));
        secondOrder.Add(CreateEntry("/xl/b.xml", "B"));

        XlsxWriterOptions writerOptions = new XlsxWriterOptions();
        writerOptions.DeterministicOutput = true;
        XlsxZipPackageWriteResult firstWrite = new XlsxZipPackageWriter().WriteBytes(firstOrder, writerOptions);
        XlsxZipPackageWriteResult secondWrite = new XlsxZipPackageWriter().WriteBytes(secondOrder, writerOptions);

        XlsxReaderOptions tightReaderOptions = new XlsxReaderOptions();
        tightReaderOptions.MaxZipEntries = 1;
        XlsxZipPackageReadResult limitedRead = new XlsxZipPackageReader().ReadBytes(firstWrite.Bytes, tightReaderOptions);

        List<XlsxPackageEntry> duplicateEntries = new List<XlsxPackageEntry>();
        duplicateEntries.Add(CreateEntry("/xl/a.xml", "A1"));
        duplicateEntries.Add(CreateEntry("/xl/a.xml", "A2"));
        XlsxZipPackageWriteResult duplicateWrite = new XlsxZipPackageWriter().WriteBytes(duplicateEntries, new XlsxWriterOptions());

        Assert.That(firstWrite.IsValid, Is.True);
        Assert.That(secondWrite.IsValid, Is.True);
        Assert.That(firstWrite.Bytes, Is.EqualTo(secondWrite.Bytes));
        Assert.That(limitedRead.IsValid, Is.False);
        Assert.That(HasDiagnostic(limitedRead.Validation, "zip.maxEntries"), Is.True);
        Assert.That(duplicateWrite.IsValid, Is.False);
        Assert.That(HasDiagnostic(duplicateWrite.Validation, "writer.duplicateEntry"), Is.True);
    }

    [Test]
    public void WorkbookSaveOpen_PreservesUnknownPackagePartsDeterministically()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.UnknownParts.AddPart("/xl/custom/item1.bin", "application/octet-stream", new byte[] { 9, 8, 7 });
        XlsxWorkbookSaveResult firstSave = workbook.SaveBytes();
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(firstSave.Bytes);
        XlsxWorkbookSaveResult secondSave = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(secondSave.Bytes, new XlsxReaderOptions());

        Assert.That(firstSave.IsValid, Is.True);
        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.UnknownParts.ContainsPart("/xl/custom/item1.bin"), Is.True);
        Assert.That(secondSave.IsValid, Is.True);
        Assert.That(firstSave.Bytes, Is.EqualTo(secondSave.Bytes));
        Assert.That(package.GetEntry("/xl/custom/item1.bin"), Is.Not.Null);
        Assert.That(package.GetEntry("/xl/custom/item1.bin")!.Data, Is.EqualTo(new byte[] { 9, 8, 7 }));
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, "application/octet-stream");
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
        return entry;
    }

    static void AssertEntryText(XlsxZipPackageReadResult package, string path, string expected)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Contain(expected));
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
}
