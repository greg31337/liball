using System.Text;
using htmlgui;

namespace htmlgui.Tests;

public class BridgeSessionIntegrationTests
{
    [Test]
    public void RunsNegotiatedNotificationRequestErrorCancelHeartbeatAndShutdownLifecycle()
    {
        (BridgeMessageFlow host, BridgeMessageFlow browser) = ReadyFlows(8, true);

        BridgeMessageFlowResult notification = host.SendBridgeNotification(UiBridgeMessageKind.Event, Text("clicked"), 121);
        BridgeMessageFlowResult delivered = browser.ReceiveBridgeMessage(notification.Envelope, true, 122);
        BridgeMessageFlowResult acknowledged = host.ReceiveBridgeMessage(delivered.Envelope, true, 123);

        BridgeMessageFlowResult request = host.SendBridgeRequest(Request("success", "lookup", 100), UiBridgeMessageKind.Request, 124);
        BridgeMessageFlowResult receivedRequest = browser.ReceiveBridgeMessage(request.Envelope, true, 125);
        BridgeMessageFlowResult response = browser.CompleteIncomingRequest("success", Text("answer"), 126);
        BridgeMessageFlowResult completed = host.ReceiveBridgeMessage(response.Envelope, true, 127);

        BridgeMessageFlowResult failingRequest = host.SendBridgeRequest(Request("failure", "lookup", 100), UiBridgeMessageKind.Request, 128);
        browser.ReceiveBridgeMessage(failingRequest.Envelope, true, 129);
        BridgeMessageFlowResult error = browser.FailIncomingRequest("failure", Error("failure", "lookup.failed"), 130);
        BridgeMessageFlowResult failed = host.ReceiveBridgeMessage(error.Envelope, true, 131);

        BridgeMessageFlowResult cancellable = host.SendBridgeRequest(Request("cancelled", "slow", 100), UiBridgeMessageKind.Request, 132);
        browser.ReceiveBridgeMessage(cancellable.Envelope, true, 133);
        BridgeMessageFlowResult cancellation = host.CancelOutgoingRequest("cancelled", 134);
        BridgeMessageFlowResult cancelled = browser.ReceiveBridgeMessage(cancellation.Envelope, true, 135);

        BridgeSessionOperation heartbeat = host.Session.SweepBridgeTimeout(620);
        BridgeSessionOperation heartbeatAccepted = browser.Session.AcceptBridgeHeartbeat(heartbeat.Envelope, true, 621);
        BridgeSessionOperation shutdown = host.Session.BeginBridgeShutdown(622);
        BridgeSessionOperation shutdownResponse = browser.Session.AcceptBridgeShutdown(shutdown.Envelope, true, 623);
        BridgeSessionOperation hostStopped = host.Session.AcceptBridgeShutdown(shutdownResponse.Envelope, true, 624);
        BridgeSessionOperation browserStopped = browser.Session.CompleteBridgeShutdown();

        Assert.Multiple(() =>
        {
            Assert.That(delivered.Message!.Payload!.StringValue, Is.EqualTo("clicked"));
            Assert.That(acknowledged.Success, Is.True);
            Assert.That(host.PendingAcknowledgements, Is.Empty);
            Assert.That(receivedRequest.Request!.Operation, Is.EqualTo("lookup"));
            Assert.That(completed.Response!.Value!.StringValue, Is.EqualTo("answer"));
            Assert.That(failed.Error!.Code, Is.EqualTo("lookup.failed"));
            Assert.That(cancellation.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Cancel));
            Assert.That(cancelled.Success, Is.True);
            Assert.That(host.PendingRequests, Is.Empty);
            Assert.That(browser.PendingRequests, Is.Empty);
            Assert.That(heartbeat.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Heartbeat));
            Assert.That(heartbeatAccepted.Success, Is.True);
            Assert.That(shutdown.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Shutdown));
            Assert.That(hostStopped.State, Is.EqualTo(UiTransportState.Stopped));
            Assert.That(browserStopped.State, Is.EqualTo(UiTransportState.Stopped));
        });
    }

    [Test]
    public void CoordinatesAcknowledgementPressureTimeoutsAndTerminalErrorReplay()
    {
        (BridgeMessageFlow host, BridgeMessageFlow browser) = ReadyFlows(2, false);
        BridgeMessageFlowResult first = host.SendBridgeNotification(UiBridgeMessageKind.Log, Text("one"), 121);
        BridgeMessageFlowResult second = host.SendBridgeNotification(UiBridgeMessageKind.Log, Text("two"), 122);
        BridgeMessageFlowResult blocked = host.SendBridgeNotification(UiBridgeMessageKind.Log, Text("three"), 123);
        BridgeMessageFlowResult firstDelivery = browser.ReceiveBridgeMessage(first.Envelope, true, 124);
        BridgeMessageFlowResult secondDelivery = browser.ReceiveBridgeMessage(second.Envelope, true, 125);
        host.ReceiveBridgeMessage(firstDelivery.Envelope, true, 126);
        host.ReceiveBridgeMessage(secondDelivery.Envelope, true, 127);
        BridgeMessageFlowResult duplicateNotification = browser.ReceiveBridgeMessage(first.Envelope, true, 128);
        BridgeMessageFlowResult duplicateAcknowledgement = host.ReceiveBridgeMessage(duplicateNotification.Envelope, true, 129);

        BridgeMessageFlowResult outbound = host.SendBridgeRequest(Request("outbound-timeout", "slow", 5), UiBridgeMessageKind.Request, 130);
        browser.ReceiveBridgeMessage(outbound.Envelope, true, 131);
        BridgeMessageFlowResult inbound = browser.SendBridgeRequest(Request("inbound-timeout", "slow", 6), UiBridgeMessageKind.Request, 132);
        host.ReceiveBridgeMessage(inbound.Envelope, true, 133);
        BridgeMessageFlowResult early = host.SweepBridgeRequestTimeouts(134);
        BridgeMessageFlowResult timedOut = host.SweepBridgeRequestTimeouts(139);
        BridgeMessageFlowResult remoteCancellation = browser.ReceiveBridgeMessage(timedOut.Envelopes[0], true, 140);
        BridgeMessageFlowResult remoteError = browser.ReceiveBridgeMessage(timedOut.Envelopes[1], true, 141);

        UiBridgeEnvelope repeatedRequest = CopyRequestEnvelope(inbound.Envelope!, inbound.Envelope!.Sequence + 1);
        BridgeMessageFlowResult replay = host.ReceiveBridgeMessage(repeatedRequest, true, 142);
        BridgeMessageFlowResult replayAccepted = browser.ReceiveBridgeMessage(replay.Envelope, true, 143);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Backpressured, Is.True);
            Assert.That(blocked.Code, Is.EqualTo("bridge.acknowledgement-backpressure"));
            Assert.That(host.PendingAcknowledgements, Is.Empty);
            Assert.That(duplicateNotification.Duplicate, Is.True);
            Assert.That(duplicateAcknowledgement.Duplicate, Is.True);
            Assert.That(early.Applied, Is.False);
            Assert.That(timedOut.TimedOut, Is.True);
            Assert.That(timedOut.CorrelationIds, Is.EqualTo(new[] { "outbound-timeout", "inbound-timeout" }));
            Assert.That(timedOut.Envelopes.Select(item => item.Kind), Is.EqualTo(new[] { UiBridgeMessageKind.Cancel, UiBridgeMessageKind.Error }));
            Assert.That(remoteCancellation.Success, Is.True);
            Assert.That(remoteError.Error!.Code, Is.EqualTo("bridge.request-timeout"));
            Assert.That(replay.Duplicate, Is.True);
            Assert.That(replay.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Error));
            Assert.That(replayAccepted.Duplicate, Is.True);
            Assert.That(host.PendingRequests, Is.Empty);
            Assert.That(browser.PendingRequests, Is.Empty);
        });
    }

    [Test]
    public void ReassemblesRedactsAndResynchronizesAcrossReplacementSession()
    {
        (BridgeSession senderSession, BridgeSession receiverSession) = ReadySessions(8, true);
        UiBridgeEnvelope sensitive = new UiBridgeEnvelope
        {
            Version = BridgeProtocolVersions.Current,
            Session = senderSession.Session,
            Sequence = 1,
            CorrelationId = "sensitive",
            Origin = BridgeProtocolNames.HostOrigin,
            Kind = UiBridgeMessageKind.Request,
            Request = new UiBridgeRequest
            {
                CorrelationId = "sensitive",
                Operation = "login",
                Arguments = new UiValueEntry[]
                {
                    new UiValueEntry { Key = "accessToken", Value = Text("token-value") },
                    new UiValueEntry { Key = "displayName", Value = Text("Ada") }
                },
                TimeoutMilliseconds = 100
            }
        };
        BridgeCodec codec = new BridgeCodec();
        byte[] bytes = Encoding.UTF8.GetBytes(codec.SerializeBridgeEnvelope(sensitive, 4096));
        BridgeChunkCore sender = new BridgeChunkCore(senderSession, 96, 4096, 4, 500);
        BridgeChunkCore receiver = new BridgeChunkCore(receiverSession, 96, 4096, 4, 500);
        BridgeChunkOperationResult split = sender.SplitBridgeMessage("sensitive-envelope", bytes, 121);
        BridgeChunkOperationResult assembled = new BridgeChunkOperationResult();
        int index = 0;
        while (index < split.Envelopes.Length)
        {
            assembled = receiver.AcceptBridgeChunk(split.Envelopes[index], true, 122 + index);
            index++;
        }

        UiBridgeEnvelope decoded = codec.DeserializeBridgeEnvelope(Encoding.UTF8.GetString(assembled.Data!), 4096);
        BridgeDiagnosticRedactionResult redacted = new BridgeDiagnosticRedactor().RedactBridgeDiagnostic(decoded, 4096);

        UpdateBatchCore updates = new UpdateBatchCore();
        updates.InitializeUpdateHistory(Snapshot(0, "initial"), 4);
        UpdateBatchOperationResult update = updates.BeginUpdateTransaction(Snapshot(1, "updated"), UiUpdatePriority.Normal, null);
        updates.AcknowledgeUpdateTransaction(update.Batch!.Id, 1);
        BridgeReconnectCore reconnect = new BridgeReconnectCore(senderSession, updates);
        reconnect.CaptureBridgeHandoff(new UiHydrationManifest { State = new UiValueEntry[] { new UiValueEntry { Key = "mode", Value = Text("saved") } } }, new UiPreservationState { FocusedNodeId = "field" });
        BridgeReconnectResult reconnecting = reconnect.BeginBridgeReconnect();
        BridgeSession replacement = new BridgeSession(null, null);
        replacement.Session = "replacement-session";
        replacement.State = UiTransportState.Ready;
        BridgeReconnectResult resync = reconnect.CompleteBridgeReconnect(replacement, 0);
        BridgeReconnectResult stale = reconnect.CreateBridgeResync("session-1", 0);

        Assert.Multiple(() =>
        {
            Assert.That(split.Envelopes.Length, Is.GreaterThan(1));
            Assert.That(assembled.Complete, Is.True);
            Assert.That(assembled.Data, Is.EqualTo(bytes));
            Assert.That(redacted.Success, Is.True);
            Assert.That(redacted.Envelope!.Session, Is.EqualTo(BridgeDiagnosticRedactor.RedactedText));
            Assert.That(redacted.Envelope.Request!.Arguments.Single(item => item.Key == "accessToken").Value.StringValue, Is.EqualTo(BridgeDiagnosticRedactor.RedactedText));
            Assert.That(redacted.Envelope.Request.Arguments.Single(item => item.Key == "displayName").Value.StringValue, Is.EqualTo("Ada"));
            Assert.That(reconnecting.State, Is.EqualTo(UiTransportState.Reconnecting));
            Assert.That(resync.Success, Is.True);
            Assert.That(resync.Code, Is.EqualTo("bridge.reconnect-incremental"));
            Assert.That(resync.Batches.Select(item => item.Revision), Is.EqualTo(new[] { 1 }));
            Assert.That(resync.ApplicationState.Single().Value.StringValue, Is.EqualTo("saved"));
            Assert.That(resync.Focus.FocusedNodeId, Is.EqualTo("field"));
            Assert.That(stale.IsStale, Is.True);
            Assert.That(stale.Code, Is.EqualTo("bridge.reconnect-stale-session"));
        });
    }

    private static (BridgeMessageFlow Host, BridgeMessageFlow Browser) ReadyFlows(int maximumPendingRequests, bool supportsChunking)
    {
        (BridgeSession host, BridgeSession browser) = ReadySessions(maximumPendingRequests, supportsChunking);
        return (new BridgeMessageFlow(host), new BridgeMessageFlow(browser));
    }

    private static (BridgeSession Host, BridgeSession Browser) ReadySessions(int maximumPendingRequests, bool supportsChunking)
    {
        BridgeSession host = Session(BridgeProtocolNames.BrowserOrigin, maximumPendingRequests, supportsChunking);
        BridgeSession browser = Session(BridgeProtocolNames.HostOrigin, maximumPendingRequests, supportsChunking);
        BridgeSessionOperation initialize = host.BeginBridgeHandshake("session-1", "nonce-1", 100);
        BridgeSessionOperation ready = browser.AcceptBridgeInitialize(initialize.Envelope, true, 110);
        BridgeSessionOperation accepted = host.AcceptBridgeReady(ready.Envelope, true, 120);
        Assert.That(accepted.Success, Is.True);
        return (host, browser);
    }

    private static BridgeSession Session(string expectedOrigin, int maximumPendingRequests, bool supportsChunking)
    {
        return new BridgeSession(
            new UiBridgeOptions
            {
                Version = BridgeProtocolVersions.Current,
                Origin = expectedOrigin,
                RequireMainFrame = true,
                MaximumMessageLength = 4096,
                MaximumPendingRequests = maximumPendingRequests,
                TimeoutMilliseconds = 1000
            },
            new UiTransportCapabilities
            {
                State = UiTransportState.Starting,
                Version = BridgeProtocolVersions.Current,
                SupportsBinary = supportsChunking,
                SupportsChunking = supportsChunking,
                Capabilities = new UiCapability[] { new UiCapability { Name = "chunking", IsSupported = supportsChunking, Version = "1" } }
            });
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

    private static UiBridgeEnvelope CopyRequestEnvelope(UiBridgeEnvelope source, int sequence)
    {
        UiBridgeRequest request = source.Request!;
        return new UiBridgeEnvelope
        {
            Version = source.Version,
            Session = source.Session,
            Sequence = sequence,
            CorrelationId = source.CorrelationId,
            Origin = source.Origin,
            Kind = source.Kind,
            Request = new UiBridgeRequest
            {
                CorrelationId = request.CorrelationId,
                Operation = request.Operation,
                Arguments = request.Arguments.Select(item => new UiValueEntry { Key = item.Key, Value = ValueJsonCodec.CloneNode(item.Value) }).ToArray(),
                TimeoutMilliseconds = request.TimeoutMilliseconds
            }
        };
    }

    private static DomSnapshot Snapshot(int revision, string text)
    {
        DomNode textNode = new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = "text",
            Id = "text",
            Text = text,
            SubtreeHash = "text-" + revision
        };
        DomNode root = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "root",
            Id = "root",
            Name = HtmlElements.Division,
            Children = new DomNode[] { textNode },
            SubtreeHash = "root-" + revision
        };
        return new DomSnapshot
        {
            SessionId = "render-session",
            RootSeed = "seed",
            Revision = revision,
            Roots = new DomNode[] { root },
            ContentHash = "content-" + revision
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
