namespace sip.Tests;

public class SipGenericExtensionTests
{
    [Test]
    public void GenericAccessFindsOrderedHeadersAndParametersWithoutChangingTheirSpelling()
    {
        SipHeader first = Header("X-Trace", "one");
        SipHeader second = Header("x-trace", "two");
        SipMessage message = new SipMessage();
        message.Headers = [first, Header("Other", "ignored"), second, Header("v", "compact")];
        SipUriParameter firstParameter = Parameter("X-Mode", "fast");
        SipUriParameter secondParameter = Parameter("x-mode", "safe");
        SipUriParameter[] parameters = [firstParameter, Parameter("other", "ignored"), secondParameter];
        ISipHeaderCodec codec = new SipHeaderCodec();

        SipHeader[] headers = codec.FindHeaders(message, "X-TRACE");
        SipUriParameter[] matches = codec.FindParameters(parameters, "X-MODE");

        Assert.Multiple(() =>
        {
            Assert.That(headers, Is.EqualTo(new SipHeader[] { first, second }));
            Assert.That(headers[0].Name, Is.EqualTo("X-Trace"));
            Assert.That(headers[1].Value, Is.EqualTo("two"));
            Assert.That(codec.FindHeaders(message, SipHeaderNames.Via).Single().Name, Is.EqualTo("v"));
            Assert.That(matches, Is.EqualTo(new SipUriParameter[] { firstParameter, secondParameter }));
            Assert.That(matches[0].Name, Is.EqualTo("X-Mode"));
            Assert.That(codec.FindHeaders(message, "bad name"), Is.Empty);
            Assert.That(codec.FindParameters(parameters, "bad name"), Is.Empty);
        });
    }

