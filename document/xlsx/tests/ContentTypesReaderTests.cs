using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ContentTypesReaderTests
{
    [Test]
    public void ReadString_ParsesDefaultsAndOverrides()
    {
        string xml = "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\" />" +
            "</Types>";
        XlsxContentTypesReader reader = new XlsxContentTypesReader();

        XlsxContentTypesPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Defaults.Count, Is.EqualTo(2));
        Assert.That(part.Overrides.Count, Is.EqualTo(2));
        Assert.That(part.GetDefault(".rels")!.ContentType, Is.EqualTo(XlsxContentTypes.Relationships));
        Assert.That(part.GetOverride("xl/workbook.xml")!.ContentType, Is.EqualTo(XlsxContentTypes.Workbook));
        Assert.That(part.GetContentType(XlsxPackagePaths.Worksheet1), Is.EqualTo(XlsxContentTypes.Worksheet));
        Assert.That(part.GetContentType("/custom/item.xml"), Is.EqualTo(XlsxContentTypes.Xml));
    }

    [Test]
    public void ReadString_NormalizesOverridePartNames()
    {
        string xml = "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Override PartName=\"xl/./workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\" />" +
            "</Types>";
        XlsxContentTypesReader reader = new XlsxContentTypesReader();

        XlsxContentTypesPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Overrides[0].PartName, Is.EqualTo(XlsxPackagePaths.Workbook));
        Assert.That(part.GetOverride(XlsxPackagePaths.Workbook), Is.Not.Null);
    }

    [Test]
    public void ReadString_ReportsDuplicateDefaultsAndOverrides()
    {
        string xml = "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Default Extension=\"XML\" ContentType=\"text/xml\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\" />" +
            "<Override PartName=\"xl/./workbook.xml\" ContentType=\"application/duplicate\" />" +
            "</Types>";
        XlsxContentTypesReader reader = new XlsxContentTypesReader();

        XlsxContentTypesPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.False);
        Assert.That(HasDiagnostic(part, "contentTypes.default.duplicate"), Is.True);
        Assert.That(HasDiagnostic(part, "contentTypes.override.duplicate"), Is.True);
        Assert.That(part.Defaults.Count, Is.EqualTo(1));
        Assert.That(part.Overrides.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_ReportsMissingAttributesAndInvalidOverridePaths()
    {
        string xml = "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default ContentType=\"application/xml\" />" +
            "<Default Extension=\"bin\" />" +
            "<Override ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\" />" +
            "<Override PartName=\"/xl/../workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\" />" +
            "<Override PartName=\"/xl/styles.xml\" />" +
            "</Types>";
        XlsxContentTypesReader reader = new XlsxContentTypesReader();

        XlsxContentTypesPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.False);
        Assert.That(HasDiagnostic(part, "contentTypes.default.extensionMissing"), Is.True);
        Assert.That(HasDiagnostic(part, "contentTypes.default.contentTypeMissing"), Is.True);
        Assert.That(HasDiagnostic(part, "contentTypes.override.partNameMissing"), Is.True);
        Assert.That(HasDiagnostic(part, "path.parentSegment"), Is.True);
        Assert.That(HasDiagnostic(part, "contentTypes.override.contentTypeMissing"), Is.True);
    }

    [Test]
    public void ReadString_PreservesUnknownAttributesAndElements()
    {
        string xml = "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" x:default=\"d\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\" x:override=\"o\" />" +
            "<x:Future x:value=\"1\"><x:Child /></x:Future>" +
            "</Types>";
        XlsxContentTypesReader reader = new XlsxContentTypesReader();

        XlsxContentTypesPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.UnknownRootAttributes[0].LocalName, Is.EqualTo("root"));
        Assert.That(part.Defaults[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Defaults[0].UnknownAttributes[0].LocalName, Is.EqualTo("default"));
        Assert.That(part.Overrides[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Overrides[0].UnknownAttributes[0].LocalName, Is.EqualTo("override"));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml[0], Does.Contain("Future"));
        Assert.That(part.UnknownXml[0], Does.Contain("Child"));
    }

    [Test]
    public void ReadString_ReportsInvalidOrMissingRoot()
    {
        XlsxContentTypesReader reader = new XlsxContentTypesReader();

        XlsxContentTypesPart invalidRoot = reader.ReadString("<Bad />");
        XlsxContentTypesPart missingRoot = reader.ReadString("");

        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "contentTypes.invalidRoot"), Is.True);
        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "contentTypes.missingRoot"), Is.True);
    }

    static bool HasDiagnostic(XlsxContentTypesPart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
