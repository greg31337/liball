namespace sip.Tests;

public class SipRegistrarBranchTests
{
    const string InstanceOne =
        "<urn:uuid:00000000-0000-1000-8000-000000000031>";
    const string InstanceTwo =
        "<urn:uuid:00000000-0000-1000-8000-000000000032>";

    [Test]
    public void RejectsEveryInvalidRegistrarPolicyShape()
    {
        AssertInvalidOptions(options => options.MinExpiresSeconds = -1);
        AssertInvalidOptions(options => options.MaxExpiresSeconds = 59);
        AssertInvalidOptions(options => options.DefaultExpiresSeconds = 59);
        AssertInvalidOptions(options => options.DefaultExpiresSeconds = 601);
        AssertInvalidOptions(options => options.MaxBindingCount = 0);
        AssertInvalidOptions(options => options.Domains = null!);
        AssertInvalidOptions(options => options.Domains = new string[] { "" });
        AssertInvalidOptions(options => options.Domains =
            new string[] { "bad domain.example" });
        AssertInvalidOptions(options => options.Domains =
            new string[] { "bad\tdomain.example" });
    }

    [Test]
    public void RejectsMalformedRequestEnvelopeAndCoreIdentityHeaders()
    {
        SipRegistrar registrar = new SipRegistrar();
        SipRegistrarOptions options = Options();

        SipRegistrationSnapshot missingRequest = registrar.ProcessRegistration(
            null!, options);
        SipMessage responseMessage = Register("envelope-response", 1, "", "");
        responseMessage.Kind = SipMessageKind.Response;
        SipRegistrationSnapshot response = registrar.ProcessRegistration(
            responseMessage, options);
        SipMessage tokenMismatch = Register("envelope-token", 1, "", "");
        tokenMismatch.MethodToken = SipMethods.Options;
        SipRegistrationSnapshot mismatchedToken = registrar.ProcessRegistration(
            tokenMismatch, options);
        SipMessage missingVia = Register("envelope-via", 1, "", "");
        missingVia.ViaValues = new SipViaValue[0];
        SipRegistrationSnapshot noVia = registrar.ProcessRegistration(
            missingVia, options);
        SipMessage missingFrom = Register("envelope-from", 1, "", "");
        missingFrom.From = null;
        SipRegistrationSnapshot noFrom = registrar.ProcessRegistration(
            missingFrom, options);
        SipMessage missingTo = Register("envelope-to", 1, "", "");
        missingTo.To = null;
        SipRegistrationSnapshot noTo = registrar.ProcessRegistration(
            missingTo, options);
        SipMessage duplicateCallId = Register("envelope-call", 1, "", "");
        AppendHeader(duplicateCallId, SipHeaderNames.CallId, "second-call");
        SipRegistrationSnapshot repeatedCallId = registrar.ProcessRegistration(
            duplicateCallId, options);
        SipMessage negativeCSeq = Register("envelope-cseq", 1, "", "");
        negativeCSeq.CSeq!.SequenceNumber = -1;
        SipRegistrationSnapshot invalidCSeq = registrar.ProcessRegistration(
            negativeCSeq, options);
        SipMessage cseqTokenMismatch = Register("envelope-cseq-token", 1, "", "");
        cseqTokenMismatch.CSeq!.MethodToken = SipMethods.Options;
        SipRegistrationSnapshot invalidCSeqToken = registrar.ProcessRegistration(
            cseqTokenMismatch, options);

        Assert.Multiple(() =>
        {
            Assert.That(missingRequest.State,
                Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(missingRequest.Response, Is.Null);
            Assert.That(response.Response, Is.Null);
            AssertFailure(mismatchedToken, SipStatusCodes.MethodNotAllowed,
                "REGISTER request");
            Assert.That(noVia.Response, Is.Null);
            Assert.That(noFrom.Response, Is.Null);
            Assert.That(noTo.Response, Is.Null);
            Assert.That(repeatedCallId.Response, Is.Null);
            AssertFailure(invalidCSeq, SipStatusCodes.BadRequest, "CSeq");
            AssertFailure(invalidCSeqToken, SipStatusCodes.BadRequest, "CSeq");
        });
    }

    [Test]
    public void RejectsInvalidTargetsAddressRecordsAndCSeqMethods()
    {
        SipRegistrarOptions options = Options();
        SipMessage target = Register("target-scheme", 1, "", "");
        target.RequestUri = ParseUri("tel:+12025550123");
        SipRegistrationSnapshot unsupportedTarget = new SipRegistrar()
            .ProcessRegistration(target, options);
        SipMessage missingTarget = Register("target-missing", 1, "", "");
        missingTarget.RequestUri = null;
        SipRegistrationSnapshot absentTarget = new SipRegistrar()
            .ProcessRegistration(missingTarget, options);
        SipMessage password = Register("aor-password", 1, "", "");
        password.To!.Uri!.Password = "secret";
        SipRegistrationSnapshot passwordAor = new SipRegistrar()
            .ProcessRegistration(password, options);
        SipMessage header = Register("aor-header", 1, "", "");
        SipUriHeader uriHeader = new SipUriHeader();
        uriHeader.Name = "subject";
        uriHeader.Value = "hello";
        header.To!.Uri!.Headers = new SipUriHeader[] { uriHeader };
        SipRegistrationSnapshot headerAor = new SipRegistrar()
            .ProcessRegistration(header, options);
        SipMessage method = Register("cseq-method", 1, "", "");
        method.CSeq!.Method = SipMethod.Options;
        method.CSeq.MethodToken = SipMethods.Options;
        SipRegistrationSnapshot wrongCSeqMethod = new SipRegistrar()
            .ProcessRegistration(method, options);

        Assert.Multiple(() =>
        {
            AssertFailure(unsupportedTarget,
                SipStatusCodes.UnsupportedUriScheme, "unsupported URI scheme");
            AssertFailure(absentTarget, SipStatusCodes.BadRequest,
                "valid SIP URIs");
            AssertFailure(passwordAor, SipStatusCodes.BadRequest,
                "valid SIP URIs");
            AssertFailure(headerAor, SipStatusCodes.BadRequest,
                "valid SIP URIs");
            AssertFailure(wrongCSeqMethod, SipStatusCodes.BadRequest, "CSeq");
        });
    }

    [Test]
    public void RejectsMalformedExpiryExtensionPathAndContactValues()
    {
        SipRegistrarOptions options = Options();
        SipMessage duplicateExpires = Register("expires-duplicate", 1,
            "Expires: 120\r\n",
            Contact("one.example.com", 120));
        AppendHeader(duplicateExpires, SipHeaderNames.Expires, "180");
        SipRegistrationSnapshot repeatedExpires = new SipRegistrar()
            .ProcessRegistration(duplicateExpires, options);
        SipMessage invalidExpires = Register("expires-invalid", 1, "",
            Contact("one.example.com", 120));
        AppendHeader(invalidExpires, SipHeaderNames.Expires, "invalid");
        SipRegistrationSnapshot malformedExpires = new SipRegistrar()
            .ProcessRegistration(invalidExpires, options);
        SipMessage malformedRequire = Register("require-malformed", 1, "",
            Contact("one.example.com", 120));
        AppendHeader(malformedRequire, SipHeaderNames.Require, ",");
        SipRegistrationSnapshot invalidRequire = new SipRegistrar()
            .ProcessRegistration(malformedRequire, options);
        SipMessage unsupported = Register("require-unsupported", 1, "",
            Contact("one.example.com", 120));
        AppendHeader(unsupported, SipHeaderNames.Require, "feature-one, FEATURE-one");
        AppendHeader(unsupported, SipHeaderNames.ProxyRequire, "feature-two");
        SipRegistrationSnapshot unsupportedOptions = new SipRegistrar()
            .ProcessRegistration(unsupported, options);
        SipMessage malformedPath = Register("path-malformed", 1, "",
            Contact("one.example.com", 120));
        AppendHeader(malformedPath, SipHeaderNames.Path, "<sip:edge.example.com");
        SipRegistrationSnapshot invalidPath = new SipRegistrar()
            .ProcessRegistration(malformedPath, options);
        SipMessage nullContact = Register("contact-null", 1, "",
            Contact("one.example.com", 120));
        nullContact.ContactValues = new SipContactValue[] { null! };
        SipRegistrationSnapshot invalidNullContact = new SipRegistrar()
            .ProcessRegistration(nullContact, options);
        SipMessage invalidModel = Register("contact-model", 1, "",
            Contact("one.example.com", 120));
        invalidModel.ContactValues[0].QualityThousandths = 1001;
        SipRegistrationSnapshot invalidContact = new SipRegistrar()
            .ProcessRegistration(invalidModel, options);

        Assert.Multiple(() =>
        {
            AssertFailure(repeatedExpires, SipStatusCodes.BadRequest,
                "Expires header");
            AssertFailure(malformedExpires, SipStatusCodes.BadRequest,
                "Expires header");
            AssertFailure(invalidRequire, SipStatusCodes.BadRequest,
                "requirements are malformed");
            Assert.That(FindHeader(invalidRequire.Response!,
                SipHeaderNames.Unsupported), Is.Null);
            AssertFailure(unsupportedOptions, SipStatusCodes.BadExtension,
                "unsupported extensions");
            Assert.That(Header(unsupportedOptions.Response!,
                SipHeaderNames.Unsupported),
                Is.EqualTo("feature-one, feature-two"));
            AssertFailure(invalidPath, SipStatusCodes.BadRequest,
                "malformed Path");
            AssertFailure(invalidNullContact, SipStatusCodes.BadRequest,
                "cannot be null");
            AssertFailure(invalidContact, SipStatusCodes.BadRequest,
                "valid concrete SIP binding");
        });
    }

    [Test]
    public void EnforcesOutboundRegistrationIdentityRules()
    {
        SipRegistrarOptions options = Options();
        SipMessage zeroRegistrationId = Register("outbound-zero", 1, "",
            Contact("one.example.com", 120));
        zeroRegistrationId.ContactValues[0].RegistrationId = 0;
        SipRegistrationSnapshot zero = new SipRegistrar()
            .ProcessRegistration(zeroRegistrationId, options);
        SipMessage missingInstance = Register("outbound-instance", 1, "",
            Contact("one.example.com", 120));
        missingInstance.ContactValues[0].RegistrationId = 1;
        missingInstance.ContactValues[0].InstanceId = null;
        SipRegistrationSnapshot noInstance = new SipRegistrar()
            .ProcessRegistration(missingInstance, options);
        string outboundContacts = "Contact: <sip:alice@one.example.com;ob>"
            + ";expires=120;reg-id=1;+sip.instance=\"" + InstanceOne + "\"\r\n"
            + "Contact: <sip:alice@two.example.com;ob>"
            + ";expires=120;reg-id=2;+sip.instance=\"" + InstanceTwo + "\"\r\n";
        SipRegistrationSnapshot multiple = new SipRegistrar()
            .ProcessRegistration(Register("outbound-multiple", 1,
                "Supported: outbound\r\n", outboundContacts), options);
        string noOutboundContact = "Contact: <sip:alice@one.example.com>"
            + ";expires=120;reg-id=1;+sip.instance=\"" + InstanceOne + "\"\r\n";
        SipRegistrationSnapshot noOutbound = new SipRegistrar()
            .ProcessRegistration(Register("outbound-negative", 1,
                "Supported: outbound\r\n", noOutboundContact), options);
        string unsupportedOutboundContact =
            "Contact: <sip:alice@one.example.com;ob>"
            + ";expires=120;reg-id=1;+sip.instance=\"" + InstanceOne + "\"\r\n";
        SipRegistrationSnapshot notNegotiated = new SipRegistrar()
            .ProcessRegistration(Register("outbound-not-negotiated", 1, "",
                unsupportedOutboundContact), options);

        Assert.Multiple(() =>
        {
            AssertFailure(zero, SipStatusCodes.BadRequest,
                "valid concrete SIP binding");
            AssertFailure(noInstance, SipStatusCodes.BadRequest,
                "instance identifier");
            AssertFailure(multiple, SipStatusCodes.BadRequest,
                "multiple active reg-id");
            Assert.That(FindHeader(noOutbound.Response!, SipHeaderNames.Require),
                Is.Null);
            Assert.That(FindHeader(notNegotiated.Response!, SipHeaderNames.Require),
                Is.Null);
        });
    }

    [Test]
    public void MapsInfrastructureFailuresAndPreservesExistingBindingsAtomically()
    {
        RegistrarFaultLocationService service =
            new RegistrarFaultLocationService();
        SipRegistrar registrar = new SipRegistrar();
        registrar.ConfigureRegistrarLocationService(service);
        SipRegistrarOptions options = Options();
        SipRegistrationSnapshot initial = registrar.ProcessRegistration(
            Register("storage-call", 1, "", Contact("original.example.com", 120)),
            options);
        registrar.PollRegistrarEvent();

        service.FailReplace = true;
        SipRegistrationSnapshot updateFailure = registrar.ProcessRegistration(
            Register("storage-call", 2, "", Contact("replacement.example.com", 120)),
            options);
        SipRegistrationEvent noFailedUpdateEvent = registrar.PollRegistrarEvent();
        service.FailReplace = false;
        service.FailQuery = true;
        SipRegistrationSnapshot queryFailure = registrar.ProcessRegistration(
            Register("storage-query", 1, "", ""), options);
        SipRegistrar unconfiguredAuthentication = new SipRegistrar();
        options.RequireAuthentication = true;
        SipRegistrationSnapshot authenticationFailure =
            unconfiguredAuthentication.ProcessRegistration(
                Register("authentication-missing", 1, "",
                    Contact("one.example.com", 120)), options);

        Assert.Multiple(() =>
        {
            Assert.That(initial.State,
                Is.EqualTo(SipRegistrationState.Registered));
            AssertFailure(updateFailure, SipStatusCodes.ServerInternalError,
                "storage unavailable");
            Assert.That(updateFailure.Bindings.Select(ContactHost),
                Is.EqualTo(new string[] { "original.example.com" }));
            Assert.That(noFailedUpdateEvent.Version, Is.EqualTo(-1));
            AssertFailure(queryFailure, SipStatusCodes.ServerInternalError,
                "storage unavailable");
            AssertFailure(authenticationFailure,
                SipStatusCodes.ServerInternalError, "not configured");
            AssertCommonResponseHeaders(updateFailure.Response!);
            AssertCommonResponseHeaders(queryFailure.Response!);
            AssertCommonResponseHeaders(authenticationFailure.Response!);
        });
    }

    [Test]
    public void ReturnsCompleteQueryAndRemovalResponses()
    {
        SipRegistrar registrar = new SipRegistrar();
        SipRegistrarOptions options = Options();
        registrar.ExpireRegistrarBindings(1000);
        registrar.ProcessRegistration(Register("query-call", 1, "",
            Contact("one.example.com", 120)
                + Contact("two.example.com", 180)), options);

        SipRegistrationSnapshot query = registrar.ProcessRegistration(
            Register("query-only", 1, "", ""), options);
        SipRegistrationSnapshot removal = registrar.ProcessRegistration(
            Register("remove-all", 1, "Expires: 0\r\n", "Contact: *\r\n"),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(query.Response!.StatusCode, Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(query.Response.ContactValues.Select(ContactHost),
                Is.EqualTo(new string[] { "two.example.com", "one.example.com" }));
            Assert.That(query.Response.ContactValues[0].ExpiresSeconds,
                Is.EqualTo(180));
            Assert.That(query.Response.ContactValues[1].ExpiresSeconds,
                Is.EqualTo(120));
            AssertCommonResponseHeaders(query.Response);
            Assert.That(removal.State,
                Is.EqualTo(SipRegistrationState.Terminated));
            Assert.That(removal.Response!.StatusCode, Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(removal.Response.ContactValues, Is.Empty);
            Assert.That(FindHeader(removal.Response, SipHeaderNames.Contact),
                Is.Null);
            AssertCommonResponseHeaders(removal.Response);
        });
    }

    [Test]
    public void SaturatesExpiryAtMaximumTimestamp()
    {
        SipRegistrar registrar = new SipRegistrar();
        SipRegistrarOptions options = Options();
        options.MinExpiresSeconds = 0;
        options.DefaultExpiresSeconds = 1;
        options.MaxExpiresSeconds = 1;
        registrar.ExpireRegistrarBindings(long.MaxValue - 500);

        SipRegistrationSnapshot result = registrar.ProcessRegistration(
            Register("expiry-overflow", 1, "", Contact("one.example.com", 1)),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Bindings[0].ExpiresAtMilliseconds,
                Is.EqualTo(long.MaxValue));
            Assert.That(result.Response!.ContactValues[0].ExpiresSeconds,
                Is.EqualTo(1));
        });
    }