    [Test]
    public void RegistryAggregatesDistinctValidatedCapabilitiesAndSupportsLookups()
    {
        TestExtension first = Extension(Capabilities(
            ["X-ONE", "bad method"], ["feature-one"], ["application/x-one"],
            ["widget.state"], ["X-Widget"], ["widgets"]), true, "first");
        TestExtension second = Extension(Capabilities(
            ["X-TWO", "X-ONE"], ["FEATURE-ONE", "feature-two"], ["APPLICATION/X-ONE", "image/*"],
            ["WIDGET.STATE", "other.event"], ["x-widget", "X-Other"], ["WIDGETS", "other+scheme"]),
            true, "second");
        ISipExtensionRegistry registry = new SipExtensionRegistry();

        bool addedFirst = registry.RegisterExtension(first);
        bool duplicate = registry.RegisterExtension(first);
        bool addedSecond = registry.RegisterExtension(second);
        SipCapabilitySet aggregate = registry.GetRegisteredCapabilities();

        Assert.Multiple(() =>
        {
            Assert.That(addedFirst, Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(addedSecond, Is.True);
            Assert.That(aggregate.Methods, Is.EqualTo(new string[] { "X-ONE", "X-TWO" }));
            Assert.That(aggregate.OptionTags, Is.EqualTo(new string[] { "feature-one", "feature-two" }));
            Assert.That(aggregate.MediaTypes, Is.EqualTo(new string[] { "application/x-one", "image/*" }));
            Assert.That(aggregate.EventPackages, Is.EqualTo(new string[] { "widget.state", "other.event" }));
            Assert.That(aggregate.HeaderNames, Is.EqualTo(new string[] { "X-Widget", "X-Other" }));
            Assert.That(aggregate.UriSchemes, Is.EqualTo(new string[] { "widgets", "other+scheme" }));
            Assert.That(registry.SupportsMethod("X-ONE"), Is.True);
            Assert.That(registry.SupportsMethod("x-one"), Is.False);
            Assert.That(registry.SupportsHeader("x-WIDGET"), Is.True);
            Assert.That(registry.SupportsUriScheme("WIDGETS"), Is.True);
            Assert.That(registry.SupportsMediaType("IMAGE/PNG"), Is.True);
            Assert.That(registry.SupportsOptionTag("FEATURE-TWO"), Is.True);
            Assert.That(registry.SupportsEventPackage("Widget.State"), Is.True);
            Assert.That(registry.SupportsHeader("bad name"), Is.False);
        });

        Assert.That(registry.RemoveExtension(first), Is.True);
        Assert.That(registry.RemoveExtension(first), Is.False);
        Assert.That(registry.SupportsMethod("X-ONE"), Is.True);
        Assert.That(registry.RemoveExtension(second), Is.True);
        Assert.That(registry.SupportsMethod("X-ONE"), Is.False);
    }

    [Test]
    public void RegistryDispatchesEachGenericCapabilityCategory()
    {
        AssertCategoryDispatch(Capabilities(["X-WIDGET"], [], [], [], [], []),
            Message("X-WIDGET", "sip", null, null, null));
        AssertCategoryDispatch(Capabilities([], [], [], [], [], ["widgets"]),
            Message("OPTIONS", "widgets", null, null, null));
        AssertCategoryDispatch(Capabilities([], [], ["application/x-widget"], [], [], []),
            Message("OPTIONS", "sip", "application/x-widget", null, null));
        AssertCategoryDispatch(Capabilities([], [], [], [], ["X-Widget"], []),
            Message("OPTIONS", "sip", null, Header("x-widget", "value"), null));
        AssertCategoryDispatch(Capabilities([], ["feature-one"], [], [], [], []),
            Message("OPTIONS", "sip", null, Header("Supported", "timer, FEATURE-ONE"), null));
        AssertCategoryDispatch(Capabilities([], [], [], ["widget.state"], [], []),
            Message("OPTIONS", "sip", null, Header("Event", "Widget.State;id=one"), null));
    }

    [Test]
    public void DispatchFallsThroughUntilAMatchingExtensionHandlesTheRequest()
    {
        TestExtension first = Extension(Capabilities(["X-WIDGET"], [], [], [], [], []), false, "first");
        TestExtension second = Extension(Capabilities(["X-WIDGET"], [], [], [], [], []), true, "second");
        TestExtension unmatched = Extension(Capabilities(["X-OTHER"], [], [], [], [], []), true, "other");
        ISipExtensionRegistry registry = new SipExtensionRegistry();
        registry.RegisterExtension(first);
        registry.RegisterExtension(unmatched);
        registry.RegisterExtension(second);

        SipExtensionResult result = registry.ProcessRegisteredRequest(
            Message("X-WIDGET", "sip", null, null, null));

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Event, Is.EqualTo("second"));
            Assert.That(first.Calls, Is.EqualTo(1));
            Assert.That(unmatched.Calls, Is.Zero);
            Assert.That(second.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidRegistryInputsRemainUnregisteredOrUndispatched()
    {
        SipExtensionRegistry registry = new SipExtensionRegistry();
        SipMessage response = new SipMessage();
        response.Kind = SipMessageKind.Response;
        response.Headers = [Header("X-Widget", "value")];

        SipExtensionResult missing = registry.ProcessRegisteredRequest(null!);
        SipExtensionResult wrongKind = registry.ProcessRegisteredRequest(response);

        Assert.Multiple(() =>
        {
            Assert.That(registry.RegisterExtension(null!), Is.False);
            Assert.That(registry.RemoveExtension(null!), Is.False);
            Assert.That(missing.Handled, Is.False);
            Assert.That(missing.ErrorMessage, Is.Not.Empty);
            Assert.That(wrongKind.Handled, Is.False);
            Assert.That(wrongKind.ErrorMessage, Is.Not.Empty);
        });
    }

    [Test]
    public void UnknownValuesSurviveMessageRoundTripAndReachRegisteredExtension()
    {
        string source = "X-WIDGET widgets:opaque-value SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-one\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=one\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: extension@example.com\r\n"
            + "CSeq: 1 X-WIDGET\r\n"
            + "Supported: feature-one\r\n"
            + "Event: widget.state;id=abc\r\n"
            + "X-Widget: Alpha;Mode=Fast\r\n"
            + "Content-Type: application/x-widget;version=2\r\n"
            + "Content-Length: 3\r\n\r\nabc";
        SipMessageCodec messageCodec = new SipMessageCodec();
        SipParseResult parsed = messageCodec.ParseStreamMessage(Bytes(source), new SipParseOptions());
        SipWriteOptions writeOptions = new SipWriteOptions();
        writeOptions.PreserveOriginal = false;
        SipWriteResult written = messageCodec.SerializeMessage(parsed.Message!, writeOptions);
        SipParseResult reparsed = messageCodec.ParseStreamMessage(written.Bytes, new SipParseOptions());
        SipExtensionRegistry registry = new SipExtensionRegistry();
        TestExtension extension = Extension(Capabilities(
            ["X-WIDGET"], ["feature-one"], ["application/x-widget"],
            ["widget.state"], ["X-Widget"], ["widgets"]), true, "handled");
        registry.RegisterExtension(extension);

        SipExtensionResult result = registry.ProcessRegisteredRequest(reparsed.Message!);
        SipHeader[] genericHeaders = new SipHeaderCodec().FindHeaders(reparsed.Message!, "x-widget");

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(written.Success, Is.True);
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(reparsed.Message!.Method, Is.EqualTo(SipMethod.Unknown));
            Assert.That(reparsed.Message.MethodToken, Is.EqualTo("X-WIDGET"));
            Assert.That(reparsed.Message.RequestUri!.Scheme, Is.EqualTo(SipUriScheme.Absolute));
            Assert.That(reparsed.Message.RequestUri.SchemeToken, Is.EqualTo("widgets"));
            Assert.That(reparsed.Message.ContentType!.Subtype, Is.EqualTo("x-widget"));
            Assert.That(genericHeaders.Single().Name, Is.EqualTo("X-Widget"));
            Assert.That(genericHeaders[0].Value, Is.EqualTo("Alpha;Mode=Fast"));
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Event, Is.EqualTo("handled"));
            Assert.That(extension.Calls, Is.EqualTo(1));
        });
    }

    static void AssertCategoryDispatch(SipCapabilitySet capabilities, SipMessage message)
    {
        TestExtension extension = Extension(capabilities, true, "matched");
        SipExtensionRegistry registry = new SipExtensionRegistry();
        registry.RegisterExtension(extension);

        SipExtensionResult result = registry.ProcessRegisteredRequest(message);

        Assert.That(result.Handled, Is.True);
        Assert.That(extension.Calls, Is.EqualTo(1));
    }

    static SipCapabilitySet Capabilities(string[] methods, string[] optionTags, string[] mediaTypes,
        string[] eventPackages, string[] headerNames, string[] uriSchemes)
    {
        SipCapabilitySet capabilities = new SipCapabilitySet();
        capabilities.Methods = methods;
        capabilities.OptionTags = optionTags;
        capabilities.MediaTypes = mediaTypes;
        capabilities.EventPackages = eventPackages;
        capabilities.HeaderNames = headerNames;
        capabilities.UriSchemes = uriSchemes;
        return capabilities;
    }

    static TestExtension Extension(SipCapabilitySet capabilities, bool handled, string eventToken)
    {
        TestExtension extension = new TestExtension();
        extension.Capabilities = capabilities;
        extension.Handled = handled;
        extension.EventToken = eventToken;
        return extension;
    }

    static SipMessage Message(string method, string scheme, string? contentType,
        SipHeader? header, SipHeader? secondHeader)
    {
        SipUri uri = new SipUri();
        uri.Scheme = scheme.Equals("sip", StringComparison.OrdinalIgnoreCase)
            ? SipUriScheme.Sip : SipUriScheme.Absolute;
        uri.SchemeToken = scheme;
        uri.Host = uri.Scheme == SipUriScheme.Sip ? "example.com" : null;
        uri.OpaquePart = uri.Scheme == SipUriScheme.Absolute ? "opaque" : null;
        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Request;
        message.MethodToken = method;
        message.RequestUri = uri;
        if (contentType != null)
        {
            string[] parts = contentType.Split('/');
            SipMediaType media = new SipMediaType();
            media.Type = parts[0];
            media.Subtype = parts[1];
            message.ContentType = media;
        }
        List<SipHeader> headers = new List<SipHeader>();
        if (header != null) headers.Add(header);
        if (secondHeader != null) headers.Add(secondHeader);
        message.Headers = headers.ToArray();
        return message;
    }

    static SipHeader Header(string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        return header;
    }

    static SipUriParameter Parameter(string name, string value)
    {
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = name;
        parameter.Value = value;
        parameter.HasValue = true;
        return parameter;
    }

    static byte[] Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    class TestExtension : ISipExtension
    {
        public SipCapabilitySet Capabilities = new SipCapabilitySet();
        public bool Handled;
        public string? EventToken;
        public int Calls;

        public SipCapabilitySet GetExtensionCapabilities()
        {
            return Capabilities;
        }

        public SipExtensionResult ProcessExtensionRequest(SipMessage message)
        {
            Calls = Calls + 1;
            SipExtensionResult result = new SipExtensionResult();
            result.Handled = Handled;
            result.Event = EventToken;
            return result;
        }
    }
}
