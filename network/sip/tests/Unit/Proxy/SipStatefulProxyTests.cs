namespace sip.Tests;

public class SipStatefulProxyTests
{
    [Test]
    public void StartsBranchesResetsTimerCAndCompletesAfterTimeouts()
    {
        SipProxy proxy = new SipProxy();
        SipProxyRequest request = StatefulRequest(1000);

        SipProxyResult started = proxy.ProcessProxyRequest(request);
        DrainEvents(proxy, 3);
        proxy.AdvanceProxyTime(200);
        SipProxyResult provisional = proxy.ProcessProxyResponse(
            ResponseFor(started.Requests[0], 180, "Ringing"));
        SipProxyEvent provisionalEvent = proxy.PollProxyEvent();

        proxy.AdvanceProxyTime(1000);
        SipProxyEvent firstTimeout = proxy.PollProxyEvent();
        proxy.AdvanceProxyTime(1199);
        SipProxyEvent none = proxy.PollProxyEvent();
        proxy.AdvanceProxyTime(1200);
        SipProxyEvent secondTimeout = proxy.PollProxyEvent();
        SipProxyEvent selected = proxy.PollProxyEvent();
        SipProxyEvent completed = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(started.Success, Is.True, started.ErrorMessage);
            Assert.That(started.Requests, Has.Length.EqualTo(2));
            Assert.That(started.SendRequests, Has.Length.EqualTo(2));
            Assert.That(started.Targets, Has.Length.EqualTo(2));
            Assert.That(started.ActiveBranchCount, Is.EqualTo(2));
            Assert.That(started.TimerCDeadlines,
                Is.EqualTo(new long[] { 1000, 1000 }));
            Assert.That(provisional.Response!.StatusCode, Is.EqualTo(180));
            Assert.That(provisional.SendRequest, Is.Not.Null);
            Assert.That(provisional.TimerCDeadlines,
                Is.EquivalentTo(new long[] { 1200, 1000 }));
            Assert.That(provisionalEvent.Kind,
                Is.EqualTo(SipProxyEventKind.ProvisionalForwarded));
            Assert.That(firstTimeout.Kind,
                Is.EqualTo(SipProxyEventKind.BranchTimedOut));
            Assert.That(firstTimeout.Result!.ActiveBranchCount, Is.EqualTo(1));
            Assert.That(none.Kind, Is.EqualTo(SipProxyEventKind.None));
            Assert.That(secondTimeout.Kind,
                Is.EqualTo(SipProxyEventKind.BranchTimedOut));
            Assert.That(selected.Kind,
                Is.EqualTo(SipProxyEventKind.FinalSelected));
            Assert.That(selected.Result!.StatusCode,
                Is.EqualTo(SipStatusCodes.RequestTimeout));
            Assert.That(selected.Result.TimedOut, Is.True);
            Assert.That(completed.Kind,
                Is.EqualTo(SipProxyEventKind.Completed));
            Assert.That(completed.Result!.ActiveBranchCount, Is.Zero);
            Assert.That(completed.Result.Completed, Is.True);
        });
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            proxy.AdvanceProxyTime(1199));
    }

    [Test]
    public void CollectsFinalResponsesSelectsBestAndIgnoresLateResponse()
    {
        SipProxy proxy = new SipProxy();
        SipProxyResult started = proxy.ProcessProxyRequest(
            StatefulRequest(5000));
        DrainEvents(proxy, 3);
        SipMessage firstFinal = ResponseFor(
            started.Requests[0], 503, "Service Unavailable");

        SipProxyResult collected = proxy.ProcessProxyResponse(firstFinal);
        SipProxyResult selected = proxy.ProcessProxyResponse(
            ResponseFor(started.Requests[1], 486, "Busy Here"));
        SipProxyEvent selectedEvent = proxy.PollProxyEvent();
        SipProxyEvent completedEvent = proxy.PollProxyEvent();
        SipProxyResult late = proxy.ProcessProxyResponse(firstFinal);
        SipProxyEvent lateEvent = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(collected.Success, Is.True);
            Assert.That(collected.Response, Is.Null);
            Assert.That(collected.ActiveBranchCount, Is.EqualTo(1));
            Assert.That(selected.Success, Is.True);
            Assert.That(selected.Response!.StatusCode, Is.EqualTo(486));
            Assert.That(selected.SendRequest, Is.Not.Null);
            Assert.That(selected.Targets, Has.Length.EqualTo(2));
            Assert.That(selected.Completed, Is.True);
            Assert.That(selected.ActiveBranchCount, Is.Zero);
            Assert.That(selectedEvent.Kind,
                Is.EqualTo(SipProxyEventKind.FinalSelected));
            Assert.That(completedEvent.Kind,
                Is.EqualTo(SipProxyEventKind.Completed));
            Assert.That(late.Success, Is.True);
            Assert.That(late.Response, Is.Null);
            Assert.That(late.Completed, Is.True);
            Assert.That(lateEvent.Kind,
                Is.EqualTo(SipProxyEventKind.LateResponseIgnored));
        });
    }

    [Test]
    public void GlobalFailureCompletesImmediatelyAndOtherBranchIsLate()
    {
        SipProxy proxy = new SipProxy();
        SipProxyResult started = proxy.ProcessProxyRequest(
            StatefulRequest(5000));
        DrainEvents(proxy, 3);

        SipProxyResult globalFailure = proxy.ProcessProxyResponse(
            ResponseFor(started.Requests[0], 603, "Decline"));
        SipProxyEvent selected = proxy.PollProxyEvent();
        SipProxyEvent completed = proxy.PollProxyEvent();
        SipProxyResult late = proxy.ProcessProxyResponse(
            ResponseFor(started.Requests[1], 200, "OK"));
        SipProxyEvent lateEvent = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(globalFailure.Success, Is.True);
            Assert.That(globalFailure.StatusCode, Is.EqualTo(603));
            Assert.That(globalFailure.Response!.StatusCode, Is.EqualTo(603));
            Assert.That(globalFailure.Completed, Is.True);
            Assert.That(globalFailure.ActiveBranchCount, Is.Zero);
            Assert.That(selected.Kind,
                Is.EqualTo(SipProxyEventKind.FinalSelected));
            Assert.That(completed.Kind,
                Is.EqualTo(SipProxyEventKind.Completed));
            Assert.That(late.Success, Is.True);
            Assert.That(late.Response!.StatusCode, Is.EqualTo(200));
            Assert.That(late.Completed, Is.True);
            Assert.That(lateEvent.Kind,
                Is.EqualTo(SipProxyEventKind.AdditionalSuccessForwarded));
        });
    }

    static SipProxyRequest StatefulRequest(int timerC)
    {
        SipMessage message = Parse(
            "INVITE sip:bob@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;"
            + "branch=z9hG4bK-client;rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: stateful-proxy-call@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = message;
        transport.Transport = SipTransportKind.Udp;
        transport.LocalAddress = Endpoint(
            "proxy.example.com", 5060, SipTransportKind.Udp);
        transport.RemoteAddress = Endpoint(
            "198.51.100.20", 62000, SipTransportKind.Udp);
        SipProxyOptions options = new SipProxyOptions();
        options.Mode = SipProxyMode.StatefulParallel;
        options.TimerCMilliseconds = timerC;
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Options = options;
        request.Targets = new SipProxyTarget[] {
            Target("203.0.113.40", 5080),
            Target("203.0.113.41", 5081) };
        request.CorrelationId = "stateful-correlation";
        return request;
    }

    static SipProxyTarget Target(string host, int port)
    {
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = host;
        candidate.Port = port;
        candidate.Transport = SipTransportKind.Udp;
        SipProxyTarget target = new SipProxyTarget();
        target.Candidate = candidate;
        return target;
    }

    static SipEndpointAddress Endpoint(string host, int port,
        SipTransportKind transport)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = transport;
        return endpoint;
    }

    static SipMessage ResponseFor(SipMessage request,
        int statusCode, string reason)
    {
        string[] vias = request.Headers.Where(header =>
            header.Name!.Equals(SipHeaderNames.Via,
                System.StringComparison.OrdinalIgnoreCase)
            || header.Name.Equals(SipHeaderNames.CompactVia,
                System.StringComparison.OrdinalIgnoreCase))
            .Select(header => "Via: " + header.Value + "\r\n").ToArray();
        return Parse("SIP/2.0 " + statusCode + " " + reason + "\r\n"
            + string.Concat(vias)
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=to-tag\r\n"
            + "Call-ID: stateful-proxy-call@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:bob@service.example.com>\r\n"
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
