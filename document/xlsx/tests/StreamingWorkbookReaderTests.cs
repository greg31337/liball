using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class StreamingWorkbookReaderTests
{
    [Test]
    public void ReadBytes_ReturnsWorkbookInfoWithoutLoadingSheetEntries()
    {
        byte[] packageBytes = CreateWorkbookPackage(false, CreateLargeWorksheetPayload(120000));
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.MaxCompressedBytes = 20000;
        options.MaxUncompressedBytes = 20000;

        XlsxStreamingWorkbookReadResult result = new XlsxStreamingWorkbookReader().ReadBytesWithOptions(packageBytes, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.WorkbookInfo.WorkbookPart.Sheets.Count, Is.EqualTo(1));
        Assert.That(result.WorkbookInfo.WorkbookPart.Sheets[0].Name, Is.EqualTo("BigSheet"));
        Assert.That(result.WorkbookInfo.WorkbookPart.Sheets[0].TargetPath, Is.EqualTo("/xl/worksheets/sheet1.xml"));
        Assert.That(result.WorkbookInfo.RelationshipResolution.Worksheets.Count, Is.EqualTo(1));
        Assert.That(result.WorkbookInfo.HasCoreProperties, Is.True);
        Assert.That(result.WorkbookInfo.CoreProperties.Title, Is.EqualTo("Streaming"));
        Assert.That(result.WorkbookInfo.PackageEntryCount, Is.EqualTo(6));
        Assert.That(result.WorkbookInfo.TotalUncompressedBytes, Is.GreaterThan(options.MaxUncompressedBytes));
        Assert.That(result.WorkbookInfo.MetadataBytesLoaded, Is.LessThan(options.MaxUncompressedBytes));
        Assert.That(result.WorkbookInfo.SkippedSheetEntryCount, Is.EqualTo(1));
        Assert.That(result.WorkbookInfo.SkippedSheetBytes, Is.GreaterThan(options.MaxUncompressedBytes));
    }

    [Test]
    public void ReadBytes_ReportsMacroPackageWarningFromWorkbookMetadata()
    {
        byte[] packageBytes = CreateWorkbookPackage(true, "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" />");

        XlsxStreamingWorkbookReadResult result = new XlsxStreamingWorkbookReader().ReadBytes(packageBytes);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.WorkbookInfo.PackageVariant, Is.EqualTo(XlsxPackageVariant.Xlsm));
        Assert.That(result.WorkbookInfo.HasVbaProject, Is.True);
        Assert.That(HasDiagnostic(result, "workbook.open.macroEnabledPackage"), Is.True);
    }

    [Test]
    public void ReadBytes_ReportsMissingRequiredMetadata()
    {
        byte[] packageBytes = CreateZipWithEntry("/xl/worksheets/sheet1.xml", "<worksheet />");

        XlsxStreamingWorkbookReadResult result = new XlsxStreamingWorkbookReader().ReadBytes(packageBytes);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "workbook.open.contentTypesMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "workbook.open.workbookMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "workbook.open.packageRelationshipsMissing"), Is.True);
        Assert.That(result.WorkbookInfo.SkippedSheetEntryCount, Is.EqualTo(1));
    }

    [Test]
    public void ReadBytes_ReportsEmptyInput()
    {
        XlsxStreamingWorkbookReadResult result = new XlsxStreamingWorkbookReader().ReadBytes(new byte[0]);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "streamingWorkbook.open.bytesEmpty"), Is.True);
    }

    [Test]
    public void ReaderInterface_ExercisesFileAndConfiguredStreamOverloads()
    {
        byte[] bytes = CreateWorkbookPackage(false, "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" />");
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, bytes);
            IXlsxStreamingWorkbookReader reader = new XlsxStreamingWorkbookReader();
            XlsxStreamingWorkbookReadResult file = reader.ReadFile(path);
            XlsxStreamingWorkbookReadResult configuredFile = reader.ReadFileWithOptions(path, new XlsxReaderOptions());
            XlsxStreamingWorkbookReadResult stream = reader.ReadStreamWithOptions(new MemoryStream(bytes), new XlsxReaderOptions());

            Assert.That(file.IsValid, Is.True);
            Assert.That(configuredFile.IsValid, Is.True);
            Assert.That(stream.IsValid, Is.True);
            Assert.That(stream.WorkbookInfo.WorkbookPart.Sheets.Count, Is.EqualTo(1));
        }
        finally
        {
            File.Delete(path);
        }
    }

    static byte[] CreateWorkbookPackage(bool macroEnabled, string worksheetXml)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                AddEntry(archive, XlsxPackagePaths.ContentTypes, CreateContentTypesXml(macroEnabled));
                AddEntry(archive, XlsxPackagePaths.PackageRelationships, CreatePackageRelationshipsXml());
                AddEntry(archive, XlsxPackagePaths.Workbook, CreateWorkbookXml());
                AddEntry(archive, XlsxPackagePaths.WorkbookRelationships, CreateWorkbookRelationshipsXml(macroEnabled));
                AddEntry(archive, XlsxPackagePaths.CoreProperties, CreateCorePropertiesXml());
                AddEntry(archive, XlsxPackagePaths.Worksheet1, worksheetXml);
            }

            return stream.ToArray();
        }
    }

    static byte[] CreateZipWithEntry(string path, string text)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                AddEntry(archive, path, text);
            }

            return stream.ToArray();
        }
    }

    static void AddEntry(ZipArchive archive, string path, string text)
    {
        string entryName = path;
        if (entryName.StartsWith("/"))
        {
            entryName = entryName.Substring(1);
        }

        ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using (Stream entryStream = entry.Open())
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            entryStream.Write(data, 0, data.Length);
        }
    }

    static string CreateContentTypesXml(bool macroEnabled)
    {
        string workbookContentType = XlsxContentTypes.Workbook;
        if (macroEnabled)
        {
            workbookContentType = XlsxContentTypes.MacroEnabledWorkbook;
        }

        return "<Types xmlns=\"" + XlsxNamespaces.ContentTypes + "\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Override PartName=\"" + XlsxPackagePaths.Workbook + "\" ContentType=\"" + workbookContentType + "\" />" +
            "<Override PartName=\"" + XlsxPackagePaths.Worksheet1 + "\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"" + XlsxPackagePaths.CoreProperties + "\" ContentType=\"" + XlsxContentTypes.CoreProperties + "\" />" +
            "</Types>";
    }

    static string CreatePackageRelationshipsXml()
    {
        return "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rId1\" Type=\"" + XlsxRelationshipTypes.OfficeDocument + "\" Target=\"xl/workbook.xml\" />" +
            "<Relationship Id=\"rId2\" Type=\"" + XlsxRelationshipTypes.CoreProperties + "\" Target=\"docProps/core.xml\" />" +
            "</Relationships>";
    }

    static string CreateWorkbookXml()
    {
        return "<workbook xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\">" +
            "<sheets><sheet name=\"BigSheet\" sheetId=\"1\" r:id=\"rId1\" /></sheets>" +
            "</workbook>";
    }

    static string CreateWorkbookRelationshipsXml(bool macroEnabled)
    {
        string xml = "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rId1\" Type=\"" + XlsxRelationshipTypes.Worksheet + "\" Target=\"worksheets/sheet1.xml\" />";
        if (macroEnabled)
        {
            xml = xml + "<Relationship Id=\"rId2\" Type=\"" + XlsxRelationshipTypes.VbaProject + "\" Target=\"vbaProject.bin\" />";
        }

        return xml + "</Relationships>";
    }

    static string CreateCorePropertiesXml()
    {
        return "<cp:coreProperties xmlns:cp=\"" + XlsxNamespaces.CoreProperties + "\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">" +
            "<dc:title>Streaming</dc:title><dc:creator>Ada</dc:creator>" +
            "</cp:coreProperties>";
    }

    static string CreateLargeWorksheetPayload(int length)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        while (builder.Length < length)
        {
            builder.Append("<row r=\"1\"><c r=\"A1\"><v>1</v></c></row>");
        }

        builder.Append("</sheetData></worksheet>");
        return builder.ToString();
    }

    static bool HasDiagnostic(XlsxStreamingWorkbookReadResult result, string code)
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
