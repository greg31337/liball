using htmlgui;

namespace htmlgui.Tests;

public class BridgeMessageFlowTests
{
    [Test]
    public void DeliversNotificationsInOrderAndAcknowledgesDuplicates()
    {
        (BridgeMessageFlow host, BridgeMessageFlow browser) = ReadyFlows(4);
        BridgeMessageFlowResult sent = host.SendBridgeNotification(UiBridgeMessageKind.Event, Text("first"), 121);
        BridgeMessageFlowResult received = browser.ReceiveBridgeMessage(sent.Envelope, true, 122);
        BridgeMessageFlowResult acknowledged = host.ReceiveBridgeMessage(received.Envelope, true, 123);
        BridgeMessageFlowResult duplicate = browser.ReceiveBridgeMessage(sent.Envelope, true, 124);
        BridgeMessageFlowResult duplicateAcknowledgement = host.ReceiveBridgeMessage(duplicate.Envelope, true, 125);
        BridgeMessageFlowResult second = host.SendBridgeNotification(UiBridgeMessageKind.Command, Text("second"), 126);
        UiBridgeEnvelope gap = CopyEnvelope(second.Envelope!);
        gap.Sequence++;
        BridgeMessageFlowResult outOfOrder = browser.ReceiveBridgeMessage(gap, true, 127);
        BridgeMessageFlowResult ordered = browser.ReceiveBridgeMessage(second.Envelope, true, 128);

        Assert.Multiple(() =>
        {
            Assert.That(sent.Envelope!.Sequence, Is.EqualTo(1));
            Assert.That(received.Success, Is.True);
            Assert.That(received.Message!.Payload!.StringValue, Is.EqualTo("first"));
            Assert.That(received.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Acknowledgement));
            Assert.That(acknowledged.Success, Is.True);
            Assert.That(host.LastAcknowledgedSequence, Is.EqualTo(1));
            Assert.That(host.PendingAcknowledgements.Select(item => item.Sequence), Is.EqualTo(new[] { 2 }));
            Assert.That(duplicate.Duplicate, Is.True);
            Assert.That(duplicate.Envelope!.Sequence, Is.EqualTo(2));
            Assert.That(duplicateAcknowledgement.Duplicate, Is.True);
            Assert.That(outOfOrder.Code, Is.EqualTo("bridge.message-out-of-order"));
            Assert.That(browser.Session.LastInboundSequence, Is.EqualTo(2));
            Assert.That(ordered.Success, Is.True);
        });
    }

    [Test]
    public void AcknowledgementWindowAppliesBackpressureAndReopensCumulatively()
    {
        (BridgeMessageFlow host, BridgeMessageFlow browser) = ReadyFlows(2);
        BridgeMessageFlowResult first = host.SendBridgeNotification(UiBridgeMessageKind.Log, Text("one"), 121);
        BridgeMessageFlowResult second = host.SendBridgeNotification(UiBridgeMessageKind.Log, Text("two"), 122);
        BridgeMessageFlowResult blocked = host.SendBridgeNotification(UiBridgeMessageKind.Log, Text("three"), 123);
        BridgeMessageFlowResult firstReceived = browser.ReceiveBridgeMessage(first.Envelope, true, 124);
        BridgeMessageFlowResult secondReceived = browser.ReceiveBridgeMessage(second.Envelope, true, 125);
        BridgeMessageFlowResult firstAcknowledged = host.ReceiveBridgeMessage(firstReceived.Envelope, true, 126);
        BridgeMessageFlowResult third = host.SendBridgeNotification(UiBridgeMessageKind.Log, Text("three"), 127);
        BridgeMessageFlowResult secondAcknowledged = host.ReceiveBridgeMessage(secondReceived.Envelope, true, 128);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Code, Is.EqualTo("bridge.acknowledgement-backpressure"));
            Assert.That(blocked.Backpressured, Is.True);
            Assert.That(blocked.Envelope, Is.Null);
            Assert.That(firstAcknowledged.Success, Is.True);
            Assert.That(third.Success, Is.True);
            Assert.That(third.Envelope!.Sequence, Is.EqualTo(3));
            Assert.That(secondAcknowledged.Success, Is.True);
            Assert.That(host.PendingAcknowledgements.Select(item => item.Sequence), Is.EqualTo(new[] { 3 }));
        });
    }

    [Test]
    public void CompletesRequestsWithResponsesAndErrors()
    {
        (BridgeMessageFlow host, BridgeMessageFlow browser) = ReadyFlows(4);
        BridgeMessageFlowResult request = host.SendBridgeRequest(Request("request-1", "lookup", 50), UiBridgeMessageKind.Request, 121);
        BridgeMessageFlowResult incoming = browser.ReceiveBridgeMessage(request.Envelope, true, 122);
        BridgeMessageFlowResult response = browser.CompleteIncomingRequest("request-1", Text("answer"), 123);
        BridgeMessageFlowResult completed = host.ReceiveBridgeMessage(response.Envelope, true, 124);
        BridgeMessageFlowResult hostCall = browser.SendBridgeRequest(Request("call-1", "native", 50), UiBridgeMessageKind.HostCall, 125);
        BridgeMessageFlowResult incomingCall = host.ReceiveBridgeMessage(hostCall.Envelope, true, 126);
        BridgeMessageFlowResult error = host.FailIncomingRequest("call-1", Error("call-1", "native.failed"), 127);
        BridgeMessageFlowResult failed = browser.ReceiveBridgeMessage(error.Envelope, true, 128);

        Assert.Multiple(() =>
        {
            Assert.That(incoming.Request!.Operation, Is.EqualTo("lookup"));
            Assert.That(response.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Response));
            Assert.That(completed.Response!.Success, Is.True);
            Assert.That(completed.Response.Value!.StringValue, Is.EqualTo("answer"));
            Assert.That(incomingCall.Request!.Operation, Is.EqualTo("native"));
            Assert.That(error.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Error));
            Assert.That(failed.Error!.Code, Is.EqualTo("native.failed"));
            Assert.That(host.PendingRequests, Is.Empty);
            Assert.That(browser.PendingRequests, Is.Empty);
        });
    }

    [Test]
    public void CancelsRequestsAndTreatsRepeatedCancellationAsDuplicate()
    {
        (BridgeMessageFlow host, BridgeMessageFlow browser) = ReadyFlows(4);
        BridgeMessageFlowResult request = host.SendBridgeRequest(Request("request-1", "slow", 500), UiBridgeMessageKind.Request, 121);
        BridgeMessageFlowResult incoming = browser.ReceiveBridgeMessage(request.Envelope, true, 122);
        BridgeMessageFlowResult cancellation = host.CancelOutgoingRequest("request-1", 123);
        BridgeMessageFlowResult cancelled = browser.ReceiveBridgeMessage(cancellation.Envelope, true, 124);
        BridgeMessageFlowResult duplicate = browser.ReceiveBridgeMessage(cancellation.Envelope, true, 125);
        BridgeMessageFlowResult repeatedLocal = host.CancelOutgoingRequest("request-1", 126);

        Assert.Multiple(() =>
        {
            Assert.That(incoming.Success, Is.True);
            Assert.That(cancellation.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Cancel));
            Assert.That(cancelled.Success, Is.True);
            Assert.That(browser.PendingRequests, Is.Empty);
            Assert.That(duplicate.Duplicate, Is.True);
            Assert.That(repeatedLocal.Success, Is.True);
            Assert.That(repeatedLocal.Duplicate, Is.True);
            Assert.That(repeatedLocal.Envelope, Is.Null);
        });
    }

    [Test]
    public void DetectsRequestDuplicatesAndReplaysCachedTerminalResponse()
    {
        (BridgeMessageFlow host, BridgeMessageFlow browser) = ReadyFlows(4);
        BridgeMessageFlowResult request = host.SendBridgeRequest(Request("request-1", "lookup", 50), UiBridgeMessageKind.Request, 121);
        BridgeMessageFlowResult first = browser.ReceiveBridgeMessage(request.Envelope, true, 122);
        BridgeMessageFlowResult sameEnvelope = browser.ReceiveBridgeMessage(request.Envelope, true, 123);
        BridgeMessageFlowResult response = browser.CompleteIncomingRequest("request-1", Text("answer"), 124);
        BridgeMessageFlowResult completed = host.ReceiveBridgeMessage(response.Envelope, true, 125);
        UiBridgeEnvelope repeatedRequest = CopyEnvelope(request.Envelope!);
        repeatedRequest.Sequence = 2;
        BridgeMessageFlowResult replay = browser.ReceiveBridgeMessage(repeatedRequest, true, 126);
        BridgeMessageFlowResult repeatedResponse = host.ReceiveBridgeMessage(replay.Envelope, true, 127);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(sameEnvelope.Duplicate, Is.True);
            Assert.That(browser.PendingRequests.Count, Is.Zero);
            Assert.That(completed.Response!.Value!.StringValue, Is.EqualTo("answer"));
            Assert.That(replay.Duplicate, Is.True);
            Assert.That(replay.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Response));
            Assert.That(replay.Envelope.Response!.Value!.StringValue, Is.EqualTo("answer"));
            Assert.That(repeatedResponse.Duplicate, Is.True);
            Assert.That(host.Session.LastInboundSequence, Is.EqualTo(2));
        });
    }

    [Test]
    public void BoundsPendingRequestsAndReturnsBackpressureError()
    {
        (BridgeMessageFlow host, BridgeMessageFlow browser) = ReadyFlows(2);
        BridgeMessageFlowResult first = host.SendBridgeRequest(Request("request-1", "one", 500), UiBridgeMessageKind.Request, 121);
        BridgeMessageFlowResult second = host.SendBridgeRequest(Request("request-2", "two", 500), UiBridgeMessageKind.Request, 122);
        BridgeMessageFlowResult locallyBlocked = host.SendBridgeRequest(Request("request-3", "three", 500), UiBridgeMessageKind.Request, 123);
        browser.ReceiveBridgeMessage(first.Envelope, true, 124);
        browser.ReceiveBridgeMessage(second.Envelope, true, 125);
        UiBridgeEnvelope overflow = RequestEnvelope(browser.Session, 3, "request-3", "three", 500);
        BridgeMessageFlowResult remotelyBlocked = browser.ReceiveBridgeMessage(overflow, true, 126);

        Assert.Multiple(() =>
        {
            Assert.That(locallyBlocked.Code, Is.EqualTo("bridge.request-backpressure"));
            Assert.That(locallyBlocked.Backpressured, Is.True);
            Assert.That(host.PendingRequests.Count, Is.EqualTo(2));
            Assert.That(remotelyBlocked.Code, Is.EqualTo("bridge.request-backpressure"));
            Assert.That(remotelyBlocked.Backpressured, Is.True);
            Assert.That(remotelyBlocked.Applied, Is.True);
            Assert.That(remotelyBlocked.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Error));
            Assert.That(remotelyBlocked.Error!.CorrelationId, Is.EqualTo("request-3"));
            Assert.That(browser.PendingRequests.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public void SweepsOutboundAndInboundRequestTimeoutsAtTheirDeadlines()
    {
        (BridgeMessageFlow host, BridgeMessageFlow browser) = ReadyFlows(4);
        host.SendBridgeRequest(Request("outbound", "slow", 9), UiBridgeMessageKind.Request, 121);
        BridgeMessageFlowResult browserRequest = browser.SendBridgeRequest(Request("inbound", "slow", 20), UiBridgeMessageKind.Request, 121);
        host.ReceiveBridgeMessage(browserRequest.Envelope, true, 121);

        BridgeMessageFlowResult early = host.SweepBridgeRequestTimeouts(129);
        BridgeMessageFlowResult outboundTimeout = host.SweepBridgeRequestTimeouts(130);
        BridgeMessageFlowResult inboundTimeout = host.SweepBridgeRequestTimeouts(141);

        Assert.Multiple(() =>
        {
            Assert.That(early.Applied, Is.False);
            Assert.That(outboundTimeout.TimedOut, Is.True);
            Assert.That(outboundTimeout.CorrelationIds, Is.EqualTo(new[] { "outbound" }));
            Assert.That(outboundTimeout.Envelopes[0].Kind, Is.EqualTo(UiBridgeMessageKind.Cancel));
            Assert.That(inboundTimeout.TimedOut, Is.True);
            Assert.That(inboundTimeout.CorrelationIds, Is.EqualTo(new[] { "inbound" }));
            Assert.That(inboundTimeout.Envelopes[0].Kind, Is.EqualTo(UiBridgeMessageKind.Error));
            Assert.That(inboundTimeout.Envelopes[0].Error!.Code, Is.EqualTo("bridge.request-timeout"));
            Assert.That(host.PendingRequests, Is.Empty);
        });
    }

    [Test]
    public void RejectsMalformedAcknowledgementsAndMismatchedResponseKindsWithoutAdvancing()
    {
        (BridgeMessageFlow host, BridgeMessageFlow browser) = ReadyFlows(4);
        BridgeMessageFlowResult notification = host.SendBridgeNotification(UiBridgeMessageKind.Event, Text("event"), 121);
        BridgeMessageFlowResult received = browser.ReceiveBridgeMessage(notification.Envelope, true, 122);
        UiValue acknowledgementValue = received.Envelope!.Payload!.ObjectValue![0].Value;
        acknowledgementValue.IntegerValue = 999;
        BridgeMessageFlowResult unknownAcknowledgement = host.ReceiveBridgeMessage(received.Envelope, true, 123);
        acknowledgementValue.IntegerValue = notification.Envelope!.Sequence;
        BridgeMessageFlowResult acceptedAcknowledgement = host.ReceiveBridgeMessage(received.Envelope, true, 124);
        BridgeMessageFlowResult hostCall = host.SendBridgeRequest(Request("call-1", "native", 50), UiBridgeMessageKind.HostCall, 125);
        browser.ReceiveBridgeMessage(hostCall.Envelope, true, 126);
        UiBridgeEnvelope wrongResponse = ResponseEnvelope(host.Session, 2, "call-1", UiBridgeMessageKind.Response, "wrong");
        BridgeMessageFlowResult mismatch = host.ReceiveBridgeMessage(wrongResponse, true, 127);
        BridgeMessageFlowResult correctResponse = browser.CompleteIncomingRequest("call-1", Text("right"), 128);
        BridgeMessageFlowResult completed = host.ReceiveBridgeMessage(correctResponse.Envelope, true, 129);
        UiBridgeRequest malformed = Request("bad", "bad", 10);
        malformed.Arguments = null!;
        BridgeMessageFlowResult invalidRequest = host.SendBridgeRequest(malformed, UiBridgeMessageKind.Request, 130);

        Assert.Multiple(() =>
        {
            Assert.That(unknownAcknowledgement.Code, Is.EqualTo("bridge.unknown-acknowledgement"));
            Assert.That(host.LastAcknowledgedSequence, Is.EqualTo(notification.Envelope.Sequence));
            Assert.That(acceptedAcknowledgement.Success, Is.True);
            Assert.That(mismatch.Code, Is.EqualTo("bridge.response-kind-mismatch"));
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Message!.Kind, Is.EqualTo(UiBridgeMessageKind.HostResult));
            Assert.That(completed.Response!.Value!.StringValue, Is.EqualTo("right"));
            Assert.That(invalidRequest.Code, Is.EqualTo("bridge.invalid-request"));
            Assert.That(host.Session.NextOutboundSequence, Is.EqualTo(3));
        });
    }

    private static (BridgeMessageFlow Host, BridgeMessageFlow Browser) ReadyFlows(int maximumPendingRequests)
    {
        BridgeSession hostSession = Session(BridgeProtocolNames.BrowserOrigin, maximumPendingRequests);
        BridgeSession browserSession = Session(BridgeProtocolNames.HostOrigin, maximumPendingRequests);
        BridgeSessionOperation initialize = hostSession.BeginBridgeHandshake("session-1", "nonce-1", 100);
        BridgeSessionOperation ready = browserSession.AcceptBridgeInitialize(initialize.Envelope, true, 110);
        BridgeSessionOperation accepted = hostSession.AcceptBridgeReady(ready.Envelope, true, 120);
        Assert.That(accepted.Success, Is.True);
        return (new BridgeMessageFlow(hostSession), new BridgeMessageFlow(browserSession));
    }

    private static BridgeSession Session(string expectedOrigin, int maximumPendingRequests)
    {
        UiBridgeOptions options = new UiBridgeOptions
        {
            Version = BridgeProtocolVersions.Current,
            Origin = expectedOrigin,
            RequireMainFrame = true,
            MaximumMessageLength = 4096,
            MaximumPendingRequests = maximumPendingRequests,
            TimeoutMilliseconds = 1000
        };
        UiTransportCapabilities capabilities = new UiTransportCapabilities
        {
            State = UiTransportState.Starting,
            Version = BridgeProtocolVersions.Current
        };
        return new BridgeSession(options, capabilities);
    }

    private static UiBridgeRequest Request(string correlationId, string operation, int timeoutMilliseconds)
    {
        return new UiBridgeRequest
        {
            CorrelationId = correlationId,
            Operation = operation,
            Arguments = new UiValueEntry[] { new UiValueEntry { Key = "value", Value = Text(operation) } },
            TimeoutMilliseconds = timeoutMilliseconds
        };
    }

    private static UiBridgeError Error(string correlationId, string code)
    {
        return new UiBridgeError
        {
            Code = code,
            Message = "Failed",
            CorrelationId = correlationId,
            IsRetryable = true
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiBridgeEnvelope RequestEnvelope(BridgeSession receiver, int sequence, string correlationId, string operation, int timeoutMilliseconds)
    {
        return new UiBridgeEnvelope
        {
            Version = receiver.ReadNegotiatedVersion(),
            Session = receiver.Session,
            Sequence = sequence,
            CorrelationId = correlationId,
            Origin = receiver.Options!.Origin,
            Kind = UiBridgeMessageKind.Request,
            Request = Request(correlationId, operation, timeoutMilliseconds)
        };
    }

    private static UiBridgeEnvelope ResponseEnvelope(BridgeSession receiver, int sequence, string correlationId, UiBridgeMessageKind kind, string value)
    {
        return new UiBridgeEnvelope
        {
            Version = receiver.ReadNegotiatedVersion(),
            Session = receiver.Session,
            Sequence = sequence,
            CorrelationId = correlationId,
            Origin = receiver.Options!.Origin,
            Kind = kind,
            Response = new UiBridgeResponse
            {
                CorrelationId = correlationId,
                Success = true,
                Value = Text(value)
            }
        };
    }

    private static UiBridgeEnvelope CopyEnvelope(UiBridgeEnvelope source)
    {
        return new UiBridgeEnvelope
        {
            Version = source.Version,
            Session = source.Session,
            Sequence = source.Sequence,
            CorrelationId = source.CorrelationId,
            Origin = source.Origin,
            Kind = source.Kind,
            Payload = source.Payload,
            Request = source.Request,
            Response = source.Response,
            Error = source.Error
        };
    }
}
