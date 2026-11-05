namespace sip.Tests;

public class SipOptionsAndUasTests
{
    [Test]
    public void AnswersOptionsWithNormalizedCapabilitiesAndMandatoryHeaders()
    {
        SipCapabilitySet capabilities = new SipCapabilitySet();
        capabilities.Methods =
            [SipMethods.Invite, "X-WIDGET", "bad method", SipMethods.Invite];
        capabilities.OptionTags =
            [SipOptionTags.Timer, "TIMER", SipOptionTags.Path];
        capabilities.MediaTypes =
            [SipMediaTypes.Sdp, "bad", "application/x-widget"];
        capabilities.EventPackages =
            ["presence", "PRESENCE", "dialog"];
        ISipOptionsService service = new SipOptionsService();
        SipMessage request = Request(SipMethods.Options);

        SipOptionsResult result = service.AnswerOptions(
            request, capabilities);
        SipOptionsResult retransmission = service.AnswerOptions(
            request, capabilities);
        SipWriteResult serialized = Serialize(result.Response!);

        capabilities.Methods[0] = SipMethods.Bye;
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(serialized.Success, Is.True,
                serialized.ErrorMessage);
            Assert.That(result.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(result.Capabilities!.Methods,
                Is.EqualTo(new string[] {
                    SipMethods.Invite, "X-WIDGET", SipMethods.Options }));
            Assert.That(result.Capabilities.OptionTags,
                Is.EqualTo(new string[] {
                    SipOptionTags.Timer, SipOptionTags.Path }));
            Assert.That(Header(result.Response, SipHeaderNames.Allow),
                Is.EqualTo("INVITE, X-WIDGET, OPTIONS"));
            Assert.That(Header(result.Response, SipHeaderNames.Supported),
                Is.EqualTo("timer, path"));
            Assert.That(Header(result.Response, SipHeaderNames.Accept),
                Is.EqualTo("application/sdp, application/x-widget"));
            Assert.That(Header(result.Response, SipHeaderNames.AllowEvents),
                Is.EqualTo("presence, dialog"));
            Assert.That(Header(result.Response, SipHeaderNames.Via),
                Is.Not.Empty);
            Assert.That(Header(result.Response, SipHeaderNames.From),
                Is.Not.Empty);
            Assert.That(Header(result.Response, SipHeaderNames.To),
                Does.Contain(";tag="));
            Assert.That(Header(retransmission.Response!, SipHeaderNames.To),
                Is.EqualTo(Header(result.Response, SipHeaderNames.To)));
            Assert.That(Header(result.Response, SipHeaderNames.CallId),
                Is.EqualTo("options@example.com"));
            Assert.That(Header(result.Response, SipHeaderNames.CSeq),
                Is.EqualTo("1 OPTIONS"));
            Assert.That(Header(result.Response, SipHeaderNames.ContentLength),
                Is.EqualTo("0"));
            Assert.That(Header(request, SipHeaderNames.To),
                Does.Not.Contain(";tag="));
        });
    }

    [Test]
    public void CreatesSecureOptionsQueriesAndHonorsCancellation()
    {
        ISipOptionsService service = new SipOptionsService();
        SipUri target = ParseUri("sips:service@example.com");
        SipCancellation active = new SipCancellation();
        SipCancellation cancelled = new SipCancellation();
        cancelled.Requested = true;
        cancelled.Reason = "stopped";

        SipOptionsResult created = service.QueryOptions(target, active);
        SipOptionsResult stopped = service.QueryOptions(target, cancelled);
        SipWriteResult serialized = Serialize(created.Request!);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True, created.ErrorMessage);
            Assert.That(serialized.Success, Is.True,
                serialized.ErrorMessage);
            Assert.That(created.Request!.Kind,
                Is.EqualTo(SipMessageKind.Request));
            Assert.That(created.Request.Method,
                Is.EqualTo(SipMethod.Options));
            Assert.That(created.Request.RequestUri!.Scheme,
                Is.EqualTo(SipUriScheme.Sips));
            Assert.That(created.Request.ViaValues[0].Transport,
                Is.EqualTo(SipTransportNames.Tls));
            Assert.That(created.Request.ViaValues[0].Branch,
                Does.StartWith("z9hG4bK"));
            Assert.That(Header(created.Request, SipHeaderNames.MaxForwards),
                Is.EqualTo("70"));
            Assert.That(Header(created.Request, SipHeaderNames.ContentLength),
                Is.EqualTo("0"));
            Assert.That(stopped.Success, Is.False);
            Assert.That(stopped.Request, Is.Null);
            Assert.That(stopped.ErrorMessage, Is.EqualTo("stopped"));
        });
    }

    [Test]
    public void CompletesApplicationProvisionalAndFinalResponses()
    {
        RespondingExtension extension = new RespondingExtension();
        SipExtensionRegistry registry = new SipExtensionRegistry();
        registry.RegisterExtension(extension);
        SipMessage request = Request("X-WIDGET");

        extension.StatusCode = SipStatusCodes.SessionProgress;
        extension.Reason = "Session Progress\r\nInjected: value";
        SipExtensionResult provisional =
            registry.ProcessRegisteredRequest(request);

        extension.StatusCode = SipStatusCodes.BusyHere;
        extension.Reason = SipReasonPhrases.BusyHere;
        SipExtensionResult final =
            registry.ProcessRegisteredRequest(request);
        SipWriteResult serialized = Serialize(provisional.Response!);

        Assert.Multiple(() =>
        {
            Assert.That(provisional.Handled, Is.True);
            Assert.That(serialized.Success, Is.True,
                serialized.ErrorMessage);
            Assert.That(provisional.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.SessionProgress));
            Assert.That(provisional.Response.ReasonPhrase,
                Does.Not.Contain("\r"));
            Assert.That(Header(provisional.Response, "X-Application"),
                Is.EqualTo("handled"));
            Assert.That(Header(provisional.Response,
                SipHeaderNames.ContentType),
                Is.EqualTo("application/x-widget"));
            Assert.That(Header(provisional.Response,
                SipHeaderNames.ContentLength), Is.EqualTo("3"));
            Assert.That(provisional.Response.Body,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(final.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.BusyHere));
            Assert.That(Header(final.Response, SipHeaderNames.To),
                Is.EqualTo(Header(
                    provisional.Response, SipHeaderNames.To)));
            Assert.That(Header(final.Response, SipHeaderNames.CSeq),
                Is.EqualTo("1 X-WIDGET"));
            Assert.That(extension.LastResponse!.Headers,
                Has.Length.EqualTo(1));
            Assert.That(Header(request, SipHeaderNames.To),
                Does.Not.Contain(";tag="));
        });
    }

    [Test]
    public void DispatchesOptionsAndProtocolErrorsWhenNoExtensionHandles()
    {
        PassiveExtension extension = new PassiveExtension();
        SipExtensionRegistry registry = new SipExtensionRegistry();
        registry.RegisterExtension(extension);

        SipExtensionResult options = registry.ProcessRegisteredRequest(
            Request(SipMethods.Options));
        SipExtensionResult known = registry.ProcessRegisteredRequest(
            Request(SipMethods.Bye));
        SipExtensionResult unknown = registry.ProcessRegisteredRequest(
            Request("X-MISSING"));
        SipExtensionResult ack = registry.ProcessRegisteredRequest(
            Request(SipMethods.Ack));
        SipMessage malformed = Request(SipMethods.Options);
        malformed.CSeq!.Method = SipMethod.Bye;
        malformed.CSeq.MethodToken = SipMethods.Bye;
        SipExtensionResult bad =
            registry.ProcessRegisteredRequest(malformed);

        Assert.Multiple(() =>
        {
            Assert.That(options.Handled, Is.True);
            Assert.That(options.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(Header(options.Response, SipHeaderNames.Allow),
                Is.EqualTo("X-WIDGET, OPTIONS"));
            Assert.That(known.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.MethodNotAllowed));
            Assert.That(Header(known.Response, SipHeaderNames.Allow),
                Is.EqualTo("X-WIDGET, OPTIONS"));
            Assert.That(unknown.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.NotImplemented));
            Assert.That(ack.Handled, Is.False);
            Assert.That(ack.Response, Is.Null);
            Assert.That(bad.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(extension.Calls, Is.Zero);
        });
    }

    static SipMessage Request(string method)
    {
        return Parse(method + " sip:service@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060"
            + ";branch=z9hG4bK-options;rport\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:service@example.com>\r\n"
            + "Call-ID: options@example.com\r\n"
            + "CSeq: 1 " + method + "\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text),
            new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult result = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text),
            new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Uri!;
    }

    static string? Header(SipMessage message, string name)
    {
        SipHeader? header = message.Headers.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name,
                System.StringComparison.OrdinalIgnoreCase));
        return header == null ? null : header.Value;
    }

    static SipWriteResult Serialize(SipMessage message)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        return new SipMessageCodec().SerializeMessage(message, options);
    }

    class RespondingExtension : ISipExtension
    {
        public int StatusCode;
        public string? Reason;
        public SipMessage? LastResponse;

        public SipCapabilitySet GetExtensionCapabilities()
        {
            SipCapabilitySet capabilities = new SipCapabilitySet();
            capabilities.Methods = ["X-WIDGET"];
            return capabilities;
        }

        public SipExtensionResult ProcessExtensionRequest(SipMessage message)
        {
            SipMediaType contentType = new SipMediaType();
            contentType.Type = "application";
            contentType.Subtype = "x-widget";
            SipMessage response = new SipMessage();
            response.Kind = SipMessageKind.Response;
            response.StatusCode = StatusCode;
            response.ReasonPhrase = Reason;
            response.Headers = [CreateHeader("X-Application", "handled")];
            response.ContentType = contentType;
            response.Body = [1, 2, 3];
            LastResponse = response;
            SipExtensionResult result = new SipExtensionResult();
            result.Handled = true;
            result.Response = response;
            return result;
        }

        static SipHeader CreateHeader(string name, string value)
        {
            SipHeader header = new SipHeader();
            header.Name = name;
            header.Value = value;
            return header;
        }
    }

    class PassiveExtension : ISipExtension
    {
        public int Calls;

        public SipCapabilitySet GetExtensionCapabilities()
        {
            SipCapabilitySet capabilities = new SipCapabilitySet();
            capabilities.Methods = ["X-WIDGET"];
            return capabilities;
        }

        public SipExtensionResult ProcessExtensionRequest(SipMessage message)
        {
            Calls = Calls + 1;
            return new SipExtensionResult();
        }
    }
}
