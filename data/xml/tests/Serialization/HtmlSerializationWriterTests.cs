using xml;

namespace xml.Tests;

[TestFixture]
public class HtmlSerializationWriterTests
{
    [Test]
    public void Html4UsesCaseInsensitiveVoidRecognitionAndPairedOtherTags()
    {
        XmlDocument document = Parse("<HTML><BODY><BR/><P/><Widget/></BODY></HTML>");
        XmlSerializationOptions options = HtmlOptions("4.0");

        string result = Serialize(document, options);

        Assert.That(result, Is.EqualTo("<HTML><BODY><BR><P></P><Widget></Widget></BODY></HTML>"));
    }

    [Test]
    public void Html5WritesAutomaticDoctypeAndItsVoidElementSet()
    {
        XmlDocument document = Parse("<html><body><source/><track/><basefont/></body></html>");
        XmlSerializationOptions options = HtmlOptions("5.0");

        string result = Serialize(document, options);

        Assert.That(result, Is.EqualTo("<!DOCTYPE html><html><body><source><track><basefont></basefont></body></html>"));
    }

    [Test]
    public void ScriptAndStyleTextAreWrittenWithoutEscaping()
    {
        XmlDocument document = Parse("<html><script>if (a &lt; b &amp;&amp; c) { run(); }</script><style>.a &gt; .b &amp; .c {}</style></html>");
        XmlSerializationOptions options = HtmlOptions("4.0");

        string result = Serialize(document, options);

        Assert.That(result, Is.EqualTo("<html><script>if (a < b && c) { run(); }</script><style>.a > .b & .c {}</style></html>"));
    }

    [Test]
    public void RawElementDescendantAttributesAlsoBypassEscaping()
    {
        XmlDocument document = Parse("<html><script><x data=\"a&lt;b&amp;c\"/></script></html>");
        XmlSerializationOptions options = HtmlOptions("4.0");

        string result = Serialize(document, options);

        Assert.That(result, Is.EqualTo("<html><script><x data=\"a<b&c\"></x></script></html>"));
    }

    [Test]
    public void MinimizesBooleanAttributesOnlyWhenTheirValuesMatch()
    {
        XmlDocument document = Parse("<HTML><INPUT CHECKED=\"checked\" disabled=\"no\"/><OPTION selected=\"SELECTED\" multiple=\"false\"/></HTML>");
        XmlSerializationOptions options = HtmlOptions("4.0");

        string result = Serialize(document, options);

        Assert.That(result, Is.EqualTo("<HTML><INPUT CHECKED disabled=\"no\"><OPTION selected multiple=\"false\"></OPTION></HTML>"));
    }

    [Test]
    public void EscapesUriNonAsciiAndUsesHtmlAttributeEscapingExceptions()
    {
        XmlDocument document = Parse("<html><a href=\"café?q=a&amp;b#part\" data-value=\"a&lt;b&amp;{token}\">link</a></html>");
        XmlSerializationOptions options = HtmlOptions("4.0");
        options.EscapeUriAttributes = true;

        string result = Serialize(document, options);

        Assert.That(result, Does.Contain("href=\"caf%C3%A9?q=a&amp;b#part\" data-value=\"a<b&{token}\""));
    }

    [Test]
    public void ExcludesUriEscapedAttributesFromCharacterMaps()
    {
        XmlDocument document = Parse("<html><a href=\"xé\" title=\"xé\">link</a></html>");
        XmlSerializationOptions options = HtmlOptions("5.0");
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
    public void InsertsContentTypeMetaAndDiscardsExistingDeclaration()
    {
        XmlDocument document = Parse("<HTML><HEAD><META http-equiv=\" content-type \" content=\"old\"/><TITLE/></HEAD><BODY/></HTML>");
        XmlSerializationOptions options = HtmlOptions("4.0");
        options.IncludeContentType = true;
        options.MediaType = "text/html";

        string result = Serialize(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("<HTML><HEAD><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-16\"><TITLE></TITLE></HEAD><BODY></BODY></HTML>"));
            Assert.That(result, Does.Not.Contain("content=\"old\""));
        });
    }

    [Test]
    public void PreservesForeignXmlIslandMarkupRules()
    {
        XmlDocument document = Parse("<html><body><svg xmlns=\"http://www.w3.org/2000/svg\"><path/></svg><x:box xmlns:x=\"urn:example\"><x:empty/></x:box></body></html>");
        XmlSerializationOptions options = HtmlOptions("5.0");

        string result = Serialize(document, options);

        Assert.That(result, Is.EqualTo("<!DOCTYPE html><html><body><svg xmlns=\"http://www.w3.org/2000/svg\"><path/></svg><x:box xmlns:x=\"urn:example\"><x:empty/></x:box></body></html>"));
    }

    [Test]
    public void SupportsPublicOnlyDoctypeAndHtmlProcessingInstructionSyntax()
    {
        XmlDocument document = Parse("<?build ready?><html/>");
        XmlSerializationOptions options = HtmlOptions("4.0");
        options.DoctypePublic = "-//W3C//DTD HTML 4.01//EN";

        string result = Serialize(document, options);

        Assert.That(result, Is.EqualTo("<?build ready><!DOCTYPE html PUBLIC \"-//W3C//DTD HTML 4.01//EN\"><html></html>"));
    }

    [Test]
    public void RejectsLegacyControlCharactersAndUnsupportedVersions()
    {
        XmlDocument document = Parse("<html><p>value</p></html>");
        document.DocumentElement!.Children![0].Children![0].Value = "bad\u0085value";
        XmlSerializationOptions legacy = HtmlOptions("4.0");
        XmlSerializationOptions unsupported = HtmlOptions("6.0");

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<XmlSerializationException>(() => Serialize(document, legacy))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0014));
            Assert.That(Assert.Throws<XmlSerializationException>(() => Serialize(document, unsupported))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SESU0013));
        });
    }

    private static XmlSerializationOptions HtmlOptions(string version)
    {
        return new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Html,
            HtmlVersion = version,
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
