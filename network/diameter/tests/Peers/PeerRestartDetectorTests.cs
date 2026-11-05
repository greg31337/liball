using System.Net;

namespace diameter.Tests;

public class PeerRestartDetectorTests
{
    [Test]
    public void FirstNonzeroCapabilityValueEstablishesBaselineWithoutRestart()
    {
        RestartTestContext context = CreateRestartTestContext();

        PeerRestartObservation result =
            context.Detector.ObservePeerCapabilities(
                "peer.example",
                CreateRestartTestCapabilities(100));

        Assert.Multiple(() =>
        {
            Assert.That(result.Observed, Is.True);
            Assert.That(result.RestartDetected, Is.False);
            Assert.That(result.Code,
                Is.EqualTo("origin-state-baseline-recorded"));
            Assert.That(result.PreviousOriginStateId, Is.Zero);
            Assert.That(result.CurrentOriginStateId, Is.EqualTo(100U));
            Assert.That(result.Peer.Capabilities.OriginStateId,
                Is.EqualTo(100U));
            Assert.That(result.NotifiedSubsystems, Is.Zero);
            Assert.That(context.Request.Events, Is.Empty);
            Assert.That(context.Session.Events, Is.Empty);
            Assert.That(context.Application.Events, Is.Empty);
        });
    }

    [Test]
    public void IncreasedCapabilityValueNotifiesEverySubsystemWithOwnedEvent()
    {
        RestartTestContext context = CreateRestartTestContext();
        context.Detector.ObservePeerCapabilities(
            "peer.example",
            CreateRestartTestCapabilities(100));
        context.Request.MutateEvent = true;
        context.Clock.AdvanceDiscoveryTime(2);

        PeerRestartObservation result =
            context.Detector.ObservePeerCapabilities(
                "peer.example",
                CreateRestartTestCapabilities(105));

        Assert.Multiple(() =>
        {
            Assert.That(result.RestartDetected, Is.True);
            Assert.That(result.Code, Is.EqualTo("peer-restart-detected"));
            Assert.That(result.NotifiedSubsystems, Is.EqualTo(3));
            Assert.That(result.PeerEvent, Is.Not.Null);
            Assert.That(result.PeerEvent!.Kind,
                Is.EqualTo(DiameterPeerEventKind.RestartDetected));
            Assert.That(result.PeerEvent.PreviousOriginStateId,
                Is.EqualTo(100U));
            Assert.That(result.PeerEvent.CurrentOriginStateId,
                Is.EqualTo(105U));
            Assert.That(result.PeerEvent.OccurredAt,
                Is.EqualTo(context.Clock.UtcTime));
            Assert.That(result.PeerEvent.Peer.Capabilities.OriginStateId,
                Is.EqualTo(105U));
            Assert.That(context.Request.Events, Has.Count.EqualTo(1));
            Assert.That(context.Session.Events, Has.Count.EqualTo(1));
            Assert.That(context.Application.Events, Has.Count.EqualTo(1));
            Assert.That(context.Request.Events[0].Code, Is.EqualTo("mutated"));
            Assert.That(context.Session.Events[0].Code,
                Is.EqualTo("peer.restart.origin-state-id"));
            Assert.That(context.Application.Events[0].Code,
                Is.EqualTo("peer.restart.origin-state-id"));
            Assert.That(context.Request.Events[0],
                Is.Not.SameAs(context.Session.Events[0]));
            Assert.That(context.Session.Events[0].Peer,
                Is.Not.SameAs(context.Application.Events[0].Peer));
        });
    }

