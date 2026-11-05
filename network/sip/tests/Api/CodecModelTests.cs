namespace sip.Tests;

public class CodecModelTests
{
    [Test]
    public void UriAndNameAddressModelsRetainOrderedComponents()
    {
        SipUriParameter transport = new SipUriParameter();
        transport.Name = "transport";
        transport.Value = "tcp";
        transport.HasValue = true;
        transport.OriginalText = "transport=tcp";

        SipUriHeader subject = new SipUriHeader();
        subject.Name = "Subject";
        subject.Value = "project x";
        subject.OriginalText = "Subject=project%20x";

        SipUri uri = new SipUri();
        uri.Scheme = SipUriScheme.Sips;
        uri.SchemeToken = "sips";
        uri.User = "alice";
        uri.Password = "secret";
        uri.Host = "2001:db8::1";
        uri.Port = 5071;
        uri.Parameters = [transport];
        uri.Headers = [subject];
        uri.OpaquePart = "opaque";
        uri.OriginalText = "sips:alice:secret@[2001:db8::1]:5071;transport=tcp?Subject=project%20x";

        SipUriParameter tag = new SipUriParameter();
        tag.Name = "tag";
        tag.Value = "a1";
        tag.HasValue = true;

        SipNameAddress address = new SipNameAddress();
        address.DisplayName = "Alice";
        address.Uri = uri;
        address.Parameters = [tag];
        address.OriginalText = "Alice <sips:alice@example.com>;tag=a1";

        Assert.Multiple(() =>
        {
            Assert.That(uri.Scheme, Is.EqualTo(SipUriScheme.Sips));
            Assert.That(uri.SchemeToken, Is.EqualTo("sips"));
            Assert.That(uri.User, Is.EqualTo("alice"));
            Assert.That(uri.Password, Is.EqualTo("secret"));
            Assert.That(uri.Host, Is.EqualTo("2001:db8::1"));
            Assert.That(uri.Port, Is.EqualTo(5071));
            Assert.That(uri.Parameters, Is.EqualTo(new SipUriParameter[] { transport }));
            Assert.That(uri.Headers, Is.EqualTo(new SipUriHeader[] { subject }));
            Assert.That(uri.OpaquePart, Is.EqualTo("opaque"));
            Assert.That(address.DisplayName, Is.EqualTo("Alice"));
            Assert.That(address.Uri, Is.SameAs(uri));
            Assert.That(address.Parameters, Is.EqualTo(new SipUriParameter[] { tag }));
            Assert.That(transport.OriginalText, Is.EqualTo("transport=tcp"));
            Assert.That(subject.OriginalText, Is.EqualTo("Subject=project%20x"));
        });
    }

