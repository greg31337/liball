using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxXmlValidatorTests
{
    [Test]
    public void ValidateString_AcceptsWellFormedXmlWithCommentsCDataAndEntities()
    {
        string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<root attr=\"Tom &amp; Jerry\">" +
            "<!-- comment -->" +
            "<![CDATA[<not-markup>&raw]]>" +
            "<child value=\"&#x41;\" />" +
            "</root>";
        PptxResult result = new PptxResult();

        PptxXmlValidator.ValidateString(xml, "/ppt/custom/item.xml", result);

        Assert.That(result.Succeeded(), Is.True, result.FormatDiagnostics());
    }

    [Test]
    public void ValidateString_ReportsMismatchedClosingTag()
    {
        PptxResult result = new PptxResult();

        PptxXmlValidator.ValidateString("<root><child></root>", "/ppt/custom/item.xml", result);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV042"));
    }

    [Test]
    public void ValidateString_ReportsUnclosedElement()
    {
        PptxResult result = new PptxResult();

        PptxXmlValidator.ValidateString("<root><child>", "/ppt/custom/item.xml", result);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV043"));
    }

    [Test]
    public void ValidateString_ReportsMultipleDocumentElements()
    {
        PptxResult result = new PptxResult();

        PptxXmlValidator.ValidateString("<root /><second />", "/ppt/custom/item.xml", result);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV044"));
    }

    [Test]
    public void ValidateString_ReportsInvalidEntityReferences()
    {
        PptxResult result = new PptxResult();

        PptxXmlValidator.ValidateString("<root>Tom & Jerry</root>", "/ppt/custom/item.xml", result);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV046"));
    }

    [Test]
    public void ValidateString_ReportsMalformedAttributesAndDtds()
    {
        PptxResult unquoted = new PptxResult();
        PptxResult dtd = new PptxResult();

        PptxXmlValidator.ValidateString("<root attr=value />", "/ppt/custom/item.xml", unquoted);
        PptxXmlValidator.ValidateString("<!DOCTYPE root><root />", "/ppt/custom/item.xml", dtd);

        Assert.That(unquoted.Failed(), Is.True);
        Assert.That(unquoted.FormatDiagnostics(), Does.Contain("PKGV041"));
        Assert.That(dtd.Failed(), Is.True);
        Assert.That(dtd.FormatDiagnostics(), Does.Contain("PKGV048"));
    }
}
