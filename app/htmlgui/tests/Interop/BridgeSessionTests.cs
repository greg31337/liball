using htmlgui;

namespace htmlgui.Tests;

public class BridgeSessionTests
{
    [Test]
    public void CompletesNonceSessionAndCapabilityHandshake()
    {
        BridgeSession host = HostSession();
        BridgeSession browser = BrowserSession();

        BridgeSessionOperation initialize = host.BeginBridgeHandshake("session-1", "nonce-1", 100);
        BridgeSessionOperation ready = browser.AcceptBridgeInitialize(initialize.Envelope, true, 110);
        BridgeSessionOperation accepted = host.AcceptBridgeReady(ready.Envelope, true, 120);

        Assert.Multiple(() =>
        {
            Assert.That(initialize.Success, Is.True);
            Assert.That(initialize.State, Is.EqualTo(UiTransportState.Handshaking));
            Assert.That(initialize.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Initialize));
            Assert.That(initialize.Envelope.Origin, Is.EqualTo(BridgeProtocolNames.HostOrigin));
            Assert.That(ready.Success, Is.True);
            Assert.That(ready.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Ready));
            Assert.That(ready.Envelope.Origin, Is.EqualTo(BridgeProtocolNames.BrowserOrigin));
            Assert.That(browser.State, Is.EqualTo(UiTransportState.Ready));
            Assert.That(accepted.Success, Is.True);
            Assert.That(host.State, Is.EqualTo(UiTransportState.Ready));
            Assert.That(host.Session, Is.EqualTo("session-1"));
            Assert.That(host.Nonce, Is.EqualTo("nonce-1"));
            Assert.That(host.NegotiatedCapabilities!.SupportsBinary, Is.True);
            Assert.That(host.NegotiatedCapabilities.SupportsChunking, Is.False);
            Assert.That(Capability(host.NegotiatedCapabilities, "binary").IsSupported, Is.True);
            Assert.That(Capability(host.NegotiatedCapabilities, "menu").IsSupported, Is.False);
            Assert.That(Capability(host.NegotiatedCapabilities, "host-only").IsSupported, Is.False);
        });
    }

    [Test]
    public void RejectsWrongFrameSessionAndNonceWithoutAdvancingHandshake()
    {
        BridgeSession host = HostSession();
        BridgeSession browser = BrowserSession();
        BridgeSessionOperation initialize = host.BeginBridgeHandshake("session-1", "nonce-1", 100);
        BridgeSessionOperation ready = browser.AcceptBridgeInitialize(initialize.Envelope, true, 110);
        UiBridgeEnvelope envelope = ready.Envelope!;

        BridgeSessionOperation wrongFrame = host.AcceptBridgeReady(envelope, false, 120);
        envelope.Session = "other-session";
        BridgeSessionOperation wrongSession = host.AcceptBridgeReady(envelope, true, 120);
        envelope.Session = "session-1";
        SetPayloadNonce(envelope.Payload!, "wrong-nonce");
        BridgeSessionOperation wrongNonce = host.AcceptBridgeReady(envelope, true, 120);
        SetPayloadNonce(envelope.Payload!, "nonce-1");
        BridgeSessionOperation accepted = host.AcceptBridgeReady(envelope, true, 120);

        Assert.Multiple(() =>
        {
            Assert.That(wrongFrame.Code, Is.EqualTo("bridge.frame-rejected"));
            Assert.That(wrongSession.Code, Is.EqualTo("bridge.session-mismatch"));
            Assert.That(wrongNonce.Code, Is.EqualTo("bridge.nonce-mismatch"));
            Assert.That(accepted.Success, Is.True);
            Assert.That(host.State, Is.EqualTo(UiTransportState.Ready));
            Assert.That(host.LastInboundSequence, Is.EqualTo(envelope.Sequence));
        });
    }

    [Test]
    public void EmitsHeartbeatResetsPeerActivityAndFaultsOnSessionTimeout()
    {
        (BridgeSession host, BridgeSession browser) = ReadyPair();

        BridgeSessionOperation early = browser.SweepBridgeTimeout(609);
        BridgeSessionOperation heartbeat = browser.SweepBridgeTimeout(610);
        BridgeSessionOperation received = host.AcceptBridgeHeartbeat(heartbeat.Envelope, true, 611);
        BridgeSessionOperation hostHeartbeat = host.SweepBridgeTimeout(620);
        BridgeSessionOperation stillAlive = host.SweepBridgeTimeout(1610);
        BridgeSessionOperation timedOut = host.SweepBridgeTimeout(1611);

        Assert.Multiple(() =>
        {
            Assert.That(early.Success, Is.True);
            Assert.That(early.Applied, Is.False);
            Assert.That(early.Envelope, Is.Null);
            Assert.That(heartbeat.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Heartbeat));
            Assert.That(received.Success, Is.True);
            Assert.That(hostHeartbeat.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Heartbeat));
            Assert.That(stillAlive.TimedOut, Is.False);
            Assert.That(timedOut.Code, Is.EqualTo("bridge.session-timeout"));
            Assert.That(timedOut.TimedOut, Is.True);
            Assert.That(host.State, Is.EqualTo(UiTransportState.Faulted));
        });
    }

    [Test]
    public void PerformsTwoSidedOrderlyShutdown()
    {
        (BridgeSession host, BridgeSession browser) = ReadyPair();

        BridgeSessionOperation request = host.BeginBridgeShutdown(200);
        BridgeSessionOperation response = browser.AcceptBridgeShutdown(request.Envelope, true, 201);
        BridgeSessionOperation hostClosed = host.AcceptBridgeShutdown(response.Envelope, true, 202);
        BridgeSessionOperation browserClosed = browser.CompleteBridgeShutdown();

        Assert.Multiple(() =>
        {
            Assert.That(request.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Shutdown));
            Assert.That(request.State, Is.EqualTo(UiTransportState.Closing));
            Assert.That(response.Envelope!.Kind, Is.EqualTo(UiBridgeMessageKind.Shutdown));
            Assert.That(response.State, Is.EqualTo(UiTransportState.Closing));
            Assert.That(hostClosed.Success, Is.True);
            Assert.That(hostClosed.Envelope, Is.Null);
            Assert.That(host.State, Is.EqualTo(UiTransportState.Stopped));
            Assert.That(browserClosed.Success, Is.True);
            Assert.That(browser.State, Is.EqualTo(UiTransportState.Stopped));
        });
    }

    [Test]
    public void BoundsClosingAndRejectsInvalidInitialization()
    {
        BridgeSession invalid = new BridgeSession(HostOptions(), new UiTransportCapabilities());
        BridgeSessionOperation invalidCapabilities = invalid.BeginBridgeHandshake("session", "nonce", 0);
        (BridgeSession host, BridgeSession browser) = ReadyPair();
        BridgeSessionOperation shutdown = host.BeginBridgeShutdown(200);
        BridgeSessionOperation waiting = host.SweepBridgeTimeout(1199);
        BridgeSessionOperation expired = host.SweepBridgeTimeout(1200);

        Assert.Multiple(() =>
        {
            Assert.That(invalidCapabilities.Code, Is.EqualTo("bridge.unsupported-capability-version"));
            Assert.That(shutdown.Success, Is.True);
            Assert.That(waiting.State, Is.EqualTo(UiTransportState.Closing));
            Assert.That(expired.Success, Is.True);
            Assert.That(expired.TimedOut, Is.True);
            Assert.That(expired.Code, Is.EqualTo("bridge.shutdown-timeout"));
            Assert.That(host.State, Is.EqualTo(UiTransportState.Stopped));
            Assert.That(browser.State, Is.EqualTo(UiTransportState.Ready));
        });
    }

    private static (BridgeSession Host, BridgeSession Browser) ReadyPair()
    {
        BridgeSession host = HostSession();
        BridgeSession browser = BrowserSession();
        BridgeSessionOperation initialize = host.BeginBridgeHandshake("session-1", "nonce-1", 100);
        BridgeSessionOperation ready = browser.AcceptBridgeInitialize(initialize.Envelope, true, 110);
        BridgeSessionOperation accepted = host.AcceptBridgeReady(ready.Envelope, true, 120);
        Assert.That(accepted.Success, Is.True);
        return (host, browser);
    }

    private static BridgeSession HostSession()
    {
        return new BridgeSession(HostOptions(), new UiTransportCapabilities
        {
            State = UiTransportState.Starting,
            Version = BridgeProtocolVersions.Current,
            SupportsBinary = true,
            SupportsChunking = true,
            Capabilities = new UiCapability[]
            {
                new UiCapability { Name = "binary", IsSupported = true, Version = "1" },
                new UiCapability { Name = "menu", IsSupported = true, Version = "2" },
                new UiCapability { Name = "host-only", IsSupported = true }
            }
        });
    }

    private static BridgeSession BrowserSession()
    {
        return new BridgeSession(BrowserOptions(), new UiTransportCapabilities
        {
            State = UiTransportState.Starting,
            Version = BridgeProtocolVersions.Current,
            SupportsBinary = true,
            SupportsChunking = false,
            Capabilities = new UiCapability[]
            {
                new UiCapability { Name = "binary", IsSupported = true, Version = "1" },
                new UiCapability { Name = "menu", IsSupported = true, Version = "3" },
                new UiCapability { Name = "browser-only", IsSupported = true }
            }
        });
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

    private static UiBridgeOptions BrowserOptions()
    {
        UiBridgeOptions options = HostOptions();
        options.Origin = BridgeProtocolNames.HostOrigin;
        return options;
    }

    private static UiCapability Capability(UiTransportCapabilities capabilities, string name)
    {
        return capabilities.Capabilities.Single(item => item.Name == name);
    }

    private static void SetPayloadNonce(UiValue payload, string nonce)
    {
        UiValueEntry entry = payload.ObjectValue!.Single(item => item.Key == BridgeProtocolNames.Nonce);
        entry.Value.StringValue = nonce;
    }
}
