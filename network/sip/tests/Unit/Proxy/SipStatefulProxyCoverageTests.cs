namespace sip.Tests;

public class SipStatefulProxyCoverageTests
{
    [Test]
    public void TimerCIgnoresTryingAndResetsForEveryLaterProvisional()
    {
        SipProxy proxy = new SipProxy();
        SipProxyResult started = proxy.ProcessProxyRequest(
            StatefulRequest(SipMethods.Invite, "timer-reset", 1000,
                new SipProxyTarget[] { CandidateTarget("203.0.113.60", 5060) }));
        AssertEventKinds(proxy, new SipProxyEventKind[] {
            SipProxyEventKind.RequestAccepted,
            SipProxyEventKind.BranchCreated });

        proxy.AdvanceProxyTime(100);
        SipProxyResult trying = proxy.ProcessProxyResponse(
            Response(started.Requests[0], 100));
        SipProxyEvent tryingEvent = proxy.PollProxyEvent();
        proxy.AdvanceProxyTime(200);
        SipProxyResult ringing = proxy.ProcessProxyResponse(
            Response(started.Requests[0], 180));
        SipProxyEvent ringingEvent = proxy.PollProxyEvent();
        proxy.AdvanceProxyTime(300);
        SipProxyResult retransmitted = proxy.ProcessProxyResponse(
            Response(started.Requests[0], 180));
        SipProxyEvent retransmittedEvent = proxy.PollProxyEvent();
        proxy.AdvanceProxyTime(1299);
        SipProxyEvent beforeDeadline = proxy.PollProxyEvent();
        proxy.AdvanceProxyTime(1300);
        SipProxyEvent timedOut = proxy.PollProxyEvent();
        SipProxyEvent selected = proxy.PollProxyEvent();
        SipProxyEvent completed = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(started.TimerCDeadlines,
                Is.EqualTo(new long[] { 1000 }));
            Assert.That(trying.TimerCDeadlines,
                Is.EqualTo(new long[] { 1000 }));
            Assert.That(tryingEvent.Kind,
                Is.EqualTo(SipProxyEventKind.ProvisionalForwarded));
            Assert.That(ringing.TimerCDeadlines,
                Is.EqualTo(new long[] { 1200 }));
            Assert.That(ringingEvent.Kind,
                Is.EqualTo(SipProxyEventKind.ProvisionalForwarded));
            Assert.That(retransmitted.TimerCDeadlines,
                Is.EqualTo(new long[] { 1300 }));
            Assert.That(retransmittedEvent.Kind,
                Is.EqualTo(SipProxyEventKind.ProvisionalForwarded));
            Assert.That(beforeDeadline.Kind, Is.EqualTo(SipProxyEventKind.None));
            Assert.That(timedOut.Kind,
                Is.EqualTo(SipProxyEventKind.BranchTimedOut));
            Assert.That(timedOut.Result!.TimerCDeadlines, Is.Empty);
            Assert.That(selected.Kind,
                Is.EqualTo(SipProxyEventKind.FinalSelected));
            Assert.That(selected.Result!.StatusCode,
                Is.EqualTo(SipStatusCodes.RequestTimeout));
            Assert.That(completed.Kind,
                Is.EqualTo(SipProxyEventKind.Completed));
        });
    }

    [Test]
    public void NonInviteBranchesDoNotStartTimerCOrExpire()
    {
        SipProxy proxy = new SipProxy();
        SipProxyResult started = proxy.ProcessProxyRequest(
            StatefulRequest(SipMethods.Options, "timer-non-invite", 1000,
                new SipProxyTarget[] { CandidateTarget("203.0.113.61", 5060) }));
        AssertEventKinds(proxy, new SipProxyEventKind[] {
            SipProxyEventKind.RequestAccepted,
            SipProxyEventKind.BranchCreated });

        proxy.AdvanceProxyTime(100000);
        SipProxyEvent noTimeout = proxy.PollProxyEvent();
        SipProxyResult completed = proxy.ProcessProxyResponse(
            Response(started.Requests[0], 200));
        SipProxyEvent selected = proxy.PollProxyEvent();
        SipProxyEvent completion = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(started.ActiveBranchCount, Is.EqualTo(1));
            Assert.That(started.TimerCDeadlines, Is.Empty);
            Assert.That(noTimeout.Kind, Is.EqualTo(SipProxyEventKind.None));
            Assert.That(completed.Success, Is.True, completed.ErrorMessage);
            Assert.That(completed.Completed, Is.True);
            Assert.That(completed.ActiveBranchCount, Is.Zero);
            Assert.That(selected.Kind,
                Is.EqualTo(SipProxyEventKind.FinalSelected));
            Assert.That(completion.Kind,
                Is.EqualTo(SipProxyEventKind.Completed));
        });
    }

    [TestCase(503, 404, 404)]
    [TestCase(404, 302, 302)]
    [TestCase(486, 404, 404)]
    [TestCase(408, 503, 503)]
    [TestCase(408, 408, 408)]
    public void SelectsBestCollectedFinalByClassStatusAndTimeoutPolicy(
        int firstStatus, int secondStatus, int expectedStatus)
    {
        SipProxy proxy = new SipProxy();
        SipProxyResult started = proxy.ProcessProxyRequest(
            StatefulRequest(SipMethods.Invite,
                "best-" + firstStatus.ToString() + "-" + secondStatus.ToString(),
                5000, new SipProxyTarget[] {
                    CandidateTarget("203.0.113.62", 5060),
                    CandidateTarget("203.0.113.63", 5060) }));
        AssertEventKinds(proxy, new SipProxyEventKind[] {
            SipProxyEventKind.RequestAccepted,
            SipProxyEventKind.BranchCreated,
            SipProxyEventKind.BranchCreated });

        SipProxyResult collected = proxy.ProcessProxyResponse(
            Response(started.Requests[0], firstStatus));
        SipProxyResult result = proxy.ProcessProxyResponse(
            Response(started.Requests[1], secondStatus));
        SipProxyEvent selected = proxy.PollProxyEvent();
        SipProxyEvent completed = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(collected.Success, Is.True, collected.ErrorMessage);
            Assert.That(collected.Response, Is.Null);
            Assert.That(collected.ActiveBranchCount, Is.EqualTo(1));
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Completed, Is.True);
            Assert.That(result.Response!.StatusCode,
                Is.EqualTo(expectedStatus));
            Assert.That(result.StatusCode, Is.EqualTo(expectedStatus));
            Assert.That(result.ActiveBranchCount, Is.Zero);
            Assert.That(result.TimerCDeadlines, Is.Empty);
            Assert.That(selected.Kind,
                Is.EqualTo(SipProxyEventKind.FinalSelected));
            Assert.That(selected.Result!.StatusCode,
                Is.EqualTo(expectedStatus));
            Assert.That(completed.Kind,
                Is.EqualTo(SipProxyEventKind.Completed));
            Assert.That(completed.Result!.StatusCode,
                Is.EqualTo(expectedStatus));
        });
    }

    [Test]
    public void SuccessfulFinalCompletesImmediatelyAndCleansUpOtherBranches()
    {
        SipProxy proxy = new SipProxy();
        SipProxyResult started = proxy.ProcessProxyRequest(
            StatefulRequest(SipMethods.Invite, "success-cleanup", 5000,
                new SipProxyTarget[] {
                    CandidateTarget("203.0.113.64", 5060),
                    CandidateTarget("203.0.113.65", 5060) }));
        AssertEventKinds(proxy, new SipProxyEventKind[] {
            SipProxyEventKind.RequestAccepted,
            SipProxyEventKind.BranchCreated,
            SipProxyEventKind.BranchCreated });
        proxy.ProcessProxyResponse(Response(started.Requests[1], 180));
        Assert.That(proxy.PollProxyEvent().Kind,
            Is.EqualTo(SipProxyEventKind.ProvisionalForwarded));

        SipProxyResult success = proxy.ProcessProxyResponse(
            Response(started.Requests[0], 200));
        SipProxyEvent cancel = proxy.PollProxyEvent();
        SipProxyEvent selected = proxy.PollProxyEvent();
        SipProxyEvent completed = proxy.PollProxyEvent();
        SipProxyResult lateFailure = proxy.ProcessProxyResponse(
            Response(started.Requests[1], 487));
        SipProxyEvent late = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(success.Completed, Is.True);
            Assert.That(success.StatusCode, Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(success.ActiveBranchCount, Is.Zero);
            Assert.That(success.Requests.Any(message =>
                message.Method == SipMethod.Cancel), Is.True);
            Assert.That(cancel.Kind,
                Is.EqualTo(SipProxyEventKind.CancelForwarded));
            Assert.That(selected.Kind,
                Is.EqualTo(SipProxyEventKind.FinalSelected));
            Assert.That(completed.Kind,
                Is.EqualTo(SipProxyEventKind.Completed));
            Assert.That(lateFailure.Success, Is.True);
            Assert.That(lateFailure.Response, Is.Null);
            Assert.That(lateFailure.Completed, Is.True);
            Assert.That(late.Kind,
                Is.EqualTo(SipProxyEventKind.LateResponseIgnored));
        });
    }

    [Test]
    public void RollsBackPartialBranchStartAfterResolutionFailure()
    {
        StatefulCoverageResolver resolver = new StatefulCoverageResolver();
        resolver.FailOnCall = 2;
        SipProxy proxy = new SipProxy();
        proxy.ConfigureProxyResolver(resolver, new SipDnsOptions());
        SipProxyTarget first = UriTarget("sip:bob@one.example.com");
        SipProxyTarget second = UriTarget("sip:bob@two.example.com");
        SipProxyRequest failedRequest = StatefulRequest(SipMethods.Invite,
            "partial-failure", 5000,
            new SipProxyTarget[] { first, second });

        SipProxyResult failed = proxy.ProcessProxyRequest(failedRequest);
        SipProxyEvent failureEvent = proxy.PollProxyEvent();
        SipProxyEvent noMoreFailureEvents = proxy.PollProxyEvent();
        resolver.FailOnCall = 0;
        SipProxyRequest retryRequest = StatefulRequest(SipMethods.Invite,
            "partial-retry", 5000,
            new SipProxyTarget[] { first, second });
        SipProxyResult retried = proxy.ProcessProxyRequest(retryRequest);
        SipProxyEvent accepted = proxy.PollProxyEvent();
        SipProxyEvent firstBranch = proxy.PollProxyEvent();
        SipProxyEvent secondBranch = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.StatusCode, Is.EqualTo(SipStatusCodes.BadGateway));
            Assert.That(failed.Requests, Is.Empty);
            Assert.That(failed.SendRequests, Is.Empty);
            Assert.That(failureEvent.Kind, Is.EqualTo(SipProxyEventKind.Failed));
            Assert.That(noMoreFailureEvents.Kind,
                Is.EqualTo(SipProxyEventKind.None));
            Assert.That(retried.Success, Is.True, retried.ErrorMessage);
            Assert.That(retried.ActiveBranchCount, Is.EqualTo(2));
            Assert.That(retried.Requests, Has.Length.EqualTo(2));
            Assert.That(accepted.Kind,
                Is.EqualTo(SipProxyEventKind.RequestAccepted));
            Assert.That(firstBranch.Kind,
                Is.EqualTo(SipProxyEventKind.BranchCreated));
            Assert.That(secondBranch.Kind,
                Is.EqualTo(SipProxyEventKind.BranchCreated));
            Assert.That(resolver.Calls, Is.EqualTo(4));
        });
    }

    [Test]
    public void QueuesIndependentSnapshotsInLifecycleOrder()
    {
        SipProxy proxy = new SipProxy();
        SipProxyResult started = proxy.ProcessProxyRequest(
            StatefulRequest(SipMethods.Invite, "snapshot-order", 4000,
                new SipProxyTarget[] {
                    CandidateTarget("203.0.113.66", 5060),
                    CandidateTarget("203.0.113.67", 5060) }));
        started.Targets[0].Candidate!.Host = "mutated.example.com";
        started.TimerCDeadlines[0] = 1;

        SipProxyEvent accepted = proxy.PollProxyEvent();
        SipProxyEvent firstBranch = proxy.PollProxyEvent();
        SipProxyEvent secondBranch = proxy.PollProxyEvent();
        SipProxyResult collected = proxy.ProcessProxyResponse(
            Response(started.Requests[0], 503));
        SipProxyResult final = proxy.ProcessProxyResponse(
            Response(started.Requests[1], 486));
        final.Targets[0].Candidate!.Host = "also-mutated.example.com";
        SipProxyEvent selected = proxy.PollProxyEvent();
        SipProxyEvent completed = proxy.PollProxyEvent();
        SipProxyEvent none = proxy.PollProxyEvent();

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Kind,
                Is.EqualTo(SipProxyEventKind.RequestAccepted));
            Assert.That(accepted.Result!.ActiveBranchCount, Is.EqualTo(2));
            Assert.That(accepted.Result.TimerCDeadlines,
                Is.EqualTo(new long[] { 4000, 4000 }));
            Assert.That(accepted.Result.Targets[0].Candidate!.Host,
                Is.EqualTo("203.0.113.66"));
            Assert.That(firstBranch.Kind,
                Is.EqualTo(SipProxyEventKind.BranchCreated));
            Assert.That(firstBranch.Target!.Candidate!.Host,
                Is.EqualTo("203.0.113.66"));
            Assert.That(secondBranch.Kind,
                Is.EqualTo(SipProxyEventKind.BranchCreated));
            Assert.That(secondBranch.Target!.Candidate!.Host,
                Is.EqualTo("203.0.113.67"));
            Assert.That(collected.ActiveBranchCount, Is.EqualTo(1));
            Assert.That(selected.Kind,
                Is.EqualTo(SipProxyEventKind.FinalSelected));
            Assert.That(selected.Result!.StatusCode, Is.EqualTo(486));
            Assert.That(selected.Result.Targets[0].Candidate!.Host,
                Is.EqualTo("203.0.113.66"));
            Assert.That(completed.Kind,
                Is.EqualTo(SipProxyEventKind.Completed));
            Assert.That(completed.Result!.Completed, Is.True);
            Assert.That(completed.Result.ActiveBranchCount, Is.Zero);
            Assert.That(none.Kind, Is.EqualTo(SipProxyEventKind.None));
        });
    }

    static SipProxyRequest StatefulRequest(string method, string callId,
        int timerC, SipProxyTarget[] targets)
    {
        string contact = method == SipMethods.Invite
            ? "Contact: <sip:alice@client.example.com>\r\n" : "";
        SipMessage message = Parse(method
            + " sip:bob@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;"
            + "branch=z9hG4bK-" + callId + ";rport\r\n"
            + "Max-Forwards: 10\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: " + callId + "@example.com\r\n"
            + "CSeq: 1 " + method + "\r\n"
            + contact
            + "Content-Length: 0\r\n\r\n");
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = message;
        transport.Transport = SipTransportKind.Udp;
        transport.LocalAddress = Endpoint("proxy.example.com", 5060);
        transport.RemoteAddress = Endpoint("198.51.100.20", 62000);
        SipProxyOptions options = new SipProxyOptions();
        options.Mode = SipProxyMode.StatefulParallel;
        options.TimerCMilliseconds = timerC;
        options.RecordRoute = false;
        SipProxyRequest request = new SipProxyRequest();
        request.Message = message;
        request.TransportMessage = transport;
        request.Options = options;
        request.Targets = targets;
        request.CorrelationId = callId;
        return request;
    }

    static SipProxyTarget CandidateTarget(string host, int port)
    {
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = host;
        candidate.Port = port;
        candidate.Transport = SipTransportKind.Udp;
        SipProxyTarget target = new SipProxyTarget();
        target.Candidate = candidate;
        return target;
    }

    static SipProxyTarget UriTarget(string text)
    {
        SipProxyTarget target = new SipProxyTarget();
        target.Uri = ParseUri(text);
        return target;
    }

    static SipEndpointAddress Endpoint(string host, int port)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = SipTransportKind.Udp;
        return endpoint;
    }

    static SipMessage Response(SipMessage request, int statusCode)
    {
        string vias = string.Concat(request.Headers.Where(header =>
            IsHeader(header, SipHeaderNames.Via)
                || IsHeader(header, SipHeaderNames.CompactVia))
            .Select(header => "Via: " + header.Value + "\r\n"));
        string contact = statusCode >= 200 && statusCode < 300
            ? "Contact: <sip:bob@service.example.com>\r\n" : "";
        return Parse("SIP/2.0 " + statusCode.ToString() + " Test\r\n"
            + vias
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + Header(request, SipHeaderNames.To)
            + ";tag=to-tag\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + contact
            + "Content-Length: 0\r\n\r\n");
    }

    static bool IsHeader(SipHeader header, string name)
    {
        return string.Equals(header.Name, name,
            System.StringComparison.OrdinalIgnoreCase);
    }

    static string Header(SipMessage message, string name)
    {
        return message.Headers.Single(header => IsHeader(header, name)).Value!;
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

    static void AssertEventKinds(SipProxy proxy,
        SipProxyEventKind[] expected)
    {
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(proxy.PollProxyEvent().Kind,
                Is.EqualTo(expected[index]));
            index++;
        }
        Assert.That(proxy.PollProxyEvent().Kind,
            Is.EqualTo(SipProxyEventKind.None));
    }
}

public class StatefulCoverageResolver : ISipDnsResolver
{
    public int Calls;
    public int FailOnCall;

    public SipResolutionResult ResolveTarget(
        SipResolutionRequest request, SipDnsOptions options)
    {
        Calls++;
        SipResolutionResult result = new SipResolutionResult();
        if (FailOnCall > 0 && Calls == FailOnCall)
        {
            result.Status = SipResolutionStatus.Timeout;
            result.ErrorMessage = "transport resolution timed out";
            return result;
        }
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = "203.0.113." + (70 + Calls).ToString();
        candidate.Port = 5060;
        candidate.Transport = SipTransportKind.Udp;
        result.Status = SipResolutionStatus.Success;
        result.Candidates = new SipServerCandidate[] { candidate };
        return result;
    }

    public void ReportCandidateFailure(SipServerCandidate candidate)
    {
    }

    public void FlushResolverCache()
    {
    }
}