    [Test]
    public void HeaderModelsRetainGenericAndTypedValues()
    {
        SipUriParameter branchParameter = new SipUriParameter();
        branchParameter.Name = "branch";
        branchParameter.Value = "z9hG4bK-test";
        branchParameter.HasValue = true;

        SipViaValue via = new SipViaValue();
        via.ProtocolName = "SIP";
        via.ProtocolVersion = "2.0";
        via.Transport = "UDP";
        via.Host = "client.example.com";
        via.Port = 5060;
        via.Parameters = [branchParameter];
        via.Branch = "z9hG4bK-test";
        via.Received = "192.0.2.10";
        via.RPort = 5080;
        via.HasRPort = true;

        SipUri routeUri = new SipUri();
        routeUri.Scheme = SipUriScheme.Sip;
        routeUri.Host = "proxy.example.com";

        SipNameAddress routeAddress = new SipNameAddress();
        routeAddress.Uri = routeUri;

        SipRouteValue route = new SipRouteValue();
        route.Address = routeAddress;
        route.LooseRouting = true;

        SipContactValue contact = new SipContactValue();
        contact.Address = routeAddress;
        contact.ExpiresSeconds = 300;
        contact.QualityThousandths = 750;

        SipCSeqValue cseq = new SipCSeqValue();
        cseq.SequenceNumber = 42;
        cseq.Method = SipMethod.Invite;
        cseq.MethodToken = SipMethods.Invite;

        SipUriParameter charset = new SipUriParameter();
        charset.Name = "charset";
        charset.Value = "utf-8";
        charset.HasValue = true;

        SipMediaType mediaType = new SipMediaType();
        mediaType.Type = "application";
        mediaType.Subtype = "sdp";
        mediaType.Parameters = [charset];
        mediaType.OriginalText = "application/sdp;charset=utf-8";

        SipContentDisposition disposition = new SipContentDisposition();
        disposition.Disposition = SipBodyDisposition.Session;
        disposition.DispositionToken = "session";
        disposition.Parameters = [charset];

        SipHeader header = new SipHeader();
        header.Name = SipHeaderNames.Via;
        header.Value = "SIP/2.0/UDP client.example.com";
        header.OriginalBytes = [86, 105, 97];

        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Warning;

        SipHeaderParseResult result = new SipHeaderParseResult();
        result.Status = SipParseStatus.Complete;
        result.Header = header;
        result.NameAddress = routeAddress;
        result.ViaValues = [via];
        result.RouteValues = [route];
        result.ContactValues = [contact];
        result.CSeq = cseq;
        result.MediaType = mediaType;
        result.ContentDisposition = disposition;
        result.Consumed = 47;
        result.RequiredBytes = 47;
        result.ValidationIssues = [issue];

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(result.Header, Is.SameAs(header));
            Assert.That(result.NameAddress, Is.SameAs(routeAddress));
            Assert.That(result.ViaValues, Is.EqualTo(new SipViaValue[] { via }));
            Assert.That(result.RouteValues, Is.EqualTo(new SipRouteValue[] { route }));
            Assert.That(result.ContactValues, Is.EqualTo(new SipContactValue[] { contact }));
            Assert.That(result.CSeq, Is.SameAs(cseq));
            Assert.That(result.MediaType, Is.SameAs(mediaType));
            Assert.That(result.ContentDisposition, Is.SameAs(disposition));
            Assert.That(result.Consumed, Is.EqualTo(47));
            Assert.That(result.RequiredBytes, Is.EqualTo(47));
            Assert.That(result.ValidationIssues, Is.EqualTo(new SipValidationIssue[] { issue }));
            Assert.That(via.RPort, Is.EqualTo(5080));
            Assert.That(route.LooseRouting, Is.True);
            Assert.That(contact.ExpiresSeconds, Is.EqualTo(300));
            Assert.That(contact.QualityThousandths, Is.EqualTo(750));
            Assert.That(cseq.SequenceNumber, Is.EqualTo(42));
            Assert.That(mediaType.Parameters, Is.EqualTo(new SipUriParameter[] { charset }));
            Assert.That(disposition.Parameters, Is.EqualTo(new SipUriParameter[] { charset }));
            Assert.That(header.OriginalBytes, Is.EqualTo(new byte[] { 86, 105, 97 }));
        });
    }

    [Test]
    public void MessageModelRetainsOrderedFieldsAndBinaryBody()
    {
        SipUri requestUri = new SipUri();
        requestUri.Scheme = SipUriScheme.Sip;
        requestUri.Host = "example.com";

        SipHeader firstHeader = new SipHeader();
        firstHeader.Name = "X-Trace";
        firstHeader.Value = "one";

        SipHeader secondHeader = new SipHeader();
        secondHeader.Name = "X-Trace";
        secondHeader.Value = "two";

        SipViaValue via = new SipViaValue();
        SipRouteValue route = new SipRouteValue();
        SipContactValue contact = new SipContactValue();
        SipNameAddress from = new SipNameAddress();
        SipNameAddress to = new SipNameAddress();
        SipCSeqValue cseq = new SipCSeqValue();
        SipMediaType mediaType = new SipMediaType();
        SipContentDisposition disposition = new SipContentDisposition();

        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Request;
        message.Method = SipMethod.Unknown;
        message.MethodToken = "X-CUSTOM";
        message.RequestUri = requestUri;
        message.StatusClass = SipStatusClass.ClientError;
        message.StatusCode = 499;
        message.ReasonPhrase = "Extension Failure";
        message.ProtocolVersion = "SIP/2.0";
        message.Headers = [firstHeader, secondHeader];
        message.ViaValues = [via];
        message.RouteValues = [route];
        message.RecordRouteValues = [route];
        message.ContactValues = [contact];
        message.From = from;
        message.To = to;
        message.CSeq = cseq;
        message.ContentType = mediaType;
        message.ContentDisposition = disposition;
        message.Body = [0, 255, 13, 10];
        message.OriginalBytes = [73, 78, 86, 73, 84, 69];

        Assert.Multiple(() =>
        {
            Assert.That(message.Kind, Is.EqualTo(SipMessageKind.Request));
            Assert.That(message.Method, Is.EqualTo(SipMethod.Unknown));
            Assert.That(message.MethodToken, Is.EqualTo("X-CUSTOM"));
            Assert.That(message.RequestUri, Is.SameAs(requestUri));
            Assert.That(message.StatusClass, Is.EqualTo(SipStatusClass.ClientError));
            Assert.That(message.StatusCode, Is.EqualTo(499));
            Assert.That(message.ReasonPhrase, Is.EqualTo("Extension Failure"));
            Assert.That(message.Headers, Is.EqualTo(new SipHeader[] { firstHeader, secondHeader }));
            Assert.That(message.ViaValues, Is.EqualTo(new SipViaValue[] { via }));
            Assert.That(message.RouteValues, Is.EqualTo(new SipRouteValue[] { route }));
            Assert.That(message.RecordRouteValues, Is.EqualTo(new SipRouteValue[] { route }));
            Assert.That(message.ContactValues, Is.EqualTo(new SipContactValue[] { contact }));
            Assert.That(message.From, Is.SameAs(from));
            Assert.That(message.To, Is.SameAs(to));
            Assert.That(message.CSeq, Is.SameAs(cseq));
            Assert.That(message.ContentType, Is.SameAs(mediaType));
            Assert.That(message.ContentDisposition, Is.SameAs(disposition));
            Assert.That(message.Body, Is.EqualTo(new byte[] { 0, 255, 13, 10 }));
            Assert.That(message.OriginalBytes, Is.EqualTo(new byte[] { 73, 78, 86, 73, 84, 69 }));
        });
    }

    [Test]
    public void MimeModelRetainsNestedPartsAndFramingBytes()
    {
        SipHeader header = new SipHeader();
        SipMediaType mediaType = new SipMediaType();
        SipContentDisposition disposition = new SipContentDisposition();
        SipMimeEntity part = new SipMimeEntity();
        part.Body = [1, 2, 3];

        SipMimeEntity entity = new SipMimeEntity();
        entity.Headers = [header];
        entity.ContentType = mediaType;
        entity.ContentDisposition = disposition;
        entity.Body = [4, 5];
        entity.Parts = [part];
        entity.Preamble = [6];
        entity.Epilogue = [7];

        Assert.Multiple(() =>
        {
            Assert.That(entity.Headers, Is.EqualTo(new SipHeader[] { header }));
            Assert.That(entity.ContentType, Is.SameAs(mediaType));
            Assert.That(entity.ContentDisposition, Is.SameAs(disposition));
            Assert.That(entity.Body, Is.EqualTo(new byte[] { 4, 5 }));
            Assert.That(entity.Parts, Is.EqualTo(new SipMimeEntity[] { part }));
            Assert.That(part.Body, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(entity.Preamble, Is.EqualTo(new byte[] { 6 }));
            Assert.That(entity.Epilogue, Is.EqualTo(new byte[] { 7 }));
        });
    }
}
