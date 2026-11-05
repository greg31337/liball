using System.Net;

namespace diameter.Tests;

public class NodeLifecycleTests
{
    [Test]
    public void ConfigureStartDrainAndStopAreIdempotent()
    {
        DiameterNodeConfiguration configuration =
            CreateValidConfiguration();
        NodeLifecycle lifecycle = new NodeLifecycle();

        NodeLifecycleResult configured =
            lifecycle.ConfigureDiameterNode(configuration);
        configuration.OriginHost.Value = "changed.example";
        NodeLifecycleResult configuredAgain =
            lifecycle.ConfigureDiameterNode(configuration);
        NodeLifecycleResult started = lifecycle.StartDiameterNode();
        NodeLifecycleResult startedAgain = lifecycle.StartDiameterNode();
        NodeLifecycleResult drained = lifecycle.DrainDiameterNode();
        NodeLifecycleResult drainedAgain = lifecycle.DrainDiameterNode();
        NodeLifecycleResult stopped = lifecycle.StopDiameterNode();
        NodeLifecycleResult stoppedAgain = lifecycle.StopDiameterNode();

        Assert.Multiple(() =>
        {
            Assert.That(configured.Succeeded, Is.True);
            Assert.That(configuredAgain.Repeated, Is.True);
            Assert.That(lifecycle.ReadDiameterNodeComposition()
                .Configuration.OriginHost.Value,
                Is.EqualTo("node.example"));
            Assert.That(started.Succeeded, Is.True);
            Assert.That(startedAgain.Repeated, Is.True);
            Assert.That(drained.Succeeded, Is.True);
            Assert.That(drainedAgain.Repeated, Is.True);
            Assert.That(stopped.Succeeded, Is.True);
            Assert.That(stoppedAgain.Repeated, Is.True);
            Assert.That(lifecycle.ReadDiameterNodeLifecycleState(),
                Is.EqualTo(NodeLifecycleState.Stopped));
        });
    }

    [Test]
    public void FailedTransportStartReturnsToConfiguredAndCanRetry()
    {
        LifecycleTransportFactory transport =
            new LifecycleTransportFactory();
        transport.FailProbe = true;
        DiameterNodeConfiguration configuration =
            CreateValidConfiguration();
        configuration.TransportFactory = transport;
        NodeLifecycle lifecycle = new NodeLifecycle();
        lifecycle.ConfigureDiameterNode(configuration);

        NodeLifecycleResult failed = lifecycle.StartDiameterNode();
        NodeLifecycleState failedState =
            lifecycle.ReadDiameterNodeLifecycleState();
        transport.FailProbe = false;
        NodeLifecycleResult retried = lifecycle.StartDiameterNode();

        Assert.Multiple(() =>
        {
            Assert.That(failed.Succeeded, Is.False);
            Assert.That(failed.Code, Is.EqualTo("test-probe-failed"));
            Assert.That(failedState,
                Is.EqualTo(NodeLifecycleState.Configured));
            Assert.That(retried.Succeeded, Is.True);
            Assert.That(lifecycle.ReadDiameterNodeLifecycleState(),
                Is.EqualTo(NodeLifecycleState.Running));
        });

        lifecycle.StopDiameterNode();
    }

