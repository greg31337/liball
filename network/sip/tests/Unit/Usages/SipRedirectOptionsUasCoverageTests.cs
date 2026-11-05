namespace sip.Tests;

public class SipRedirectOptionsUasCoverageTests
{
    [Test]
    public void RedirectRetransmissionUsesStableTagAndMandatoryHeaders()
    {
        SipRedirectServer server = new SipRedirectServer();
        SipMessage request = Request(SipMethods.Invite, "redirect-retransmission",
            "Via: SIP/2.0/TCP backup.example.com:5070;"
                + "branch=z9hG4bK-backup\r\n",
            "Contact: <sip:alice@client.example.com>\r\n");
        SipRouteDecision decision = RedirectDecision(
            SipStatusCodes.MovedTemporarily,
            Target("sip:bob@target.example.com", 800, 90));

        SipRedirectResult first = server.CreateRedirect(request, decision);
        SipRedirectResult retransmission = server.CreateRedirect(
            request, decision);
        SipMessage differentRequest = Request(SipMethods.Invite,
            "redirect-different", "",
            "Contact: <sip:alice@client.example.com>\r\n");
        SipRedirectResult different = server.CreateRedirect(
            differentRequest, decision);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(first.Response!.ViaValues, Has.Length.EqualTo(2));
            Assert.That(Header(first.Response, SipHeaderNames.From),
                Is.EqualTo(Header(request, SipHeaderNames.From)));
            Assert.That(Header(first.Response, SipHeaderNames.CallId),
                Is.EqualTo("redirect-retransmission@example.com"));
            Assert.That(Header(first.Response, SipHeaderNames.CSeq),
                Is.EqualTo("1 INVITE"));
            Assert.That(Header(first.Response, SipHeaderNames.ContentLength),
                Is.EqualTo("0"));
            Assert.That(Header(first.Response, SipHeaderNames.To),
                Does.Contain(";tag=uas-"));
            Assert.That(Header(retransmission.Response!, SipHeaderNames.To),
                Is.EqualTo(Header(first.Response, SipHeaderNames.To)));
            Assert.That(Header(different.Response!, SipHeaderNames.To),
                Is.Not.EqualTo(Header(first.Response, SipHeaderNames.To)));
            Assert.That(first.Response.ContactValues[0].QualityThousandths,
                Is.EqualTo(800));
            Assert.That(first.Response.ContactValues[0].ExpiresSeconds,
                Is.EqualTo(90));
        });
    }

    [TestCase(SipStatusCodes.UseProxy, SipReasonPhrases.UseProxy)]
    [TestCase(SipStatusCodes.AlternativeService,
        SipReasonPhrases.AlternativeService)]
    [TestCase(399, "Redirect")]
    public void RedirectSelectsDefaultReasonForRemainingSuccessPolicies(
        int statusCode, string expectedReason)
    {
        SipRouteDecision decision = RedirectDecision(statusCode,
            Target("sip:bob@target.example.com", 1000, -1));

        SipRedirectResult result = new SipRedirectServer().CreateRedirect(
            Request(SipMethods.Options, "redirect-status-" + statusCode,
                "", ""), decision);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Response!.StatusCode, Is.EqualTo(statusCode));
            Assert.That(result.Response.ReasonPhrase,
                Is.EqualTo(expectedReason));
        });
    }

    [Test]
    public void RedirectRejectsMissingPolicyModeAndEveryInvalidTargetShape()
    {
        SipMessage request = Request(SipMethods.Options,
            "redirect-invalid", "", "");
        SipRedirectResult missing = new SipRedirectServer().CreateRedirect(
            request, null!);
        SipRouteDecision wrongMode = new SipRouteDecision();
        SipRedirectResult mode = new SipRedirectServer().CreateRedirect(
            request, wrongMode);
        SipRouteDecision invalidRejection = new SipRouteDecision();
        invalidRejection.RejectStatusCode = 399;
        SipRedirectResult rejection = new SipRedirectServer().CreateRedirect(
            request, invalidRejection);

        AssertInvalidRedirectTarget(request, null!);
        AssertInvalidRedirectTarget(request, new SipProxyTarget());
        AssertInvalidRedirectTarget(request,
            Target("tel:+12025550123", 1000, -1));
        AssertInvalidRedirectTarget(request,
            Target("sip:bob@target.example.com", -1, -1));
        AssertInvalidRedirectTarget(request,
            Target("sip:bob@target.example.com", 1001, -1));
        AssertInvalidRedirectTarget(request,
            Target("sip:bob@target.example.com", 1000, -2));

        Assert.Multiple(() =>
        {
            Assert.That(missing.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ServerInternalError));
            Assert.That(mode.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ServerInternalError));
            Assert.That(mode.ErrorMessage,
                Does.Contain("did not select redirect"));
            Assert.That(rejection.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ServerInternalError));
        });
    }

    [Test]
    public void OptionsWithEmptyCapabilitiesAdvertisesOnlyOptions()
    {
        SipOptionsService service = new SipOptionsService();
        SipMessage request = Request(SipMethods.Options,
            "options-empty", "", "");

        SipOptionsResult result = service.AnswerOptions(request, null!);
        SipOptionsResult retransmission = service.AnswerOptions(request, null!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Capabilities!.Methods,
                Is.EqualTo(new string[] { SipMethods.Options }));
            Assert.That(Header(result.Response!, SipHeaderNames.Allow),
                Is.EqualTo(SipMethods.Options));
            Assert.That(FindHeader(result.Response!, SipHeaderNames.Supported),
                Is.Null);
            Assert.That(FindHeader(result.Response!, SipHeaderNames.Accept),
                Is.Null);
            Assert.That(FindHeader(result.Response!, SipHeaderNames.AllowEvents),
                Is.Null);
            Assert.That(Header(retransmission.Response!, SipHeaderNames.To),
                Is.EqualTo(Header(result.Response!, SipHeaderNames.To)));
        });
    }

    [Test]
    public void OptionsRejectsInvalidQueriesAndMalformedIncomingRequests()
    {
        SipOptionsService service = new SipOptionsService();
        SipCancellation active = new SipCancellation();
        SipOptionsResult noCancellation = service.QueryOptions(
            ParseUri("sip:service@example.com"), null!);
        SipCancellation cancelled = new SipCancellation();
        cancelled.Requested = true;
        SipOptionsResult cancelledResult = service.QueryOptions(
            ParseUri("sip:service@example.com"), cancelled);
        SipOptionsResult uriHeaders = service.QueryOptions(
            ParseUri("sip:service@example.com?subject=test"), active);
        SipUri invalid = ParseUri("sip:service@example.com");
        invalid.Host = "";
        SipOptionsResult invalidTarget = service.QueryOptions(invalid, active);

        SipMessage malformed = Request(SipMethods.Options,
            "options-malformed", "", "");
        malformed.ViaValues = new SipViaValue[0];
        SipOptionsResult malformedResult = service.AnswerOptions(
            malformed, new SipCapabilitySet());
        SipOptionsResult missingRequest = service.AnswerOptions(
            null!, new SipCapabilitySet());

        Assert.Multiple(() =>
        {
            Assert.That(noCancellation.Success, Is.False);
            Assert.That(noCancellation.ErrorMessage,
                Does.Contain("cancellation"));
            Assert.That(cancelledResult.Success, Is.False);
            Assert.That(cancelledResult.ErrorMessage,
                Is.EqualTo("The OPTIONS query was cancelled."));
            Assert.That(uriHeaders.Success, Is.False);
            Assert.That(uriHeaders.ErrorMessage,
                Does.Contain("valid OPTIONS target"));
            Assert.That(invalidTarget.Success, Is.False);
            Assert.That(malformedResult.Success, Is.False);
            Assert.That(malformedResult.Response, Is.Null);
            Assert.That(malformedResult.Capabilities!.Methods,
                Is.EqualTo(new string[] { SipMethods.Options }));
            Assert.That(missingRequest.Success, Is.False);
            Assert.That(missingRequest.Response, Is.Null);
            Assert.That(missingRequest.Capabilities!.Methods,
                Is.EqualTo(new string[] { SipMethods.Options }));
        });
    }

    [Test]
    public void ApplicationResponseCannotOverrideRoutingOrMandatoryHeaders()
    {
        ApplicationUasExtension extension = new ApplicationUasExtension();
        SipExtensionRegistry registry = new SipExtensionRegistry();
        registry.RegisterExtension(extension);
        SipMessage request = Request("X-APPLICATION", "uas-application",
            "Via: SIP/2.0/TCP backup.example.com:5070;"
                + "branch=z9hG4bK-backup\r\n",
            "");

        SipExtensionResult first = registry.ProcessRegisteredRequest(request);
        SipExtensionResult retransmission =
            registry.ProcessRegisteredRequest(request);
        SipWriteResult serialized = Serialize(first.Response!);

        Assert.Multiple(() =>
        {
            Assert.That(first.Handled, Is.True);
            Assert.That(first.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Accepted));
            Assert.That(first.Response.ViaValues, Has.Length.EqualTo(2));
            Assert.That(Header(first.Response, SipHeaderNames.From),
                Is.EqualTo(Header(request, SipHeaderNames.From)));
            Assert.That(Header(first.Response, SipHeaderNames.CallId),
                Is.EqualTo("uas-application@example.com"));
            Assert.That(Header(first.Response, SipHeaderNames.CSeq),
                Is.EqualTo("1 X-APPLICATION"));
            Assert.That(Header(first.Response, SipHeaderNames.ContentLength),
                Is.EqualTo("4"));
            Assert.That(CountHeaders(first.Response,
                SipHeaderNames.ContentLength), Is.EqualTo(1));
            Assert.That(Header(first.Response, SipHeaderNames.ContentType),
                Is.EqualTo("application/x-result"));
            Assert.That(Header(first.Response,
                SipHeaderNames.ContentDisposition), Is.EqualTo("render"));
            Assert.That(Header(first.Response, "X-Application"),
                Is.EqualTo("kept"));
            Assert.That(first.Response.Body,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(Header(retransmission.Response!, SipHeaderNames.To),
                Is.EqualTo(Header(first.Response, SipHeaderNames.To)));
            Assert.That(serialized.Success, Is.True, serialized.ErrorMessage);
            Assert.That(extension.Calls, Is.EqualTo(2));
        });
    }

    [Test]
    public void GenericUasRejectsNullInvalidAndAckApplicationResponses()
    {
        NullResultExtension nullExtension = new NullResultExtension();
        SipExtensionRegistry nullRegistry = new SipExtensionRegistry();
        nullRegistry.RegisterExtension(nullExtension);
        SipExtensionResult nullResult = nullRegistry.ProcessRegisteredRequest(
            Request("X-NULL", "uas-null", "", ""));

        InvalidResponseExtension invalidExtension =
            new InvalidResponseExtension();
        invalidExtension.Method = "X-INVALID";
        invalidExtension.ResponseKind = SipMessageKind.Response;
        invalidExtension.StatusCode = 700;
        SipExtensionRegistry invalidRegistry = new SipExtensionRegistry();
        invalidRegistry.RegisterExtension(invalidExtension);
        SipExtensionResult invalidStatus =
            invalidRegistry.ProcessRegisteredRequest(
                Request("X-INVALID", "uas-invalid", "", ""));

        InvalidResponseExtension wrongKindExtension =
            new InvalidResponseExtension();
        wrongKindExtension.Method = "X-WRONG-KIND";
        wrongKindExtension.ResponseKind = SipMessageKind.Request;
        wrongKindExtension.StatusCode = SipStatusCodes.Ok;
        SipExtensionRegistry wrongKindRegistry = new SipExtensionRegistry();
        wrongKindRegistry.RegisterExtension(wrongKindExtension);
        SipExtensionResult wrongKind =
            wrongKindRegistry.ProcessRegisteredRequest(
                Request("X-WRONG-KIND", "uas-wrong-kind", "", ""));

        InvalidResponseExtension ackExtension =
            new InvalidResponseExtension();
        ackExtension.Method = SipMethods.Ack;
        ackExtension.ResponseKind = SipMessageKind.Response;
        ackExtension.StatusCode = SipStatusCodes.Ok;
        SipExtensionRegistry ackRegistry = new SipExtensionRegistry();
        ackRegistry.RegisterExtension(ackExtension);
        SipExtensionResult ack = ackRegistry.ProcessRegisteredRequest(
            Request(SipMethods.Ack, "uas-ack", "", ""));

        Assert.Multiple(() =>
        {
            Assert.That(nullResult.Handled, Is.False);
            Assert.That(nullResult.Response, Is.Null);
            Assert.That(nullResult.ErrorMessage,
                Does.Contain("no processing result"));
            Assert.That(invalidStatus.Handled, Is.False);
            Assert.That(invalidStatus.Response, Is.Null);
            Assert.That(invalidStatus.ErrorMessage,
                Does.Contain("invalid SIP response"));
            Assert.That(wrongKind.Handled, Is.False);
            Assert.That(wrongKind.Response, Is.Null);
            Assert.That(wrongKind.ErrorMessage,
                Does.Contain("invalid SIP response"));
            Assert.That(ack.Handled, Is.False);
            Assert.That(ack.Response, Is.Null);
            Assert.That(ack.ErrorMessage,
                Does.Contain("ACK request cannot receive"));
        });
    }

    static void AssertInvalidRedirectTarget(SipMessage request,
        SipProxyTarget target)
    {
        SipRouteDecision decision = RedirectDecision(
            SipStatusCodes.MovedTemporarily, target);
        SipRedirectResult result = new SipRedirectServer().CreateRedirect(
            request, decision);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Response!.StatusCode,
            Is.EqualTo(SipStatusCodes.ServerInternalError));
        Assert.That(result.ErrorMessage,
            Does.Contain("invalid redirect target"));
    }

    static SipRouteDecision RedirectDecision(int status,
        SipProxyTarget target)
    {
        SipRouteDecision decision = new SipRouteDecision();
        decision.Redirect = true;
        decision.RedirectStatusCode = status;
        decision.Targets = new SipProxyTarget[] { target };
        return decision;
    }

    static SipProxyTarget Target(string uri, int quality, int expires)
    {
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = ParseUri(uri);
        target.QualityThousandths = quality;
        target.ExpiresSeconds = expires;
        return target;
    }

    static SipMessage Request(string method, string callId,
        string extraVia, string additionalHeaders)
    {
        return Parse(method + " sip:service@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-"
            + callId + ";rport\r\n"
            + extraVia
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:service@example.com>\r\n"
            + "Call-ID: " + callId + "@example.com\r\n"
            + "CSeq: 1 " + method + "\r\n"
            + additionalHeaders
            + "Content-Length: 0\r\n\r\n");
    }

    static SipHeader? FindHeader(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header => string.Equals(
            header.Name, name, System.StringComparison.OrdinalIgnoreCase));
    }

    static string Header(SipMessage message, string name)
    {
        return FindHeader(message, name)!.Value!;
    }

    static int CountHeaders(SipMessage message, string name)
    {
        return message.Headers.Count(header => string.Equals(
            header.Name, name, System.StringComparison.OrdinalIgnoreCase));
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult result = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Uri!;
    }

    static SipWriteResult Serialize(SipMessage message)
    {
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        return new SipMessageCodec().SerializeMessage(message, options);
    }

    class ApplicationUasExtension : ISipExtension
    {
        public int Calls;

        public SipCapabilitySet GetExtensionCapabilities()
        {
            SipCapabilitySet capabilities = new SipCapabilitySet();
            capabilities.Methods = new string[] { "X-APPLICATION" };
            return capabilities;
        }

        public SipExtensionResult ProcessExtensionRequest(SipMessage message)
        {
            Calls++;
            SipMessage response = new SipMessage();
            response.Kind = SipMessageKind.Response;
            response.StatusCode = SipStatusCodes.Accepted;
            response.ReasonPhrase = "Accepted";
            response.Headers = new SipHeader[] {
                HeaderValue(SipHeaderNames.Via, "SIP/2.0/UDP fake.invalid"),
                HeaderValue(SipHeaderNames.From, "<sip:fake@invalid>"),
                HeaderValue(SipHeaderNames.To, "<sip:fake@invalid>"),
                HeaderValue(SipHeaderNames.CompactCallId, "fake-call"),
                HeaderValue(SipHeaderNames.CSeq, "99 BYE"),
                HeaderValue(SipHeaderNames.CompactContentLength, "999"),
                HeaderValue("X-Application", "kept") };
            response.Body = new byte[] { 1, 2, 3, 4 };
            SipMediaType contentType = new SipMediaType();
            contentType.Type = "application";
            contentType.Subtype = "x-result";
            response.ContentType = contentType;
            SipContentDisposition disposition = new SipContentDisposition();
            disposition.Disposition = SipBodyDisposition.Render;
            disposition.DispositionToken = "render";
            response.ContentDisposition = disposition;
            SipExtensionResult result = new SipExtensionResult();
            result.Handled = true;
            result.Response = response;
            return result;
        }
    }

    class NullResultExtension : ISipExtension
    {
        public SipCapabilitySet GetExtensionCapabilities()
        {
            SipCapabilitySet capabilities = new SipCapabilitySet();
            capabilities.Methods = new string[] { "X-NULL" };
            return capabilities;
        }

        public SipExtensionResult ProcessExtensionRequest(SipMessage message)
        {
            return null!;
        }
    }

    class InvalidResponseExtension : ISipExtension
    {
        public string Method = "X-INVALID";
        public SipMessageKind ResponseKind;
        public int StatusCode;

        public SipCapabilitySet GetExtensionCapabilities()
        {
            SipCapabilitySet capabilities = new SipCapabilitySet();
            capabilities.Methods = new string[] { Method };
            return capabilities;
        }

        public SipExtensionResult ProcessExtensionRequest(SipMessage message)
        {
            SipMessage response = new SipMessage();
            response.Kind = ResponseKind;
            response.StatusCode = StatusCode;
            response.ReasonPhrase = "Application response";
            SipExtensionResult result = new SipExtensionResult();
            result.Handled = true;
            result.Response = response;
            return result;
        }
    }

    static SipHeader HeaderValue(string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        return header;
    }
}
