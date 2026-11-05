using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PackageApiCompletionTests
{
    [Test]
    public void WorkbookOperations_OpenMemoryAndSpan_ReadCompletePackage()
    {
        XlsxWorkbookSaveResult save = XlsxWorkbook.Create().SaveBytes();
        IXlsxWorkbookOperations operations = new XlsxWorkbookOperations();
        ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(save.Bytes);

        XlsxWorkbookOpenResult memoryResult = operations.OpenMemory(memory);
        XlsxWorkbookOpenResult spanResult = operations.OpenSpan(save.Bytes.AsSpan());
        XlsxWorkbookOpenResult emptyResult = operations.OpenSpan(ReadOnlySpan<byte>.Empty);

        Assert.That(memoryResult.IsValid, Is.True);
        Assert.That(spanResult.IsValid, Is.True);
        Assert.That(memoryResult.Workbook!.PackageParts.Count, Is.GreaterThan(0));
        Assert.That(spanResult.Workbook!.Conformance, Is.EqualTo(XlsxPackageConformance.Transitional));
        Assert.That(emptyResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(emptyResult, "workbook.open.bytesEmpty"), Is.True);
    }

    [Test]
    public void PackageReaders_ReadMemoryAndSpan_ParseXmlModels()
    {
        byte[] contentTypesBytes = Encoding.UTF8.GetBytes(ContentTypesXml());
        byte[] relationshipsBytes = Encoding.UTF8.GetBytes(RelationshipsXml());
        IXlsxContentTypesReader contentTypesReader = new XlsxContentTypesReader();
        IXlsxPackageRelationshipsReader packageReader = new XlsxPackageRelationshipsReader();
        IXlsxPartRelationshipsReader partReader = new XlsxPartRelationshipsReader();

        XlsxContentTypesPart contentTypesMemory = contentTypesReader.ReadMemory(new ReadOnlyMemory<byte>(contentTypesBytes));
        XlsxContentTypesPart contentTypesSpan = contentTypesReader.ReadSpan(contentTypesBytes.AsSpan());
        XlsxRelationshipsPart packageMemory = packageReader.ReadMemory(new ReadOnlyMemory<byte>(relationshipsBytes), new XlsxReaderOptions());
        XlsxRelationshipsPart packageSpan = packageReader.ReadSpan(relationshipsBytes.AsSpan(), new XlsxReaderOptions());
        XlsxRelationshipsPart partMemory = partReader.ReadMemory(XlsxPackagePaths.Workbook, new ReadOnlyMemory<byte>(relationshipsBytes), new XlsxReaderOptions());
        XlsxRelationshipsPart partSpan = partReader.ReadSpan(XlsxPackagePaths.Workbook, relationshipsBytes.AsSpan(), new XlsxReaderOptions());

        Assert.That(contentTypesMemory.IsValid, Is.True);
        Assert.That(contentTypesSpan.GetContentType("/docProps/thumbnail.png"), Is.EqualTo("image/png"));
        Assert.That(packageMemory.Relationships.Count, Is.EqualTo(1));
        Assert.That(packageSpan.Relationships[0].Type, Is.EqualTo(XlsxRelationshipTypes.Thumbnail));
        Assert.That(partMemory.SourcePartPath, Is.EqualTo(XlsxPackagePaths.Workbook));
        Assert.That(partSpan.PartPath, Is.EqualTo(XlsxPackagePaths.WorkbookRelationships));
    }

    [TestCase(false, XlsxPackageConformance.Transitional)]
    [TestCase(true, XlsxPackageConformance.Strict)]
    public void OpenPackage_EnumeratesPartsRelationshipsAndThumbnail(bool strict, XlsxPackageConformance expectedConformance)
    {
        byte[] thumbnail = new byte[] { 137, 80, 78, 71, 1, 2, 3, 4 };
        byte[] bytes = CreatePackage(strict, thumbnail);

        XlsxWorkbookOpenResult open = new XlsxWorkbookOperations().OpenMemory(new ReadOnlyMemory<byte>(bytes));

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook, Is.Not.Null);
        XlsxWorkbook workbook = open.Workbook!;
        Assert.That(workbook.Conformance, Is.EqualTo(expectedConformance));
        Assert.That(workbook.PackageParts.Count, Is.EqualTo(7));
        Assert.That(workbook.RelationshipParts.Count, Is.EqualTo(3));
        Assert.That(workbook.RelationshipParts[0].PartPath, Is.EqualTo(XlsxPackagePaths.PackageRelationships));
        Assert.That(workbook.RelationshipParts[2].SourcePartPath, Is.EqualTo(XlsxPackagePaths.Worksheet1));
        Assert.That(GetPart(workbook, "/docProps/thumbnail.png")!.ContentType, Is.EqualTo("image/png"));
        Assert.That(GetPart(workbook, XlsxPackagePaths.WorkbookRelationships)!.IsRelationshipPart, Is.True);
        Assert.That(workbook.PreviewMetadata.HasThumbnail, Is.True);
        Assert.That(workbook.PreviewMetadata.RelationshipId, Is.EqualTo("rIdPreview"));
        Assert.That(workbook.PreviewMetadata.PartPath, Is.EqualTo("/docProps/thumbnail.png"));
        Assert.That(workbook.PreviewMetadata.ContentType, Is.EqualTo("image/png"));
        Assert.That(workbook.PreviewMetadata.ByteLength, Is.EqualTo(thumbnail.Length));
        Assert.That(workbook.PreviewMetadata.Data, Is.EqualTo(thumbnail));
    }

    [Test]
    public void OpenSaveReopen_PreservesThumbnailPreviewMetadata()
    {
        byte[] thumbnail = new byte[] { 10, 20, 30, 40, 50 };
        XlsxWorkbookOpenResult firstOpen = XlsxWorkbook.OpenBytes(CreatePackage(false, thumbnail));

        XlsxWorkbookSaveResult save = firstOpen.Workbook!.SaveBytes();
        XlsxWorkbookOpenResult secondOpen = XlsxWorkbook.OpenBytes(save.Bytes);

        Assert.That(save.IsValid, Is.True);
        Assert.That(secondOpen.IsValid, Is.True);
        Assert.That(secondOpen.Workbook!.PreviewMetadata.HasThumbnail, Is.True);
        Assert.That(secondOpen.Workbook.PreviewMetadata.Data, Is.EqualTo(thumbnail));
        Assert.That(secondOpen.Workbook.PreviewMetadata.ContentType, Is.EqualTo("image/png"));
    }

    static byte[] CreatePackage(bool strict, byte[] thumbnail)
    {
        string spreadsheetNamespace = strict ? XlsxNamespaces.StrictSpreadsheet : XlsxNamespaces.Spreadsheet;
        using MemoryStream stream = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddEntry(archive, "[Content_Types].xml", ContentTypesXml());
            AddEntry(archive, "_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rIdWorkbook\" Type=\"" + XlsxRelationshipTypes.OfficeDocument + "\" Target=\"xl/workbook.xml\" />" +
                "<Relationship Id=\"rIdPreview\" Type=\"" + XlsxRelationshipTypes.Thumbnail + "\" Target=\"docProps/thumbnail.png\" />" +
                "</Relationships>");
            AddEntry(archive, "xl/workbook.xml", "<workbook xmlns=\"" + spreadsheetNamespace + "\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" /></sheets></workbook>");
            AddEntry(archive, "xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"" + XlsxRelationshipTypes.Worksheet + "\" Target=\"worksheets/sheet1.xml\" /></Relationships>");
            AddEntry(archive, "xl/worksheets/sheet1.xml", "<worksheet xmlns=\"" + spreadsheetNamespace + "\"><sheetData /></worksheet>");
            AddEntry(archive, "xl/worksheets/_rels/sheet1.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\" />");
            AddEntry(archive, "docProps/thumbnail.png", thumbnail);
        }

        return stream.ToArray();
    }

    static string ContentTypesXml()
    {
        return "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Default Extension=\"png\" ContentType=\"image/png\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\" />" +
            "</Types>";
    }

    static string RelationshipsXml()
    {
        return "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rIdPreview\" Type=\"" + XlsxRelationshipTypes.Thumbnail + "\" Target=\"docProps/thumbnail.png\" /></Relationships>";
    }

    static void AddEntry(ZipArchive archive, string path, string text)
    {
        AddEntry(archive, path, Encoding.UTF8.GetBytes(text));
    }

    static void AddEntry(ZipArchive archive, string path, byte[] bytes)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path);
        using Stream entryStream = entry.Open();
        entryStream.Write(bytes, 0, bytes.Length);
    }

    static XlsxPackagePart? GetPart(XlsxWorkbook workbook, string path)
    {
        foreach (XlsxPackagePart part in workbook.PackageParts)
        {
            if (part.Path == path)
            {
                return part;
            }
        }

        return null;
    }

    static bool HasDiagnostic(XlsxWorkbookOpenResult result, string code)
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
