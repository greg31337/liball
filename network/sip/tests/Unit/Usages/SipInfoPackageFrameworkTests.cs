namespace sip.Tests;

public class SipInfoPackageFrameworkTests
{
    [Test]
    public void RegistrationControlsOrderedReceiveInfoAdvertisement()
    {
        SipInfoPackageFramework framework = new SipInfoPackageFramework();
        RecordingInfoPackage first = new RecordingInfoPackage();
        RecordingInfoPackage second = new RecordingInfoPackage();
        Assert.That(framework.RegisterInfoPackage(
            "alpha", first, new SipMediaType[]
            {
                MediaType("application", "alpha")
            }),
            Is.True);
        Assert.That(framework.RegisterInfoPackage(
            "Beta", second, new SipMediaType[]
            {
                MediaType("application", "beta")
            }),
            Is.True);
        Assert.That(framework.RegisterInfoPackage(
            "alpha", first, new SipMediaType[]
            {
                MediaType("application", "alpha")
            }),
            Is.False);

        SipMessage advertised = framework.AddReceiveInfoAdvertisement(
            InviteRequest());
        Assert.That(Header(advertised, SipHeaderNames.ReceiveInfo),
            Is.EqualTo("alpha, Beta"));

        Assert.That(framework.RemoveInfoPackage("alpha"), Is.True);
        SipMessage remaining = framework.AddReceiveInfoAdvertisement(
            InviteRequest());
        Assert.That(Header(remaining, SipHeaderNames.ReceiveInfo),
            Is.EqualTo("Beta"));

        Assert.That(framework.RemoveInfoPackage("Beta"), Is.True);
        SipMessage empty = framework.AddReceiveInfoAdvertisement(
            InviteRequest());
        Assert.That(Header(empty, SipHeaderNames.ReceiveInfo), Is.Empty);
    }

    [Test]
    public void AdvertisementRejectsInfoAndOtherIneligibleMessages()
    {
        SipInfoPackageFramework framework = new SipInfoPackageFramework();
        SipMessage info = Parse(IncomingInfoText(
            "Info-Package: alpha\r\n", "", ""));
        SipMessage bye = Parse(InviteRequestText().Replace(
            "INVITE sip:bob@example.com", "BYE sip:bob@example.com")
            .Replace("CSeq: 10 INVITE", "CSeq: 10 BYE"));

        Assert.Multiple(() =>
        {
            Assert.That(() => framework.AddReceiveInfoAdvertisement(info),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => framework.AddReceiveInfoAdvertisement(bye),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void NegotiationParsesMultipleHeadersParametersAndEmptyUpdate()
    {
        SipInfoPackageFramework framework = new SipInfoPackageFramework();
        framework.RegisterInfoPackage("alpha",
            new RecordingInfoPackage(), new SipMediaType[]
            {
                MediaType("application", "alpha")
            });
        SipDialogId dialogId = DialogId();
        SipMessage advertisement = InviteRequest();
        AddHeader(advertisement, SipHeaderNames.ReceiveInfo,
            "alpha;mode=fast, Beta");
        AddHeader(advertisement, SipHeaderNames.ReceiveInfo,
            "gamma;note=\"a,b\"");

        SipInfoNegotiation negotiated = framework.UpdateInfoNegotiation(
            dialogId, advertisement);
        SipMessage emptyAdvertisement = InviteRequest();
        AddHeader(emptyAdvertisement, SipHeaderNames.ReceiveInfo, "");
        SipInfoNegotiation empty = framework.UpdateInfoNegotiation(
            dialogId, emptyAdvertisement);

        Assert.Multiple(() =>
        {
            Assert.That(negotiated.RemoteSupportsInfoPackages, Is.True);
            Assert.That(negotiated.RemotePackages,
                Is.EqualTo(new string[] { "alpha", "Beta", "gamma" }));
            Assert.That(negotiated.LocalPackages,
                Is.EqualTo(new string[] { "alpha" }));
            Assert.That(empty.RemoteSupportsInfoPackages, Is.True);
            Assert.That(empty.RemotePackages, Is.Empty);
        });
    }

    [Test]
    public void SendCreatesNegotiatedInDialogPackageRequest()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer);
        SipInfoPackageFramework framework = new SipInfoPackageFramework();
        framework.ConfigureInfoDialogLayer(layer);
        SipMessage advertisement = InviteRequest();
        AddHeader(advertisement, SipHeaderNames.ReceiveInfo,
            "alpha;remote=value");
        framework.UpdateInfoNegotiation(dialog.Id!, advertisement);
        SipInfoRequest request = InfoRequest(
            dialog.Id!, "alpha;local=value", "payload");
        request.CorrelationId = "info-send";

        SipInfoResult sent = framework.SendInfo(request);

        Assert.Multiple(() =>
        {
            Assert.That(sent.Accepted, Is.True);
            Assert.That(sent.Message!.Method, Is.EqualTo(SipMethod.Info));
            Assert.That(sent.Message.CSeq!.SequenceNumber, Is.EqualTo(11));
            Assert.That(Header(sent.Message, SipHeaderNames.InfoPackage),
                Is.EqualTo("alpha;local=value"));
            Assert.That(Header(sent.Message,
                SipHeaderNames.ContentDisposition), Is.EqualTo("info-package"));
            Assert.That(Header(sent.Message, SipHeaderNames.ReceiveInfo),
                Is.Null);
            Assert.That(sent.CorrelationId, Is.EqualTo("info-send"));
        });
    }

    [Test]
    public void SendUsesCaseSensitiveNegotiationAndLegacyPolicy()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer);
        SipInfoPackageFramework framework = new SipInfoPackageFramework();
        framework.ConfigureInfoDialogLayer(layer);
        SipMessage advertisement = InviteRequest();
        AddHeader(advertisement, SipHeaderNames.ReceiveInfo, "Alpha");
        framework.UpdateInfoNegotiation(dialog.Id!, advertisement);

        SipInfoResult caseMismatch = framework.SendInfo(
            InfoRequest(dialog.Id!, "alpha", "payload"));
        SipInfoRequest deniedLegacy =
            InfoRequest(dialog.Id!, null, "legacy");
        SipInfoResult denied = framework.SendInfo(deniedLegacy);
        SipInfoRequest allowedLegacy =
            InfoRequest(dialog.Id!, null, "legacy");
        allowedLegacy.Options.AllowLegacyInfo = true;
        SipInfoResult allowed = framework.SendInfo(allowedLegacy);

        Assert.Multiple(() =>
        {
            Assert.That(caseMismatch.Accepted, Is.False);
            Assert.That(caseMismatch.StatusCode, Is.EqualTo(469));
            Assert.That(denied.StatusCode, Is.EqualTo(469));
            Assert.That(allowed.Accepted, Is.True);
            Assert.That(allowed.Legacy, Is.True);
            Assert.That(Header(allowed.Message!, SipHeaderNames.InfoPackage),
                Is.Null);
            Assert.That(Header(allowed.Message!,
                SipHeaderNames.ContentDisposition), Is.Null);
        });
    }

