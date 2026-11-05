using System.Collections.Generic;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiWebViewBridgeTests
{
    [Test]
    public void StartsTransportCompletesHandshakeAndReturnsDetachedNegotiatedCapabilities()
    {
        BridgeTestContext context = ReadyContext();

        UiTransportCapabilities first = context.Bridge.GetUiBridgeCapabilities();
        first.Capabilities[0].Name = "mutated";
        UiTransportCapabilities second = context.Bridge.GetUiBridgeCapabilities();

        Assert.Multiple(() =>
        {
            Assert.That(context.Started.Success, Is.True);
            Assert.That(context.Ready.Success, Is.True);
            Assert.That(context.Transport.StartCount, Is.EqualTo(1));
            Assert.That(context.Transport.Messages[0].Kind, Is.EqualTo(UiBridgeMessageKind.Initialize));
            Assert.That(context.Bridge.Session.State, Is.EqualTo(UiTransportState.Ready));
            Assert.That(second.State, Is.EqualTo(UiTransportState.Ready));
            Assert.That(second.SupportsBinary, Is.True);
            Assert.That(second.SupportsChunking, Is.False);
            Assert.That(second.Capabilities[0].Name, Is.EqualTo("binary"));
            Assert.That(context.Handler.Messages, Is.Empty);
        });
    }

    [Test]
    public void SendsRequestsAndNotificationsAndProcessesTheirAcknowledgements()
    {
        BridgeTestContext context = ReadyContext();
        UiResult notification = context.Bridge.SendUiBridgeNotification(UiBridgeMessageKind.Log, Text("notice"), 121);
        UiBridgeEnvelope notificationEnvelope = context.Transport.Messages.Last();
        BridgeMessageFlowResult receivedNotification = context.Browser.ReceiveBridgeMessage(notificationEnvelope, true, 122);
        UiResult acknowledged = context.Bridge.ReceiveUiBridgeMessage(receivedNotification.Envelope, true, 123);
        UiBridgeRequest request = new UiBridgeRequest
        {
            CorrelationId = "request-1",
            Operation = "lookup",
            Arguments = new UiValueEntry[] { new UiValueEntry { Key = "id", Value = Text("7") } },
            TimeoutMilliseconds = 100
        };
        UiResult requested = context.Bridge.SendUiBridgeRequest(request, 124);
        request.Arguments[0].Value.StringValue = "mutated";
        UiBridgeEnvelope requestEnvelope = context.Transport.Messages.Last();

        Assert.Multiple(() =>
        {
            Assert.That(notification.Success, Is.True);
            Assert.That(notificationEnvelope.Kind, Is.EqualTo(UiBridgeMessageKind.Log));
            Assert.That(notificationEnvelope.Payload!.StringValue, Is.EqualTo("notice"));
            Assert.That(acknowledged.Success, Is.True);
            Assert.That(context.Bridge.Flow.LastAcknowledgedSequence, Is.EqualTo(notificationEnvelope.Sequence));
            Assert.That(requested.Success, Is.True);
            Assert.That(requestEnvelope.Kind, Is.EqualTo(UiBridgeMessageKind.Request));
            Assert.That(requestEnvelope.Request!.Operation, Is.EqualTo("lookup"));
            Assert.That(requestEnvelope.Request.Arguments.Single().Value.StringValue, Is.EqualTo("7"));
            Assert.That(context.Handler.Messages.Single().Kind, Is.EqualTo(UiBridgeMessageKind.Acknowledgement));
        });
    }

    [Test]
    public void DispatchesDetachedInboundNotificationsAndSendsAcknowledgements()
    {
        BridgeTestContext context = ReadyContext();
        context.Handler.MutateMessages = true;
        BridgeMessageFlowResult outgoing = context.Browser.SendBridgeNotification(UiBridgeMessageKind.Event, Text("click"), 121);

        UiResult received = context.Bridge.ReceiveUiBridgeMessage(outgoing.Envelope, true, 122);
        UiBridgeEnvelope acknowledgement = context.Transport.Messages.Last();
        BridgeMessageFlowResult browserAcknowledged = context.Browser.ReceiveBridgeMessage(acknowledgement, true, 123);

        Assert.Multiple(() =>
        {
            Assert.That(received.Success, Is.True);
            Assert.That(context.Handler.Messages.Single().Payload!.StringValue, Is.EqualTo("mutated by handler"));
            Assert.That(outgoing.Envelope!.Payload!.StringValue, Is.EqualTo("click"));
            Assert.That(acknowledgement.Kind, Is.EqualTo(UiBridgeMessageKind.Acknowledgement));
            Assert.That(browserAcknowledged.Success, Is.True);
            Assert.That(context.Browser.LastAcknowledgedSequence, Is.EqualTo(outgoing.Envelope.Sequence));
        });
    }

    [Test]
    public void CompletesInboundRequestsFromHandlerValuesAndFailures()
    {
        BridgeTestContext context = ReadyContext();
        context.Handler.Result = new UiResult { Success = true, Value = Text("answer") };
        BridgeMessageFlowResult request = context.Browser.SendBridgeRequest(Request("request-1", "lookup"), UiBridgeMessageKind.Request, 121);
        UiResult completed = context.Bridge.ReceiveUiBridgeMessage(request.Envelope, true, 122);
        UiBridgeEnvelope response = context.Transport.Messages.Last();
        BridgeMessageFlowResult browserResponse = context.Browser.ReceiveBridgeMessage(response, true, 123);

        context.Handler.Result = Failure("handler.denied", "Denied");
        BridgeMessageFlowResult deniedRequest = context.Browser.SendBridgeRequest(Request("request-2", "denied"), UiBridgeMessageKind.Request, 124);
        UiResult denied = context.Bridge.ReceiveUiBridgeMessage(deniedRequest.Envelope, true, 125);
        UiBridgeEnvelope error = context.Transport.Messages.Last();
        BridgeMessageFlowResult browserError = context.Browser.ReceiveBridgeMessage(error, true, 126);

        Assert.Multiple(() =>
        {
            Assert.That(completed.Success, Is.True);
            Assert.That(response.Kind, Is.EqualTo(UiBridgeMessageKind.Response));
            Assert.That(browserResponse.Response!.Value!.StringValue, Is.EqualTo("answer"));
            Assert.That(denied.Success, Is.False);
            Assert.That(error.Kind, Is.EqualTo(UiBridgeMessageKind.Error));
            Assert.That(browserError.Error!.Code, Is.EqualTo("handler.denied"));
            Assert.That(browserError.Error.Message, Is.EqualTo("Denied"));
            Assert.That(context.Handler.Messages.Select(item => item.Request!.Operation), Is.EqualTo(new string[] { "lookup", "denied" }));
        });
    }

    [Test]
    public void SweepsRequestAndHeartbeatTimeoutsAndPerformsOrderlyShutdown()
    {
        BridgeTestContext context = ReadyContext();
        context.Bridge.SendUiBridgeRequest(Request("slow", "wait", 10), 121);
        UiResult swept = context.Bridge.SweepUiBridgeTimeouts(131);
        UiBridgeMessageKind[] timeoutKinds = context.Transport.Messages.Skip(2).Select(item => item.Kind).ToArray();
        UiResult heartbeat = context.Bridge.SweepUiBridgeTimeouts(620);
        UiBridgeEnvelope heartbeatEnvelope = context.Transport.Messages.Last();
        BridgeSessionOperation receivedHeartbeat = context.Browser.Session.AcceptBridgeHeartbeat(heartbeatEnvelope, true, 621);
        UiResult shutdown = context.Bridge.ShutdownUiBridge(622);
        UiBridgeEnvelope shutdownEnvelope = context.Transport.Messages.Last();
        BridgeSessionOperation browserShutdown = context.Browser.Session.AcceptBridgeShutdown(shutdownEnvelope, true, 623);
        UiResult closed = context.Bridge.ReceiveUiBridgeMessage(browserShutdown.Envelope, true, 624);

        Assert.Multiple(() =>
        {
            Assert.That(swept.Success, Is.True);
            Assert.That(timeoutKinds, Does.Contain(UiBridgeMessageKind.Cancel));
            Assert.That(heartbeat.Success, Is.True);
            Assert.That(heartbeatEnvelope.Kind, Is.EqualTo(UiBridgeMessageKind.Heartbeat));
            Assert.That(receivedHeartbeat.Success, Is.True);
            Assert.That(shutdown.Success, Is.True);
            Assert.That(shutdownEnvelope.Kind, Is.EqualTo(UiBridgeMessageKind.Shutdown));
            Assert.That(closed.Success, Is.True);
            Assert.That(context.Bridge.Session.State, Is.EqualTo(UiTransportState.Stopped));
            Assert.That(context.Transport.StopCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReconnectsWithANewHandshakeAndRejectsMessagesFromTheRetiredSession()
    {
        BridgeTestContext context = ReadyContext();
        UiResult reconnecting = context.Bridge.ReconnectUiBridge("session-2", "nonce-2", 0, 200);
        UiBridgeEnvelope initialize = context.Transport.Messages.Last();
        BridgeSession browserSession = BrowserSession();
        BridgeSessionOperation browserReady = browserSession.AcceptBridgeInitialize(initialize, true, 201);
        UiResult reconnected = context.Bridge.ReceiveUiBridgeMessage(browserReady.Envelope, true, 202);
        UiTransportCapabilities capabilities = context.Bridge.GetUiBridgeCapabilities();
        UiBridgeEnvelope stale = new UiBridgeEnvelope
        {
            Version = BridgeProtocolVersions.Current,
            Session = "session-1",
            Sequence = 1,
            Origin = BridgeProtocolNames.BrowserOrigin,
            Kind = UiBridgeMessageKind.Log,
            Payload = Text("stale")
        };
        UiResult rejected = context.Bridge.ReceiveUiBridgeMessage(stale, true, 203);

        Assert.Multiple(() =>
        {
            Assert.That(reconnecting.Success, Is.True);
            Assert.That(initialize.Kind, Is.EqualTo(UiBridgeMessageKind.Initialize));
            Assert.That(initialize.Session, Is.EqualTo("session-2"));
            Assert.That(context.Transport.StartCount, Is.EqualTo(2));
            Assert.That(context.Transport.StopCount, Is.EqualTo(1));
            Assert.That(reconnected.Success, Is.True);
            Assert.That(context.Bridge.Session.State, Is.EqualTo(UiTransportState.Ready));
            Assert.That(context.Bridge.Session.Session, Is.EqualTo("session-2"));
            Assert.That(capabilities.State, Is.EqualTo(UiTransportState.Ready));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Diagnostics.Single().Code, Is.EqualTo("bridge.session-mismatch"));
            Assert.That(context.Handler.Messages, Is.Empty);
        });
    }

    [Test]
    public void ValidatesDependenciesStatesAcknowledgementsAndTransportFailures()
    {
        RecordingTransport transport = new RecordingTransport(Capabilities());
        RecordingHandler handler = new RecordingHandler();
        Assert.Throws<System.ArgumentNullException>(() => new UiWebViewBridge(null, transport, handler));
        Assert.Throws<System.ArgumentNullException>(() => new UiWebViewBridge(HostOptions(), null, handler));
        Assert.Throws<System.ArgumentNullException>(() => new UiWebViewBridge(HostOptions(), transport, null));
        UiWebViewBridge bridge = new UiWebViewBridge(HostOptions(), transport, handler);
        UiResult beforeReady = bridge.SendUiBridgeNotification(UiBridgeMessageKind.Log, null, 0);
        UiResult invalidAcknowledgement = bridge.AcknowledgeUiBridgeMessage(1, 0);
        transport.StartResult = Failure("transport.start", "Cannot start");
        UiResult startFailed = bridge.StartUiBridge("session-1", "nonce-1", 0);

        BridgeTestContext ready = ReadyContext();
        int stoppedBefore = ready.Transport.StopCount;
        UiResult reused = ready.Bridge.ReconnectUiBridge("session-1", "nonce-2", 0, 200);
        UiResult future = ready.Bridge.ReconnectUiBridge("session-2", "nonce-2", 1, 200);

        Assert.Multiple(() =>
        {
            Assert.That(beforeReady.Diagnostics.Single().Code, Is.EqualTo("bridge.invalid-state"));
            Assert.That(invalidAcknowledgement.Diagnostics.Single().Code, Is.EqualTo("bridge.invalid-state"));
            Assert.That(startFailed.Success, Is.False);
            Assert.That(startFailed.Diagnostics.Single().Code, Is.EqualTo("transport.start"));
            Assert.That(bridge.Session.State, Is.EqualTo(UiTransportState.Stopped));
            Assert.That(reused.Diagnostics.Single().Code, Is.EqualTo("bridge.reconnect-session-reused"));
            Assert.That(future.Diagnostics.Single().Code, Is.EqualTo("bridge.reconnect-future-revision"));
            Assert.That(ready.Transport.StopCount, Is.EqualTo(stoppedBefore));
            Assert.That(ready.Bridge.Session.State, Is.EqualTo(UiTransportState.Ready));
        });
    }

    private static BridgeTestContext ReadyContext()
    {
        RecordingTransport transport = new RecordingTransport(Capabilities());
        RecordingHandler handler = new RecordingHandler();
        UiWebViewBridge bridge = new UiWebViewBridge(HostOptions(), transport, handler);
        UiResult started = bridge.StartUiBridge("session-1", "nonce-1", 100);
        BridgeSession browserSession = BrowserSession();
        BridgeSessionOperation browserReady = browserSession.AcceptBridgeInitialize(transport.Messages.Single(), true, 110);
        UiResult ready = bridge.ReceiveUiBridgeMessage(browserReady.Envelope, true, 120);
        return new BridgeTestContext
        {
            Bridge = bridge,
            Transport = transport,
            Handler = handler,
            Browser = new BridgeMessageFlow(browserSession),
            Started = started,
            Ready = ready
        };
    }

    private static BridgeSession BrowserSession()
    {
        UiBridgeOptions options = HostOptions();
        options.Origin = BridgeProtocolNames.HostOrigin;
        return new BridgeSession(options, Capabilities());
    }

    private static UiBridgeOptions HostOptions()
    {
        return new UiBridgeOptions
        {
            Version = BridgeProtocolVersions.Current,
            Origin = BridgeProtocolNames.BrowserOrigin,
            RequireMainFrame = true,
            MaximumMessageLength = 4096,
            MaximumPendingRequests = 8,
            TimeoutMilliseconds = 1000
        };
    }

    private static UiTransportCapabilities Capabilities()
    {
        return new UiTransportCapabilities
        {
            State = UiTransportState.Starting,
            Version = BridgeProtocolVersions.Current,
            SupportsBinary = true,
            SupportsChunking = false,
            Capabilities = new UiCapability[]
            {
                new UiCapability { Name = "binary", IsSupported = true, Version = "1" }
            }
        };
    }

    private static UiBridgeRequest Request(string correlationId, string operation)
    {
        return Request(correlationId, operation, 100);
    }

    private static UiBridgeRequest Request(string correlationId, string operation, int timeoutMilliseconds)
    {
        return new UiBridgeRequest
        {
            CorrelationId = correlationId,
            Operation = operation,
            TimeoutMilliseconds = timeoutMilliseconds
        };
    }

    private static UiValue Text(string text)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = text };
    }

    private static UiResult Failure(string code, string message)
    {
        return new UiResult
        {
            Success = false,
            Diagnostics = new UiDiagnostic[]
            {
                new UiDiagnostic { Severity = UiDiagnosticSeverity.Error, Code = code, Message = message }
            }
        };
    }
}

internal class BridgeTestContext
{
    public UiWebViewBridge Bridge = null!;
    public RecordingTransport Transport = null!;
    public RecordingHandler Handler = null!;
    public BridgeMessageFlow Browser = null!;
    public UiResult Started = new UiResult();
    public UiResult Ready = new UiResult();
}

internal class RecordingTransport : IUiMessageTransport
{
    public UiTransportCapabilities Capabilities;
    public List<UiBridgeEnvelope> Messages = new List<UiBridgeEnvelope>();
    public UiResult StartResult = new UiResult { Success = true };
    public UiResult SendResult = new UiResult { Success = true };
    public UiResult StopResult = new UiResult { Success = true };
    public int StartCount;
    public int StopCount;

    public RecordingTransport(UiTransportCapabilities capabilities)
    {
        Capabilities = capabilities;
    }

    public UiResult StartUiMessageTransport()
    {
        StartCount++;
        return StartResult;
    }

    public UiResult SendUiMessage(UiBridgeEnvelope message)
    {
        Messages.Add(message);
        return SendResult;
    }

    public UiResult StopUiMessageTransport()
    {
        StopCount++;
        return StopResult;
    }

    public UiTransportCapabilities GetUiMessageTransportCapabilities()
    {
        return Capabilities;
    }
}

internal class RecordingHandler : IUiBridgeMessageHandler
{
    public List<UiBridgeEnvelope> Messages = new List<UiBridgeEnvelope>();
    public UiResult Result = new UiResult { Success = true };
    public bool MutateMessages;

    public UiResult HandleUiBridgeMessage(UiBridgeEnvelope message)
    {
        Messages.Add(message);
        if (MutateMessages && message.Payload != null)
        {
            message.Payload.StringValue = "mutated by handler";
        }

        return Result;
    }
}
