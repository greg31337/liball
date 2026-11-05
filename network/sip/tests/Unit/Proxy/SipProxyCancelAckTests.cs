namespace sip.Tests;

public class SipProxyCancelAckTests
{
    [Test]
    public void MatchingCancelGetsOkAndCancelsEveryProceedingBranchOnce()
    {
        SipProxy proxy = new SipProxy();
        SipProxyRequest inviteRequest = InviteRequest();
        SipMessage invite = inviteRequest.Message!;
        SipProxyResult started = proxy.ProcessProxyRequest(inviteRequest);
        DrainEvents(proxy, 3);
        proxy.ProcessProxyResponse(ResponseFor(
            started.Requests[0], 180, "Ringing"));
        proxy.ProcessProxyResponse(ResponseFor(
            started.Requests[1], 183, "Session Progress"));
        DrainEvents(proxy, 2);

        SipProxyRequest cancelRequest = ControlRequest(
            CancelFor(invite), new SipProxyTarget[0]);
        SipProxyResult cancelled =
            proxy.ProcessProxyRequest(cancelRequest);
        SipProxyResult duplicate =
            proxy.ProcessProxyRequest(cancelRequest);

        SipProxyResult firstFinal = proxy.ProcessProxyResponse(
            ResponseFor(started.Requests[0], 487, "Request Terminated"));
        SipProxyResult secondFinal = proxy.ProcessProxyResponse(
            ResponseFor(started.Requests[1], 487, "Request Terminated"));

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Success, Is.True);
            Assert.That(cancelled.Response!.StatusCode, Is.EqualTo(200));
            Assert.That(cancelled.Response.CSeq!.Method,
                Is.EqualTo(SipMethod.Cancel));
            Assert.That(cancelled.SendRequest, Is.Not.Null);
            Assert.That(cancelled.Requests, Has.Length.EqualTo(2));
            Assert.That(cancelled.Requests.All(message =>
                message.Method == SipMethod.Cancel), Is.True);
            Assert.That(cancelled.Requests.Select(message =>
                message.ViaValues[0].Branch),
                Is.EquivalentTo(started.Requests.Select(message =>
                    message.ViaValues[0].Branch)));
            Assert.That(cancelled.SendRequests, Has.Length.EqualTo(2));
            Assert.That(duplicate.Response!.StatusCode, Is.EqualTo(200));
            Assert.That(duplicate.Requests, Is.Empty);
            Assert.That(firstFinal.Requests.Single().Method,
                Is.EqualTo(SipMethod.Ack));
            Assert.That(firstFinal.ActiveBranchCount, Is.EqualTo(1));
            Assert.That(secondFinal.StatusCode, Is.EqualTo(487));
            Assert.That(secondFinal.Completed, Is.True);
            Assert.That(secondFinal.Requests.Single().Method,
                Is.EqualTo(SipMethod.Ack));
        });
    }

    [Test]
    public void CancelBeforeProvisionalWaitsAndFinalRaceSuppressesBranchCancel()
    {
        SipProxy proxy = new SipProxy();
        SipProxyRequest inviteRequest = InviteRequest();
        SipMessage invite = inviteRequest.Message!;
        inviteRequest.Targets = new SipProxyTarget[] {
            Target("sip:bob@one.example.com", "192.0.2.1") };
        SipProxyResult started = proxy.ProcessProxyRequest(inviteRequest);
        DrainEvents(proxy, 2);

        SipProxyResult pending = proxy.ProcessProxyRequest(
            ControlRequest(CancelFor(invite), new SipProxyTarget[0]));
        SipProxyResult provisional = proxy.ProcessProxyResponse(
            ResponseFor(started.Request!, 180, "Ringing"));
        SipProxyResult final = proxy.ProcessProxyResponse(
            ResponseFor(started.Request!, 487, "Request Terminated"));

        SipProxy lateRaceProxy = new SipProxy();
        SipProxyRequest racedInvite = InviteRequest();
        racedInvite.Targets = new SipProxyTarget[] {
            Target("sip:bob@one.example.com", "192.0.2.1") };
        SipMessage racedMessage = racedInvite.Message!;
        SipProxyResult racedStart =
            lateRaceProxy.ProcessProxyRequest(racedInvite);
        SipProxyResult racedFinal = lateRaceProxy.ProcessProxyResponse(
            ResponseFor(racedStart.Request!, 486, "Busy Here"));
        SipProxyResult lateCancel = lateRaceProxy.ProcessProxyRequest(
            ControlRequest(CancelFor(racedMessage),
                new SipProxyTarget[0]));

        Assert.Multiple(() =>
        {
            Assert.That(pending.Response!.StatusCode, Is.EqualTo(200));
            Assert.That(pending.Requests, Is.Empty);
            Assert.That(provisional.Requests.Single().Method,
                Is.EqualTo(SipMethod.Cancel));
            Assert.That(final.Completed, Is.True);
            Assert.That(final.StatusCode, Is.EqualTo(487));
            Assert.That(racedFinal.Requests.Single().Method,
                Is.EqualTo(SipMethod.Ack));
            Assert.That(lateCancel.Response!.StatusCode, Is.EqualTo(200));
            Assert.That(lateCancel.Requests, Is.Empty);
        });
    }

    [Test]
    public void FailureAckIsAbsorbedWhileSuccessAckIsForwardedStatelessly()
    {
        SipProxy failureProxy = new SipProxy();
        SipProxyRequest failureInvite = InviteRequest();
        failureInvite.Targets = new SipProxyTarget[] {
            Target("sip:bob@one.example.com", "192.0.2.1") };
        SipMessage failureSource = failureInvite.Message!;
        SipProxyResult failureStart =
            failureProxy.ProcessProxyRequest(failureInvite);
        DrainEvents(failureProxy, 2);
        SipProxyResult failure = failureProxy.ProcessProxyResponse(
            ResponseFor(failureStart.Request!, 486, "Busy Here"));
        DrainEvents(failureProxy, 2);
        SipProxyResult absorbed = failureProxy.ProcessProxyRequest(
            ControlRequest(AckFor(failureSource,
                "sip:bob@one.example.com"),
                new SipProxyTarget[0]));
        SipProxyEvent absorbedEvent = failureProxy.PollProxyEvent();

        SipProxy successProxy = new SipProxy();
        SipProxyRequest successInvite = InviteRequest();
        successInvite.Targets = new SipProxyTarget[] {
            Target("sip:bob@one.example.com", "192.0.2.1") };
        SipMessage successSource = successInvite.Message!;
        SipProxyResult successStart =
            successProxy.ProcessProxyRequest(successInvite);
        successProxy.ProcessProxyResponse(
            ResponseFor(successStart.Request!, 200, "OK"));
        DrainEvents(successProxy, 4);
        SipProxyTarget ackTarget =
            Target("sip:bob@one.example.com", "192.0.2.1");
        SipProxyResult forwarded = successProxy.ProcessProxyRequest(
            ControlRequest(AckFor(successSource,
                "sip:bob@one.example.com"),
                new SipProxyTarget[] { ackTarget }));
        SipProxyEvent forwardedEvent = successProxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(failure.Requests.Single().Method,
                Is.EqualTo(SipMethod.Ack));
            Assert.That(absorbed.Success, Is.True);
            Assert.That(absorbed.Request, Is.Null);
            Assert.That(absorbedEvent.Kind,
                Is.EqualTo(SipProxyEventKind.AckAbsorbed));
            Assert.That(forwarded.Success, Is.True,
                forwarded.ErrorMessage);
            Assert.That(forwarded.Request!.Method,
                Is.EqualTo(SipMethod.Ack));
            Assert.That(forwarded.Request.ViaValues, Has.Length.EqualTo(2));
            Assert.That(forwarded.Completed, Is.True);
            Assert.That(forwardedEvent.Kind,
                Is.EqualTo(SipProxyEventKind.AckForwarded));
        });
    }

    [Test]
    public void FirstSuccessCancelsProceedingSiblingAndLateSuccessIsForwarded()
    {
        SipProxy proxy = new SipProxy();
        SipProxyResult started = proxy.ProcessProxyRequest(InviteRequest());
        DrainEvents(proxy, 3);
        proxy.ProcessProxyResponse(ResponseFor(
            started.Requests[1], 180, "Ringing"));
        DrainEvents(proxy, 1);

        SipProxyResult first = proxy.ProcessProxyResponse(
            ResponseFor(started.Requests[0], 200, "OK"));
        SipProxyEvent cancelEvent = proxy.PollProxyEvent();
        SipProxyEvent selectedEvent = proxy.PollProxyEvent();
        SipProxyEvent completedEvent = proxy.PollProxyEvent();
        SipProxyResult additional = proxy.ProcessProxyResponse(
            ResponseFor(started.Requests[1], 200, "OK"));
        SipProxyEvent additionalEvent = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(first.Completed, Is.True);
            Assert.That(first.Response!.StatusCode, Is.EqualTo(200));
            Assert.That(first.Requests.Single().Method,
                Is.EqualTo(SipMethod.Cancel));
            Assert.That(cancelEvent.Kind,
                Is.EqualTo(SipProxyEventKind.CancelForwarded));
            Assert.That(selectedEvent.Kind,
                Is.EqualTo(SipProxyEventKind.FinalSelected));
            Assert.That(completedEvent.Kind,
                Is.EqualTo(SipProxyEventKind.Completed));
            Assert.That(additional.Success, Is.True);
            Assert.That(additional.Response!.StatusCode, Is.EqualTo(200));
            Assert.That(additional.SendRequest, Is.Not.Null);
            Assert.That(additionalEvent.Kind,
                Is.EqualTo(SipProxyEventKind.AdditionalSuccessForwarded));
        });
    }

    [Test]
    public void CancellationCommandUsesCorrelationAndDoesNotRepeatCancel()
    {
        SipProxy proxy = new SipProxy();
        SipProxyRequest invite = InviteRequest();
        invite.Targets = new SipProxyTarget[] {
            Target("sip:bob@one.example.com", "192.0.2.1") };
        SipProxyResult started = proxy.ProcessProxyRequest(invite);
        DrainEvents(proxy, 2);
        proxy.ProcessProxyResponse(ResponseFor(
            started.Request!, 180, "Ringing"));
        DrainEvents(proxy, 1);
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Id = "cancel-correlation";

        proxy.CancelProxyBranches(cancellation);
        SipProxyEvent forwarded = proxy.PollProxyEvent();
        SipProxyEvent accepted = proxy.PollProxyEvent();
        proxy.CancelProxyBranches(cancellation);
        SipProxyEvent none = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(forwarded.Kind,
                Is.EqualTo(SipProxyEventKind.CancelForwarded));
            Assert.That(forwarded.Result!.Requests.Single().Method,
                Is.EqualTo(SipMethod.Cancel));
            Assert.That(forwarded.Result.SendRequests, Has.Length.EqualTo(1));
            Assert.That(accepted.Kind,
                Is.EqualTo(SipProxyEventKind.RequestAccepted));
            Assert.That(none.Kind, Is.EqualTo(SipProxyEventKind.None));
        });
    }

    static SipProxyRequest InviteRequest()
    {
        SipMessage invite = Parse(
            "INVITE sip:bob@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;"
            + "branch=z9hG4bK-upstream;rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: cancel-proxy-call@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        SipProxyRequest request = ControlRequest(invite,
            new SipProxyTarget[] {
                Target("sip:bob@one.example.com", "192.0.2.1"),
                Target("sip:bob@two.example.com", "192.0.2.2") });
        request.Options.Mode = SipProxyMode.StatefulParallel;
        request.Options.TimerCMilliseconds = 5000;
        return request;
    }

    static SipProxyRequest ControlRequest(SipMessage message,
        SipProxyTarget[] targets)
    {
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = message;
        transport.Transport = SipTransportKind.Udp;
        transport.LocalAddress = Endpoint("proxy.example.com", 5060);
        transport.RemoteAddress = Endpoint("198.51.100.2", 62000);
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Targets = targets;
        request.CorrelationId = "cancel-correlation";
        return request;
    }

    static SipProxyTarget Target(string uri, string host)
    {
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = host;
        candidate.Port = 5060;
        candidate.Transport = SipTransportKind.Udp;
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = ParseUri(uri);
        target.Candidate = candidate;
        return target;
    }

    static SipMessage CancelFor(SipMessage invite)
    {
        return Parse("CANCEL " + UriText(invite.RequestUri!) + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;"
            + "branch=z9hG4bK-upstream;rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: cancel-proxy-call@example.com\r\n"
            + "CSeq: 1 CANCEL\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage AckFor(SipMessage invite, string requestUri)
    {
        return Parse("ACK " + requestUri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;"
            + "branch=z9hG4bK-upstream;rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=to-tag\r\n"
            + "Call-ID: cancel-proxy-call@example.com\r\n"
            + "CSeq: 1 ACK\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage ResponseFor(SipMessage request,
        int status, string reason)
    {
        string[] vias = request.Headers.Where(header =>
            header.Name!.Equals(SipHeaderNames.Via,
                System.StringComparison.OrdinalIgnoreCase)
            || header.Name.Equals(SipHeaderNames.CompactVia,
                System.StringComparison.OrdinalIgnoreCase))
            .Select(header => "Via: " + header.Value + "\r\n").ToArray();
        return Parse("SIP/2.0 " + status + " " + reason + "\r\n"
            + string.Concat(vias)
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=to-tag\r\n"
            + "Call-ID: cancel-proxy-call@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:bob@service.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static string UriText(SipUri uri)
    {
        return "sip:" + (uri.User == null ? "" : uri.User + "@")
            + uri.Host;
    }

    static SipEndpointAddress Endpoint(string host, int port)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = SipTransportKind.Udp;
        return endpoint;
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

    static void DrainEvents(SipProxy proxy, int count)
    {
        int index = 0;
        while (index < count)
        {
            Assert.That(proxy.PollProxyEvent().Kind,
                Is.Not.EqualTo(SipProxyEventKind.None));
            index = index + 1;
        }
    }
}
