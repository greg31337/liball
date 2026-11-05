using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class HybridWorkbookLoadOptionsTests
{
    [Test]
    public void OpenBytes_ValuesOnlySelectedSheetAndRange_LoadsOnlyValuesInRange()
    {
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.ValuesOnly = true;
        options.SelectedSheets.Add("Data");
        options.SelectedRanges.Add("A1:B1");

        XlsxWorkbookOpenResult result = XlsxWorkbook.OpenBytes(CreateHybridPackage(), options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Workbook!.Worksheets.Count, Is.EqualTo(1));
        Assert.That(result.Workbook.Worksheets[0].PartPath, Is.EqualTo("/xl/worksheets/sheet2.xml"));
        Assert.That(result.Workbook.Worksheets[0].Rows.Count, Is.EqualTo(2));
        Assert.That(result.Workbook.Worksheets[0].Rows[0].Cells.Count, Is.EqualTo(2));
        Assert.That(result.Workbook.Worksheets[0].Rows[0].Cells[0].Reference, Is.EqualTo("A1"));
        Assert.That(result.Workbook.Worksheets[0].Rows[0].Cells[1].Reference, Is.EqualTo("B1"));
        Assert.That(result.Workbook.Worksheets[0].Rows[0].Cells[1].Value, Is.EqualTo("2"));
        Assert.That(result.Workbook.Worksheets[0].Rows[0].Cells[1].HasFormula, Is.False);
        Assert.That(result.Workbook.Worksheets[0].Rows[1].Cells.Count, Is.EqualTo(0));
    }

    [Test]
    public void OpenBytes_FormulasOnlySelectedSheet_LoadsOnlyFormulaCells()
    {
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.FormulasOnly = true;
        options.SelectedSheets.Add("Data");

        XlsxWorkbookOpenResult result = XlsxWorkbook.OpenBytes(CreateHybridPackage(), options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Workbook!.Worksheets.Count, Is.EqualTo(1));
        Assert.That(result.Workbook.Worksheets[0].Rows[0].Cells.Count, Is.EqualTo(1));
        Assert.That(result.Workbook.Worksheets[0].Rows[0].Cells[0].Reference, Is.EqualTo("B1"));
        Assert.That(result.Workbook.Worksheets[0].Rows[0].Cells[0].HasFormula, Is.True);
        Assert.That(result.Workbook.Worksheets[0].Rows[0].Cells[0].Formula, Is.EqualTo("A1*2"));
        Assert.That(result.Workbook.Worksheets[0].Rows[1].Cells.Count, Is.EqualTo(0));
    }

    [Test]
    public void OpenBytes_MetadataOnlyAndStylesOnly_ControlPartLoading()
    {
        XlsxReaderOptions metadataOnly = new XlsxReaderOptions();
        metadataOnly.MetadataOnly = true;
        XlsxReaderOptions stylesOnly = new XlsxReaderOptions();
        stylesOnly.StylesOnly = true;

        XlsxWorkbookOpenResult metadataResult = XlsxWorkbook.OpenBytes(CreateHybridPackage(), metadataOnly);
        XlsxWorkbookOpenResult stylesResult = XlsxWorkbook.OpenBytes(CreateHybridPackage(), stylesOnly);

        Assert.That(metadataResult.IsValid, Is.True);
        Assert.That(metadataResult.Workbook!.WorkbookPart.Sheets.Count, Is.EqualTo(2));
        Assert.That(metadataResult.Workbook.HasStyles, Is.False);
        Assert.That(metadataResult.Workbook.Worksheets.Count, Is.EqualTo(0));
        Assert.That(stylesResult.IsValid, Is.True);
        Assert.That(stylesResult.Workbook!.HasStyles, Is.True);
        Assert.That(stylesResult.Workbook.Styles.CellFormats.Count, Is.EqualTo(1));
        Assert.That(stylesResult.Workbook.Worksheets.Count, Is.EqualTo(0));
    }

    [Test]
    public void OpenBytes_ReportsConflictingLoadModes()
    {
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.MetadataOnly = true;
        options.ValuesOnly = true;

        XlsxWorkbookOpenResult result = XlsxWorkbook.OpenBytes(CreateHybridPackage(), options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "reader.conflictingLoadModes"), Is.True);
    }

    static byte[] CreateHybridPackage()
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                AddEntry(archive, XlsxPackagePaths.ContentTypes, CreateContentTypesXml());
                AddEntry(archive, XlsxPackagePaths.PackageRelationships, CreatePackageRelationshipsXml());
                AddEntry(archive, XlsxPackagePaths.Workbook, CreateWorkbookXml());
                AddEntry(archive, XlsxPackagePaths.WorkbookRelationships, CreateWorkbookRelationshipsXml());
                AddEntry(archive, XlsxPackagePaths.Styles, CreateStylesXml());
                AddEntry(archive, XlsxPackagePaths.Worksheet1, CreateRawWorksheetXml());
                AddEntry(archive, "/xl/worksheets/sheet2.xml", CreateDataWorksheetXml());
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

    static string CreateContentTypesXml()
    {
        return "<Types xmlns=\"" + XlsxNamespaces.ContentTypes + "\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Override PartName=\"" + XlsxPackagePaths.Workbook + "\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"" + XlsxPackagePaths.Worksheet1 + "\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"" + XlsxPackagePaths.Styles + "\" ContentType=\"" + XlsxContentTypes.Styles + "\" />" +
            "</Types>";
    }

    static string CreatePackageRelationshipsXml()
    {
        return "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rId1\" Type=\"" + XlsxRelationshipTypes.OfficeDocument + "\" Target=\"xl/workbook.xml\" />" +
            "</Relationships>";
    }

    static string CreateWorkbookXml()
    {
        return "<workbook xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\">" +
            "<sheets><sheet name=\"Raw\" sheetId=\"1\" r:id=\"rId1\" /><sheet name=\"Data\" sheetId=\"2\" r:id=\"rId2\" /></sheets>" +
            "</workbook>";
    }

    static string CreateWorkbookRelationshipsXml()
    {
        return "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rId1\" Type=\"" + XlsxRelationshipTypes.Worksheet + "\" Target=\"worksheets/sheet1.xml\" />" +
            "<Relationship Id=\"rId2\" Type=\"" + XlsxRelationshipTypes.Worksheet + "\" Target=\"worksheets/sheet2.xml\" />" +
            "<Relationship Id=\"rId3\" Type=\"" + XlsxRelationshipTypes.Styles + "\" Target=\"styles.xml\" />" +
            "</Relationships>";
    }

    static string CreateStylesXml()
    {
        return "<styleSheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><cellXfs count=\"1\"><xf numFmtId=\"0\" /></cellXfs></styleSheet>";
    }

    static string CreateRawWorksheetXml()
    {
        return "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><sheetData><row r=\"1\"><c r=\"A1\"><f>1+1</f><v>2</v></c><c r=\"B1\"><v>9</v></c></row></sheetData></worksheet>";
    }

    static string CreateDataWorksheetXml()
    {
        return "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><sheetData>" +
            "<row r=\"1\"><c r=\"A1\"><v>1</v></c><c r=\"B1\"><f>A1*2</f><v>2</v></c><c r=\"C1\"><v>3</v></c></row>" +
            "<row r=\"2\"><c r=\"A2\"><v>4</v></c></row>" +
            "</sheetData></worksheet>";
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
