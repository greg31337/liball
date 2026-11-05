namespace sip.Tests;

public class SipXmlBodyComprehensiveTests
{
    [Test]
    public void ParsesAndRoundTripsFullDocumentsForEveryXmlEventFamily()
    {
        var documents = new[]
        {
            (Subtype: "pidf+xml", Root: "presence",
                Namespace: "urn:ietf:params:xml:ns:pidf",
                Xml: "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                    + "<presence xmlns=\"urn:ietf:params:xml:ns:pidf\" "
                    + "xmlns:rpid=\"urn:ietf:params:xml:ns:pidf:rpid\" "
                    + "xmlns:x=\"urn:example:device\" entity=\"pres:someone@example.com\">"
                    + "<tuple id=\"sg89ae\"><status><basic>open</basic>"
                    + "<rpid:activities><rpid:busy/></rpid:activities></status>"
                    + "<contact priority=\"0.8\">tel:+09012345678</contact>"
                    + "<note xml:lang=\"en\">Ready &amp; available</note>"
                    + "<x:device x:id=\"phone\"/></tuple></presence>"),
            (Subtype: "watcherinfo+xml", Root: "watcherinfo",
                Namespace: "urn:ietf:params:xml:ns:watcherinfo",
                Xml: "<?xml version=\"1.0\"?>"
                    + "<watcherinfo xmlns=\"urn:ietf:params:xml:ns:watcherinfo\" "
                    + "version=\"0\" state=\"full\"><watcher-list "
                    + "resource=\"sip:professor@example.net\" package=\"presence\">"
                    + "<watcher status=\"active\" id=\"one\" event=\"approved\">"
                    + "sip:userA@example.net</watcher></watcher-list></watcherinfo>"),
            (Subtype: "dialog-info+xml", Root: "dialog-info",
                Namespace: "urn:ietf:params:xml:ns:dialog-info",
                Xml: "<?xml version=\"1.0\"?>"
                    + "<dialog-info xmlns=\"urn:ietf:params:xml:ns:dialog-info\" "
                    + "version=\"3\" state=\"partial\" entity=\"sip:alice@example.com\">"
                    + "<dialog id=\"as7d900as8\"><state>confirmed</state>"
                    + "</dialog></dialog-info>"),
            (Subtype: "reginfo+xml", Root: "reginfo",
                Namespace: "urn:ietf:params:xml:ns:reginfo",
                Xml: "<?xml version=\"1.0\"?>"
                    + "<reginfo xmlns=\"urn:ietf:params:xml:ns:reginfo\" "
                    + "version=\"8\" state=\"full\"><registration "
                    + "aor=\"sip:user@example.com\" id=\"as9\" state=\"active\">"
                    + "<contact id=\"76\" state=\"active\" event=\"registered\">"
                    + "<uri>sip:user@pc887.example.com</uri></contact>"
                    + "</registration></reginfo>"),
            (Subtype: "im-iscomposing+xml", Root: "isComposing",
                Namespace: "urn:ietf:params:xml:ns:im-iscomposing",
                Xml: "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                    + "<isComposing xmlns=\"urn:ietf:params:xml:ns:im-iscomposing\">"
                    + "<state>active</state><contenttype>text/plain</contenttype>"
                    + "<refresh>90</refresh></isComposing>")
        };
        SipXmlBodyCodec codec = new SipXmlBodyCodec();
        SipWriteOptions canonical = new SipWriteOptions();
        canonical.PreserveOriginal = false;

        Assert.Multiple(() =>
        {
            foreach (var document in documents)
            {
                SipParseResult parsed = codec.ParseXmlBody(
                    Bytes(document.Xml), MediaType(document.Subtype), new SipParseOptions());
                Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete), document.Subtype);
                Assert.That(parsed.ValidationIssues, Is.Empty, document.Subtype);
                Assert.That(parsed.XmlBody!.RootName, Is.EqualTo(document.Root), document.Subtype);
                Assert.That(parsed.XmlBody.NamespaceUri,
                    Is.EqualTo(document.Namespace), document.Subtype);
                SipWriteResult written = codec.FormatXmlBody(parsed.XmlBody, canonical);
                Assert.That(written.Success, Is.True, document.Subtype);
                SipParseResult reparsed = codec.ParseXmlBody(
                    written.Bytes, MediaType(document.Subtype), new SipParseOptions());
                Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete), document.Subtype);
                Assert.That(reparsed.XmlBody!.XmlText, Is.EqualTo(document.Xml), document.Subtype);
            }
        });
    }

    [Test]
    public void PreservesNestedNamespaceExtensionsWithoutResolvingExternalResources()
    {
        string xml = "<?xml version=\"1.0\"?>"
            + "<p:presence xmlns:p=\"urn:ietf:params:xml:ns:pidf\" "
            + "xmlns:x=\"urn:example:extension\" "
            + "xmlns:xi=\"http://www.w3.org/2001/XInclude\" "
            + "entity=\"pres:alice@example.com\">"
            + "<x:container x:label=\"A &amp; B\">"
            + "<status xmlns=\"urn:example:nested\"><p:basic>open</p:basic></status>"
            + "<xi:include href=\"file:///etc/passwd\" parse=\"text\"/>"
            + "</x:container></p:presence>";
        SipXmlBodyCodec codec = new SipXmlBodyCodec();

        SipParseResult parsed = codec.ParseXmlBody(
            Bytes(xml), MediaType("pidf+xml"), new SipParseOptions());
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        SipWriteResult written = codec.FormatXmlBody(parsed.XmlBody!, options);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(parsed.XmlBody!.RootName, Is.EqualTo("presence"));
            Assert.That(parsed.XmlBody.NamespaceUri,
                Is.EqualTo("urn:ietf:params:xml:ns:pidf"));
            Assert.That(parsed.XmlBody.XmlText, Does.Contain("x:container"));
            Assert.That(parsed.XmlBody.XmlText, Does.Contain("file:///etc/passwd"));
            Assert.That(written.Success, Is.True);
            Assert.That(Text(written.Bytes), Is.EqualTo(xml));
        });
    }

    [Test]
    public void RejectsDtdEntityAndDeclarationExpansionForms()
    {
        string suffix = "<presence xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "entity=\"pres:a@example.com\"/>";
        string[] attacks =
        [
            "<!DOCTYPE presence SYSTEM \"file:///etc/passwd\">" + suffix,
            "<!DOCTYPE presence PUBLIC \"id\" \"http://example.invalid/pidf.dtd\">" + suffix,
            "<!DOCTYPE presence [<!ENTITY x SYSTEM \"http://example.invalid/x\">]>"
                + suffix.Replace("/>", ">&x;</presence>", StringComparison.Ordinal),
            "<!DOCTYPE presence [<!ENTITY % p SYSTEM \"file:///etc/passwd\">%p;]>" + suffix,
            "<!ELEMENT presence ANY>" + suffix,
            "<?xml version=\"1.0\"?>"
                + suffix.Replace("/>", ">&notDeclared;</presence>", StringComparison.Ordinal)
        ];
        SipXmlBodyCodec codec = new SipXmlBodyCodec();

        SipParseResult[] strict = attacks.Select(xml => codec.ParseXmlBody(
            Bytes(xml), MediaType("pidf+xml"), new SipParseOptions())).ToArray();
        SipParseOptions compatibleOptions = new SipParseOptions();
        compatibleOptions.Strict = false;
        SipParseResult[] compatible = attacks.Select(xml => codec.ParseXmlBody(
            Bytes(xml), MediaType("pidf+xml"), compatibleOptions)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(strict, Has.All.Matches<SipParseResult>(result =>
                result.Status == SipParseStatus.Error));
            Assert.That(compatible, Has.All.Matches<SipParseResult>(result =>
                result.Status == SipParseStatus.Error));
        });
    }

    [Test]
    public void RejectsMisplacedDeclarationsAndDuplicateExpandedAttributes()
    {
        string leadingDeclaration = " \r\n<?xml version=\"1.0\"?>"
            + "<presence xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "entity=\"pres:a@example.com\"/>";
        string duplicateExpandedAttribute = "<?xml version=\"1.0\"?>"
            + "<presence xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "xmlns:a=\"urn:example:attributes\" xmlns:b=\"urn:example:attributes\" "
            + "entity=\"pres:a@example.com\" a:id=\"one\" b:id=\"two\"/>";
        SipXmlBodyCodec codec = new SipXmlBodyCodec();

        SipParseResult declaration = codec.ParseXmlBody(
            Bytes(leadingDeclaration), MediaType("pidf+xml"), new SipParseOptions());
        SipParseResult attributes = codec.ParseXmlBody(
            Bytes(duplicateExpandedAttribute), MediaType("pidf+xml"), new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(declaration.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(declaration.ErrorMessage, Does.Contain("declaration"));
            Assert.That(attributes.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(attributes.ErrorMessage, Does.Contain("expanded name"));
        });
    }

    [Test]
    public void RejectsMalformedXmlNamespaceAndReferenceForms()
    {
        string pidfStart = "<?xml version=\"1.0\"?><presence "
            + "xmlns=\"urn:ietf:params:xml:ns:pidf\" entity=\"pres:a@example.com\">";
        string[] malformed =
        [
            pidfStart + "<tuple></presence>",
            pidfStart + "</presence><presence/>",
            pidfStart + "<x:extension/></presence>",
            pidfStart + "<tuple id=\"one\" id=\"two\"/></presence>",
            pidfStart + "<!-- invalid--comment --></presence>",
            "<![CDATA[outside]]>" + pidfStart + "</presence>",
            pidfStart + "&#0;</presence>",
            pidfStart + "&#xD800;</presence>",
            pidfStart + "&amp</presence>",
            pidfStart + "<tuple attribute=no/></presence>",
            pidfStart + "<tuple></tuple",
            pidfStart + "<!SOMETHING></presence>"
        ];

        SipParseResult[] results = malformed.Select(xml => new SipXmlBodyCodec().ParseXmlBody(
            Bytes(xml), MediaType("pidf+xml"), new SipParseOptions())).ToArray();

        Assert.That(results, Has.All.Matches<SipParseResult>(result =>
            result.Status == SipParseStatus.Error));
    }

    [Test]
    public void AcceptsUtf8BomNumericReferencesCommentsCdataAndProcessingInstructions()
    {
        string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
            + "<!--before--><?event safe?>"
            + "<presence xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "entity=\"pres:alice&#64;example.com\">"
            + "<note><![CDATA[<ready>&safe</ready>]]></note>"
            + "<note>&#x1F600;</note></presence><!--after-->";
        byte[] source = new byte[] { 239, 187, 191 }.Concat(Bytes(xml)).ToArray();
        SipXmlBodyCodec codec = new SipXmlBodyCodec();

        SipParseResult parsed = codec.ParseXmlBody(
            source, MediaType("pidf+xml"), new SipParseOptions());
        SipWriteOptions preserve = new SipWriteOptions();
        preserve.PreserveOriginal = true;
        SipWriteResult written = codec.FormatXmlBody(parsed.XmlBody!, preserve);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(parsed.XmlBody!.XmlText, Is.EqualTo(xml));
            Assert.That(written.Success, Is.True);
            Assert.That(written.Bytes, Is.EqualTo(source));
        });
    }

    [Test]
    public void EnforcesExactXmlDepthAndBodySizeLimits()
    {
        string xml = "<?xml version=\"1.0\"?><presence "
            + "xmlns=\"urn:ietf:params:xml:ns:pidf\" entity=\"pres:a@example.com\">"
            + "<tuple><status><basic>open</basic></status></tuple></presence>";
        byte[] source = Bytes(xml);
        SipXmlBodyCodec codec = new SipXmlBodyCodec();
        SipParseOptions exact = new SipParseOptions();
        exact.MaxMessageBytes = source.Length;
        exact.MaxBodyBytes = source.Length;
        exact.MaxXmlDepth = 4;
        SipParseOptions messageLimited = CopyLimits(exact);
        messageLimited.MaxMessageBytes = source.Length - 1;
        SipParseOptions bodyLimited = CopyLimits(exact);
        bodyLimited.MaxBodyBytes = source.Length - 1;
        SipParseOptions depthLimited = CopyLimits(exact);
        depthLimited.MaxXmlDepth = 3;

        Assert.Multiple(() =>
        {
            Assert.That(codec.ParseXmlBody(source, MediaType("pidf+xml"), exact).Status,
                Is.EqualTo(SipParseStatus.Complete));
            Assert.That(codec.ParseXmlBody(source, MediaType("pidf+xml"), messageLimited).Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(codec.ParseXmlBody(source, MediaType("pidf+xml"), bodyLimited).Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(codec.ParseXmlBody(source, MediaType("pidf+xml"), depthLimited).Status,
                Is.EqualTo(SipParseStatus.Error));
        });
    }

    [Test]
    public void ParsesCanonicalMessageSummaryAndRejectsMalformedVariants()
    {
        string summary = "Messages-Waiting: yes\r\n"
            + "Message-Account: sip:alice@example.com\r\n"
            + "Voice-Message: 1/3 (0/1)\r\n"
            + "Fax-Message: 2/4\r\n"
            + "\r\n"
            + "From: sip:bob@example.com\r\n"
            + "Subject: retained\r\n";
        string[] malformed =
        [
            "Message-Account: sip:alice@example.com\r\n",
            "Messages-Waiting: maybe\r\n",
            "Messages-Waiting: yes\r\nMessage-Account: <sip:alice@example.com>\r\n",
            "Messages-Waiting: yes\r\nVoice-Message: 4294967296/0\r\n",
            "Messages-Waiting: yes\r\nVoice-Message: 1/2 (1)\r\n",
            "Messages-Waiting: yes\r\n\r\n\r\nSubject: duplicate separator\r\n",
            "Messages-Waiting: yes\r\nSubject: missing separator\r\n"
        ];
        SipXmlBodyCodec codec = new SipXmlBodyCodec();
        SipMediaType mediaType = MediaType("simple-message-summary");

        SipParseResult parsed = codec.ParseXmlBody(Bytes(summary), mediaType, new SipParseOptions());
        SipWriteOptions canonical = new SipWriteOptions();
        canonical.PreserveOriginal = false;
        SipWriteResult written = codec.FormatXmlBody(parsed.XmlBody!, canonical);
        SipParseResult reparsed = codec.ParseXmlBody(written.Bytes, mediaType, new SipParseOptions());
        SipParseResult[] rejected = malformed.Select(text => codec.ParseXmlBody(
            Bytes(text), mediaType, new SipParseOptions())).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(parsed.XmlBody!.RootName, Is.EqualTo("message-summary"));
            Assert.That(written.Success, Is.True);
            Assert.That(Text(written.Bytes), Is.EqualTo(summary));
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(rejected, Has.All.Matches<SipParseResult>(result =>
                result.Status == SipParseStatus.Error));
        });
    }

    [Test]
    public void EnforcesExactMessageSummaryLineLimitAndUtf8MediaParameters()
    {
        string summary = "Messages-Waiting: no\r\nVoice-Message: 0/0\r\n";
        int longestLine = "Messages-Waiting: no".Length;
        SipMediaType mediaType = MediaType("simple-message-summary");
        mediaType.Parameters =
        [
            Parameter("charset", "utf-8"),
            Parameter("profile", "voice")
        ];
        SipParseOptions exact = new SipParseOptions();
        exact.MaxHeaderLineBytes = longestLine;
        SipParseOptions limited = new SipParseOptions();
        limited.MaxHeaderLineBytes = longestLine - 1;
        SipXmlBodyCodec codec = new SipXmlBodyCodec();

        SipParseResult accepted = codec.ParseXmlBody(Bytes(summary), mediaType, exact);
        SipParseResult rejected = codec.ParseXmlBody(Bytes(summary), mediaType, limited);
        SipMediaType duplicates = MediaType("simple-message-summary");
        duplicates.Parameters =
        [
            Parameter("charset", "UTF-8"),
            Parameter("CHARSET", "UTF-8")
        ];
        SipParseResult duplicateResult = codec.ParseXmlBody(
            Bytes(summary), duplicates, new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(rejected.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(rejected.ErrorMessage, Does.Contain("line"));
            Assert.That(duplicateResult.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(duplicateResult.ValidationIssues.Select(issue => issue.Code),
                Does.Contain("xml-charset"));
        });
    }

    static SipParseOptions CopyLimits(SipParseOptions source)
    {
        SipParseOptions copy = new SipParseOptions();
        copy.MaxMessageBytes = source.MaxMessageBytes;
        copy.MaxBodyBytes = source.MaxBodyBytes;
        copy.MaxXmlDepth = source.MaxXmlDepth;
        return copy;
    }

    static SipUriParameter Parameter(string name, string value)
    {
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = name;
        parameter.Value = value;
        parameter.HasValue = true;
        return parameter;
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
