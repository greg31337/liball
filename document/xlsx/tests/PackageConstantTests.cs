using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PackageConstantTests
{
    [Test]
    public void Namespaces_ExposeCoreSpreadsheetAndPackageUris()
    {
        Assert.That(XlsxNamespaces.Spreadsheet, Is.EqualTo("http://schemas.openxmlformats.org/spreadsheetml/2006/main"));
        Assert.That(XlsxNamespaces.StrictSpreadsheet, Is.EqualTo("http://purl.oclc.org/ooxml/spreadsheetml/main"));
        Assert.That(XlsxNamespaces.ContentTypes, Is.EqualTo("http://schemas.openxmlformats.org/package/2006/content-types"));
        Assert.That(XlsxNamespaces.PackageRelationships, Is.EqualTo("http://schemas.openxmlformats.org/package/2006/relationships"));
        Assert.That(XlsxNamespaces.OfficeDocumentRelationships, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships"));
        Assert.That(XlsxNamespaces.MarkupCompatibility, Is.EqualTo("http://schemas.openxmlformats.org/markup-compatibility/2006"));
        Assert.That(XlsxNamespaces.Drawing, Is.EqualTo("http://schemas.openxmlformats.org/drawingml/2006/main"));
        Assert.That(XlsxNamespaces.CoreProperties, Is.EqualTo("http://schemas.openxmlformats.org/package/2006/metadata/core-properties"));
    }

    [Test]
    public void ContentTypes_ExposeMinimumWorkbookAndVariantTypes()
    {
        Assert.That(XlsxContentTypes.Relationships, Is.EqualTo("application/vnd.openxmlformats-package.relationships+xml"));
        Assert.That(XlsxContentTypes.Xml, Is.EqualTo("application/xml"));
        Assert.That(XlsxContentTypes.Workbook, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"));
        Assert.That(XlsxContentTypes.MacroEnabledWorkbook, Is.EqualTo("application/vnd.ms-excel.sheet.macroEnabled.main+xml"));
        Assert.That(XlsxContentTypes.TemplateWorkbook, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.template.main+xml"));
        Assert.That(XlsxContentTypes.MacroEnabledTemplateWorkbook, Is.EqualTo("application/vnd.ms-excel.template.macroEnabled.main+xml"));
        Assert.That(XlsxContentTypes.Worksheet, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"));
        Assert.That(XlsxContentTypes.Styles, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"));
        Assert.That(XlsxContentTypes.SharedStrings, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml"));
        Assert.That(XlsxContentTypes.CoreProperties, Is.EqualTo("application/vnd.openxmlformats-package.core-properties+xml"));
    }

    [Test]
    public void RelationshipTypes_ExposePackageWorkbookAndPartRelationships()
    {
        Assert.That(XlsxRelationshipTypes.OfficeDocument, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"));
        Assert.That(XlsxRelationshipTypes.Workbook, Is.EqualTo(XlsxRelationshipTypes.OfficeDocument));
        Assert.That(XlsxRelationshipTypes.Worksheet, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"));
        Assert.That(XlsxRelationshipTypes.Styles, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"));
        Assert.That(XlsxRelationshipTypes.SharedStrings, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"));
        Assert.That(XlsxRelationshipTypes.Theme, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme"));
        Assert.That(XlsxRelationshipTypes.CoreProperties, Is.EqualTo("http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties"));
        Assert.That(XlsxRelationshipTypes.ExtendedProperties, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties"));
        Assert.That(XlsxRelationshipTypes.CustomProperties, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/custom-properties"));
        Assert.That(XlsxRelationshipTypes.VbaProject, Is.EqualTo("http://schemas.microsoft.com/office/2006/relationships/vbaProject"));
    }

    [Test]
    public void PackagePaths_ExposeCanonicalAbsolutePartNames()
    {
        Assert.That(XlsxPackagePaths.ContentTypes, Is.EqualTo("/[Content_Types].xml"));
        Assert.That(XlsxPackagePaths.PackageRelationships, Is.EqualTo("/_rels/.rels"));
        Assert.That(XlsxPackagePaths.Workbook, Is.EqualTo("/xl/workbook.xml"));
        Assert.That(XlsxPackagePaths.WorkbookRelationships, Is.EqualTo("/xl/_rels/workbook.xml.rels"));
        Assert.That(XlsxPackagePaths.Worksheet1, Is.EqualTo("/xl/worksheets/sheet1.xml"));
        Assert.That(XlsxPackagePaths.Styles, Is.EqualTo("/xl/styles.xml"));
        Assert.That(XlsxPackagePaths.SharedStrings, Is.EqualTo("/xl/sharedStrings.xml"));
        Assert.That(XlsxPackagePaths.Theme, Is.EqualTo("/xl/theme/theme1.xml"));
        Assert.That(XlsxPackagePaths.CoreProperties, Is.EqualTo("/docProps/core.xml"));
        Assert.That(XlsxPackagePaths.ExtendedProperties, Is.EqualTo("/docProps/app.xml"));
        Assert.That(XlsxPackagePaths.CustomProperties, Is.EqualTo("/docProps/custom.xml"));
    }
}