    [Test]
    public void RuntimePeerRouteDictionaryAndApplicationChangesAreGuarded()
    {
        NodeLifecycle lifecycle = new NodeLifecycle();
        lifecycle.ConfigureDiameterNode(CreateValidConfiguration());
        lifecycle.StartDiameterNode();

        DiameterPeerConfiguration peer = CreatePeerConfiguration();
        DiameterPeerSnapshot addedPeer =
            lifecycle.AddDiameterNodePeer(peer);
        peer.Identity.Value = "changed.example";
        DiameterIdentity peerIdentity = new DiameterIdentity();
        peerIdentity.Value = "peer.example";
        bool removedPeer = lifecycle.RemoveDiameterNodePeer(peerIdentity);

        DiameterRouteConfiguration route = CreateRouteConfiguration();
        DiameterRouteSnapshot addedRoute =
            lifecycle.AddDiameterNodeRoute(route);
        route.RouteIdentifier = "changed-route";
        bool removedRoute =
            lifecycle.RemoveDiameterNodeRoute("runtime-route");

        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "runtime-test";
        module.Version = "1";
        DiameterDictionaryRegistrationResult dictionary =
            lifecycle.RegisterDiameterNodeDictionary(module);

        LifecycleApplication application =
            new LifecycleApplication();
        DiameterApplicationRegistration registration =
            lifecycle.RegisterDiameterNodeApplication(application);
        bool removedApplication =
            lifecycle.UnregisterDiameterNodeApplication(
                "runtime-application",
                true);
        lifecycle.DrainDiameterNode();

        Assert.Multiple(() =>
        {
            Assert.That(addedPeer.Configuration.Identity.Value,
                Is.EqualTo("peer.example"));
            Assert.That(removedPeer, Is.True);
            Assert.That(addedRoute.Configuration.RouteIdentifier,
                Is.EqualTo("runtime-route"));
            Assert.That(removedRoute, Is.True);
            Assert.That(dictionary.Status,
                Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
            Assert.That(registration.Name,
                Is.EqualTo("runtime-application"));
            Assert.That(removedApplication, Is.True);
            Assert.That(application.StartCount, Is.EqualTo(1));
            Assert.That(application.DrainCount, Is.EqualTo(1));
            Assert.That(application.StopCount, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() =>
                lifecycle.AddDiameterNodePeer(
                    CreatePeerConfiguration()));
        });

        lifecycle.StopDiameterNode();
    }

    [Test]
    public void StopClosesOwnedResourcesAndConnectionsExactlyOnce()
    {
        LifecycleTransportFactory transport =
            new LifecycleTransportFactory();
        DiameterNodeConfiguration configuration =
            CreateValidConfiguration();
        configuration.TransportFactory = transport;
        NodeLifecycle lifecycle = new NodeLifecycle();
        lifecycle.ConfigureDiameterNode(configuration);
        lifecycle.StartDiameterNode();
        LifecycleConnection connection = new LifecycleConnection();
        lifecycle.AcceptedConnections.Add(connection);
        DiameterPeerManager manager = (DiameterPeerManager)lifecycle
            .ReadDiameterNodeComposition().PeerManager;
        manager.Connections.Add("peer.example", connection);

        lifecycle.StopDiameterNode();
        lifecycle.StopDiameterNode();

        Assert.Multiple(() =>
        {
            Assert.That(connection.CloseCount, Is.EqualTo(1));
            Assert.That(transport.StopCount, Is.EqualTo(1));
            Assert.That(manager.Connections, Is.Empty);
        });
    }

    [Test]
    public void StopClosesListenerAndWaitsForItsWorker()
    {
        DiameterNodeConfiguration configuration =
            CreateValidConfiguration();
        configuration.Roles =
            DiameterNodeRole.Client | DiameterNodeRole.Server;
        configuration.Transport.EnableListener = true;
        LifecycleTransportFactory transport =
            new LifecycleTransportFactory();
        LifecycleListener listener =
            new LifecycleListener(configuration.Transport);
        transport.Listener = listener;
        configuration.TransportFactory = transport;
        NodeLifecycle lifecycle = new NodeLifecycle();
        lifecycle.ConfigureDiameterNode(configuration);

        NodeLifecycleResult started = lifecycle.StartDiameterNode();
        bool workerEntered = listener.AcceptEntered.Wait(2000);
        NodeLifecycleResult stopped = lifecycle.StopDiameterNode();

        Assert.Multiple(() =>
        {
            Assert.That(started.Succeeded, Is.True);
            Assert.That(workerEntered, Is.True);
            Assert.That(stopped.Succeeded, Is.True);
            Assert.That(listener.CloseCount, Is.EqualTo(1));
            Assert.That(lifecycle.ReadDiameterNodeLifecycleState(),
                Is.EqualTo(NodeLifecycleState.Stopped));
        });
    }

    private static DiameterNodeConfiguration CreateValidConfiguration()
    {
        DiameterNodeConfiguration configuration =
            new DiameterNodeConfiguration();
        configuration.OriginHost.Value = "node.example";
        configuration.OriginRealm.Value = "example";
        configuration.ProductName = "diameter.Tests";
        configuration.Roles = DiameterNodeRole.Client;
        configuration.Security.AllowUnsecuredTransport = true;
        return configuration;
    }

    private static DiameterPeerConfiguration CreatePeerConfiguration()
    {
        DiameterPeerConfiguration peer =
            new DiameterPeerConfiguration();
        peer.Identity.Value = "peer.example";
        peer.Realm.Value = "example";
        peer.Source = DiameterPeerSource.Static;
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "peer.example";
        endpoint.Host = "peer.example";
        endpoint.Address = IPAddress.Loopback;
        endpoint.Port = 3868;
        endpoint.Transport = DiameterTransportKind.Tcp;
        endpoint.Security = DiameterSecurityMode.None;
        peer.Endpoints.Add(endpoint);
        return peer;
    }

    private static DiameterRouteConfiguration CreateRouteConfiguration()
    {
        DiameterRouteConfiguration route =
            new DiameterRouteConfiguration();
        route.RouteIdentifier = "runtime-route";
        route.DestinationRealm.Value = "example";
        route.Action = DiameterRouteAction.Local;
        route.AgentRole = DiameterAgentRole.None;
        return route;
    }

    private class LifecycleApplication : IDiameterApplication
    {
        internal int StartCount;
        internal int DrainCount;
        internal int StopCount;

        public DiameterApplicationRegistration
            ReadDiameterApplicationRegistration()
        {
            DiameterApplicationRegistration registration =
                new DiameterApplicationRegistration();
            registration.Name = "runtime-application";
            registration.ApplicationId = 10000U;
            registration.Roles = DiameterNodeRole.Server;
            DiameterCommandRegistration command =
                new DiameterCommandRegistration();
            command.CommandCode = 10000U;
            command.Name = "Runtime-Command";
            command.RequireValidation = false;
            registration.Commands.Add(command);
            return registration;
        }

        public IDiameterRequestHandler?
            FindDiameterApplicationRequestHandler(uint commandCode)
        {
            return null;
        }

        public List<IMessageInterceptor>
            ReadDiameterApplicationInterceptors()
        {
            return new List<IMessageInterceptor>();
        }

        public void StartDiameterApplication()
        {
            StartCount = StartCount + 1;
        }

        public void DrainDiameterApplication()
        {
            DrainCount = DrainCount + 1;
        }

        public void StopDiameterApplication()
        {
            StopCount = StopCount + 1;
        }
    }

    private class LifecycleTransportFactory :
        IDiameterTransportFactory,
        IDiameterNodeResource
    {
        internal bool FailProbe;
        internal int StopCount;
        internal LifecycleListener? Listener;

        public DiameterTransportSupportResult
            ProbeDiameterTransportSupport(
                DiameterTransportConfiguration? transport,
                DiameterSecurityConfiguration? security)
        {
            if (FailProbe)
            {
                DiameterTransportSupportResult failure =
                    new DiameterTransportSupportResult();
                failure.Status = DiameterTransportStatus.Failed;
                failure.Code = "test-probe-failed";
                failure.Message = "The test transport probe failed.";
                return failure;
            }

            DiameterTcpTransportFactory factory =
                new DiameterTcpTransportFactory();
            return factory.ProbeDiameterTransportSupport(
                transport,
                security);
        }

        public DiameterListenerResult CreateDiameterListener(
            DiameterTransportConfiguration? transport,
            DiameterSecurityConfiguration? security)
        {
            DiameterListenerResult result =
                new DiameterListenerResult();
            if (Listener == null)
            {
                result.Status = DiameterTransportStatus.Unsupported;
                result.Code = "test-listener-disabled";
            }
            else
            {
                result.Listener = Listener;
                result.Metadata = Listener.ReadDiameterListenerMetadata();
            }

            return result;
        }

        public DiameterConnectionResult ConnectDiameterEndpoint(
            DiameterEndpoint? endpoint,
            DiameterTransportConfiguration? transport,
            DiameterSecurityConfiguration? security,
            int timeoutMilliseconds)
        {
            DiameterConnectionResult result =
                new DiameterConnectionResult();
            result.Status = DiameterTransportStatus.Unsupported;
            result.Code = "test-connect-disabled";
            return result;
        }

        public void StopDiameterNodeResource()
        {
            StopCount = StopCount + 1;
        }
    }

    private class LifecycleListener : IDiameterListener
    {
        internal ManualResetEventSlim AcceptEntered =
            new ManualResetEventSlim(false);
        internal ManualResetEventSlim Closed =
            new ManualResetEventSlim(false);
        internal DiameterListenerMetadata Metadata =
            new DiameterListenerMetadata();
        internal int CloseCount;

        internal LifecycleListener(
            DiameterTransportConfiguration configuration)
        {
            DiameterTcpTransportFactory factory =
                new DiameterTcpTransportFactory();
            DiameterSecurityConfiguration security =
                new DiameterSecurityConfiguration();
            security.AllowUnsecuredTransport = true;
            Metadata.Transport = factory.ProbeDiameterTransportSupport(
                configuration,
                security).Metadata;
            Metadata.LocalEndpoint.Host = "127.0.0.1";
            Metadata.LocalEndpoint.Address = IPAddress.Loopback;
            Metadata.LocalEndpoint.Port = configuration.Port;
            Metadata.LocalEndpoint.Transport = DiameterTransportKind.Tcp;
            Metadata.LocalEndpoint.Security = DiameterSecurityMode.None;
            Metadata.Open = true;
        }

        public DiameterConnectionResult AcceptDiameterConnection(
            int timeoutMilliseconds)
        {
            AcceptEntered.Set();
            bool closed = Closed.Wait(timeoutMilliseconds);
            DiameterConnectionResult result =
                new DiameterConnectionResult();
            result.Status = closed ?
                DiameterTransportStatus.Closed :
                DiameterTransportStatus.TimedOut;
            return result;
        }

        public DiameterListenerMetadata ReadDiameterListenerMetadata()
        {
            Metadata.Open = !Closed.IsSet;
            return Metadata;
        }

        public DiameterTransportOperationResult CloseDiameterListener()
        {
            CloseCount = CloseCount + 1;
            Closed.Set();
            Metadata.Open = false;
            return new DiameterTransportOperationResult();
        }
    }

    private class LifecycleConnection : IDiameterConnection
    {
        internal int CloseCount;

        public DiameterTransportIoResult SendDiameterConnectionBytes(
            ReadOnlySpan<byte> source,
            int timeoutMilliseconds)
        {
            return new DiameterTransportIoResult();
        }

        public DiameterTransportIoResult ReceiveDiameterConnectionBytes(
            Span<byte> destination,
            int timeoutMilliseconds)
        {
            return new DiameterTransportIoResult();
        }

        public DiameterConnectionMetadata ReadDiameterConnectionMetadata()
        {
            return new DiameterConnectionMetadata();
        }

        public DiameterTransportOperationResult CloseDiameterConnection()
        {
            CloseCount = CloseCount + 1;
            return new DiameterTransportOperationResult();
        }
    }
}