    static void AssertInvalidOptions(
        System.Action<SipRegistrarOptions> invalidate)
    {
        SipRegistrarOptions options = Options();
        invalidate(options);
        SipRegistrationSnapshot result = new SipRegistrar()
            .ProcessRegistration(Register("policy-call", 1, "", ""), options);
        Assert.That(result.State, Is.EqualTo(SipRegistrationState.Failed));
        Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Response, Is.Not.Null);
        Assert.That(result.Response!.StatusCode,
            Is.EqualTo(SipStatusCodes.BadRequest));
    }

    static void AssertFailure(SipRegistrationSnapshot snapshot,
        int statusCode, string errorFragment)
    {
        Assert.That(snapshot.State, Is.EqualTo(SipRegistrationState.Failed));
        Assert.That(snapshot.ErrorMessage, Does.Contain(errorFragment));
        Assert.That(snapshot.Response, Is.Not.Null);
        Assert.That(snapshot.Response!.StatusCode, Is.EqualTo(statusCode));
    }

    static void AssertCommonResponseHeaders(SipMessage response)
    {
        Assert.That(FindHeader(response, SipHeaderNames.Date), Is.Not.Null);
        Assert.That(Header(response, SipHeaderNames.Allow),
            Is.EqualTo(SipMethods.Register));
        Assert.That(Header(response, SipHeaderNames.Supported),
            Is.EqualTo("path, outbound, gruu"));
    }

    static SipRegistrarOptions Options()
    {
        SipRegistrarOptions options = new SipRegistrarOptions();
        options.RequireAuthentication = false;
        options.DefaultExpiresSeconds = 120;
        options.MinExpiresSeconds = 60;
        options.MaxExpiresSeconds = 600;
        options.MaxBindingCount = 10;
        options.Domains = new string[] { "example.com" };
        return options;
    }

    static SipMessage Register(string callId, int cseq,
        string additionalHeaders, string contacts)
    {
        string text = "REGISTER sip:example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-branches\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:alice@example.com>\r\n"
            + "Call-ID: " + callId + "\r\n"
            + "CSeq: " + cseq.ToString() + " REGISTER\r\n"
            + additionalHeaders + contacts
            + "Content-Length: 0\r\n\r\n";
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static string Contact(string host, int expiresSeconds)
    {
        return "Contact: <sip:alice@" + host + ">;expires="
            + expiresSeconds.ToString() + "\r\n";
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Uri!;
    }

    static void AppendHeader(SipMessage message, string name, string value)
    {
        SipHeader[] headers = new SipHeader[message.Headers.Length + 1];
        System.Array.Copy(message.Headers, headers, message.Headers.Length);
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        headers[message.Headers.Length] = header;
        message.Headers = headers;
    }

    static SipHeader? FindHeader(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(candidate => candidate.Name!.Equals(
            name, System.StringComparison.OrdinalIgnoreCase));
    }

    static string? Header(SipMessage message, string name)
    {
        return FindHeader(message, name)?.Value;
    }

    static string ContactHost(SipContactValue contact)
    {
        return contact.Address!.Uri!.Host!;
    }

    static string ContactHost(SipRegistrationBinding binding)
    {
        return binding.Contact!.Address!.Uri!.Host!;
    }
}

