namespace sip.Tests;

public class SipMessageValidationTests
{
    [Test]
    public void StrictParsingAcceptsSemanticallyCompleteRequestAndResponse()
    {
        SipMessageCodec codec = new SipMessageCodec();
        byte[] request = Bytes(RequestText("INVITE", "sips:bob@example.com", "INVITE",
            "Contact: <sips:alice@client.example.com>\r\n", ""));
        byte[] response = Bytes(ResponseText(200, "OK", "INVITE",
            "Contact: <sips:alice@client.example.com>\r\n", ""));

        SipParseResult requestResult = codec.ParseStreamMessage(request, new SipParseOptions());
        SipParseResult responseResult = codec.ParseStreamMessage(response, new SipParseOptions());

        Assert.That(requestResult.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(requestResult.ValidationIssues, Is.Empty);
        Assert.That(codec.ValidateMessage(requestResult.Message!), Is.Empty);
        Assert.That(responseResult.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(responseResult.ValidationIssues, Is.Empty);
        Assert.That(codec.ValidateMessage(responseResult.Message!), Is.Empty);
    }

    [Test]
    public void StrictModeRejectsMissingMandatoryHeadersWhileCompatibilityWarns()
    {
        byte[] source = Bytes("OPTIONS sip:service@example.com SIP/2.0\r\nContent-Length: 0\r\n\r\n");
        SipMessageCodec codec = new SipMessageCodec();
        SipParseOptions compatible = new SipParseOptions();
        compatible.Strict = false;

        SipParseResult strict = codec.ParseStreamMessage(source, new SipParseOptions());
        SipParseResult accepted = codec.ParseStreamMessage(source, compatible);

        Assert.That(strict.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(strict.Message, Is.Null);
        Assert.That(strict.ValidationIssues, Is.Not.Empty);
        Assert.That(strict.ValidationIssues.All(issue => issue.Severity == SipValidationSeverity.Error), Is.True);
        Assert.That(accepted.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(accepted.Message, Is.Not.Null);
        Assert.That(accepted.ValidationIssues, Has.Length.EqualTo(6));
        Assert.That(accepted.ValidationIssues.All(issue => issue.Severity == SipValidationSeverity.Warning), Is.True);
        Assert.That(accepted.ValidationIssues.Select(issue => issue.HeaderName),
            Does.Contain(SipHeaderNames.MaxForwards));
    }

    [Test]
    public void PublicValidationReportsEachMissingMandatoryRequestHeader()
    {
        SipMessage message = ParseCompatible("OPTIONS sip:service@example.com SIP/2.0\r\n\r\n");
        SipMessageCodec codec = new SipMessageCodec();

        SipValidationIssue[] issues = codec.ValidateMessage(message);
        string?[] names = issues.Where(issue => issue.Code == "header-missing")
            .Select(issue => issue.HeaderName).ToArray();

        Assert.That(names, Is.EquivalentTo(new string?[]
        {
            SipHeaderNames.Via,
            SipHeaderNames.From,
            SipHeaderNames.To,
            SipHeaderNames.CallId,
            SipHeaderNames.CSeq,
            SipHeaderNames.MaxForwards
        }));
    }

    [Test]
    public void CompactAndLongSingletonDuplicatesShareOneCardinality()
    {
        string additional = "f: Alice <sip:alice@example.com>;tag=duplicate\r\nl: 0\r\n";
        SipMessage message = ParseCompatible(RequestText(
            "OPTIONS", "sip:service@example.com", "OPTIONS", additional, ""));
        SipMessageCodec codec = new SipMessageCodec();

        SipValidationIssue[] issues = codec.ValidateMessage(message);

        Assert.That(issues.Count(issue => issue.Code == "header-duplicate"), Is.EqualTo(2));
        Assert.That(issues.Where(issue => issue.Code == "header-duplicate").Select(issue => issue.HeaderName),
            Is.EquivalentTo(new string?[] { SipHeaderNames.From, SipHeaderNames.ContentLength }));
    }

    [Test]
    public void RequestMethodMustAgreeWithCSeqAndRecognizedMethod()
    {
        SipMessage mismatch = ParseCompatible(RequestText(
            "INVITE", "sip:bob@example.com", "ACK", "Contact: <sip:alice@example.com>\r\n", ""));
        SipMessage modelMismatch = ParseCompatible(RequestText(
            "OPTIONS", "sip:bob@example.com", "OPTIONS", "", ""));
        modelMismatch.Method = SipMethod.Bye;
        SipMessageCodec codec = new SipMessageCodec();

        SipValidationIssue[] mismatchIssues = codec.ValidateMessage(mismatch);
        SipValidationIssue[] modelIssues = codec.ValidateMessage(modelMismatch);

        Assert.That(mismatchIssues, Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "cseq-method"));
        Assert.That(modelIssues,
            Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "request-method-model"));
    }

    [Test]
    public void MatchingExtensionMethodIsValid()
    {
        string text = RequestText("FROB", "sip:service@example.com", "FROB", "", "");
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult result = codec.ParseStreamMessage(Bytes(text), new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.Message!.Method, Is.EqualTo(SipMethod.Unknown));
        Assert.That(result.Message.CSeq!.Method, Is.EqualTo(SipMethod.Unknown));
        Assert.That(result.ValidationIssues, Is.Empty);
    }

    [Test]
    public void RequestAndResponseOnlyFieldsAreRejectedOnTheWrongKind()
    {
        SipMessage request = ParseCompatible(RequestText(
            "OPTIONS", "sip:service@example.com", "OPTIONS", "", ""));
        request.StatusCode = 200;
        request.StatusClass = SipStatusClass.Successful;
        request.ReasonPhrase = "OK";
        SipMessage response = ParseCompatible(ResponseText(486, "Busy Here", "INVITE", "", ""));
        response.RequestUri = ParseUri("sip:service@example.com");
        response.Method = SipMethod.Invite;
        response.MethodToken = "INVITE";
        response.Headers = response.Headers.Concat(new SipHeader[] { Header("Max-Forwards", "70") }).ToArray();
        SipMessageCodec codec = new SipMessageCodec();

        SipValidationIssue[] requestIssues = codec.ValidateMessage(request);
        SipValidationIssue[] responseIssues = codec.ValidateMessage(response);

        Assert.That(requestIssues,
            Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "request-response-fields"));
        Assert.That(responseIssues,
            Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "response-request-fields"));
        Assert.That(responseIssues,
            Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "response-max-forwards"));
    }

    [Test]
    public void BodyRequiresContentTypeAndExactContentLength()
    {
        SipMessage missingType = ParseCompatible(RequestText(
            "MESSAGE", "sip:bob@example.com", "MESSAGE", "", "abc"));
        SipMessage wrongLength = ParseCompatible(RequestText(
            "MESSAGE", "sip:bob@example.com", "MESSAGE", "Content-Type: text/plain\r\n", "abc"));
        wrongLength.Body = Bytes("four");
        SipMessageCodec codec = new SipMessageCodec();

        SipValidationIssue[] typeIssues = codec.ValidateMessage(missingType);
        SipValidationIssue[] lengthIssues = codec.ValidateMessage(wrongLength);

        Assert.That(typeIssues,
            Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "content-type-required"));
        Assert.That(lengthIssues,
            Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "content-length"));
    }

    [Test]
    public void SipsTargetsRequireSipsContactsAndRouteUrisRemainSipBased()
    {
        SipMessage insecureContact = ParseCompatible(RequestText(
            "INVITE", "sips:bob@example.com", "INVITE", "Contact: <sip:alice@example.com>\r\n", ""));
        SipMessage secureContact = ParseCompatible(RequestText(
            "INVITE", "sips:bob@example.com", "INVITE", "Contact: <sips:alice@example.com>\r\n", ""));
        SipMessage secureRoute = ParseCompatible(RequestText(
            "INVITE", "sip:bob@example.com", "INVITE",
            "Route: <sips:proxy.example.com;lr>\r\nContact: <sip:alice@example.com>\r\n", ""));
        SipMessage invalidRoute = ParseCompatible(RequestText(
            "OPTIONS", "sip:bob@example.com", "OPTIONS", "Route: <https:proxy.example.com>\r\n", ""));
        SipMessageCodec codec = new SipMessageCodec();

        Assert.That(codec.ValidateMessage(insecureContact),
            Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "sips-contact"));
        Assert.That(codec.ValidateMessage(secureContact), Is.Empty);
        Assert.That(codec.ValidateMessage(secureRoute),
            Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "sips-contact"));
        Assert.That(codec.ValidateMessage(invalidRoute),
            Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "route-uri"));
    }

    [Test]
    public void WildcardContactCannotMixOrAppearOutsideRegister()
    {
        SipMessage mixed = ParseCompatible(RequestText("REGISTER", "sip:example.com", "REGISTER",
            "Contact: *\r\nContact: <sip:alice@example.com>\r\n", ""));
        SipMessage wrongMethod = ParseCompatible(RequestText(
            "OPTIONS", "sip:example.com", "OPTIONS", "Contact: *\r\n", ""));
        SipMessageCodec codec = new SipMessageCodec();

        SipValidationIssue[] mixedIssues = codec.ValidateMessage(mixed);
        SipValidationIssue[] methodIssues = codec.ValidateMessage(wrongMethod);

        Assert.That(mixedIssues,
            Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "contact-wildcard"));
        Assert.That(methodIssues,
            Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "contact-wildcard-method"));
    }

    [Test]
    public void ResponseStatusAndProtocolVersionMustBeConsistent()
    {
        SipMessage response = ParseCompatible(ResponseText(799, "Future", "OPTIONS", "", ""));
        response.ProtocolVersion = "SIP/3.0";
        response.StatusClass = SipStatusClass.Successful;
        SipMessageCodec codec = new SipMessageCodec();

        SipValidationIssue[] issues = codec.ValidateMessage(response);

        Assert.That(issues, Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "protocol-version"));
        Assert.That(issues, Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "response-status"));
        Assert.That(issues, Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "response-status-class"));
    }

    [Test]
    public void NullMessageReturnsAValidationIssue()
    {
        SipMessageCodec codec = new SipMessageCodec();

        SipValidationIssue[] issues = codec.ValidateMessage(null!);

        Assert.That(issues, Has.Length.EqualTo(1));
        Assert.That(issues[0].Code, Is.EqualTo("message-required"));
        Assert.That(issues[0].Severity, Is.EqualTo(SipValidationSeverity.Error));
    }

    static string RequestText(string method, string uri, string cseqMethod, string additional, string body)
    {
        return method + " " + uri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-one\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: call@example.com\r\n"
            + "CSeq: 1 " + cseqMethod + "\r\n"
            + additional
            + "Content-Length: " + Bytes(body).Length.ToString() + "\r\n\r\n"
            + body;
    }

    static string ResponseText(int status, string reason, string cseqMethod, string additional, string body)
    {
        return "SIP/2.0 " + status.ToString() + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-one\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=to-tag\r\n"
            + "Call-ID: call@example.com\r\n"
            + "CSeq: 1 " + cseqMethod + "\r\n"
            + additional
            + "Content-Length: " + Bytes(body).Length.ToString() + "\r\n\r\n"
            + body;
    }

    static SipMessage ParseCompatible(string text)
    {
        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        return new SipMessageCodec().ParseDatagramMessage(Bytes(text), options).Message!;
    }

    static SipUri ParseUri(string text)
    {
        return new SipUriCodec().ParseUri(Bytes(text), new SipParseOptions()).Uri!;
    }

    static SipHeader Header(string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        return header;
    }

    static byte[] Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }
}
