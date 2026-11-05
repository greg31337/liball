namespace sip.Tests;

public class CodecResultAndBodyModelTests
{
    [Test]
    public void ParseAndWriteOptionsUseSafeDefaults()
    {
        SipParseOptions parse = new SipParseOptions();
        SipWriteOptions write = new SipWriteOptions();

        Assert.Multiple(() =>
        {
            Assert.That(parse.Strict, Is.True);
            Assert.That(parse.PreserveOriginal, Is.True);
            Assert.That(parse.AllowHeaderFolding, Is.False);
            Assert.That(parse.AllowCompatibleWhitespace, Is.False);
            Assert.That(parse.MaxMessageBytes, Is.EqualTo(SipDefaults.MaxMessageBytes));
            Assert.That(parse.MaxBodyBytes, Is.EqualTo(SipDefaults.MaxBodyBytes));
            Assert.That(parse.MaxHeaderBytes, Is.EqualTo(SipDefaults.MaxHeaderBytes));
            Assert.That(parse.MaxStartLineBytes, Is.EqualTo(SipDefaults.MaxStartLineBytes));
            Assert.That(parse.MaxHeaderLineBytes, Is.EqualTo(SipDefaults.MaxHeaderLineBytes));
            Assert.That(parse.MaxHeaderCount, Is.EqualTo(SipDefaults.MaxHeaderCount));
            Assert.That(parse.MaxUriBytes, Is.EqualTo(SipDefaults.MaxUriBytes));
            Assert.That(parse.MaxParameterCount, Is.EqualTo(SipDefaults.MaxParameterCount));
            Assert.That(parse.MaxListValueCount, Is.EqualTo(SipDefaults.MaxListValueCount));
            Assert.That(parse.MaxMimeDepth, Is.EqualTo(SipDefaults.MaxMimeDepth));
            Assert.That(parse.MaxXmlDepth, Is.EqualTo(SipDefaults.MaxXmlDepth));
            Assert.That(write.Canonical, Is.True);
            Assert.That(write.PreserveOriginal, Is.False);
            Assert.That(write.CompactHeaderNames, Is.False);
            Assert.That(write.AllowHeaderFolding, Is.False);
            Assert.That(write.ReconcileContentLength, Is.True);
            Assert.That(write.MaxHeaderLineBytes, Is.EqualTo(SipDefaults.MaxHeaderLineBytes));
        });
    }