    [Test]
    public void EqualLowerAndZeroValuesDoNotInferRestart()
    {
        RestartTestContext context = CreateRestartTestContext();
        context.Detector.ObservePeerCapabilities(
            "peer.example",
            CreateRestartTestCapabilities(100));

        PeerRestartObservation equal =
            context.Detector.ObservePeerCapabilities(
                "peer.example",
                CreateRestartTestCapabilities(100));
        PeerRestartObservation lower =
            context.Detector.ObservePeerCapabilities(
                "peer.example",
                CreateRestartTestCapabilities(90));
        PeerRestartObservation zero =
            context.Detector.ObservePeerCapabilities(
                "peer.example",
                CreateRestartTestCapabilities(0));

        Assert.Multiple(() =>
        {
            Assert.That(equal.RestartDetected, Is.False);
            Assert.That(equal.Code, Is.EqualTo("origin-state-not-advanced"));
            Assert.That(lower.RestartDetected, Is.False);
            Assert.That(lower.Code, Is.EqualTo("origin-state-not-advanced"));
            Assert.That(zero.Observed, Is.False);
            Assert.That(zero.RestartDetected, Is.False);
            Assert.That(zero.Code,
                Is.EqualTo("origin-state-inference-disabled"));
            Assert.That(context.Request.Events, Is.Empty);
            Assert.That(context.Session.Events, Is.Empty);
            Assert.That(context.Application.Events, Is.Empty);
        });
    }

