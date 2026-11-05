using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class MacroVbaProjectTests
{
    [Test]
    public void WorkbookOpenSave_PreservesVbaProjectAndMacroEnabledPackageType()
    {
        byte[] bytes = CreateMacroEnabledPackage();
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.AllowMacroEnabledPackages = true;

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes, options);
        XlsxWorkbookSaveResult saved = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saved.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.PackageVariant, Is.EqualTo(XlsxPackageVariant.Xlsm));
        Assert.That(open.Workbook.HasVbaProject, Is.True);
        Assert.That(open.Workbook.VbaProject.PartPath, Is.EqualTo(XlsxPackagePaths.VbaProject));
        Assert.That(open.Workbook.VbaProject.Data, Is.EqualTo(VbaBytes()));
        Assert.That(saved.IsValid, Is.True);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.MacroEnabledWorkbook);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.VbaProject);
        AssertEntryText(package, XlsxPackagePaths.WorkbookRelationships, "Type=\"" + XlsxRelationshipTypes.VbaProject + "\"");
        Assert.That(package.GetEntry(XlsxPackagePaths.VbaProject), Is.Not.Null);
        Assert.That(package.GetEntry(XlsxPackagePaths.VbaProject)!.Data, Is.EqualTo(VbaBytes()));
    }

    [Test]
    public void WorkbookStripMacros_RemovesVbaProjectAndDowngradesPackageType()
    {
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.AllowMacroEnabledPackages = true;
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(CreateMacroEnabledPackage(), options);

        XlsxValidationResult strip = open.Workbook!.StripMacros();
        XlsxWorkbookSaveResult saved = open.Workbook.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saved.Bytes, new XlsxReaderOptions());

        Assert.That(strip.IsValid, Is.True);
        Assert.That(open.Workbook.HasVbaProject, Is.False);
        Assert.That(open.Workbook.PackageVariant, Is.EqualTo(XlsxPackageVariant.Xlsx));
        Assert.That(saved.IsValid, Is.True);
        Assert.That(package.GetEntry(XlsxPackagePaths.VbaProject), Is.Null);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.Workbook);
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.MacroEnabledWorkbook);
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.VbaProject);
        AssertEntryTextDoesNotContain(package, XlsxPackagePaths.WorkbookRelationships, XlsxRelationshipTypes.VbaProject);
    }

    [Test]
    public void WorkbookSave_UsesExplicitMacroEnabledTemplatePackageType()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWriterOptions options = new XlsxWriterOptions();
        options.PackageVariant = XlsxPackageVariant.Xltm;

        XlsxWorkbookSaveResult saved = workbook.SaveBytesWithOptions(options);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saved.Bytes, new XlsxReaderOptions());

        Assert.That(saved.IsValid, Is.True);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.MacroEnabledTemplateWorkbook);
    }

    [Test]
    public void VbaProjectWriter_ReportsInvalidPart()
    {
        XlsxVbaProjectPart invalid = new XlsxVbaProjectPart();
        invalid.Data = null!;

        XlsxVbaProjectPartWriteResult write = new XlsxVbaProjectPartWriter().WriteBytes(invalid);

        Assert.That(write.IsValid, Is.False);
        Assert.That(HasDiagnostic(write.Validation, "vbaProjectWriter.dataNull"), Is.True);
    }

    static byte[] CreateMacroEnabledPackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"" + XlsxContentTypes.Xml + "\" />" +
            "<Default Extension=\"bin\" ContentType=\"application/octet-stream\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.MacroEnabledWorkbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/vbaProject.bin\" ContentType=\"" + XlsxContentTypes.VbaProject + "\" />" +
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
            Relationship("rIdVbaProject", XlsxRelationshipTypes.VbaProject, "vbaProject.bin") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1,
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData /></worksheet>"));
        entries.Add(CreateEntry(XlsxPackagePaths.VbaProject, VbaBytes()));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static byte[] VbaBytes()
    {
        return new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0x01, 0x02, 0x03, 0x04 };
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
