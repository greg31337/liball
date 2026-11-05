using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PackagePathTests
{
    [Test]
    public void NormalizePartPath_AddsRootSlashAndCollapsesCurrentDirectorySegments()
    {
        XlsxPackagePathResult relative = XlsxPackagePath.NormalizePartPath("xl/workbook.xml");
        XlsxPackagePathResult currentDirectory = XlsxPackagePath.NormalizePartPath("/xl/./worksheets/./sheet1.xml");

        Assert.That(relative.IsValid(), Is.True);
        Assert.That(relative.Path, Is.EqualTo("/xl/workbook.xml"));
        Assert.That(currentDirectory.IsValid(), Is.True);
        Assert.That(currentDirectory.Path, Is.EqualTo("/xl/worksheets/sheet1.xml"));
    }

    [Test]
    public void NormalizePartPath_RejectsTraversalAndMalformedPaths()
    {
        XlsxPackagePathResult parent = XlsxPackagePath.NormalizePartPath("/xl/../docProps/core.xml");
        XlsxPackagePathResult backslash = XlsxPackagePath.NormalizePartPath("xl\\workbook.xml");
        XlsxPackagePathResult emptySegment = XlsxPackagePath.NormalizePartPath("/xl//workbook.xml");
        XlsxPackagePathResult fragment = XlsxPackagePath.NormalizePartPath("/xl/workbook.xml#Sheet1");
        XlsxPackagePathResult external = XlsxPackagePath.NormalizePartPath("https://example.com/workbook.xml");

        Assert.That(parent.IsValid(), Is.False);
        Assert.That(parent.Validation.Diagnostics[0].Code, Is.EqualTo("path.parentSegment"));
        Assert.That(backslash.IsValid(), Is.False);
        Assert.That(backslash.Validation.Diagnostics[0].Code, Is.EqualTo("path.backslash"));
        Assert.That(emptySegment.IsValid(), Is.False);
        Assert.That(emptySegment.Validation.Diagnostics[0].Code, Is.EqualTo("path.emptySegment"));
        Assert.That(fragment.IsValid(), Is.False);
        Assert.That(fragment.Validation.Diagnostics[0].Code, Is.EqualTo("path.fragment"));
        Assert.That(external.IsValid(), Is.False);
        Assert.That(external.Validation.Diagnostics[0].Code, Is.EqualTo("path.externalUri"));
    }

    [Test]
    public void ValidatePartPath_ReportsValidityWithoutThrowing()
    {
        XlsxValidationResult valid = XlsxPackagePath.ValidatePartPath(XlsxPackagePaths.Workbook);
        XlsxValidationResult invalid = XlsxPackagePath.ValidatePartPath("../xl/workbook.xml");

        Assert.That(valid.IsValid, Is.True);
        Assert.That(XlsxPackagePath.IsValidPartPath(XlsxPackagePaths.Worksheet1), Is.True);
        Assert.That(invalid.IsValid, Is.False);
        Assert.That(invalid.HasErrors(), Is.True);
    }

    [Test]
    public void GetZipEntryName_RemovesPackageRootSlash()
    {
        string entryName = XlsxPackagePath.GetZipEntryName(XlsxPackagePaths.Workbook);

        Assert.That(entryName, Is.EqualTo("xl/workbook.xml"));
        Assert.Throws<XlsxException>(() => XlsxPackagePath.GetZipEntryName("/xl/../workbook.xml"));
    }

    [Test]
    public void ResolveRelationshipTarget_ResolvesRelativeAndAbsolutePackageTargets()
    {
        XlsxPackagePathResult packageTarget = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.PackageRelationships, "xl/workbook.xml", false);
        XlsxPackagePathResult workbookTarget = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, "worksheets/sheet1.xml", false);
        XlsxPackagePathResult sidecarTarget = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.WorkbookRelationships, "styles.xml", false);
        XlsxPackagePathResult absoluteTarget = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, "/docProps/core.xml", false);

        Assert.That(packageTarget.IsValid(), Is.True);
        Assert.That(packageTarget.Path, Is.EqualTo(XlsxPackagePaths.Workbook));
        Assert.That(workbookTarget.IsValid(), Is.True);
        Assert.That(workbookTarget.Path, Is.EqualTo(XlsxPackagePaths.Worksheet1));
        Assert.That(sidecarTarget.IsValid(), Is.True);
        Assert.That(sidecarTarget.Path, Is.EqualTo(XlsxPackagePaths.Styles));
        Assert.That(absoluteTarget.IsValid(), Is.True);
        Assert.That(absoluteTarget.Path, Is.EqualTo(XlsxPackagePaths.CoreProperties));
    }

    [Test]
    public void ResolveRelationshipTarget_AllowsParentSegmentsInsidePackageOnly()
    {
        XlsxPackagePathResult insidePackage = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Worksheet1, "../theme/theme1.xml", false);
        XlsxPackagePathResult traversal = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Worksheet1, "../../../evil.xml", false);
        XlsxPackagePathResult invalidSource = XlsxPackagePath.ResolveRelationshipTarget("/xl/../workbook.xml", "worksheets/sheet1.xml", false);

        Assert.That(insidePackage.IsValid(), Is.True);
        Assert.That(insidePackage.Path, Is.EqualTo(XlsxPackagePaths.Theme));
        Assert.That(traversal.IsValid(), Is.False);
        Assert.That(traversal.Validation.Diagnostics[0].Code, Is.EqualTo("path.traversal"));
        Assert.That(invalidSource.IsValid(), Is.False);
        Assert.That(invalidSource.Validation.Diagnostics[0].Code, Is.EqualTo("path.parentSegment"));
    }

    [Test]
    public void ResolveRelationshipTarget_PreservesFragmentsAndExternalTargets()
    {
        XlsxPackagePathResult fragmentOnly = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, "#definedName", false);
        XlsxPackagePathResult partFragment = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, "worksheets/sheet1.xml#A1", false);
        XlsxPackagePathResult external = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, "https://example.com/book.xlsx", true);
        XlsxPackagePathResult missingExternalMode = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, "https://example.com/book.xlsx", false);

        Assert.That(fragmentOnly.IsValid(), Is.True);
        Assert.That(fragmentOnly.Path, Is.EqualTo(XlsxPackagePaths.Workbook));
        Assert.That(fragmentOnly.Fragment, Is.EqualTo("definedName"));
        Assert.That(partFragment.IsValid(), Is.True);
        Assert.That(partFragment.Path, Is.EqualTo(XlsxPackagePaths.Worksheet1));
        Assert.That(partFragment.Fragment, Is.EqualTo("A1"));
        Assert.That(external.IsValid(), Is.True);
        Assert.That(external.IsExternal, Is.True);
        Assert.That(external.ExternalTarget, Is.EqualTo("https://example.com/book.xlsx"));
        Assert.That(external.Path, Is.EqualTo(""));
        Assert.That(missingExternalMode.IsValid(), Is.False);
        Assert.That(missingExternalMode.Validation.Diagnostics[0].Code, Is.EqualTo("relationship.target.externalModeRequired"));
    }
}