    [Test]
    public void MessageOriginStateAdvancementUpdatesPeerAndNotifiesSubsystems()
    {
        RestartTestContext context = CreateRestartTestContext();
        context.Detector.ObservePeerCapabilities(
            "peer.example",
            CreateRestartTestCapabilities(200));
        DiameterMessage message = CreateRestartTestMessage(
            "peer.example",
            201);

        PeerRestartObservation result = context.Detector.ObservePeerMessage(
            "peer.example",
            message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Observed, Is.True);
            Assert.That(result.RestartDetected, Is.True);
            Assert.That(result.PreviousOriginStateId, Is.EqualTo(200U));
            Assert.That(result.CurrentOriginStateId, Is.EqualTo(201U));
            Assert.That(result.Peer.Capabilities.OriginStateId,
                Is.EqualTo(201U));
            Assert.That(result.NotifiedSubsystems, Is.EqualTo(3));
            Assert.That(context.Request.Events[0].Peer.Capabilities.OriginStateId,
                Is.EqualTo(201U));
            Assert.That(context.Session.Events[0].Kind,
                Is.EqualTo(DiameterPeerEventKind.RestartDetected));
            Assert.That(context.Application.Events[0].CurrentOriginStateId,
                Is.EqualTo(201U));
        });
    }

    [Test]
    public void FirstMessageOriginStateValueOnlyEstablishesBaseline()
    {
        RestartTestContext context = CreateRestartTestContext();

        PeerRestartObservation result = context.Detector.ObservePeerMessage(
            "peer.example",
            CreateRestartTestMessage("peer.example", 50));

        Assert.Multiple(() =>
        {
            Assert.That(result.Observed, Is.True);
            Assert.That(result.RestartDetected, Is.False);
            Assert.That(result.Code,
                Is.EqualTo("origin-state-baseline-recorded"));
            Assert.That(result.Peer.Capabilities.OriginStateId,
                Is.EqualTo(50U));
            Assert.That(context.Request.Events, Is.Empty);
        });
    }

    [Test]
    public void MismatchedDuplicateAndMalformedMessageValuesAreIgnored()
    {
        RestartTestContext context = CreateRestartTestContext();
        context.Detector.ObservePeerCapabilities(
            "peer.example",
            CreateRestartTestCapabilities(300));
        DiameterMessage mismatch = CreateRestartTestMessage(
            "other.example",
            301);
        DiameterMessage duplicate = CreateRestartTestMessage(
            "peer.example",
            302);
        DiameterBaseAvpFactory factory = new DiameterBaseAvpFactory();
        duplicate.Avps.Add(factory.CreateOriginStateIdAvp(303));
        DiameterMessage malformed = CreateRestartTestMessage(
            "peer.example",
            304);
        FindRestartTestAvp(malformed, DiameterAvpCodes.OriginStateId).Data =
            new byte[3];

        PeerRestartObservation mismatched =
            context.Detector.ObservePeerMessage("peer.example", mismatch);
        PeerRestartObservation duplicated =
            context.Detector.ObservePeerMessage("peer.example", duplicate);
        PeerRestartObservation invalid =
            context.Detector.ObservePeerMessage("peer.example", malformed);

        Assert.Multiple(() =>
        {
            Assert.That(mismatched.Code,
                Is.EqualTo("origin-state-peer-mismatch"));
            Assert.That(duplicated.Code,
                Is.EqualTo("ambiguous-origin-state-id"));
            Assert.That(invalid.Code, Is.EqualTo("invalid-origin-state-id"));
            Assert.That(mismatched.RestartDetected, Is.False);
            Assert.That(duplicated.RestartDetected, Is.False);
            Assert.That(invalid.RestartDetected, Is.False);
            Assert.That(invalid.Peer.Capabilities.OriginStateId,
                Is.EqualTo(300U));
            Assert.That(context.Request.Events, Is.Empty);
            Assert.That(context.Session.Events, Is.Empty);
            Assert.That(context.Application.Events, Is.Empty);
        });
    }

    private static RestartTestContext CreateRestartTestContext()
    {
        RestartTestContext result = new RestartTestContext();
        DiameterPeerConfiguration configuration =
            new DiameterPeerConfiguration();
        configuration.Identity.Value = "peer.example";
        configuration.Realm.Value = "example";
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "peer.example";
        endpoint.Host = "peer.example";
        endpoint.Address = IPAddress.Parse("192.0.2.10");
        endpoint.Port = DiameterProtocolConstants.Port;
        endpoint.Transport = DiameterTransportKind.Tcp;
        configuration.Endpoints.Add(endpoint);
        result.Table = new PeerTable(
            result.Clock,
            new DiameterTimerConfiguration(),
            1);
        result.Table.AddOrUpdatePeerConfiguration(configuration);
        result.Detector = new PeerRestartDetector(
            result.Table,
            result.Request,
            result.Session,
            result.Application);
        return result;
    }

    private static DiameterPeerCapabilities CreateRestartTestCapabilities(
        uint originStateId)
    {
        DiameterPeerCapabilities result = new DiameterPeerCapabilities();
        result.OriginHost.Value = "peer.example";
        result.OriginRealm.Value = "example";
        result.HostAddresses.Add(IPAddress.Parse("192.0.2.10"));
        result.ProductName = "restart-test-peer";
        result.OriginStateId = originStateId;
        return result;
    }

    private static DiameterMessage CreateRestartTestMessage(
        string originHost,
        uint originStateId)
    {
        DiameterBaseAvpFactory factory = new DiameterBaseAvpFactory();
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = originHost;
        DiameterMessage result = new DiameterMessage();
        result.Avps.Add(factory.CreateOriginHostAvp(identity));
        result.Avps.Add(factory.CreateOriginStateIdAvp(originStateId));
        return result;
    }

    private static DiameterAvp FindRestartTestAvp(
        DiameterMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == code && avp.VendorId == DiameterVendorIds.Ietf)
            {
                return avp;
            }

            index = index + 1;
        }

        throw new AssertionException("Expected restart test AVP was not found.");
    }
}

internal class RestartTestContext
{
    internal FakeDiscoveryClock Clock = new FakeDiscoveryClock();
    internal RecordingPeerRestartSink Request = new RecordingPeerRestartSink();
    internal RecordingPeerRestartSink Session = new RecordingPeerRestartSink();
    internal RecordingPeerRestartSink Application =
        new RecordingPeerRestartSink();
    internal PeerTable Table = null!;
    internal PeerRestartDetector Detector = null!;
}

internal class RecordingPeerRestartSink : IPeerRestartNotificationSink
{
    internal List<DiameterPeerEvent> Events = new List<DiameterPeerEvent>();
    internal bool MutateEvent;

    public void HandlePeerRestart(DiameterPeerEvent peerEvent)
    {
        Events.Add(peerEvent);
        if (MutateEvent)
        {
            peerEvent.Code = "mutated";
            peerEvent.Peer.Capabilities.OriginStateId = 999;
        }
    }
}