    [Test]
    public void ReceiveDispatchesRegisteredPackageAndCreatesOkResponse()
    {
        RecordingInfoPackage handler = new RecordingInfoPackage();
        SipInfoPackageFramework framework = RegisteredFramework(
            "alpha", handler, MediaType("application", "alpha"));
        SipMessage message = Parse(IncomingInfoText(
            "Info-Package: alpha;mode=fast\r\n"
                + "Content-Disposition: info-package\r\n",
            "Content-Type: application/alpha\r\n", "payload"));

        SipInfoResult result = framework.ReceiveInfo(
            DialogId(), message, new SipInfoOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Response!.StatusCode, Is.EqualTo(200));
            Assert.That(result.Package, Is.EqualTo("alpha"));
            Assert.That(result.Legacy, Is.False);
            Assert.That(handler.LastRequest!.Package, Is.EqualTo("alpha"));
            Assert.That(System.Text.Encoding.UTF8.GetString(
                handler.LastRequest.Body), Is.EqualTo("payload"));
            Assert.That(handler.LastRequest.DialogId!.CallId,
                Is.EqualTo("dialog-call"));
        });
    }

    [Test]
    public void UnknownOrCaseMismatchedPackageReturns469AndCurrentRecvInfo()
    {
        SipInfoPackageFramework framework = RegisteredFramework(
            "alpha", new RecordingInfoPackage(),
            MediaType("application", "alpha"));
        SipMessage message = Parse(IncomingInfoText(
            "Info-Package: Alpha\r\n"
                + "Content-Disposition: info-package\r\n",
            "Content-Type: application/alpha\r\n", "payload"));

        SipInfoResult result = framework.ReceiveInfo(
            DialogId(), message, new SipInfoOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(469));
            Assert.That(Header(result.Response!, SipHeaderNames.ReceiveInfo),
                Is.EqualTo("alpha"));
            Assert.That(result.Response!.StatusCode, Is.EqualTo(469));
        });
    }

    [Test]
    public void ReceiveValidatesDispositionAndPackageMediaType()
    {
        SipInfoPackageFramework framework = RegisteredFramework(
            "alpha", new RecordingInfoPackage(),
            MediaType("application", "alpha"));
        SipMessage noDisposition = Parse(IncomingInfoText(
            "Info-Package: alpha\r\n",
            "Content-Type: application/alpha\r\n", "payload"));
        SipMessage wrongType = Parse(IncomingInfoText(
            "Info-Package: alpha\r\n"
                + "Content-Disposition: info-package\r\n",
            "Content-Type: text/plain\r\n", "payload"));

        SipInfoResult malformed = framework.ReceiveInfo(
            DialogId(), noDisposition, new SipInfoOptions());
        SipInfoResult unsupported = framework.ReceiveInfo(
            DialogId(), wrongType, new SipInfoOptions());

        Assert.Multiple(() =>
        {
            Assert.That(malformed.StatusCode, Is.EqualTo(400));
            Assert.That(unsupported.StatusCode, Is.EqualTo(415));
            Assert.That(Header(unsupported.Response!, SipHeaderNames.Accept),
                Is.EqualTo("application/alpha"));
        });
    }

    [Test]
    public void ReceiveEnforcesDialogRecvInfoAndSizeRules()
    {
        SipInfoPackageFramework framework = RegisteredFramework(
            "alpha", new RecordingInfoPackage(),
            MediaType("application", "alpha"));
        SipMessage wrongDialog = Parse(IncomingInfoText(
            "Info-Package: alpha\r\n"
                + "Content-Disposition: info-package\r\n",
            "Content-Type: application/alpha\r\n", "payload"));
        SipDialogId other = DialogId();
        other.CallId = "another-call";
        SipInfoResult missingDialog = framework.ReceiveInfo(
            other, wrongDialog, new SipInfoOptions());

        SipMessage withRecvInfo = Parse(IncomingInfoText(
            "Info-Package: alpha\r\nRecv-Info: alpha\r\n"
                + "Content-Disposition: info-package\r\n",
            "Content-Type: application/alpha\r\n", "payload"));
        SipInfoResult invalidHeader = framework.ReceiveInfo(
            DialogId(), withRecvInfo, new SipInfoOptions());

        SipInfoOptions small = new SipInfoOptions();
        small.MaxBodyBytes = 3;
        SipInfoResult tooLarge = framework.ReceiveInfo(
            DialogId(), wrongDialog, small);

        Assert.Multiple(() =>
        {
            Assert.That(missingDialog.StatusCode, Is.EqualTo(481));
            Assert.That(invalidHeader.StatusCode, Is.EqualTo(400));
            Assert.That(tooLarge.StatusCode, Is.EqualTo(413));
        });
    }

    [Test]
    public void LegacyInfoRequiresPolicyAndCanUseDedicatedHandler()
    {
        SipInfoPackageFramework framework = new SipInfoPackageFramework();
        SipMessage message = Parse(IncomingInfoText(
            "", "Content-Type: application/legacy\r\n", "legacy"));
        SipInfoResult denied = framework.ReceiveInfo(
            DialogId(), message, new SipInfoOptions());

        RecordingInfoPackage legacyHandler = new RecordingInfoPackage();
        framework.ConfigureLegacyInfoHandler(legacyHandler);
        SipInfoOptions allowedOptions = new SipInfoOptions();
        allowedOptions.AllowLegacyInfo = true;
        allowedOptions.LegacyContentTypes = new SipMediaType[]
        {
            MediaType("application", "legacy")
        };
        SipInfoResult allowed = framework.ReceiveInfo(
            DialogId(), message, allowedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(denied.StatusCode, Is.EqualTo(469));
            Assert.That(allowed.Accepted, Is.True);
            Assert.That(allowed.Legacy, Is.True);
            Assert.That(legacyHandler.LastRequest!.Package, Is.Null);
            Assert.That(allowed.Response!.StatusCode, Is.EqualTo(200));
        });
    }

    [Test]
    public void HandlerCanRejectWithoutTerminatingDialogAssociation()
    {
        RecordingInfoPackage handler = new RecordingInfoPackage();
        handler.Accept = false;
        handler.StatusCode = SipStatusCodes.NotAcceptableHere;
        SipInfoPackageFramework framework = RegisteredFramework(
            "alpha", handler, MediaType("application", "alpha"));
        SipMessage message = Parse(IncomingInfoText(
            "Info-Package: alpha\r\n"
                + "Content-Disposition: info-package\r\n",
            "Content-Type: application/alpha\r\n", "payload"));

        SipInfoResult result = framework.ReceiveInfo(
            DialogId(), message, new SipInfoOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(488));
            Assert.That(result.Response!.StatusCode, Is.EqualTo(488));
            Assert.That(result.DialogId!.CallId, Is.EqualTo("dialog-call"));
        });
    }

    [Test]
    public void SendReportsNegotiationFailureAndRejectsInvalidInputs()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer);
        SipInfoPackageFramework framework = new SipInfoPackageFramework();
        framework.ConfigureInfoDialogLayer(layer);
        framework.RegisterInfoPackage("alpha",
            new RecordingInfoPackage(), new SipMediaType[]
            {
                MediaType("application", "alpha")
            });
        SipInfoRequest unnegotiated = InfoRequest(
            dialog.Id!, "alpha", "payload");
        unnegotiated.CorrelationId = "info-unnegotiated";
        SipInfoResult rejected = framework.SendInfo(unnegotiated);

        SipInfoRequest malformed = InfoRequest(
            dialog.Id!, "alpha;mode=one;MODE=two", "payload");
        SipInfoRequest tooLarge = InfoRequest(
            dialog.Id!, "alpha", "payload");
        tooLarge.Options.MaxBodyBytes = 3;

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.StatusCode, Is.EqualTo(469));
            Assert.That(rejected.CorrelationId,
                Is.EqualTo("info-unnegotiated"));
            Assert.That(rejected.ErrorMessage,
                Does.Contain("not advertised"));
            Assert.That(() => framework.SendInfo(malformed),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => framework.SendInfo(tooLarge),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void ReceiveRejectsDuplicateAndMalformedInfoPackages()
    {
        SipInfoPackageFramework framework = RegisteredFramework(
            "alpha", new RecordingInfoPackage(),
            MediaType("application", "alpha"));
        SipMessage duplicate = Parse(IncomingInfoText(
            "Info-Package: alpha\r\nInfo-Package: alpha\r\n"
                + "Content-Disposition: info-package\r\n",
            "Content-Type: application/alpha\r\n", "payload"));
        SipMessage malformed = Parse(IncomingInfoText(
            "Info-Package: alpha;mode=one;MODE=two\r\n"
                + "Content-Disposition: info-package\r\n",
            "Content-Type: application/alpha\r\n", "payload"));

        SipInfoResult duplicateResult = framework.ReceiveInfo(
            DialogId(), duplicate, new SipInfoOptions());
        SipInfoResult malformedResult = framework.ReceiveInfo(
            DialogId(), malformed, new SipInfoOptions());

        Assert.Multiple(() =>
        {
            Assert.That(duplicateResult.Accepted, Is.False);
            Assert.That(duplicateResult.StatusCode, Is.EqualTo(400));
            Assert.That(duplicateResult.ErrorMessage,
                Does.Contain("only one"));
            Assert.That(malformedResult.Accepted, Is.False);
            Assert.That(malformedResult.StatusCode, Is.EqualTo(400));
            Assert.That(malformedResult.ErrorMessage,
                Does.Contain("repeated"));
        });
    }

    [Test]
    public void MalformedNegotiationDoesNotReplacePriorDialogState()
    {
        SipInfoPackageFramework framework = RegisteredFramework(
            "alpha", new RecordingInfoPackage(),
            MediaType("application", "alpha"));
        SipMessage valid = InviteRequest();
        AddHeader(valid, SipHeaderNames.ReceiveInfo, "alpha");
        framework.UpdateInfoNegotiation(DialogId(), valid);
        SipMessage malformed = InviteRequest();
        AddHeader(malformed, SipHeaderNames.ReceiveInfo,
            "alpha;mode=one;MODE=two");

        Assert.That(() => framework.UpdateInfoNegotiation(
            DialogId(), malformed), Throws.TypeOf<ArgumentException>());
        SipInfoNegotiation retained = framework.GetInfoNegotiation(
            DialogId());

        Assert.Multiple(() =>
        {
            Assert.That(retained.RemoteSupportsInfoPackages, Is.True);
            Assert.That(retained.RemotePackages,
                Is.EqualTo(new string[] { "alpha" }));
            Assert.That(retained.CorrelationId,
                Is.EqualTo("dialog-call"));
        });
    }

    [Test]
    public void LegacyInfoWithoutHandlerIsAcceptedAndBodyIsDetached()
    {
        SipInfoPackageFramework framework = new SipInfoPackageFramework();
        SipMessage message = Parse(IncomingInfoText(
            "", "Content-Type: application/legacy\r\n", "legacy"));
        SipInfoOptions options = new SipInfoOptions();
        options.AllowLegacyInfo = true;
        options.LegacyContentTypes = new SipMediaType[]
        {
            MediaType("application", "legacy")
        };

        SipInfoResult result = framework.ReceiveInfo(
            DialogId(), message, options);
        message.Body[0] = (byte)'X';

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Legacy, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.CorrelationId, Is.EqualTo("dialog-call"));
            Assert.That(System.Text.Encoding.UTF8.GetString(
                result.Request!.Body), Is.EqualTo("legacy"));
            Assert.That(result.Response!.StatusCode, Is.EqualTo(200));
        });
    }

    [Test]
    public void NonInfoRequestReturnsMethodFailureWithAllowHeader()
    {
        SipInfoPackageFramework framework = new SipInfoPackageFramework();

        SipInfoResult result = framework.ReceiveInfo(
            DialogId(), InviteRequest(), new SipInfoOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(405));
            Assert.That(result.Response!.StatusCode, Is.EqualTo(405));
            Assert.That(Header(result.Response, SipHeaderNames.Allow),
                Is.EqualTo(SipMethods.Info));
        });
    }

    static SipInfoPackageFramework RegisteredFramework(
        string package, ISipInfoPackage handler, SipMediaType contentType)
    {
        SipInfoPackageFramework framework = new SipInfoPackageFramework();
        framework.RegisterInfoPackage(package, handler,
            new SipMediaType[] { contentType });
        return framework;
    }

    static SipInfoRequest InfoRequest(
        SipDialogId dialogId, string? package, string body)
    {
        SipInfoRequest request = new SipInfoRequest();
        request.DialogId = dialogId;
        request.Package = package;
        request.Body = System.Text.Encoding.UTF8.GetBytes(body);
        request.ContentType = package == null
            ? MediaType("application", "legacy")
            : MediaType("application", "alpha");
        return request;
    }

    static SipDialogSnapshot CreateDialog(SipDialogLayer layer)
    {
        SipMessage request = InviteRequest();
        SipMessage response = Parse("SIP/2.0 200 Dialog\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-info\r\n"
            + "From: Alice <sip:alice@example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=remote-tag\r\n"
            + "Call-ID: dialog-call\r\nCSeq: 10 INVITE\r\n"
            + "Contact: <sip:bob@device.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        return layer.CreateDialog(request, response, new SipDialogOptions());
    }

    static SipMessage InviteRequest()
    {
        return Parse(InviteRequestText());
    }

    static string InviteRequestText()
    {
        return "INVITE sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-info\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: dialog-call\r\nCSeq: 10 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n";
    }

    static string IncomingInfoText(
        string packageHeaders, string bodyHeaders, string body)
    {
        return "INFO sip:alice@client.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP server.example.com:5060;branch=z9hG4bK-peer-info\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Bob <sip:bob@example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@example.com>;tag=local-tag\r\n"
            + "Call-ID: dialog-call\r\nCSeq: 11 INFO\r\n"
            + packageHeaders + bodyHeaders + "Content-Length: "
            + System.Text.Encoding.UTF8.GetByteCount(body).ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + "\r\n\r\n" + body;
    }

    static SipDialogId DialogId()
    {
        SipDialogId id = new SipDialogId();
        id.CallId = "dialog-call";
        id.LocalTag = "local-tag";
        id.RemoteTag = "remote-tag";
        return id;
    }

    static SipMediaType MediaType(string type, string subtype)
    {
        SipMediaType mediaType = new SipMediaType();
        mediaType.Type = type;
        mediaType.Subtype = subtype;
        return mediaType;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static string? Header(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header =>
            string.Equals(header.Name, name,
                StringComparison.OrdinalIgnoreCase))?.Value;
    }

    static void AddHeader(SipMessage message, string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        message.Headers = message.Headers.Append(header).ToArray();
    }
}

public class RecordingInfoPackage : ISipInfoPackage
{
    public SipInfoRequest? LastRequest;
    public bool Accept = true;
    public int StatusCode = SipStatusCodes.Ok;

    public SipInfoResult ProcessInfo(SipInfoRequest request)
    {
        LastRequest = request;
        SipInfoResult result = new SipInfoResult();
        result.Accepted = Accept;
        result.StatusCode = StatusCode;
        if (!Accept)
        {
            result.ErrorMessage = "The application rejected the INFO data.";
        }
        return result;
    }
}
