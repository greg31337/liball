namespace sip.Tests;

public class SipReferUsageTests
{
    [Test]
    public void OutOfDialogReferCreatesMandatoryHeaders()
    {
        SipReferClient client = new SipReferClient();
        SipReferRequest request = Request();
        request.CorrelationId = "transfer-one";

        SipReferEvent sent = client.SendRefer(request);

        Assert.Multiple(() =>
        {
            Assert.That(sent.State, Is.EqualTo(SipReferState.Sending));
            Assert.That(sent.Message!.Method, Is.EqualTo(SipMethod.Refer));
            Assert.That(Header(sent.Message, SipHeaderNames.ReferTo),
                Does.Contain("carol@example.net"));
            Assert.That(sent.Message.ContactValues, Has.Length.EqualTo(1));
            Assert.That(sent.Message.ContactValues[0].Address!.Uri!.Host,
                Is.EqualTo("device.example.com"));
            Assert.That(sent.Message.CSeq!.SequenceNumber, Is.EqualTo(1));
            Assert.That(sent.SubscriptionId,
                Does.EndWith(":1"));
            Assert.That(sent.CorrelationId, Is.EqualTo("transfer-one"));
        });
    }

    [Test]
    public void ImplicitSubscriptionSupportsNotifyBeforeReferResponseAndFinalProgress()
    {
        SipReferClient client = new SipReferClient();
        SipReferEvent sent = client.SendRefer(Request());
        SipReferServer server = new SipReferServer();
        SipReferEvent accepted = server.ReceiveRefer(
            null, sent.Message!, new SipReferServerOptions());

        SipReferEvent pending = client.ReceiveReferNotification(
            accepted.Notification!);
        SipReferEvent response = client.ReceiveReferResponse(
            accepted.Response!);
        SipReferProgress progress = new SipReferProgress();
        progress.SubscriptionId = accepted.SubscriptionId;
        progress.StatusCode = SipStatusCodes.Ok;
        progress.ReasonPhrase = SipReasonPhrases.Ok;
        progress.Final = true;
        SipReferEvent completed = server.ReportReferProgress(progress);
        SipReferEvent succeeded = client.ReceiveReferNotification(
            completed.Notification!);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.State, Is.EqualTo(SipReferState.Pending));
            Assert.That(accepted.Response!.StatusCode, Is.EqualTo(202));
            Assert.That(pending.State, Is.EqualTo(SipReferState.Pending));
            Assert.That(pending.StatusCode, Is.EqualTo(100));
            Assert.That(pending.Response!.StatusCode, Is.EqualTo(200));
            Assert.That(response.State, Is.EqualTo(SipReferState.Accepted));
            Assert.That(succeeded.State, Is.EqualTo(SipReferState.Succeeded));
            Assert.That(succeeded.Success, Is.True);
            Assert.That(succeeded.Final, Is.True);
            Assert.That(Header(completed.Notification!,
                SipHeaderNames.SubscriptionState),
                Is.EqualTo("terminated;reason=noresource"));
            Assert.That(System.Text.Encoding.UTF8.GetString(
                completed.Notification!.Body), Does.StartWith("SIP/2.0 200"));
        });
    }

    [Test]
    public void FailedReferredOperationProducesFinalFailureEvent()
    {
        SipReferClient client = new SipReferClient();
        SipReferEvent sent = client.SendRefer(Request());
        SipReferServer server = new SipReferServer();
        SipReferEvent accepted = server.ReceiveRefer(
            null, sent.Message!, new SipReferServerOptions());
        client.ReceiveReferResponse(accepted.Response!);
        SipReferProgress progress = new SipReferProgress();
        progress.SubscriptionId = accepted.SubscriptionId;
        progress.StatusCode = 486;
        progress.ReasonPhrase = "Busy Here";
        progress.Final = true;

        SipReferEvent notification = server.ReportReferProgress(progress);
        SipReferEvent failed = client.ReceiveReferNotification(
            notification.Notification!);

        Assert.Multiple(() =>
        {
            Assert.That(notification.State, Is.EqualTo(SipReferState.Failed));
            Assert.That(failed.State, Is.EqualTo(SipReferState.Failed));
            Assert.That(failed.StatusCode, Is.EqualTo(486));
            Assert.That(failed.Final, Is.True);
            Assert.That(failed.Success, Is.False);
        });
    }

    [Test]
    public void ReferSubFalseSuppressesImplicitSubscriptionWhenGranted()
    {
        SipReferRequest request = Request();
        request.NoSubscription = true;
        request.RequireNoSubscription = true;
        SipReferClient client = new SipReferClient();
        SipReferEvent sent = client.SendRefer(request);
        SipReferServer server = new SipReferServer();

        SipReferEvent accepted = server.ReceiveRefer(
            null, sent.Message!, new SipReferServerOptions());
        SipReferEvent completed = client.ReceiveReferResponse(
            accepted.Response!);

        Assert.Multiple(() =>
        {
            Assert.That(Header(sent.Message!, SipHeaderNames.ReferSub),
                Is.EqualTo("false"));
            Assert.That(Header(sent.Message!, SipHeaderNames.Require),
                Is.EqualTo(SipOptionTags.NoReferSub));
            Assert.That(accepted.NoSubscription, Is.True);
            Assert.That(accepted.Notification, Is.Null);
            Assert.That(Header(accepted.Response!, SipHeaderNames.ReferSub),
                Is.EqualTo("false"));
            Assert.That(completed.State, Is.EqualTo(SipReferState.Succeeded));
            Assert.That(completed.Final, Is.True);
            Assert.That(completed.NoSubscription, Is.True);
        });
    }

    [Test]
    public void RequiredNoReferSubIsRejectedWhenUnsupported()
    {
        SipReferRequest request = Request();
        request.NoSubscription = true;
        request.RequireNoSubscription = true;
        SipReferEvent sent = new SipReferClient().SendRefer(request);
        SipReferServerOptions options = new SipReferServerOptions();
        options.AllowNoSubscription = false;

        SipReferEvent rejected = new SipReferServer().ReceiveRefer(
            null, sent.Message!, options);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.State, Is.EqualTo(SipReferState.Failed));
            Assert.That(rejected.StatusCode, Is.EqualTo(420));
            Assert.That(Header(rejected.Response!, SipHeaderNames.Unsupported),
                Is.EqualTo(SipOptionTags.NoReferSub));
        });
    }

    [Test]
    public void InDialogReferUsesDialogRoutingAndServerAssociation()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer);
        SipReferClient client = new SipReferClient();
        client.ConfigureReferDialogLayer(layer);
        SipReferRequest request = Request();
        request.DialogId = dialog.Id;
        request.Target = null;
        request.Sender = null;
        SipReferEvent sent = client.SendRefer(request);
        SipDialogId serverDialog = ReverseDialog(dialog.Id!);

        SipReferEvent accepted = new SipReferServer().ReceiveRefer(
            serverDialog, sent.Message!, new SipReferServerOptions());

        Assert.Multiple(() =>
        {
            Assert.That(sent.Message!.CSeq!.SequenceNumber, Is.EqualTo(11));
            Assert.That(sent.Message.RequestUri!.Host,
                Is.EqualTo("device.example.com"));
            Assert.That(Header(sent.Message, SipHeaderNames.CallId),
                Is.EqualTo("refer-dialog"));
            Assert.That(accepted.Response!.StatusCode, Is.EqualTo(202));
            Assert.That(accepted.DialogId!.LocalTag,
                Is.EqualTo("remote-tag"));
            Assert.That(accepted.Notification, Is.Not.Null);
        });
    }

    [Test]
    public void ClientAndServerValidateReferBodyTargetAndDialogPolicy()
    {
        SipReferRequest tooLarge = Request();
        tooLarge.Body = System.Text.Encoding.UTF8.GetBytes("body");
        tooLarge.ContentType = MediaType("text", "plain");
        tooLarge.Options.MaxBodyBytes = 3;
        Assert.That(() => new SipReferClient().SendRefer(tooLarge),
            Throws.TypeOf<ArgumentException>());

        SipReferEvent sent = new SipReferClient().SendRefer(Request());
        RemoveHeader(sent.Message!, SipHeaderNames.ReferTo);
        SipReferEvent malformed = new SipReferServer().ReceiveRefer(
            null, sent.Message!, new SipReferServerOptions());

        SipReferEvent valid = new SipReferClient().SendRefer(Request());
        SipReferServerOptions schemePolicy = new SipReferServerOptions();
        schemePolicy.AllowedReferToSchemes = new string[] { "https" };
        SipReferEvent forbidden = new SipReferServer().ReceiveRefer(
            null, valid.Message!, schemePolicy);

        SipReferServerOptions outsidePolicy = new SipReferServerOptions();
        outsidePolicy.AllowOutOfDialog = false;
        SipReferEvent outside = new SipReferServer().ReceiveRefer(
            null, valid.Message!, outsidePolicy);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.StatusCode, Is.EqualTo(400));
            Assert.That(forbidden.StatusCode, Is.EqualTo(403));
            Assert.That(outside.StatusCode, Is.EqualTo(481));
        });
    }

    [Test]
    public void AuthenticationChallengeCreatesBoundedReferRetry()
    {
        RecordingMessageAuthenticationClient clientAuthentication =
            new RecordingMessageAuthenticationClient();
        SipReferClient client = new SipReferClient();
        client.ConfigureReferAuthenticationClient(clientAuthentication);
        SipReferRequest request = Request();
        request.AuthenticationContextId = "refer-auth";
        SipReferEvent sent = client.SendRefer(request);

        RecordingMessageAuthenticationServer serverAuthentication =
            new RecordingMessageAuthenticationServer();
        SipReferServer server = new SipReferServer();
        server.ConfigureReferAuthenticationServer(
            serverAuthentication, new SipNonceOptions());
        SipReferServerOptions options = new SipReferServerOptions();
        options.RequireAuthentication = true;
        SipReferEvent challenged = server.ReceiveRefer(
            null, sent.Message!, options);

        SipReferEvent retried = client.ReceiveReferResponse(
            challenged.Response!);
        SipReferEvent accepted = server.ReceiveRefer(
            null, retried.Message!, options);

        Assert.Multiple(() =>
        {
            Assert.That(challenged.StatusCode, Is.EqualTo(401));
            Assert.That(retried.State, Is.EqualTo(SipReferState.Sending));
            Assert.That(retried.Message!.CSeq!.SequenceNumber, Is.EqualTo(2));
            Assert.That(Header(retried.Message, SipHeaderNames.Authorization),
                Is.EqualTo("Digest test-response"));
            Assert.That(clientAuthentication.AuthorizationCorrelation,
                Is.EqualTo("refer-auth"));
            Assert.That(accepted.StatusCode, Is.EqualTo(202));
            Assert.That(accepted.SubscriptionId,
                Is.EqualTo(retried.SubscriptionId));
        });
    }

    [Test]
    public void InvalidSipfragNotificationIsRejectedWithoutLosingSubscription()
    {
        SipReferClient client = new SipReferClient();
        SipReferEvent sent = client.SendRefer(Request());
        SipReferServer server = new SipReferServer();
        SipReferEvent accepted = server.ReceiveRefer(
            null, sent.Message!, new SipReferServerOptions());
        SipMessage invalid = SipSnapshotCopy.CopyMessageModel(
            accepted.Notification)!;
        invalid.Body = System.Text.Encoding.UTF8.GetBytes("not a status line");
        SetHeader(invalid, SipHeaderNames.ContentLength,
            invalid.Body.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture));

        Assert.That(() => client.ReceiveReferNotification(invalid),
            Throws.TypeOf<ArgumentException>());

        SipReferEvent pending = client.ReceiveReferNotification(
            accepted.Notification!);
        Assert.That(pending.State, Is.EqualTo(SipReferState.Pending));
    }

    [Test]
    public void FinalNotifyBeforeReferResponseCompletesRaceAndTerminatesContext()
    {
        SipReferClient client = new SipReferClient();
        SipReferEvent sent = client.SendRefer(Request());
        SipReferServer server = new SipReferServer();
        SipReferEvent accepted = server.ReceiveRefer(
            null, sent.Message!, new SipReferServerOptions());
        SipReferProgress progress = new SipReferProgress();
        progress.SubscriptionId = accepted.SubscriptionId;
        progress.StatusCode = SipStatusCodes.Ok;
        progress.Final = true;
        SipReferEvent notification = server.ReportReferProgress(progress);

        SipReferEvent notified = client.ReceiveReferNotification(
            notification.Notification!);
        SipReferEvent responded = client.ReceiveReferResponse(
            accepted.Response!);

        Assert.Multiple(() =>
        {
            Assert.That(notified.State, Is.EqualTo(SipReferState.Succeeded));
            Assert.That(notified.Final, Is.True);
            Assert.That(responded.State, Is.EqualTo(SipReferState.Succeeded));
            Assert.That(responded.Final, Is.True);
            Assert.That(() => client.ReceiveReferNotification(
                notification.Notification!),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => server.ReportReferProgress(progress),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void PreferredNoReferSubFallsBackToImplicitSubscription()
    {
        SipReferRequest request = Request();
        request.NoSubscription = true;
        SipReferClient client = new SipReferClient();
        SipReferEvent sent = client.SendRefer(request);
        SipReferServerOptions options = new SipReferServerOptions();
        options.AllowNoSubscription = false;
        SipReferEvent accepted = new SipReferServer().ReceiveRefer(
            null, sent.Message!, options);

        SipReferEvent response = client.ReceiveReferResponse(
            accepted.Response!);

        Assert.Multiple(() =>
        {
            Assert.That(Header(sent.Message!, SipHeaderNames.Supported),
                Is.EqualTo(SipOptionTags.NoReferSub));
            Assert.That(Header(sent.Message!, SipHeaderNames.Require),
                Is.Null);
            Assert.That(accepted.NoSubscription, Is.False);
            Assert.That(accepted.Notification, Is.Not.Null);
            Assert.That(Header(accepted.Response!, SipHeaderNames.ReferSub),
                Is.Null);
            Assert.That(response.State, Is.EqualTo(SipReferState.Accepted));
            Assert.That(response.Final, Is.False);
        });
    }

    [Test]
    public void ProgressTransitionsActiveThenTerminatesOnFinalStatus()
    {
        SipReferClient client = new SipReferClient();
        SipReferEvent sent = client.SendRefer(Request());
        SipReferServer server = new SipReferServer();
        SipReferEvent accepted = server.ReceiveRefer(
            null, sent.Message!, new SipReferServerOptions());
        client.ReceiveReferResponse(accepted.Response!);
        SipReferProgress ringing = new SipReferProgress();
        ringing.SubscriptionId = accepted.SubscriptionId;
        ringing.StatusCode = 180;
        ringing.ReasonPhrase = "Ringing";
        ringing.ExpiresSeconds = 30;
        SipReferEvent activeNotification =
            server.ReportReferProgress(ringing);
        SipReferEvent active = client.ReceiveReferNotification(
            activeNotification.Notification!);
        SipReferProgress completed = new SipReferProgress();
        completed.SubscriptionId = accepted.SubscriptionId;
        completed.StatusCode = SipStatusCodes.Accepted;
        completed.ReasonPhrase = "Accepted";
        SipReferEvent finalNotification =
            server.ReportReferProgress(completed);
        SipReferEvent final = client.ReceiveReferNotification(
            finalNotification.Notification!);

        Assert.Multiple(() =>
        {
            Assert.That(active.State, Is.EqualTo(SipReferState.Active));
            Assert.That(active.StatusCode, Is.EqualTo(180));
            Assert.That(active.Final, Is.False);
            Assert.That(Header(activeNotification.Notification!,
                SipHeaderNames.SubscriptionState),
                Is.EqualTo("active;expires=30"));
            Assert.That(final.State, Is.EqualTo(SipReferState.Succeeded));
            Assert.That(final.StatusCode, Is.EqualTo(202));
            Assert.That(final.Final, Is.True);
            Assert.That(finalNotification.Notification!.CSeq!.SequenceNumber,
                Is.GreaterThan(activeNotification.Notification!.CSeq!
                    .SequenceNumber));
            Assert.That(() => server.ReportReferProgress(completed),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void ProvisionalResponseKeepsReferUntilFinalRejection()
    {
        SipReferClient client = new SipReferClient();
        SipReferEvent sent = client.SendRefer(Request());

        SipReferEvent provisional = client.ReceiveReferResponse(
            Response(sent.Message!, 180, "Ringing"));
        SipReferEvent failed = client.ReceiveReferResponse(
            Response(sent.Message!, SipStatusCodes.Decline, "Decline"));

        Assert.Multiple(() =>
        {
            Assert.That(provisional.State, Is.EqualTo(SipReferState.Sending));
            Assert.That(provisional.StatusCode, Is.EqualTo(180));
            Assert.That(provisional.Final, Is.False);
            Assert.That(failed.State, Is.EqualTo(SipReferState.Failed));
            Assert.That(failed.StatusCode, Is.EqualTo(603));
            Assert.That(failed.Final, Is.True);
            Assert.That(() => client.ReceiveReferResponse(
                Response(sent.Message!, SipStatusCodes.Decline, "Decline")),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void UnconfiguredProtectedReferQueuesSingleServerFailure()
    {
        SipReferEvent sent = new SipReferClient().SendRefer(Request());
        SipReferServer server = new SipReferServer();
        SipReferServerOptions options = new SipReferServerOptions();
        options.RequireAuthentication = true;

        SipReferEvent failed = server.ReceiveRefer(
            null, sent.Message!, options);
        SipReferEvent queued = server.PollReceivedReferEvent();
        SipReferEvent empty = server.PollReceivedReferEvent();

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipReferState.Failed));
            Assert.That(failed.StatusCode, Is.EqualTo(500));
            Assert.That(failed.ErrorMessage, Does.Contain("not configured"));
            Assert.That(queued.StatusCode, Is.EqualTo(500));
            Assert.That(empty.State, Is.EqualTo(SipReferState.None));
        });
    }

    static SipReferRequest Request()
    {
        SipReferRequest request = new SipReferRequest();
        request.Target = Uri("sip:bob@example.com");
        request.Sender = Uri("sip:alice@example.com");
        request.Contact = Uri("sip:alice@device.example.com");
        request.ReferTo = new SipNameAddress();
        request.ReferTo.Uri = Uri("sip:carol@example.net");
        return request;
    }

    static SipDialogSnapshot CreateDialog(SipDialogLayer layer)
    {
        SipMessage invite = Parse("INVITE sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-refer\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=local-tag\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: refer-dialog\r\nCSeq: 10 INVITE\r\n"
            + "Contact: <sip:alice@device.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        SipMessage response = Parse("SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-refer\r\n"
            + "From: <sip:alice@example.com>;tag=local-tag\r\n"
            + "To: <sip:bob@example.com>;tag=remote-tag\r\n"
            + "Call-ID: refer-dialog\r\nCSeq: 10 INVITE\r\n"
            + "Contact: <sip:bob@device.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        return layer.CreateDialog(invite, response, new SipDialogOptions());
    }

    static SipDialogId ReverseDialog(SipDialogId source)
    {
        SipDialogId result = new SipDialogId();
        result.CallId = source.CallId;
        result.LocalTag = source.RemoteTag;
        result.RemoteTag = source.LocalTag;
        return result;
    }

    static SipUri Uri(string text)
    {
        return new SipUriCodec().ParseUri(
            System.Text.Encoding.UTF8.GetBytes(text),
            new SipParseOptions()).Uri!;
    }

    static SipMediaType MediaType(string type, string subtype)
    {
        SipMediaType value = new SipMediaType();
        value.Type = type;
        value.Subtype = subtype;
        return value;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.UTF8.GetBytes(text),
            new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }

    static SipMessage Response(
        SipMessage request, int statusCode, string reasonPhrase)
    {
        return SipTransactionMessageFactory.CreateRequestResponse(
            request, statusCode, reasonPhrase, "remote-tag");
    }

    static string? Header(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header =>
            string.Equals(header.Name, name,
                StringComparison.OrdinalIgnoreCase))?.Value;
    }

    static void RemoveHeader(SipMessage message, string name)
    {
        message.Headers = message.Headers.Where(header =>
            !string.Equals(header.Name, name,
                StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    static void SetHeader(SipMessage message, string name, string value)
    {
        SipHeader header = message.Headers.First(item =>
            string.Equals(item.Name, name,
                StringComparison.OrdinalIgnoreCase));
        header.Value = value;
    }
}
