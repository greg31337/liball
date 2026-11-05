using System.Net;

namespace diameter.Tests;

public class PeerTableTests
{
    [Test]
    public void ConfigurationEntriesAreOwnedOrderedAndCapacityBounded()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreatePeerTable(clock, 2);
        DiameterPeerConfiguration first = CreatePeerConfiguration(
            "first.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset));

        DiameterPeerSnapshot added = table.AddOrUpdatePeerConfiguration(first);
        first.Identity.Value = "mutated.example";
        first.Endpoints[0].Address = IPAddress.Parse("203.0.113.1");
        added.Configuration.Identity.Value = "changed-result.example";
        added.Configuration.Endpoints[0].Host = "changed-result.example";
        DiameterPeerConfiguration second = CreatePeerConfiguration(
            "second.example",
            DiameterPeerSource.Dynamic,
            clock.UtcTime.AddSeconds(60));
        table.AddOrUpdatePeerConfiguration(second);

        DiameterPeerSnapshot? stored = table.ReadPeerTableSnapshot("FIRST.EXAMPLE");
        List<DiameterPeerSnapshot> all = table.ReadAllPeerTableSnapshots();
        DiameterPeerConfiguration third = CreatePeerConfiguration(
            "third.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset));

        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.Configuration.Identity.Value, Is.EqualTo("first.example"));
            Assert.That(stored.Configuration.Endpoints[0].Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.10")));
            Assert.That(stored.Configuration.Endpoints[0].Host,
                Is.EqualTo("first.example"));
            Assert.That(stored.State, Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(stored.StateChangedAt, Is.EqualTo(clock.UtcTime));
            Assert.That(stored.NextConnectionAttemptAt, Is.EqualTo(clock.UtcTime));
            Assert.That(all, Has.Count.EqualTo(2));
            Assert.That(all[0].Configuration.Identity.Value, Is.EqualTo("first.example"));
            Assert.That(all[1].Configuration.Identity.Value, Is.EqualTo("second.example"));
            Assert.That(() => table.AddOrUpdatePeerConfiguration(third),
                Throws.TypeOf<DiameterPeerException>());
        });

        all[0].Configuration.Identity.Value = "changed-list.example";
        Assert.That(table.ReadPeerTableSnapshot("first.example")!
            .Configuration.Identity.Value, Is.EqualTo("first.example"));
        Assert.That(table.RemovePeerTableEntry("SECOND.EXAMPLE"), Is.True);
        Assert.That(table.RemovePeerTableEntry("second.example"), Is.False);
        Assert.That(table.ReadPeerTableSnapshot("second.example"), Is.Null);
    }