    [Test]
    public void CodecResultsRetainAllOutcomeValues()
    {
        SipMessage message = new SipMessage();
        SipUri uri = new SipUri();
        SipMimeEntity mime = new SipMimeEntity();
        SipSessionDescription session = new SipSessionDescription();
        SipXmlBody xml = new SipXmlBody();

        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Error;
        issue.Code = "invalid-value";
        issue.Message = "The value is invalid.";
        issue.ByteOffset = 12;
        issue.Path = "headers[0]";
        issue.HeaderName = SipHeaderNames.Contact;

        SipParseResult parse = new SipParseResult();
        parse.Status = SipParseStatus.Error;
        parse.Message = message;
        parse.Uri = uri;
        parse.MimeEntity = mime;
        parse.SessionDescription = session;
        parse.XmlBody = xml;
        parse.Consumed = 11;
        parse.RequiredBytes = 13;
        parse.ErrorOffset = 12;
        parse.ErrorMessage = "The value is invalid.";
        parse.ValidationIssues = [issue];

        SipWriteResult write = new SipWriteResult();
        write.Success = true;
        write.Bytes = [65, 66, 67];
        write.Written = 3;
        write.RequiredBytes = 3;
        write.ErrorMessage = "none";

        Assert.Multiple(() =>
        {
            Assert.That(parse.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(parse.Message, Is.SameAs(message));
            Assert.That(parse.Uri, Is.SameAs(uri));
            Assert.That(parse.MimeEntity, Is.SameAs(mime));
            Assert.That(parse.SessionDescription, Is.SameAs(session));
            Assert.That(parse.XmlBody, Is.SameAs(xml));
            Assert.That(parse.Consumed, Is.EqualTo(11));
            Assert.That(parse.RequiredBytes, Is.EqualTo(13));
            Assert.That(parse.ErrorOffset, Is.EqualTo(12));
            Assert.That(parse.ErrorMessage, Is.EqualTo("The value is invalid."));
            Assert.That(parse.ValidationIssues, Is.EqualTo(new SipValidationIssue[] { issue }));
            Assert.That(issue.Severity, Is.EqualTo(SipValidationSeverity.Error));
            Assert.That(issue.Code, Is.EqualTo("invalid-value"));
            Assert.That(issue.Message, Is.EqualTo("The value is invalid."));
            Assert.That(issue.ByteOffset, Is.EqualTo(12));
            Assert.That(issue.Path, Is.EqualTo("headers[0]"));
            Assert.That(issue.HeaderName, Is.EqualTo(SipHeaderNames.Contact));
            Assert.That(write.Success, Is.True);
            Assert.That(write.Bytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
            Assert.That(write.Written, Is.EqualTo(3));
            Assert.That(write.RequiredBytes, Is.EqualTo(3));
            Assert.That(write.ErrorMessage, Is.EqualTo("none"));
        });
    }

    [Test]
    public void SdpModelsRetainSessionMediaAndUnknownLines()
    {
        SipSdpAttribute sessionAttribute = new SipSdpAttribute();
        sessionAttribute.Name = "group";
        sessionAttribute.Value = "BUNDLE audio";
        sessionAttribute.HasValue = true;

        SipSdpAttribute mediaAttribute = new SipSdpAttribute();
        mediaAttribute.Name = "sendrecv";
        mediaAttribute.HasValue = false;

        SipMediaDescription media = new SipMediaDescription();
        media.Media = "audio";
        media.Port = 49170;
        media.PortCount = 2;
        media.Protocol = "RTP/AVP";
        media.Formats = ["0", "96"];
        media.Title = "Primary audio";
        media.Connection = "IN IP4 203.0.113.1";
        media.Bandwidths = ["AS:64"];
        media.EncryptionKey = "prompt";
        media.Attributes = [mediaAttribute];
        media.UnknownLines = ["x=media-extension"];

        SipSessionDescription session = new SipSessionDescription();
        session.ProtocolVersion = 0;
        session.Origin = "alice 2890844526 2890842807 IN IP4 192.0.2.1";
        session.SessionName = "SIP Call";
        session.SessionInformation = "Example";
        session.Uri = "https://example.com/session";
        session.Emails = ["alice@example.com"];
        session.Phones = ["+1 555 0100"];
        session.Connection = "IN IP4 203.0.113.1";
        session.Bandwidths = ["AS:128"];
        session.Timings = ["0 0"];
        session.RepeatTimes = ["7d 1h 0 25h"];
        session.TimeZones = "2882844526 -1h";
        session.EncryptionKey = "clear:password";
        session.Attributes = [sessionAttribute];
        session.MediaDescriptions = [media];
        session.UnknownLines = ["y=session-extension"];
        session.OriginalBytes = [118, 61, 48];

        Assert.Multiple(() =>
        {
            Assert.That(session.ProtocolVersion, Is.EqualTo(0));
            Assert.That(session.Origin, Does.StartWith("alice "));
            Assert.That(session.SessionName, Is.EqualTo("SIP Call"));
            Assert.That(session.SessionInformation, Is.EqualTo("Example"));
            Assert.That(session.Uri, Is.EqualTo("https://example.com/session"));
            Assert.That(session.Emails, Is.EqualTo(new string[] { "alice@example.com" }));
            Assert.That(session.Phones, Is.EqualTo(new string[] { "+1 555 0100" }));
            Assert.That(session.Connection, Is.EqualTo("IN IP4 203.0.113.1"));
            Assert.That(session.Bandwidths, Is.EqualTo(new string[] { "AS:128" }));
            Assert.That(session.Timings, Is.EqualTo(new string[] { "0 0" }));
            Assert.That(session.RepeatTimes, Is.EqualTo(new string[] { "7d 1h 0 25h" }));
            Assert.That(session.TimeZones, Is.EqualTo("2882844526 -1h"));
            Assert.That(session.EncryptionKey, Is.EqualTo("clear:password"));
            Assert.That(session.Attributes, Is.EqualTo(new SipSdpAttribute[] { sessionAttribute }));
            Assert.That(session.MediaDescriptions, Is.EqualTo(new SipMediaDescription[] { media }));
            Assert.That(session.UnknownLines, Is.EqualTo(new string[] { "y=session-extension" }));
            Assert.That(session.OriginalBytes, Is.EqualTo(new byte[] { 118, 61, 48 }));
            Assert.That(sessionAttribute.Value, Is.EqualTo("BUNDLE audio"));
            Assert.That(media.Media, Is.EqualTo("audio"));
            Assert.That(media.Port, Is.EqualTo(49170));
            Assert.That(media.PortCount, Is.EqualTo(2));
            Assert.That(media.Protocol, Is.EqualTo("RTP/AVP"));
            Assert.That(media.Formats, Is.EqualTo(new string[] { "0", "96" }));
            Assert.That(media.Title, Is.EqualTo("Primary audio"));
            Assert.That(media.Connection, Is.EqualTo("IN IP4 203.0.113.1"));
            Assert.That(media.Bandwidths, Is.EqualTo(new string[] { "AS:64" }));
            Assert.That(media.EncryptionKey, Is.EqualTo("prompt"));
            Assert.That(media.Attributes, Is.EqualTo(new SipSdpAttribute[] { mediaAttribute }));
            Assert.That(media.UnknownLines, Is.EqualTo(new string[] { "x=media-extension" }));
            Assert.That(mediaAttribute.HasValue, Is.False);
        });
    }

    [Test]
    public void XmlBodyRetainsMediaIdentityAndOriginalBytes()
    {
        SipMediaType mediaType = new SipMediaType();
        mediaType.Type = "application";
        mediaType.Subtype = "pidf+xml";

        SipXmlBody body = new SipXmlBody();
        body.MediaType = mediaType;
        body.RootName = "presence";
        body.NamespaceUri = "urn:ietf:params:xml:ns:pidf";
        body.XmlText = "<presence xmlns=\"urn:ietf:params:xml:ns:pidf\"/>";
        body.OriginalBytes = [60, 112, 114, 101, 115, 101, 110, 99, 101, 47, 62];

        Assert.Multiple(() =>
        {
            Assert.That(body.MediaType, Is.SameAs(mediaType));
            Assert.That(body.RootName, Is.EqualTo("presence"));
            Assert.That(body.NamespaceUri, Is.EqualTo("urn:ietf:params:xml:ns:pidf"));
            Assert.That(body.XmlText, Does.Contain("presence"));
            Assert.That(body.OriginalBytes, Is.EqualTo(new byte[] { 60, 112, 114, 101, 115, 101, 110, 99, 101, 47, 62 }));
        });
    }

    [Test]
    public void AbsentValuesUseExplicitSentinelsAndEmptyCollections()
    {
        SipUri firstUri = new SipUri();
        SipUri secondUri = new SipUri();
        SipViaValue via = new SipViaValue();
        SipContactValue contact = new SipContactValue();
        SipHeaderParseResult headerResult = new SipHeaderParseResult();
        SipParseResult parseResult = new SipParseResult();
        SipMediaDescription media = new SipMediaDescription();
        SipWriteResult firstWrite = new SipWriteResult();
        SipWriteResult secondWrite = new SipWriteResult();

        Assert.Multiple(() =>
        {
            Assert.That(firstUri.Port, Is.EqualTo(-1));
            Assert.That(firstUri.Parameters, Is.Empty);
            Assert.That(firstUri.Headers, Is.Empty);
            Assert.That(firstUri.Parameters, Is.Not.SameAs(secondUri.Parameters));
            Assert.That(firstUri.Headers, Is.Not.SameAs(secondUri.Headers));
            Assert.That(via.Port, Is.EqualTo(-1));
            Assert.That(via.RPort, Is.EqualTo(-1));
            Assert.That(via.Parameters, Is.Empty);
            Assert.That(contact.ExpiresSeconds, Is.EqualTo(-1));
            Assert.That(contact.QualityThousandths, Is.EqualTo(-1));
            Assert.That(headerResult.Status, Is.EqualTo(SipParseStatus.None));
            Assert.That(headerResult.ErrorOffset, Is.EqualTo(-1));
            Assert.That(headerResult.RequiredBytes, Is.EqualTo(-1));
            Assert.That(headerResult.ViaValues, Is.Empty);
            Assert.That(headerResult.RouteValues, Is.Empty);
            Assert.That(headerResult.ContactValues, Is.Empty);
            Assert.That(headerResult.ValidationIssues, Is.Empty);
            Assert.That(parseResult.Status, Is.EqualTo(SipParseStatus.None));
            Assert.That(parseResult.ErrorOffset, Is.EqualTo(-1));
            Assert.That(parseResult.RequiredBytes, Is.EqualTo(-1));
            Assert.That(parseResult.ValidationIssues, Is.Empty);
            Assert.That(media.Port, Is.EqualTo(-1));
            Assert.That(media.PortCount, Is.EqualTo(1));
            Assert.That(firstWrite.Success, Is.False);
            Assert.That(firstWrite.Bytes, Is.Empty);
            Assert.That(firstWrite.Bytes, Is.Not.SameAs(secondWrite.Bytes));
        });
    }

    [Test]
    public void MutableCollectionDefaultsAreOwnedPerModelInstance()
    {
        SipMessage firstMessage = new SipMessage();
        SipMessage secondMessage = new SipMessage();
        SipMimeEntity firstMime = new SipMimeEntity();
        SipMimeEntity secondMime = new SipMimeEntity();
        SipSessionDescription firstSession = new SipSessionDescription();
        SipSessionDescription secondSession = new SipSessionDescription();
        SipMediaDescription firstMedia = new SipMediaDescription();
        SipMediaDescription secondMedia = new SipMediaDescription();

        Assert.Multiple(() =>
        {
            Assert.That(firstMessage.Headers, Is.Not.SameAs(secondMessage.Headers));
            Assert.That(firstMessage.ViaValues, Is.Not.SameAs(secondMessage.ViaValues));
            Assert.That(firstMessage.RouteValues, Is.Not.SameAs(secondMessage.RouteValues));
            Assert.That(firstMessage.RecordRouteValues, Is.Not.SameAs(secondMessage.RecordRouteValues));
            Assert.That(firstMessage.ContactValues, Is.Not.SameAs(secondMessage.ContactValues));
            Assert.That(firstMessage.Body, Is.Not.SameAs(secondMessage.Body));
            Assert.That(firstMessage.OriginalBytes, Is.Not.SameAs(secondMessage.OriginalBytes));
            Assert.That(firstMime.Headers, Is.Not.SameAs(secondMime.Headers));
            Assert.That(firstMime.Body, Is.Not.SameAs(secondMime.Body));
            Assert.That(firstMime.Parts, Is.Not.SameAs(secondMime.Parts));
            Assert.That(firstMime.Preamble, Is.Not.SameAs(secondMime.Preamble));
            Assert.That(firstMime.Epilogue, Is.Not.SameAs(secondMime.Epilogue));
            Assert.That(firstSession.Attributes, Is.Not.SameAs(secondSession.Attributes));
            Assert.That(firstSession.MediaDescriptions, Is.Not.SameAs(secondSession.MediaDescriptions));
            Assert.That(firstSession.OriginalBytes, Is.Not.SameAs(secondSession.OriginalBytes));
            Assert.That(firstMedia.Formats, Is.Not.SameAs(secondMedia.Formats));
            Assert.That(firstMedia.Attributes, Is.Not.SameAs(secondMedia.Attributes));
            Assert.That(firstMedia.UnknownLines, Is.Not.SameAs(secondMedia.UnknownLines));
        });
    }
}
