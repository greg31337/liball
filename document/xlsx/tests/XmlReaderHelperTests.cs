using System.Xml;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class XmlReaderHelperTests
{
    [Test]
    public void CreateSafeReaderSettings_DisablesDtdProcessingAndIgnoresWhitespace()
    {
        XmlReaderSettings settings = XlsxXmlReaderHelpers.CreateSafeReaderSettings();

        Assert.That(settings.DtdProcessing, Is.EqualTo(DtdProcessing.Prohibit));
        Assert.That(settings.IgnoreWhitespace, Is.True);
        Assert.That(settings.IgnoreComments, Is.False);
    }

    [Test]
    public void ValidateStringLimits_TracksObservedDepthAndTokenSize()
    {
        string xml = "<root><child attr=\"123\">text</child></root>";
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.MaxXmlDepth = 16;
        options.MaxXmlTokenCharacters = 64;

        XlsxXmlReadLimitResult result = XlsxXmlReaderHelpers.ValidateStringLimits(xml, options, "/xl/workbook.xml");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.MaxObservedDepth, Is.EqualTo(1));
        Assert.That(result.MaxObservedTokenCharacters, Is.GreaterThanOrEqualTo(12));
    }

    [Test]
    public void ValidateStringLimits_ReportsDepthAndTokenLimitDiagnostics()
    {
        string xml = "<root><child attr=\"123456789\"><grand>text</grand></child></root>";
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.MaxXmlDepth = 1;
        options.MaxXmlTokenCharacters = 5;

        XlsxXmlReadLimitResult result = XlsxXmlReaderHelpers.ValidateStringLimits(xml, options, "/xl/workbook.xml");

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "xml.maxDepth"), Is.True);
        Assert.That(HasDiagnostic(result, "xml.maxTokenCharacters"), Is.True);
        Assert.That(result.MaxObservedDepth, Is.EqualTo(2));
        Assert.That(result.MaxObservedTokenCharacters, Is.GreaterThan(5));
    }

    [Test]
    public void ValidateStringLimits_PropagatesInvalidReaderOptions()
    {
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.MaxXmlDepth = 0;

        XlsxXmlReadLimitResult result = XlsxXmlReaderHelpers.ValidateStringLimits("<root />", options, "/xl/workbook.xml");

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "reader.maxXmlDepth"), Is.True);
        Assert.That(result.MaxObservedDepth, Is.EqualTo(0));
    }

    [Test]
    public void ValidateStringLimits_RejectsDtdsBeforeExternalEntitiesCanResolve()
    {
        string xml = "<!DOCTYPE root [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><root>&xxe;</root>";
        XlsxReaderOptions options = new XlsxReaderOptions();

        Assert.Throws<XmlException>(() => XlsxXmlReaderHelpers.ValidateStringLimits(xml, options, "/xl/workbook.xml"));
    }

    [Test]
    public void ExistingXmlParsersUseSafeReaderSettings()
    {
        string xml = "<!DOCTYPE Types [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">&xxe;</Types>";
        XlsxContentTypesReader reader = new XlsxContentTypesReader();

        Assert.Throws<XmlException>(() => reader.ReadString(xml));
    }

    static bool HasDiagnostic(XlsxXmlReadLimitResult result, string code)
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
