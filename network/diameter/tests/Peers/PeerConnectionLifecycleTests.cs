using System.Net;

namespace diameter.Tests;

public class PeerConnectionLifecycleTests
{
    [Test]
    public void RetrySchedulingAppliesFreshDeterministicJitter()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreateLifecyclePeerTable(clock, 10, 0, 10, 20);
        table.AddOrUpdatePeerConfiguration(CreateLifecyclePeerConfiguration(
            "peer.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset)));

        DiameterPeerSnapshot minimum =
            table.SchedulePeerTableConnectionRetry("peer.example");
        DiameterPeerSnapshot center =
            table.SchedulePeerTableConnectionRetry("peer.example");
        DiameterPeerSnapshot maximum =
            table.SchedulePeerTableConnectionRetry("peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(
                minimum.NextConnectionAttemptAt,
                Is.EqualTo(clock.UtcTime.AddMilliseconds(900)));
            Assert.That(
                center.NextConnectionAttemptAt,
                Is.EqualTo(clock.UtcTime.AddMilliseconds(1000)));
            Assert.That(
                maximum.NextConnectionAttemptAt,
                Is.EqualTo(clock.UtcTime.AddMilliseconds(1100)));
        });
    }

    [Test]
    public void DueRetryCanBeClaimedOnlyOnce()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreateLifecyclePeerTable(clock, 0, 0);
        table.AddOrUpdatePeerConfiguration(CreateLifecyclePeerConfiguration(
            "peer.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset)));
        PeerConnectionLifecycle lifecycle = new PeerConnectionLifecycle(table);

        PeerConnectionLifecycleResult first =
            lifecycle.ClaimDuePeerConnectionAttempt("peer.example");
        PeerConnectionLifecycleResult second =
            lifecycle.ClaimDuePeerConnectionAttempt("peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(first.Accepted, Is.True);
            Assert.That(first.Code, Is.EqualTo("peer-retry-claimed"));
            Assert.That(first.Peer.State,
                Is.EqualTo(DiameterPeerState.WaitConnectionAcknowledgement));
            Assert.That(first.Peer.Direction,
                Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(first.Peer.NextConnectionAttemptAt,
                Is.EqualTo(default(DateTimeOffset)));
            Assert.That(first.Peer.ReconnectCount, Is.EqualTo(1));
            Assert.That(first.Actions, Is.EqualTo(new[]
            {
                PeerStateMachineAction.InitiatorSendConnectionRequest
            }));
            Assert.That(second.Accepted, Is.False);
            Assert.That(second.Code, Is.EqualTo("peer-connection-already-active"));
            Assert.That(table.ReadDuePeerConnectionRetries(), Is.Empty);
        });
    }

    [TestCase(
        DiameterDisconnectCause.Busy,
        "peer-busy-reconnect-suppressed")]
    [TestCase(
        DiameterDisconnectCause.DoNotWantToTalkToYou,
        "peer-communication-reconnect-suppressed")]
    public void RemoteSuppressionCausesBlockAutomaticRetryUntilManualOverride(
        DiameterDisconnectCause cause,
        string expectedCode)
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreateLifecyclePeerTable(clock, 0, 0);
        table.AddOrUpdatePeerConfiguration(CreateLifecyclePeerConfiguration(
            "peer.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset)));
        PeerConnectionLifecycle lifecycle = new PeerConnectionLifecycle(table);
        lifecycle.ClaimDuePeerConnectionAttempt("peer.example");

        PeerConnectionLifecycleResult disconnected =
            lifecycle.RecordRemotePeerDisconnect("peer.example", cause);
        clock.AdvanceDiscoveryTime(60);
        PeerConnectionLifecycleResult manual =
            lifecycle.RequestManualPeerConnection("peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(disconnected.Accepted, Is.True);
            Assert.That(disconnected.Code, Is.EqualTo(expectedCode));
            Assert.That(disconnected.Peer.State, Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(disconnected.Peer.ReconnectSuppressed, Is.True);
            Assert.That(disconnected.Peer.ReconnectSuppressionCause, Is.EqualTo(cause));
            Assert.That(disconnected.Peer.ReconnectSuppressedAt,
                Is.EqualTo(clock.UtcTime.AddSeconds(-60)));
            Assert.That(disconnected.Peer.NextConnectionAttemptAt,
                Is.EqualTo(default(DateTimeOffset)));
            Assert.That(disconnected.Actions, Is.EqualTo(new[]
            {
                PeerStateMachineAction.InitiatorDisconnect
            }));
            Assert.That(table.ReadDuePeerConnectionRetries(), Is.Empty);
            Assert.That(manual.Accepted, Is.True);
            Assert.That(manual.Code, Is.EqualTo("manual-peer-connect-started"));
            Assert.That(manual.Peer.ReconnectSuppressed, Is.False);
            Assert.That(manual.Peer.State,
                Is.EqualTo(DiameterPeerState.WaitConnectionAcknowledgement));
        });
    }

    [Test]
    public void RebootingCauseRearmsTcWithoutSuppressingReconnect()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreateLifecyclePeerTable(clock, 10, 10);
        table.AddOrUpdatePeerConfiguration(CreateLifecyclePeerConfiguration(
            "peer.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset)));
        PeerConnectionLifecycle lifecycle = new PeerConnectionLifecycle(table);
        lifecycle.ClaimDuePeerConnectionAttempt("peer.example");

        PeerConnectionLifecycleResult disconnected =
            lifecycle.RecordRemotePeerDisconnect(
                "peer.example",
                DiameterDisconnectCause.Rebooting);
        List<DiameterPeerSnapshot> early = table.ReadDuePeerConnectionRetries();
        clock.AdvanceDiscoveryTime(1);
        List<DiameterPeerSnapshot> due = table.ReadDuePeerConnectionRetries();

        Assert.Multiple(() =>
        {
            Assert.That(disconnected.Code,
                Is.EqualTo("peer-rebooting-retry-scheduled"));
            Assert.That(disconnected.Peer.ReconnectSuppressed, Is.False);
            Assert.That(disconnected.Peer.NextConnectionAttemptAt,
                Is.EqualTo(clock.UtcTime));
            Assert.That(early, Is.Empty);
            Assert.That(due, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ExpiredDynamicPeerCannotRetryOrBeManuallyConnectedAndIsSwept()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreateLifecyclePeerTable(clock, 0, 0);
        table.AddOrUpdatePeerConfiguration(CreateLifecyclePeerConfiguration(
            "dynamic.example",
            DiameterPeerSource.Dynamic,
            clock.UtcTime.AddSeconds(5)));
        PeerConnectionLifecycle lifecycle = new PeerConnectionLifecycle(table);

        clock.AdvanceDiscoveryTime(6);
        List<DiameterPeerSnapshot> due = table.ReadDuePeerConnectionRetries();
        PeerConnectionLifecycleResult manual =
            lifecycle.RequestManualPeerConnection("dynamic.example");
        List<DiameterPeerSnapshot> expired = lifecycle.SweepExpiredDynamicPeers();

        Assert.Multiple(() =>
        {
            Assert.That(due, Is.Empty);
            Assert.That(manual.Accepted, Is.False);
            Assert.That(manual.Code, Is.EqualTo("dynamic-peer-expired"));
            Assert.That(expired, Has.Count.EqualTo(1));
            Assert.That(expired[0].Configuration.Identity.Value,
                Is.EqualTo("dynamic.example"));
            Assert.That(table.ReadPeerTableSnapshot("dynamic.example"), Is.Null);
        });
    }

    [Test]
    public void GracefulManualDisconnectIsIdempotentAndOwnsItsRequest()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreateLifecyclePeerTable(clock, 0, 0);
        DiameterPeerConfiguration configuration =
            CreateLifecyclePeerConfiguration(
                "peer.example",
                DiameterPeerSource.Static,
                default(DateTimeOffset));
        table.AddOrUpdatePeerConfiguration(configuration);
        table.AttachPeerTableConnection(
            "peer.example",
            configuration.Endpoints[0],
            CreateLifecycleConnection(configuration.Endpoints[0]),
            DiameterConnectionDirection.Outbound);
        table.TransitionPeerTableState(
            "peer.example",
            DiameterPeerState.InitiatorOpen);
        PeerConnectionLifecycle lifecycle = new PeerConnectionLifecycle(table);
        DiameterDisconnectRequest request = CreateLifecycleDisconnectRequest(
            "peer.example",
            true,
            true);

        PeerConnectionLifecycleResult first =
            lifecycle.RequestManualPeerDisconnection(request);
        PeerConnectionLifecycleResult repeated =
            lifecycle.RequestManualPeerDisconnection(request);
        request.PeerIdentity.Value = "mutated.example";

        Assert.Multiple(() =>
        {
            Assert.That(first.Accepted, Is.True);
            Assert.That(first.Code, Is.EqualTo("manual-peer-disconnect-started"));
            Assert.That(first.Peer.State, Is.EqualTo(DiameterPeerState.Closing));
            Assert.That(first.Peer.ReconnectSuppressed, Is.True);
            Assert.That(first.Actions, Is.EqualTo(new[]
            {
                PeerStateMachineAction.InitiatorSendDisconnectRequest
            }));
            Assert.That(first.DisconnectRequest, Is.Not.Null);
            Assert.That(first.DisconnectRequest!.PeerIdentity.Value,
                Is.EqualTo("peer.example"));
            Assert.That(repeated.Accepted, Is.True);
            Assert.That(repeated.Code,
                Is.EqualTo("peer-disconnect-already-in-progress"));
            Assert.That(repeated.Actions, Is.Empty);
        });
    }

    [Test]
    public void ImmediateManualDisconnectClosesTransportAndRearmsTc()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreateLifecyclePeerTable(clock, 0, 0);
        DiameterPeerConfiguration configuration =
            CreateLifecyclePeerConfiguration(
                "peer.example",
                DiameterPeerSource.Static,
                default(DateTimeOffset));
        table.AddOrUpdatePeerConfiguration(configuration);
        table.AttachPeerTableConnection(
            "peer.example",
            configuration.Endpoints[0],
            CreateLifecycleConnection(configuration.Endpoints[0]),
            DiameterConnectionDirection.Inbound);
        table.TransitionPeerTableState(
            "peer.example",
            DiameterPeerState.ResponderOpen);
        PeerConnectionLifecycle lifecycle = new PeerConnectionLifecycle(table);
        DiameterDisconnectRequest request = CreateLifecycleDisconnectRequest(
            "peer.example",
            false,
            false);

        PeerConnectionLifecycleResult result =
            lifecycle.RequestManualPeerDisconnection(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Code, Is.EqualTo("manual-peer-disconnect-completed"));
            Assert.That(result.Peer.State, Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(result.Peer.Connection, Is.Null);
            Assert.That(result.Peer.ReconnectSuppressed, Is.False);
            Assert.That(result.Peer.NextConnectionAttemptAt,
                Is.EqualTo(clock.UtcTime.AddSeconds(1)));
            Assert.That(result.Actions, Is.EqualTo(new[]
            {
                PeerStateMachineAction.ResponderSendDisconnectRequest,
                PeerStateMachineAction.ResponderDisconnect
            }));
        });
    }

    [Test]
    public void ManualConnectionOverridesAutomaticConnectionPreferenceButNotDisabledPeer()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreateLifecyclePeerTable(clock, 0, 0);
        DiameterPeerConfiguration configuration =
            CreateLifecyclePeerConfiguration(
                "peer.example",
                DiameterPeerSource.Static,
                default(DateTimeOffset));
        configuration.ConnectAutomatically = false;
        table.AddOrUpdatePeerConfiguration(configuration);
        PeerConnectionLifecycle lifecycle = new PeerConnectionLifecycle(table);

        PeerConnectionLifecycleResult manual =
            lifecycle.RequestManualPeerConnection("peer.example");
        lifecycle.RecordRemotePeerDisconnect(
            "peer.example",
            DiameterDisconnectCause.Rebooting);
        configuration.Enabled = false;
        table.AddOrUpdatePeerConfiguration(configuration);
        PeerConnectionLifecycleResult disabled =
            lifecycle.RequestManualPeerConnection("peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(manual.Accepted, Is.True);
            Assert.That(table.ReadDuePeerConnectionRetries(), Is.Empty);
            Assert.That(disabled.Accepted, Is.False);
            Assert.That(disabled.Code, Is.EqualTo("peer-disabled"));
        });
    }

    [Test]
    public void OutOfRangeJitterSourceValueIsRejected()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreateLifecyclePeerTable(clock, 10, 21);
        table.AddOrUpdatePeerConfiguration(CreateLifecyclePeerConfiguration(
            "peer.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset)));

        Assert.That(
            () => table.SchedulePeerTableConnectionRetry("peer.example"),
            Throws.TypeOf<InvalidOperationException>());
    }

    private static PeerTable CreateLifecyclePeerTable(
        FakeDiscoveryClock clock,
        int jitterPercent,
        params int[] jitterValues)
    {
        DiameterTimerConfiguration timers = new DiameterTimerConfiguration();
        timers.ConnectionRetryMilliseconds = 1000;
        timers.ConnectionRetryJitterPercent = jitterPercent;
        PeerTable table = new PeerTable(clock, timers, 4);
        table.RetryJitterSource = new SequencePeerRetryJitterSource(jitterValues);
        return table;
    }

    private static DiameterPeerConfiguration CreateLifecyclePeerConfiguration(
        string identity,
        DiameterPeerSource source,
        DateTimeOffset expiresAt)
    {
        DiameterPeerConfiguration result = new DiameterPeerConfiguration();
        result.Identity.Value = identity;
        result.Realm.Value = "example";
        result.Source = source;
        result.ExpiresAt = expiresAt;
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = identity;
        endpoint.Host = identity;
        endpoint.Address = IPAddress.Parse("192.0.2.10");
        endpoint.Port = DiameterProtocolConstants.Port;
        endpoint.Transport = DiameterTransportKind.Tcp;
        endpoint.Security = DiameterSecurityMode.None;
        result.Endpoints.Add(endpoint);
        return result;
    }

    private static DiameterConnectionMetadata CreateLifecycleConnection(
        DiameterEndpoint endpoint)
    {
        DiameterConnectionMetadata result = new DiameterConnectionMetadata();
        result.ConnectionId = "connection-1";
        result.LocalEndpoint = new DiameterEndpoint();
        result.LocalEndpoint.Identity.Value = "local.example";
        result.LocalEndpoint.Address = IPAddress.Parse("192.0.2.1");
        result.LocalEndpoint.Port = DiameterProtocolConstants.Port;
        result.LocalEndpoint.Transport = DiameterTransportKind.Tcp;
        result.RemoteEndpoint = PeerTableCloner.ClonePeerTableEndpoint(endpoint);
        result.Transport.Kind = DiameterTransportKind.Tcp;
        result.Security.Mode = DiameterSecurityMode.None;
        result.Open = true;
        return result;
    }

    private static DiameterDisconnectRequest CreateLifecycleDisconnectRequest(
        string identity,
        bool waitForAnswer,
        bool suppressReconnect)
    {
        DiameterDisconnectRequest result = new DiameterDisconnectRequest();
        result.PeerIdentity.Value = identity;
        result.Cause = DiameterDisconnectCause.Rebooting;
        result.WaitForAnswer = waitForAnswer;
        result.SuppressReconnect = suppressReconnect;
        return result;
    }
}

internal class SequencePeerRetryJitterSource : IPeerRetryJitterSource
{
    internal List<int> Values = new List<int>();
    internal int Index;

    internal SequencePeerRetryJitterSource(params int[] values)
    {
        Values.AddRange(values);
    }

    public int ReadPeerRetryJitterValue(int maximumExclusive)
    {
        if (Values.Count == 0)
        {
            return 0;
        }

        int value = Values[Index % Values.Count];
        Index++;
        return value;
    }
}