class RegistrarFaultLocationService : ISipLocationService
{
    internal SipLocationService Inner = new SipLocationService();
    internal bool FailQuery;
    internal bool FailReplace;

    public void ConfigureLocationStorage(ISipLocationStorage storage)
    {
        Inner.ConfigureLocationStorage(storage);
    }

    public SipLocationResult QueryLocation(SipLocationQuery query)
    {
        if (FailQuery)
        {
            return Failure();
        }
        return Inner.QueryLocation(query);
    }

    public SipLocationResult UpdateLocation(SipRegistrationBinding binding)
    {
        return Inner.UpdateLocation(binding);
    }

    public SipLocationResult ReplaceLocations(SipLocationUpdate update)
    {
        if (FailReplace)
        {
            return Failure();
        }
        return Inner.ReplaceLocations(update);
    }

    public SipLocationResult RemoveLocation(SipLocationQuery query)
    {
        return Inner.RemoveLocation(query);
    }

    public SipLocationResult ExpireLocations(long nowMilliseconds)
    {
        return Inner.ExpireLocations(nowMilliseconds);
    }

    static SipLocationResult Failure()
    {
        SipLocationResult result = new SipLocationResult();
        result.Success = false;
        result.ErrorMessage = "storage unavailable";
        return result;
    }
}
