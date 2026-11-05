namespace sip.Tests;

public class SipXmlBodyCodecTests
{
    [Test]
    public void ParsesPidfWithRichPresenceAndPreservesExtensions()
    {
        string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
            + "<presence xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "xmlns:rpid=\"urn:ietf:params:xml:ns:pidf:rpid\" "
            + "xmlns:device=\"urn:example:device\" entity=\"pres:alice@example.com\">"
            + "<tuple id=\"phone\"><status><basic>open</basic>"
            + "<rpid:activities><rpid:busy/></rpid:activities></status>"
            + "<device:capability enabled=\"true\"/></tuple></presence>";
        byte[] source = Bytes(xml);
        SipXmlBodyCodec codec = new SipXmlBodyCodec();

        SipParseResult parsed = codec.ParseXmlBody(source, MediaType("pidf+xml"),
            new SipParseOptions());
        SipWriteOptions preserve = new SipWriteOptions();
        preserve.PreserveOriginal = true;
        SipWriteResult preserved = codec.FormatXmlBody(parsed.XmlBody!, preserve);
        SipWriteOptions canonical = new SipWriteOptions();
        canonical.PreserveOriginal = false;
        SipWriteResult written = codec.FormatXmlBody(parsed.XmlBody!, canonical);
        SipParseResult reparsed = codec.ParseXmlBody(written.Bytes, MediaType("pidf+xml"),
            new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete), parsed.ErrorMessage);
            Assert.That(parsed.ValidationIssues, Is.Empty);
            Assert.That(parsed.XmlBody!.RootName, Is.EqualTo("presence"));
            Assert.That(parsed.XmlBody.NamespaceUri, Is.EqualTo("urn:ietf:params:xml:ns:pidf"));
            Assert.That(parsed.XmlBody.XmlText, Does.Contain("rpid:activities"));
            Assert.That(parsed.XmlBody.XmlText, Does.Contain("device:capability"));
            Assert.That(preserved.Success, Is.True);
            Assert.That(preserved.Bytes, Is.EqualTo(source));
            Assert.That(preserved.Bytes, Is.Not.SameAs(source));
            Assert.That(written.Success, Is.True);
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(reparsed.XmlBody!.XmlText, Does.Contain("rpid:busy"));
        });
    }

    [TestCase("watcherinfo+xml", "watcherinfo", "urn:ietf:params:xml:ns:watcherinfo",
        "version=\"0\" state=\"full\"", "")]
    [TestCase("dialog-info+xml", "dialog-info", "urn:ietf:params:xml:ns:dialog-info",
        "version=\"3\" state=\"partial\" entity=\"sip:alice@example.com\"", "<dialog id=\"one\"/>")]
    [TestCase("reginfo+xml", "reginfo", "urn:ietf:params:xml:ns:reginfo",
        "version=\"8\" state=\"full\"", "<registration aor=\"sip:a@example.com\" id=\"a\" state=\"active\"/>")]
    [TestCase("im-iscomposing+xml", "isComposing", "urn:ietf:params:xml:ns:im-iscomposing",
        "", "<state>active</state><contenttype>text/plain</contenttype><refresh>90</refresh>")]
    public void ParsesSupportedXmlEventFamilies(string subtype, string root, string namespaceUri,
        string attributes, string content)
    {
        string separator = attributes.Length == 0 ? "" : " ";
        string xml = "<?xml version=\"1.0\"?><" + root + " xmlns=\"" + namespaceUri + "\""
            + separator + attributes + ">" + content + "</" + root + ">";

        SipParseResult result = new SipXmlBodyCodec().ParseXmlBody(
            Bytes(xml), MediaType(subtype), new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(result.ValidationIssues, Is.Empty);
            Assert.That(result.XmlBody!.RootName, Is.EqualTo(root));
            Assert.That(result.XmlBody.NamespaceUri, Is.EqualTo(namespaceUri));
        });
    }

    [Test]
    public void ResolvesNamespaceOfPrefixedRootElement()
    {
        string xml = "<?xml version=\"1.0\"?>"
            + "<p:presence xmlns:p=\"urn:ietf:params:xml:ns:pidf\" "
            + "entity=\"pres:bob@example.com\"><p:note>Ready</p:note></p:presence>";

        SipParseResult result = new SipXmlBodyCodec().ParseXmlBody(
            Bytes(xml), MediaType("pidf+xml"), new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(result.XmlBody!.RootName, Is.EqualTo("presence"));
            Assert.That(result.XmlBody.NamespaceUri, Is.EqualTo("urn:ietf:params:xml:ns:pidf"));
        });
    }

    [Test]
    public void ParsesAndCanonicalizesMessageSummaryBodies()
    {
        string compatibleText = "Messages-Waiting: yes\n"
            + "Message-Account: sip:alice@vmail.example.com\n"
            + "Voice-Message: 2/8 (0/2)\n"
            + "Fax-Message: 0/1\n"
            + "\n"
            + "X-Detail: retained\n";
        byte[] source = Bytes(compatibleText);
        SipParseOptions compatible = new SipParseOptions();
        compatible.Strict = false;
        SipXmlBodyCodec codec = new SipXmlBodyCodec();

        SipParseResult parsed = codec.ParseXmlBody(source,
            MediaType("simple-message-summary"), compatible);
        SipWriteOptions canonical = new SipWriteOptions();
        canonical.PreserveOriginal = false;
        SipWriteResult written = codec.FormatXmlBody(parsed.XmlBody!, canonical);
        SipWriteOptions preserve = new SipWriteOptions();
        preserve.PreserveOriginal = true;
        SipWriteResult preserved = codec.FormatXmlBody(parsed.XmlBody!, preserve);
        SipParseResult strict = codec.ParseXmlBody(source,
            MediaType("simple-message-summary"), new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete), parsed.ErrorMessage);
            Assert.That(parsed.XmlBody!.RootName, Is.EqualTo("message-summary"));
            Assert.That(parsed.XmlBody.NamespaceUri, Is.Null);
            Assert.That(parsed.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(Text(written.Bytes), Does.Contain("Messages-Waiting: yes\r\n"));
            Assert.That(Text(written.Bytes), Does.Contain("\r\n\r\nX-Detail: retained\r\n"));
            Assert.That(preserved.Bytes, Is.EqualTo(source));
            Assert.That(strict.Status, Is.EqualTo(SipParseStatus.Error));
        });
    }

    [TestCase("<!DOCTYPE presence SYSTEM \"file:///etc/passwd\"><presence xmlns=\"urn:ietf:params:xml:ns:pidf\" entity=\"pres:a@example.com\"/>", true)]
    [TestCase("<!DOCTYPE presence [<!ENTITY xxe SYSTEM \"http://example.invalid/secret\">]><presence xmlns=\"urn:ietf:params:xml:ns:pidf\" entity=\"pres:a@example.com\">&xxe;</presence>", true)]
    [TestCase("<?xml version=\"1.0\"?><presence xmlns=\"urn:ietf:params:xml:ns:pidf\" entity=\"pres:a@example.com\"><tuple></presence>", true)]
    [TestCase("<?xml version=\"1.0\"?><presence xmlns=\"urn:example:wrong\" entity=\"pres:a@example.com\"/>", false)]
    [TestCase("<?xml version=\"1.0\"?><presence xmlns=\"urn:ietf:params:xml:ns:pidf\" entity=\"pres:a@example.com\">&custom;</presence>", true)]
    [TestCase("<?xml version=\"2.0\"?><presence xmlns=\"urn:ietf:params:xml:ns:pidf\" entity=\"pres:a@example.com\"/>", true)]
    public void RejectsDtdExternalEntitiesMalformedXmlAndWrongNamespaces(string xml,
        bool compatibleMustReject)
    {
        SipParseResult result = new SipXmlBodyCodec().ParseXmlBody(
            Bytes(xml), MediaType("pidf+xml"), new SipParseOptions());
        SipParseOptions compatibleOptions = new SipParseOptions();
        compatibleOptions.Strict = false;
        SipParseResult compatible = new SipXmlBodyCodec().ParseXmlBody(
            Bytes(xml), MediaType("pidf+xml"), compatibleOptions);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(compatible.Status, Is.EqualTo(
                compatibleMustReject ? SipParseStatus.Error : SipParseStatus.Complete));
        });
    }

    [Test]
    public void EnforcesXmlBodyAndDepthLimits()
    {
        string xml = "<?xml version=\"1.0\"?><presence xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "entity=\"pres:a@example.com\"><tuple><status><basic>open</basic>"
            + "</status></tuple></presence>";
        SipXmlBodyCodec codec = new SipXmlBodyCodec();
        SipParseOptions sizeOptions = new SipParseOptions();
        sizeOptions.MaxBodyBytes = 20;
        SipParseOptions depthOptions = new SipParseOptions();
        depthOptions.MaxXmlDepth = 3;

        SipParseResult size = codec.ParseXmlBody(Bytes(xml), MediaType("pidf+xml"), sizeOptions);
        SipParseResult depth = codec.ParseXmlBody(Bytes(xml), MediaType("pidf+xml"), depthOptions);
        SipParseResult utf8 = codec.ParseXmlBody(new byte[] { 60, 255, 62 },
            MediaType("pidf+xml"), new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(size.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(size.ErrorMessage, Does.Contain("maximum size"));
            Assert.That(depth.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(depth.ErrorMessage, Does.Contain("depth"));
            Assert.That(utf8.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(utf8.ErrorMessage, Does.Contain("UTF-8"));
        });
    }

    [Test]
    public void ValidatesRootRequirementsAndUnsupportedMediaTypes()
    {
        string xml = "<?xml version=\"1.0\"?><dialog-info "
            + "xmlns=\"urn:ietf:params:xml:ns:dialog-info\" version=\"4294967296\" "
            + "state=\"other\"></dialog-info>";
        SipParseResult parsed = new SipXmlBodyCodec().ParseXmlBody(
            Bytes(xml), MediaType("dialog-info+xml"), new SipParseOptions());
        SipParseResult unsupported = new SipXmlBodyCodec().ParseXmlBody(
            Bytes("<root/>"), MediaType("unknown+xml"), new SipParseOptions());
        SipXmlBody model = new SipXmlBody();
        model.MediaType = MediaType("pidf+xml");
        model.RootName = "wrong";
        model.NamespaceUri = "urn:ietf:params:xml:ns:pidf";
        model.XmlText = "<?xml version=\"1.0\"?><presence "
            + "xmlns=\"urn:ietf:params:xml:ns:pidf\" entity=\"pres:a@example.com\"/>";
        SipValidationIssue[] modelIssues = new SipXmlBodyCodec().ValidateXmlBody(model);
        SipWriteResult write = new SipXmlBodyCodec().FormatXmlBody(model, new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(parsed.ValidationIssues.Select(issue => issue.Code), Does.Contain("xml-version"));
            Assert.That(parsed.ValidationIssues.Select(issue => issue.Code), Does.Contain("xml-state"));
            Assert.That(parsed.ValidationIssues.Select(issue => issue.Code), Does.Contain("xml-entity"));
            Assert.That(unsupported.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(unsupported.ErrorMessage, Does.Contain("not supported"));
            Assert.That(modelIssues.Select(issue => issue.Code), Does.Contain("xml-model-identity"));
            Assert.That(write.Success, Is.False);
        });
    }

    [Test]
    public void RejectsNonUtf8CharsetParameters()
    {
        SipMediaType mediaType = MediaType("pidf+xml");
        SipUriParameter charset = new SipUriParameter();
        charset.Name = "charset";
        charset.Value = "ISO-8859-1";
        charset.HasValue = true;
        mediaType.Parameters = new[] { charset };
        string xml = "<?xml version=\"1.0\"?><presence "
            + "xmlns=\"urn:ietf:params:xml:ns:pidf\" entity=\"pres:a@example.com\"/>";

        SipParseResult result = new SipXmlBodyCodec().ParseXmlBody(
            Bytes(xml), mediaType, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ValidationIssues.Select(issue => issue.Code), Does.Contain("xml-charset"));
    }

    static SipMediaType MediaType(string subtype)
    {
        SipMediaType mediaType = new SipMediaType();
        mediaType.Type = "application";
        mediaType.Subtype = subtype;
        return mediaType;
    }

    static byte[] Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    static string Text(byte[] bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
