using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class PeerManagerTests
{
    [Test]
    public void PublicContractAddsAndReadsOwnedSnapshots()
    {
        DiameterPeerManager implementation = CreatePeerManager();
        IDiameterPeerManager manager = implementation;
        PeerManagerTestHandler handler = new PeerManagerTestHandler(implementation);
        manager.RegisterPeerEventHandler(handler);
        DiameterPeerConfiguration configuration =
            CreatePeerManagerConfiguration("peer.example", "192.0.2.10");

        DiameterPeerSnapshot added = manager.AddDiameterPeer(configuration);
        configuration.Identity.Value = "mutated.example";
        configuration.Endpoints[0].Host = "mutated.example";
        added.Configuration.Realm.Value = "mutated";
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = "peer.example";
        DiameterPeerSnapshot? read = manager.ReadDiameterPeerSnapshot(identity);
        List<DiameterPeerSnapshot> all = manager.ReadDiameterPeerSnapshots();
        all[0].Configuration.Identity.Value = "list-mutated.example";
        DiameterPeerSnapshot? repeated =
            manager.ReadDiameterPeerSnapshot(identity);

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.Not.Null);
            Assert.That(read!.Configuration.Identity.Value,
                Is.EqualTo("peer.example"));
            Assert.That(read.Configuration.Realm.Value, Is.EqualTo("example"));
            Assert.That(read.Configuration.Endpoints[0].Host,
                Is.EqualTo("peer.example"));
            Assert.That(repeated!.Configuration.Identity.Value,
                Is.EqualTo("peer.example"));
            Assert.That(all, Has.Count.EqualTo(1));
            Assert.That(handler.Events, Has.Count.EqualTo(1));
            Assert.That(handler.Events[0].Kind,
                Is.EqualTo(DiameterPeerEventKind.Added));
            Assert.That(handler.Events[0].Code, Is.EqualTo("peer.added"));
            Assert.That(handler.Events[0].Peer.Configuration.Identity.Value,
                Is.EqualTo("peer.example"));
            Assert.That(handler.CallbackUnderHandlerLock, Is.False);
            Assert.That(handler.CallbackUnderTableLock, Is.False);
        });
    }

    [Test]
    public void HandlersAreIdempotentIndependentAndInvokedOutsideLocks()
    {
        DiameterPeerManager manager = CreatePeerManager();
        PeerManagerTestHandler first = new PeerManagerTestHandler(manager);
        PeerManagerTestHandler second = new PeerManagerTestHandler(manager);
        first.MutateEvent = true;
        manager.RegisterPeerEventHandler(first);
        manager.RegisterPeerEventHandler(first);
        manager.RegisterPeerEventHandler(second);

        manager.AddDiameterPeer(
            CreatePeerManagerConfiguration("first.example", "192.0.2.11"));
        manager.UnregisterPeerEventHandler(first);
        manager.UnregisterPeerEventHandler(first);
        manager.AddDiameterPeer(
            CreatePeerManagerConfiguration("second.example", "192.0.2.12"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Events, Has.Count.EqualTo(1));
            Assert.That(second.Events, Has.Count.EqualTo(2));
            Assert.That(first.Events[0].Code, Is.EqualTo("mutated"));
            Assert.That(second.Events[0].Code, Is.EqualTo("peer.added"));
            Assert.That(first.Events[0], Is.Not.SameAs(second.Events[0]));
            Assert.That(first.Events[0].Peer,
                Is.Not.SameAs(second.Events[0].Peer));
            Assert.That(second.Events[0].Peer.Configuration.Identity.Value,
                Is.EqualTo("first.example"));
            Assert.That(first.CallbackUnderHandlerLock, Is.False);
            Assert.That(first.CallbackUnderTableLock, Is.False);
            Assert.That(second.CallbackUnderHandlerLock, Is.False);
            Assert.That(second.CallbackUnderTableLock, Is.False);
        });
    }

    [Test]
    public void ManualConnectPublishesOneStateTransition()
    {
        DiameterPeerManager manager = CreatePeerManager();
        manager.AddDiameterPeer(
            CreatePeerManagerConfiguration("peer.example", "192.0.2.10"));
        PeerManagerTestHandler handler = new PeerManagerTestHandler(manager);
        manager.RegisterPeerEventHandler(handler);
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = "peer.example";

        DiameterPeerSnapshot connected = manager.ConnectDiameterPeer(identity);
        DiameterPeerSnapshot repeated = manager.ConnectDiameterPeer(identity);

        Assert.Multiple(() =>
        {
            Assert.That(connected.State,
                Is.EqualTo(DiameterPeerState.WaitConnectionAcknowledgement));
            Assert.That(connected.Direction,
                Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(connected.ReconnectCount, Is.EqualTo(1));
            Assert.That(repeated.State,
                Is.EqualTo(DiameterPeerState.WaitConnectionAcknowledgement));
            Assert.That(handler.Events, Has.Count.EqualTo(1));
            Assert.That(handler.Events[0].Kind,
                Is.EqualTo(DiameterPeerEventKind.StateChanged));
            Assert.That(handler.Events[0].PreviousState,
                Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(handler.Events[0].CurrentState,
                Is.EqualTo(DiameterPeerState.WaitConnectionAcknowledgement));
            Assert.That(handler.Events[0].Code,
                Is.EqualTo("peer.connection.requested"));
        });
    }

    [Test]
    public void GracefulDisconnectPublishesClosingSnapshot()
    {
        DiameterPeerManager manager = CreateOpenPeerManager();
        PeerManagerTestHandler handler = new PeerManagerTestHandler(manager);
        manager.RegisterPeerEventHandler(handler);
        DiameterDisconnectRequest request = new DiameterDisconnectRequest();
        request.PeerIdentity.Value = "peer.example";
        request.Cause = DiameterDisconnectCause.Busy;
        request.TimeoutMilliseconds = 5000;
        request.WaitForAnswer = true;
        request.SuppressReconnect = true;

        DiameterPeerSnapshot disconnected =
            manager.DisconnectDiameterPeer(request);

        Assert.Multiple(() =>
        {
            Assert.That(disconnected.State,
                Is.EqualTo(DiameterPeerState.Closing));
            Assert.That(disconnected.Connection, Is.Not.Null);
            Assert.That(disconnected.Available, Is.False);
            Assert.That(disconnected.DisconnectPending, Is.True);
            Assert.That(disconnected.DisconnectDrainPending, Is.True);
            Assert.That(disconnected.DisconnectAnswerPending, Is.True);
            Assert.That(disconnected.LastDisconnectCause,
                Is.EqualTo(DiameterDisconnectCause.Busy));
            Assert.That(disconnected.ReconnectSuppressed, Is.True);
            Assert.That(handler.Events, Has.Count.EqualTo(1));
            Assert.That(handler.Events[0].PreviousState,
                Is.EqualTo(DiameterPeerState.InitiatorOpen));
            Assert.That(handler.Events[0].CurrentState,
                Is.EqualTo(DiameterPeerState.Closing));
            Assert.That(handler.Events[0].Code,
                Is.EqualTo("peer.disconnect.requested"));
        });
    }

    [Test]
    public void ConcurrentShutdownIsIdempotentAndCallbackCanReenterManager()
    {
        DiameterPeerManager manager = CreateOpenPeerManager();
        PeerManagerTestHandler handler = new PeerManagerTestHandler(manager);
        handler.ReadSnapshotDuringCallback = true;
        handler.UnregisterDuringCallback = true;
        manager.RegisterPeerEventHandler(handler);
        using Barrier barrier = new Barrier(3);
        Task<DiameterPeerSnapshot> first = Task.Run(() =>
        {
            barrier.SignalAndWait();
            return manager.DisconnectDiameterPeer(
                CreatePeerManagerDisconnectRequest());
        });
        Task<DiameterPeerSnapshot> second = Task.Run(() =>
        {
            barrier.SignalAndWait();
            return manager.DisconnectDiameterPeer(
                CreatePeerManagerDisconnectRequest());
        });

        barrier.SignalAndWait();
        Task.WaitAll(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(first.Result.State,
                Is.EqualTo(DiameterPeerState.Closing));
            Assert.That(second.Result.State,
                Is.EqualTo(DiameterPeerState.Closing));
            Assert.That(handler.Events, Has.Count.EqualTo(1));
            Assert.That(handler.Events[0].PreviousState,
                Is.EqualTo(DiameterPeerState.InitiatorOpen));
            Assert.That(handler.Events[0].CurrentState,
                Is.EqualTo(DiameterPeerState.Closing));
            Assert.That(handler.CallbackSnapshot, Is.Not.Null);
            Assert.That(handler.CallbackSnapshot!.State,
                Is.EqualTo(DiameterPeerState.Closing));
            Assert.That(handler.CallbackUnderHandlerLock, Is.False);
            Assert.That(handler.CallbackUnderTableLock, Is.False);
            Assert.That(manager.Handlers, Is.Empty);
        });
    }

    [Test]
    public void RemovalPublishesSnapshotAndRejectsActivePeer()
    {
        DiameterPeerManager manager = CreatePeerManager();
        manager.AddDiameterPeer(
            CreatePeerManagerConfiguration("peer.example", "192.0.2.10"));
        PeerManagerTestHandler handler = new PeerManagerTestHandler(manager);
        manager.RegisterPeerEventHandler(handler);
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = "peer.example";

        bool removed = manager.RemoveDiameterPeer(identity);
        bool repeated = manager.RemoveDiameterPeer(identity);
        DiameterPeerManager active = CreateOpenPeerManager();

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(repeated, Is.False);
            Assert.That(manager.ReadDiameterPeerSnapshot(identity), Is.Null);
            Assert.That(handler.Events, Has.Count.EqualTo(1));
            Assert.That(handler.Events[0].Kind,
                Is.EqualTo(DiameterPeerEventKind.Removed));
            Assert.That(handler.Events[0].Peer.Configuration.Identity.Value,
                Is.EqualTo("peer.example"));
            Assert.That(() => active.RemoveDiameterPeer(identity),
                Throws.TypeOf<DiameterPeerException>());
        });
    }

    [Test]
    public void CapabilityAndRestartUpdatesPublishOwnedEvents()
    {
        DiameterPeerManager manager = CreatePeerManager();
        manager.AddDiameterPeer(
            CreatePeerManagerConfiguration("peer.example", "192.0.2.10"));
        PeerManagerTestHandler handler = new PeerManagerTestHandler(manager);
        manager.RegisterPeerEventHandler(handler);
        DiameterPeerCapabilities first =
            CreatePeerManagerCapabilities(100, "peer-v1");
        manager.RecordPeerManagerCapabilities("peer.example", first);
        handler.Events.Clear();
        DiameterPeerCapabilities restarted =
            CreatePeerManagerCapabilities(101, "peer-v2");

        DiameterPeerSnapshot updated = manager.RecordPeerManagerCapabilities(
            "peer.example",
            restarted);
        restarted.ProductName = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(updated.Capabilities.OriginStateId, Is.EqualTo(101U));
            Assert.That(updated.Capabilities.ProductName, Is.EqualTo("peer-v2"));
            Assert.That(handler.Events, Has.Count.EqualTo(2));
            Assert.That(handler.Events[0].Kind,
                Is.EqualTo(DiameterPeerEventKind.RestartDetected));
            Assert.That(handler.Events[0].PreviousOriginStateId,
                Is.EqualTo(100U));
            Assert.That(handler.Events[0].CurrentOriginStateId,
                Is.EqualTo(101U));
            Assert.That(handler.Events[1].Kind,
                Is.EqualTo(DiameterPeerEventKind.CapabilitiesChanged));
            Assert.That(handler.Events[1].CapabilityChange,
                Is.EqualTo(DiameterCapabilityChange.ProductName));
            Assert.That(handler.Events[1].Peer.Capabilities.ProductName,
                Is.EqualTo("peer-v2"));
            Assert.That(handler.CallbackUnderHandlerLock, Is.False);
            Assert.That(handler.CallbackUnderTableLock, Is.False);
        });
    }

    [Test]
    public void TransportFailurePublishesClosedUnavailablePeer()
    {
        DiameterPeerManager manager = CreateOpenPeerManager();
        PeerManagerTestHandler handler = new PeerManagerTestHandler(manager);
        manager.RegisterPeerEventHandler(handler);

        PeerWatchdogResult failed =
            manager.RecordPeerManagerTransportFailure("peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(failed.Handled, Is.True);
            Assert.That(failed.FailoverRequired, Is.True);
            Assert.That(failed.Peer.State, Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(failed.Peer.Connection, Is.Null);
            Assert.That(failed.Peer.Available, Is.False);
            Assert.That(handler.Events, Has.Count.EqualTo(1));
            Assert.That(handler.Events[0].Kind,
                Is.EqualTo(DiameterPeerEventKind.TransportFailed));
            Assert.That(handler.Events[0].PreviousState,
                Is.EqualTo(DiameterPeerState.InitiatorOpen));
            Assert.That(handler.Events[0].CurrentState,
                Is.EqualTo(DiameterPeerState.Closed));
        });
    }

    [Test]
    public void AttachedConnectionCarriesPeerBytesAndDetachesOnFailure()
    {
        DiameterPeerManager manager = CreateOpenPeerManager();
        ScriptedDiameterConnection connection =
            new ScriptedDiameterConnection();
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = "peer.example";
        manager.AttachPeerManagerConnection(identity, connection);

        DiameterTransportIoResult sent = manager.SendDiameterPeerBytes(
            identity,
            new byte[] { 1, 2, 3, 4 },
            1000);
        manager.RecordPeerManagerTransportFailure("peer.example");
        DiameterTransportIoResult detached = manager.SendDiameterPeerBytes(
            identity,
            new byte[] { 5 },
            1000);

        Assert.Multiple(() =>
        {
            Assert.That(sent.Status,
                Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(sent.TransferredCount, Is.EqualTo(4));
            Assert.That(connection.SentBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(detached.Status,
                Is.EqualTo(DiameterTransportStatus.Closed));
            Assert.That(detached.Code,
                Is.EqualTo("peer-connection-unavailable"));
        });
    }

    [Test]
    public void ManagerRejectsEveryMissingDependencyAndUnknownPeerOperation()
    {
        DiameterNodeConfiguration configuration =
            CreatePeerManagerNodeConfiguration();
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        BaseCommandTestIdentifierSource identifiers =
            new BaseCommandTestIdentifierSource();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new DiameterPeerManager(null, clock, identifiers));
            Assert.Throws<ArgumentException>(() =>
                new DiameterPeerManager(configuration, null, identifiers));
            Assert.Throws<ArgumentException>(() =>
                new DiameterPeerManager(configuration, clock, null));
        });

        DiameterPeerManager manager = new DiameterPeerManager(
            configuration,
            clock,
            identifiers);
        DiameterIdentity missing = new DiameterIdentity();
        missing.Value = "missing.example";
        DiameterDisconnectRequest disconnect =
            new DiameterDisconnectRequest();
        disconnect.PeerIdentity = missing;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                manager.AddDiameterPeer(null));
            Assert.Throws<DiameterPeerException>(() =>
                manager.ConnectDiameterPeer(missing));
            Assert.Throws<ArgumentException>(() =>
                manager.DisconnectDiameterPeer(null));
            Assert.Throws<DiameterPeerException>(() =>
                manager.DisconnectDiameterPeer(disconnect));
            Assert.Throws<ArgumentException>(() =>
                manager.RegisterPeerEventHandler(null));
            Assert.Throws<ArgumentException>(() =>
                manager.UnregisterPeerEventHandler(null));
            Assert.Throws<DiameterPeerException>(() =>
                manager.RecordPeerManagerCapabilities(
                    missing.Value,
                    new DiameterPeerCapabilities()));
            Assert.Throws<DiameterPeerException>(() =>
                manager.RecordPeerManagerTransportFailure(missing.Value));
            Assert.Throws<ArgumentException>(() =>
                manager.ReadPeerManagerIdentity(null));
            Assert.Throws<ArgumentException>(() =>
                manager.ReadPeerManagerIdentity(
                    CreatePeerManagerIdentity("bad peer")));
        });
    }

    [Test]
    public void ManagerCoversReplacementSendAndConnectionValidationBranches()
    {
        DiameterPeerManager manager = CreatePeerManager();
        DiameterPeerConfiguration original =
            CreatePeerManagerConfiguration(
                "peer.example",
                "192.0.2.10");
        manager.AddDiameterPeer(original);
        PeerManagerTestHandler handler = new PeerManagerTestHandler(manager);
        manager.RegisterPeerEventHandler(handler);
        DiameterPeerConfiguration replacement =
            CreatePeerManagerConfiguration(
                "peer.example",
                "192.0.2.11");

        DiameterPeerSnapshot replaced =
            manager.AddDiameterPeer(replacement);
        DiameterIdentity valid =
            CreatePeerManagerIdentity("peer.example");
        DiameterIdentity invalid =
            CreatePeerManagerIdentity("bad peer");

        DiameterTransportIoResult invalidIdentity =
            manager.SendDiameterPeerBytes(invalid, new byte[] { 1 }, 1);
        DiameterTransportIoResult missingBytes =
            manager.SendDiameterPeerBytes(valid, null, 1);
        DiameterTransportIoResult invalidTimeout =
            manager.SendDiameterPeerBytes(valid, new byte[] { 1 }, 0);

        Assert.Multiple(() =>
        {
            Assert.That(replaced.Configuration.Endpoints[0].Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.11")));
            Assert.That(handler.Events[0].Code,
                Is.EqualTo("peer.configuration.updated"));
            Assert.That(invalidIdentity.Status,
                Is.EqualTo(
                    DiameterTransportStatus.InvalidConfiguration));
            Assert.That(missingBytes.Status,
                Is.EqualTo(
                    DiameterTransportStatus.InvalidConfiguration));
            Assert.That(invalidTimeout.Status,
                Is.EqualTo(
                    DiameterTransportStatus.InvalidConfiguration));
            Assert.Throws<ArgumentException>(() =>
                manager.AttachPeerManagerConnection(invalid, null!));
            Assert.Throws<ArgumentException>(() =>
                manager.AttachPeerManagerConnection(valid, null!));
            Assert.Throws<ArgumentException>(() =>
                manager.DetachPeerManagerConnection(invalid));
        });
    }

    private static DiameterPeerManager CreatePeerManager()
    {
        DiameterNodeConfiguration configuration =
            CreatePeerManagerNodeConfiguration();
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        return new DiameterPeerManager(
            configuration,
            clock,
            new BaseCommandTestIdentifierSource());
    }

    private static DiameterNodeConfiguration
        CreatePeerManagerNodeConfiguration()
    {
        DiameterNodeConfiguration result =
            new DiameterNodeConfiguration();
        result.OriginHost.Value = "node.example";
        result.OriginRealm.Value = "example";
        result.ProductName = "peer-manager-tests";
        result.Roles = DiameterNodeRole.Client;
        result.Security.AllowUnsecuredTransport = true;
        return result;
    }

    private static DiameterIdentity CreatePeerManagerIdentity(
        string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DiameterPeerManager CreateOpenPeerManager()
    {
        DiameterPeerManager result = CreatePeerManager();
        DiameterPeerConfiguration configuration =
            CreatePeerManagerConfiguration("peer.example", "192.0.2.10");
        result.AddDiameterPeer(configuration);
        result.Table.AttachPeerTableConnection(
            "peer.example",
            configuration.Endpoints[0],
            CreatePeerManagerConnection(configuration.Endpoints[0]),
            DiameterConnectionDirection.Outbound);
        result.Table.TransitionPeerTableState(
            "peer.example",
            DiameterPeerState.InitiatorOpen);
        result.WatchdogController.HandlePeerWatchdogConnectionOpened(
            "peer.example");
        return result;
    }

    private static DiameterPeerConfiguration CreatePeerManagerConfiguration(
        string identity,
        string address)
    {
        DiameterPeerConfiguration result = new DiameterPeerConfiguration();
        result.Identity.Value = identity;
        result.Realm.Value = "example";
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = identity;
        endpoint.Host = identity;
        endpoint.Address = IPAddress.Parse(address);
        endpoint.Port = DiameterProtocolConstants.Port;
        endpoint.Transport = DiameterTransportKind.Tcp;
        result.Endpoints.Add(endpoint);
        return result;
    }

    private static DiameterConnectionMetadata CreatePeerManagerConnection(
        DiameterEndpoint endpoint)
    {
        DiameterConnectionMetadata result = new DiameterConnectionMetadata();
        result.ConnectionId = "peer-manager-connection";
        result.LocalEndpoint = new DiameterEndpoint();
        result.LocalEndpoint.Address = IPAddress.Parse("192.0.2.1");
        result.LocalEndpoint.Port = DiameterProtocolConstants.Port;
        result.LocalEndpoint.Transport = DiameterTransportKind.Tcp;
        result.RemoteEndpoint = PeerTableCloner.ClonePeerTableEndpoint(endpoint);
        result.Transport.Kind = DiameterTransportKind.Tcp;
        result.Open = true;
        return result;
    }

    private static DiameterDisconnectRequest CreatePeerManagerDisconnectRequest()
    {
        DiameterDisconnectRequest result = new DiameterDisconnectRequest();
        result.PeerIdentity.Value = "peer.example";
        result.Cause = DiameterDisconnectCause.Rebooting;
        result.TimeoutMilliseconds = 5000;
        result.WaitForAnswer = true;
        return result;
    }

    private static DiameterPeerCapabilities CreatePeerManagerCapabilities(
        uint originStateId,
        string productName)
    {
        DiameterPeerCapabilities result = new DiameterPeerCapabilities();
        result.OriginHost.Value = "peer.example";
        result.OriginRealm.Value = "example";
        result.HostAddresses.Add(IPAddress.Parse("192.0.2.10"));
        result.VendorId = 10415;
        result.ProductName = productName;
        result.FirmwareRevision = 1;
        result.AuthApplicationIds.Add(4);
        result.InbandSecurityIds.Add(0);
        result.OriginStateId = originStateId;
        return result;
    }
}

internal class PeerManagerTestHandler : IPeerEventHandler
{
    internal DiameterPeerManager Manager;
    internal List<DiameterPeerEvent> Events = new List<DiameterPeerEvent>();
    internal bool MutateEvent;
    internal bool CallbackUnderHandlerLock;
    internal bool CallbackUnderTableLock;
    internal bool ReadSnapshotDuringCallback;
    internal bool UnregisterDuringCallback;
    internal DiameterPeerSnapshot? CallbackSnapshot;

    internal PeerManagerTestHandler(DiameterPeerManager manager)
    {
        Manager = manager;
    }

    public void HandlePeerEvent(DiameterPeerEvent peerEvent)
    {
        CallbackUnderHandlerLock = CallbackUnderHandlerLock ||
            Monitor.IsEntered(Manager.HandlerLock);
        CallbackUnderTableLock = CallbackUnderTableLock ||
            Monitor.IsEntered(Manager.Table.TableLock);
        Events.Add(peerEvent);
        if (ReadSnapshotDuringCallback)
        {
            DiameterIdentity identity = new DiameterIdentity();
            identity.Value = peerEvent.Peer.Configuration.Identity.Value;
            CallbackSnapshot = Manager.ReadDiameterPeerSnapshot(identity);
        }

        if (UnregisterDuringCallback)
        {
            Manager.UnregisterPeerEventHandler(this);
        }

        if (MutateEvent)
        {
            peerEvent.Code = "mutated";
            peerEvent.Peer.Configuration.Identity.Value = "mutated.example";
        }
    }
}
