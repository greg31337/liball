using xml;

namespace xml.Tests;

[TestFixture]
public class XhtmlSerializationWriterTests
{
    [Test]
    public void UsesLegacyEmptyElementFormsAndXmlEscapingForScriptAndStyle()
    {
        XmlDocument document = Parse("<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><br/><p/><script>if (a &lt; b &amp;&amp; c) {}</script><style>.x&gt;.y &amp; z</style></body></html>");
        XmlSerializationOptions options = XhtmlOptions();

        string result = Serialize(document, options);

        Assert.That(result, Is.EqualTo("<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><br /><p></p><script>if (a &lt; b &amp;&amp; c) {}</script><style>.x>.y &amp; z</style></body></html>"));
    }

    [Test]
    public void DoesNotApplyXhtmlEmptyRulesToForeignOrLegacyNullNamespaceElements()
    {
        XmlDocument document = Parse("<root><p/><foreign xmlns=\"urn:foreign\"><br/></foreign></root>");
        XmlSerializationOptions options = XhtmlOptions();

        string result = Serialize(document, options);

        Assert.That(result, Is.EqualTo("<root><p/><foreign xmlns=\"urn:foreign\"><br/></foreign></root>"));
    }

    [Test]
    public void AppliesHtml5RecognitionPrefixNormalizationAndAutomaticDoctype()
    {
        XmlDocument document = Parse("<h:html xmlns:h=\"http://www.w3.org/1999/xhtml\"><h:head/><h:body><h:p/><h:br/></h:body></h:html>");
        XmlSerializationOptions options = XhtmlOptions();
        options.HtmlVersion = "5.0";

        string result = Serialize(document, options);

        Assert.That(result, Is.EqualTo("<!DOCTYPE html><html xmlns=\"http://www.w3.org/1999/xhtml\"><head></head><body><p></p><br/></body></html>"));
    }

    [Test]
    public void RecognizesNullNamespaceHtml5ElementsWithoutTreatingUnknownNamesAsHtml()
    {
        XmlDocument document = Parse("<html><head/><body><track/><p/><widget/></body></html>");
        XmlSerializationOptions options = XhtmlOptions();
        options.HtmlVersion = "5.0";

        string result = Serialize(document, options);

        Assert.That(result, Is.EqualTo("<!DOCTYPE html><html><head></head><body><track/><p></p><widget/></body></html>"));
    }

    [Test]
    public void EscapesOnlyNonAsciiUriCharactersBeforeXmlAttributeEscaping()
    {
        XmlDocument document = Parse("<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><a href=\"café?q=a&amp;b#part\" title=\"café\">link</a></body></html>");
        XmlSerializationOptions options = XhtmlOptions();
        options.EscapeUriAttributes = true;

        string result = Serialize(document, options);

        Assert.That(result, Does.Contain("href=\"caf%C3%A9?q=a&amp;b#part\" title=\"café\""));
    }

    [Test]
    public void ExcludesUriEscapedAttributesFromCharacterMaps()
    {
        XmlDocument document = Parse("<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><a href=\"xé\" title=\"xé\">link</a></body></html>");
        XmlSerializationOptions options = XhtmlOptions();
        options.EscapeUriAttributes = true;
        options.UseCharacterMaps =
        [
            new XmlCharacterMapEntry { CodePoint = 0x78, Replacement = "[x]" },
            new XmlCharacterMapEntry { CodePoint = 0xE9, Replacement = "[accent]" }
        ];

        string result = Serialize(document, options);

        Assert.That(result, Does.Contain("href=\"x%C3%A9\" title=\"[x][accent]\""));
    }

    [Test]
    public void ReplacesExistingContentTypeMetaWithEffectiveEncodingMetadata()
    {
        XmlDocument document = Parse("<html xmlns=\"http://www.w3.org/1999/xhtml\"><head><meta http-equiv=\" content-type \" content=\"old\"/><title/></head><body/></html>");
        XmlSerializationOptions options = XhtmlOptions();
        options.IncludeContentType = true;
        options.MediaType = "application/xhtml+xml";

        string result = Serialize(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("<html xmlns=\"http://www.w3.org/1999/xhtml\"><head><meta http-equiv=\"Content-Type\" content=\"application/xhtml+xml; charset=utf-16\" /><title></title></head><body></body></html>"));
            Assert.That(result, Does.Not.Contain("content=\"old\""));
        });
    }

    [Test]
    public void KeepsFormattedXhtmlContentFreeOfAddedIndentation()
    {
        XmlDocument document = Parse("<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><div><span/></div><script><x/></script></body></html>");
        XmlSerializationOptions options = XhtmlOptions();
        options.Indent = true;
        options.NewLine = "\n";

        string result = Serialize(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Contain("<div>\n      <span></span>\n    </div>"));
            Assert.That(result, Does.Contain("<script><x></x></script>"));
        });
    }

    [Test]
    public void RejectsInvalidHtmlVersionValues()
    {
        XmlDocument document = Parse("<html xmlns=\"http://www.w3.org/1999/xhtml\"/>");
        XmlSerializationOptions options = XhtmlOptions();
        options.HtmlVersion = "five";

        XmlSerializationException exception = Assert.Throws<XmlSerializationException>(() => Serialize(document, options))!;

        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SEPM0016));
    }

    private static XmlSerializationOptions XhtmlOptions()
    {
        return new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Xhtml,
            OmitXmlDeclaration = true,
            InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Error
        };
    }

    private static string Serialize(XmlDocument document, XmlSerializationOptions options)
    {
        StringWriter writer = new StringWriter();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer };
        new XmlSerializer().SerializeXmlDocument(document, output, options);
        return writer.ToString();
    }

    private static XmlDocument Parse(string source)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = source };
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreservePrefixes = true,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            RetainNamespaceDeclarations = true
        };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }
}