    [Test]
    public void StaticConfigurationWinsAndUpdatesPreserveOperationalState()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreatePeerTable(clock, 2);
        DiameterPeerConfiguration configured = CreatePeerConfiguration(
            "peer.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset));
        table.AddOrUpdatePeerConfiguration(configured);
        table.IncrementPeerTableReconnectCount("peer.example");

        DiameterPeerConfiguration discovered = CreatePeerConfiguration(
            "PEER.EXAMPLE",
            DiameterPeerSource.Dynamic,
            clock.UtcTime.AddSeconds(30));
        discovered.Endpoints[0].Address = IPAddress.Parse("198.51.100.1");
        DiameterPeerSnapshot retained =
            table.AddOrUpdatePeerConfiguration(discovered);

        DiameterPeerConfiguration disabled = CreatePeerConfiguration(
            "peer.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset));
        disabled.Enabled = false;
        disabled.Endpoints.RemoveAt(0);
        DiameterPeerSnapshot updated = table.AddOrUpdatePeerConfiguration(disabled);

        Assert.Multiple(() =>
        {
            Assert.That(retained.Configuration.Source, Is.EqualTo(DiameterPeerSource.Static));
            Assert.That(retained.Configuration.Endpoints[0].Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.10")));
            Assert.That(updated.Configuration.Enabled, Is.False);
            Assert.That(updated.ReconnectCount, Is.EqualTo(1));
            Assert.That(updated.CurrentEndpoint!.Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.11")));
            Assert.That(updated.NextConnectionAttemptAt,
                Is.EqualTo(default(DateTimeOffset)));
            Assert.That(updated.Available, Is.False);
        });
    }

    [Test]
    public void EndpointSelectionUsesConfiguredEndpointsAndWraps()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreatePeerTable(clock, 1);
        DiameterPeerConfiguration configuration = CreatePeerConfiguration(
            "peer.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset));
        table.AddOrUpdatePeerConfiguration(configuration);

        DiameterPeerSnapshot selected = table.SelectPeerTableEndpoint(
            "peer.example",
            configuration.Endpoints[1]);
        DiameterPeerSnapshot wrapped = table.AdvancePeerTableEndpoint("peer.example");
        DiameterPeerSnapshot advanced = table.AdvancePeerTableEndpoint("peer.example");
        DiameterEndpoint unknown = CreateEndpoint(
            "peer.example",
            "192.0.2.99");

        Assert.Multiple(() =>
        {
            Assert.That(selected.CurrentEndpoint!.Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.11")));
            Assert.That(wrapped.CurrentEndpoint!.Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.10")));
            Assert.That(advanced.CurrentEndpoint!.Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.11")));
            Assert.That(() => table.SelectPeerTableEndpoint("peer.example", unknown),
                Throws.ArgumentException);
        });
    }

    [Test]
    public void ConnectionAndStateChangesMaintainDirectionAndSnapshotOwnership()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreatePeerTable(clock, 1);
        DiameterPeerConfiguration configuration = CreatePeerConfiguration(
            "peer.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset));
        table.AddOrUpdatePeerConfiguration(configuration);
        DiameterConnectionMetadata connection = CreateConnection(
            configuration.Endpoints[0]);

        DiameterPeerSnapshot attached = table.AttachPeerTableConnection(
            "peer.example",
            configuration.Endpoints[0],
            connection,
            DiameterConnectionDirection.Outbound);
        connection.ConnectionId = "mutated";
        connection.Security.PeerCertificateDnsNames[0] = "mutated.example";
        attached.Connection!.ConnectionId = "changed-result";
        clock.AdvanceDiscoveryTime(1);
        DiameterPeerSnapshot initiator = table.TransitionPeerTableState(
            "peer.example",
            DiameterPeerState.InitiatorOpen);
        clock.AdvanceDiscoveryTime(1);
        DiameterPeerSnapshot responder = table.TransitionPeerTableState(
            "peer.example",
            DiameterPeerState.ResponderOpen);

        Assert.Multiple(() =>
        {
            Assert.That(initiator.Connection!.ConnectionId, Is.EqualTo("connection-1"));
            Assert.That(initiator.Connection.Security.PeerCertificateDnsNames[0],
                Is.EqualTo("peer.example"));
            Assert.That(initiator.State, Is.EqualTo(DiameterPeerState.InitiatorOpen));
            Assert.That(initiator.Direction,
                Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(initiator.Connection.Direction,
                Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(initiator.Available, Is.True);
            Assert.That(responder.Direction,
                Is.EqualTo(DiameterConnectionDirection.Inbound));
            Assert.That(responder.Connection!.Direction,
                Is.EqualTo(DiameterConnectionDirection.Inbound));
            Assert.That(responder.StateChangedAt, Is.EqualTo(clock.UtcTime));
        });

        DiameterPeerSnapshot closed = table.TransitionPeerTableState(
            "peer.example",
            DiameterPeerState.Closed);
        Assert.Multiple(() =>
        {
            Assert.That(closed.Connection, Is.Null);
            Assert.That(closed.ConnectedAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(closed.Available, Is.False);
            Assert.That(() => table.TransitionPeerTableState(
                "peer.example",
                DiameterPeerState.InitiatorOpen),
                Throws.TypeOf<DiameterPeerException>());
            Assert.That(() => table.TransitionPeerTableState(
                "peer.example",
                (DiameterPeerState)99), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void CapabilitiesAreValidatedTimestampedAndDeepCopied()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreatePeerTable(clock, 1);
        table.AddOrUpdatePeerConfiguration(CreatePeerConfiguration(
            "peer.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset)));
        DiameterPeerCapabilities capabilities = CreateCapabilities();

        DiameterPeerSnapshot updated = table.UpdatePeerTableCapabilities(
            "peer.example",
            capabilities);
        capabilities.OriginHost.Value = "mutated.example";
        capabilities.HostAddresses[0] = IPAddress.Parse("203.0.113.9");
        capabilities.VendorSpecificApplications[0].Id = 999;
        updated.Capabilities.CommonApplications[0].Id = 998;
        DiameterPeerSnapshot stored = table.ReadPeerTableSnapshot("peer.example")!;
        DiameterPeerCapabilities mismatch = CreateCapabilities();
        mismatch.OriginRealm.Value = "other.example";

        Assert.Multiple(() =>
        {
            Assert.That(stored.Capabilities.OriginHost.Value, Is.EqualTo("peer.example"));
            Assert.That(stored.Capabilities.HostAddresses[0],
                Is.EqualTo(IPAddress.Parse("192.0.2.20")));
            Assert.That(stored.Capabilities.VendorSpecificApplications[0].Id,
                Is.EqualTo(DiameterApplicationIds.CreditControl));
            Assert.That(stored.Capabilities.CommonApplications[0].Id,
                Is.EqualTo(DiameterApplicationIds.CreditControl));
            Assert.That(stored.Capabilities.UpdatedAt, Is.EqualTo(clock.UtcTime));
            Assert.That(() => table.UpdatePeerTableCapabilities(
                "peer.example",
                mismatch), Throws.ArgumentException);
        });
    }

    [Test]
    public void DynamicExpiryWaitsForConnectionClosure()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreatePeerTable(clock, 3);
        table.AddOrUpdatePeerConfiguration(CreatePeerConfiguration(
            "static.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset)));
        DiameterPeerConfiguration dynamicPeer = CreatePeerConfiguration(
            "dynamic.example",
            DiameterPeerSource.Dynamic,
            clock.UtcTime.AddSeconds(10));
        table.AddOrUpdatePeerConfiguration(dynamicPeer);
        table.AttachPeerTableConnection(
            "dynamic.example",
            dynamicPeer.Endpoints[0],
            CreateConnection(dynamicPeer.Endpoints[0]),
            DiameterConnectionDirection.Outbound);
        table.TransitionPeerTableState(
            "dynamic.example",
            DiameterPeerState.InitiatorOpen);

        clock.AdvanceDiscoveryTime(11);
        List<DiameterPeerSnapshot> connectedExpiry = table.ExpireDynamicPeerEntries();
        table.TransitionPeerTableState("dynamic.example", DiameterPeerState.Closed);
        List<DiameterPeerSnapshot> closedExpiry = table.ExpireDynamicPeerEntries();
        DiameterPeerConfiguration expired = CreatePeerConfiguration(
            "expired.example",
            DiameterPeerSource.Dynamic,
            clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(connectedExpiry, Is.Empty);
            Assert.That(closedExpiry, Has.Count.EqualTo(1));
            Assert.That(closedExpiry[0].Configuration.Identity.Value,
                Is.EqualTo("dynamic.example"));
            Assert.That(table.ReadPeerTableSnapshot("dynamic.example"), Is.Null);
            Assert.That(table.ReadPeerTableSnapshot("static.example"), Is.Not.Null);
            Assert.That(() => table.AddOrUpdatePeerConfiguration(expired),
                Throws.ArgumentException);
        });
    }

    [Test]
    public void RetryWatchdogAndReconnectAccountingUseDeterministicTimers()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PeerTable table = CreatePeerTable(clock, 1);
        DiameterPeerConfiguration configuration = CreatePeerConfiguration(
            "peer.example",
            DiameterPeerSource.Static,
            default(DateTimeOffset));
        table.AddOrUpdatePeerConfiguration(configuration);

        Assert.That(table.ReadDuePeerConnectionRetries(), Has.Count.EqualTo(1));
        DiameterPeerSnapshot retry = table.SchedulePeerTableConnectionRetry("peer.example");
        Assert.That(retry.NextConnectionAttemptAt, Is.EqualTo(clock.UtcTime.AddSeconds(1)));
        Assert.That(table.ReadDuePeerConnectionRetries(), Is.Empty);
        clock.AdvanceDiscoveryTime(1);
        Assert.That(table.ReadDuePeerConnectionRetries(), Has.Count.EqualTo(1));

        table.AttachPeerTableConnection(
            "peer.example",
            configuration.Endpoints[0],
            CreateConnection(configuration.Endpoints[0]),
            DiameterConnectionDirection.Outbound);
        table.TransitionPeerTableState(
            "peer.example",
            DiameterPeerState.InitiatorOpen);
        Assert.That(table.ReadDuePeerWatchdogs(), Is.Empty);
        clock.AdvanceDiscoveryTime(6);
        Assert.That(table.ReadDuePeerWatchdogs(), Has.Count.EqualTo(1));
        DiameterPeerSnapshot sent = table.RecordPeerTableWatchdogSent("peer.example");
        Assert.That(sent.WatchdogPending, Is.True);
        clock.AdvanceDiscoveryTime(6);
        Assert.That(table.ReadDuePeerWatchdogs(), Has.Count.EqualTo(1));

        DiameterPeerSnapshot failed = table.RecordPeerTableWatchdogOutcome(
            "peer.example",
            false);
        DiameterPeerSnapshot successful = table.RecordPeerTableWatchdogOutcome(
            "peer.example",
            true);
        DiameterPeerSnapshot reconnected =
            table.IncrementPeerTableReconnectCount("peer.example");
        table.RecordPeerTableActivity("peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(failed.WatchdogPending, Is.False);
            Assert.That(failed.ConsecutiveWatchdogFailures, Is.EqualTo(1));
            Assert.That(failed.ConsecutiveWatchdogSuccesses, Is.Zero);
            Assert.That(successful.ConsecutiveWatchdogSuccesses, Is.EqualTo(1));
            Assert.That(successful.ConsecutiveWatchdogFailures, Is.Zero);
            Assert.That(reconnected.ReconnectCount, Is.EqualTo(1));
            Assert.That(table.ReadDuePeerWatchdogs(), Is.Empty);
        });
    }

    private static PeerTable CreatePeerTable(FakeDiscoveryClock clock, int capacity)
    {
        DiameterTimerConfiguration timers = new DiameterTimerConfiguration();
        timers.ConnectionRetryMilliseconds = 1000;
        timers.ConnectionRetryJitterPercent = 0;
        timers.WatchdogIntervalMilliseconds = 6000;
        timers.WatchdogJitterMilliseconds = 0;
        return new PeerTable(clock, timers, capacity);
    }

    private static DiameterPeerConfiguration CreatePeerConfiguration(
        string identity,
        DiameterPeerSource source,
        DateTimeOffset expiresAt)
    {
        DiameterPeerConfiguration result = new DiameterPeerConfiguration();
        result.Identity.Value = identity;
        result.Realm.Value = "example";
        result.Source = source;
        result.ExpiresAt = expiresAt;
        result.Endpoints.Add(CreateEndpoint(identity, "192.0.2.10"));
        result.Endpoints.Add(CreateEndpoint(identity, "192.0.2.11"));
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = DiameterApplicationIds.CreditControl;
        result.RequiredApplications.Add(application);
        return result;
    }

    private static DiameterEndpoint CreateEndpoint(string identity, string address)
    {
        DiameterEndpoint result = new DiameterEndpoint();
        result.Identity.Value = identity;
        result.Host = identity;
        result.Address = IPAddress.Parse(address);
        result.Port = DiameterProtocolConstants.Port;
        result.Transport = DiameterTransportKind.Tcp;
        result.Security = DiameterSecurityMode.None;
        return result;
    }

    private static DiameterConnectionMetadata CreateConnection(DiameterEndpoint endpoint)
    {
        DiameterConnectionMetadata result = new DiameterConnectionMetadata();
        result.ConnectionId = "connection-1";
        result.LocalEndpoint = CreateEndpoint("local.example", "192.0.2.1");
        result.RemoteEndpoint = CreateEndpoint(
            endpoint.Identity.Value,
            endpoint.Address!.ToString());
        result.Direction = DiameterConnectionDirection.Outbound;
        result.Transport.Kind = DiameterTransportKind.Tcp;
        result.Security.Mode = DiameterSecurityMode.None;
        result.Security.PeerCertificateDnsNames.Add("peer.example");
        result.Open = true;
        return result;
    }

    private static DiameterPeerCapabilities CreateCapabilities()
    {
        DiameterPeerCapabilities result = new DiameterPeerCapabilities();
        result.OriginHost.Value = "peer.example";
        result.OriginRealm.Value = "example";
        result.HostAddresses.Add(IPAddress.Parse("192.0.2.20"));
        result.VendorId = 10415;
        result.ProductName = "peer";
        result.FirmwareRevision = 1;
        result.SupportedVendorIds.Add(10415);
        result.AuthApplicationIds.Add(DiameterApplicationIds.CreditControl);
        result.InbandSecurityIds.Add(0);
        DiameterApplicationId vendorSpecific = new DiameterApplicationId();
        vendorSpecific.Id = DiameterApplicationIds.CreditControl;
        vendorSpecific.VendorId = 10415;
        result.VendorSpecificApplications.Add(vendorSpecific);
        DiameterApplicationId common = new DiameterApplicationId();
        common.Id = DiameterApplicationIds.CreditControl;
        result.CommonApplications.Add(common);
        return result;
    }
}
