using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class MinimumPackageValidatorTests
{
    [Test]
    public void ValidateBytes_AcceptsMinimumXlsxPackage()
    {
        byte[] bytes = CreatePackage(CreateMinimumContentTypes(XlsxContentTypes.Workbook, true), CreatePackageRelationships("xl/workbook.xml", ""));
        XlsxMinimumPackageValidator validator = new XlsxMinimumPackageValidator();

        XlsxValidationResult result = validator.ValidateBytes(bytes, new XlsxReaderOptions());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(0));
    }

    [Test]
    public void ValidateBytes_ReportsMissingRequiredParts()
    {
        XlsxContentTypesPart contentTypes = CreateMinimumContentTypes(XlsxContentTypes.Workbook, true);
        XlsxContentTypesWriteResult contentTypesWrite = new XlsxContentTypesWriter().WriteBytes(contentTypes);
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes, contentTypesWrite.Bytes));
        byte[] bytes = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions()).Bytes;
        XlsxMinimumPackageValidator validator = new XlsxMinimumPackageValidator();

        XlsxValidationResult result = validator.ValidateBytes(bytes, new XlsxReaderOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "minimum.missingPackageRelationshipsPart"), Is.True);
        Assert.That(HasDiagnostic(result, "minimum.missingWorkbookPart"), Is.True);
        Assert.That(HasDiagnostic(result, "minimum.relationshipsNull"), Is.True);
    }

    [Test]
    public void ValidateBytes_ReportsContentTypeRuleFailures()
    {
        byte[] bytes = CreatePackage(CreateMinimumContentTypes("application/vnd.example.workbook+xml", false), CreatePackageRelationships("xl/workbook.xml", ""));
        XlsxMinimumPackageValidator validator = new XlsxMinimumPackageValidator();

        XlsxValidationResult result = validator.ValidateBytes(bytes, new XlsxReaderOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "minimum.relationshipsDefaultMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "minimum.workbookContentTypeUnsupported"), Is.True);
    }

    [Test]
    public void ValidateBytes_ReportsMissingOfficeDocumentRelationship()
    {
        XlsxRelationshipsPart relationships = CreatePackageRelationships("docProps/core.xml", "");
        relationships.Relationships[0].Type = XlsxRelationshipTypes.CoreProperties;
        byte[] bytes = CreatePackage(CreateMinimumContentTypes(XlsxContentTypes.Workbook, true), relationships);
        XlsxMinimumPackageValidator validator = new XlsxMinimumPackageValidator();

        XlsxValidationResult result = validator.ValidateBytes(bytes, new XlsxReaderOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "minimum.officeDocumentRelationshipMissing"), Is.True);
    }

    [Test]
    public void ValidateBytes_ReportsWrongOfficeDocumentTarget()
    {
        byte[] bytes = CreatePackage(CreateMinimumContentTypes(XlsxContentTypes.Workbook, true), CreatePackageRelationships("xl/otherWorkbook.xml", ""));
        XlsxMinimumPackageValidator validator = new XlsxMinimumPackageValidator();

        XlsxValidationResult result = validator.ValidateBytes(bytes, new XlsxReaderOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "minimum.officeDocumentTargetWrong"), Is.True);
    }

    [Test]
    public void ValidateBytes_ReportsExternalOfficeDocumentRelationship()
    {
        byte[] bytes = CreatePackage(CreateMinimumContentTypes(XlsxContentTypes.Workbook, true), CreatePackageRelationships("https://example.com/book.xlsx", "External"));
        XlsxMinimumPackageValidator validator = new XlsxMinimumPackageValidator();

        XlsxValidationResult result = validator.ValidateBytes(bytes, new XlsxReaderOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "minimum.officeDocumentRelationshipExternal"), Is.True);
    }

    [Test]
    public void ValidateMinimumXlsxPackage_PropagatesParserDiagnostics()
    {
        XlsxZipPackageReadResult package = new XlsxZipPackageReadResult();
        package.Entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes, new byte[] { 1 }));
        package.Entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships, new byte[] { 2 }));
        package.Entries.Add(CreateEntry(XlsxPackagePaths.Workbook, new byte[] { 3 }));
        XlsxContentTypesPart contentTypes = new XlsxContentTypesPart();
        contentTypes.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "contentTypes.test", "test"));
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        relationships.SourcePartPath = "/";
        relationships.PartPath = XlsxPackagePaths.PackageRelationships;
        relationships.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.test", "test"));
        XlsxMinimumPackageValidator validator = new XlsxMinimumPackageValidator();

        XlsxValidationResult result = validator.ValidateMinimumXlsxPackage(package, contentTypes, relationships);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "contentTypes.test"), Is.True);
        Assert.That(HasDiagnostic(result, "relationships.test"), Is.True);
    }

    static XlsxContentTypesPart CreateMinimumContentTypes(string workbookContentType, bool includeRelationshipsDefault)
    {
        XlsxContentTypesPart part = new XlsxContentTypesPart();
        if (includeRelationshipsDefault)
        {
            XlsxContentTypeDefault relationshipsDefault = new XlsxContentTypeDefault();
            relationshipsDefault.Extension = "rels";
            relationshipsDefault.ContentType = XlsxContentTypes.Relationships;
            part.Defaults.Add(relationshipsDefault);
        }

        XlsxContentTypeOverride workbookOverride = new XlsxContentTypeOverride();
        workbookOverride.PartName = XlsxPackagePaths.Workbook;
        workbookOverride.ContentType = workbookContentType;
        part.Overrides.Add(workbookOverride);
        return part;
    }

    static XlsxRelationshipsPart CreatePackageRelationships(string target, string targetMode)
    {
        XlsxRelationshipsPart part = new XlsxRelationshipsPart();
        part.SourcePartPath = "/";
        part.PartPath = XlsxPackagePaths.PackageRelationships;
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = "rId1";
        relationship.Type = XlsxRelationshipTypes.OfficeDocument;
        relationship.Target = target;
        relationship.TargetMode = targetMode;
        if (targetMode == "External")
        {
            relationship.IsExternal = true;
        }

        part.Relationships.Add(relationship);
        return part;
    }

    static byte[] CreatePackage(XlsxContentTypesPart contentTypes, XlsxRelationshipsPart relationships)
    {
        XlsxContentTypesWriteResult contentTypesWrite = new XlsxContentTypesWriter().WriteBytes(contentTypes);
        XlsxRelationshipsWriteResult relationshipsWrite = new XlsxPartRelationshipsWriter().WriteBytes(relationships);
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes, contentTypesWrite.Bytes));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships, relationshipsWrite.Bytes));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook, System.Text.Encoding.UTF8.GetBytes("<workbook />")));
        return new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions()).Bytes;
    }

    static XlsxPackageEntry CreateEntry(string path, byte[] bytes)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = bytes;
        entry.UncompressedBytes = bytes.Length;
        return entry;
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
