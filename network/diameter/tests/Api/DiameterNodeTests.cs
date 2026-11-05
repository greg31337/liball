using System.Net;

namespace diameter.Tests;

public class DiameterNodeTests
{
    [Test]
    public void FacadeExposesIdempotentPublicLifecycle()
    {
        IDiameterNode node = new DiameterNode();

        DiameterNodeLifecycleResult premature =
            node.StartDiameterNode();
        DiameterNodeLifecycleResult configured =
            node.ConfigureDiameterNode(CreateValidConfiguration());
        DiameterNodeLifecycleResult started = node.StartDiameterNode();
        DiameterNodeLifecycleResult startedAgain =
            node.StartDiameterNode();
        DiameterNodeLifecycleResult drained = node.DrainDiameterNode();
        DiameterNodeLifecycleResult stopped = node.StopDiameterNode();
        DiameterNodeLifecycleResult stoppedAgain = node.StopDiameterNode();

        Assert.Multiple(() =>
        {
            Assert.That(premature.Succeeded, Is.False);
            Assert.That(premature.State,
                Is.EqualTo(DiameterNodeState.Unconfigured));
            Assert.That(configured.State,
                Is.EqualTo(DiameterNodeState.Configured));
            Assert.That(started.State,
                Is.EqualTo(DiameterNodeState.Running));
            Assert.That(startedAgain.Repeated, Is.True);
            Assert.That(drained.State,
                Is.EqualTo(DiameterNodeState.Drained));
            Assert.That(stopped.State,
                Is.EqualTo(DiameterNodeState.Stopped));
            Assert.That(stoppedAgain.Repeated, Is.True);
            Assert.That(node.ReadDiameterNodeState(),
                Is.EqualTo(DiameterNodeState.Stopped));
        });
    }

    [Test]
    public void FacadeRetainsDirectSubsystemAccessAndOwnedSnapshots()
    {
        IDiameterNode node = new DiameterNode();
        Assert.Throws<ArgumentException>(() =>
            node.ReadDiameterDictionary());
        node.ConfigureDiameterNode(CreateValidConfiguration());

        DiameterNodeStatistics firstStatistics =
            node.ReadDiameterNodeStatistics();
        DiameterNodeStatistics secondStatistics =
            node.ReadDiameterNodeStatistics();
        DiameterNodeHealth firstHealth = node.ReadDiameterNodeHealth();
        DiameterNodeHealth secondHealth = node.ReadDiameterNodeHealth();

        Assert.Multiple(() =>
        {
            Assert.That(node.ReadDiameterMessageCodec(),
                Is.TypeOf<DiameterMessageCodec>());
            Assert.That(node.ReadDiameterDictionary(),
                Is.TypeOf<DiameterDictionary>());
            Assert.That(node.ReadDiameterValidator(),
                Is.TypeOf<DiameterValidator>());
            Assert.That(node.ReadDiameterPeerManager(),
                Is.TypeOf<DiameterPeerManager>());
            Assert.That(node.ReadDiameterRouter(),
                Is.TypeOf<DiameterRouter>());
            Assert.That(node.ReadDiameterRequestClient(),
                Is.TypeOf<DiameterRequestClient>());
            Assert.That(node.ReadDiameterSessionManager(),
                Is.TypeOf<DiameterSessionManager>());
            Assert.That(node.ReadDiameterAccountingClient(),
                Is.TypeOf<DiameterAccountingClient>());
            Assert.That(node.ReadDiameterCreditControlClient(), Is.Null);
            Assert.That(node.ReadDiameterApplicationHost(),
                Is.TypeOf<DiameterApplicationHost>());
            Assert.That(node.ReadDiameterOverloadManager(), Is.Null);
            Assert.That(secondStatistics,
                Is.Not.SameAs(firstStatistics));
            Assert.That(secondHealth, Is.Not.SameAs(firstHealth));
            Assert.That(firstHealth.Status,
                Is.EqualTo(DiameterHealthStatus.Healthy));
            Assert.That(firstHealth.AcceptingRequests, Is.False);
        });

        node.StopDiameterNode();
    }

    [Test]
    public void FacadeForwardsRuntimeChangesAndGuardsRequestSending()
    {
        IDiameterNode node = new DiameterNode();
        node.ConfigureDiameterNode(CreateValidConfiguration());
        Assert.Throws<ArgumentException>(() =>
            node.SendDiameterRequest(CreateSendRequest(), null));
        node.StartDiameterNode();

        DiameterPeerSnapshot peer = node.AddDiameterPeer(
            CreatePeerConfiguration());
        DiameterRouteSnapshot route = node.AddDiameterRoute(
            CreateRouteConfiguration());
        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = "facade-test";
        module.Version = "1";
        DiameterDictionaryRegistrationResult dictionary =
            node.RegisterDiameterDictionary(module);
        FacadeApplication application = new FacadeApplication();
        DiameterApplicationRegistration registration =
            node.RegisterDiameterApplication(application);
        DiameterSendResult send = node.SendDiameterRequest(
            CreateSendRequest(),
            null);

        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = "peer.example";
        bool removedPeer = node.RemoveDiameterPeer(identity);
        bool removedRoute = node.RemoveDiameterRoute("facade-route");
        bool removedApplication = node.UnregisterDiameterApplication(
            "facade-application",
            true);
        node.DrainDiameterNode();

        Assert.Multiple(() =>
        {
            Assert.That(peer.Configuration.Identity.Value,
                Is.EqualTo("peer.example"));
            Assert.That(route.Configuration.RouteIdentifier,
                Is.EqualTo("facade-route"));
            Assert.That(dictionary.Status,
                Is.EqualTo(DiameterDictionaryRegistrationStatus.Success));
            Assert.That(registration.Name,
                Is.EqualTo("facade-application"));
            Assert.That(send.Status,
                Is.EqualTo(DiameterSendStatus.NoRoute));
            Assert.That(removedPeer, Is.True);
            Assert.That(removedRoute, Is.True);
            Assert.That(removedApplication, Is.True);
            Assert.That(application.StartCount, Is.EqualTo(1));
            Assert.That(application.DrainCount, Is.EqualTo(1));
            Assert.That(application.StopCount, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() =>
                node.SendDiameterRequest(CreateSendRequest(), null));
        });

        node.StopDiameterNode();
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
        route.RouteIdentifier = "facade-route";
        route.DestinationRealm.Value = "example";
        route.Action = DiameterRouteAction.Local;
        route.AgentRole = DiameterAgentRole.None;
        return route;
    }

    private static DiameterSendRequest CreateSendRequest()
    {
        DiameterSendRequest request = new DiameterSendRequest();
        request.Request.Version = DiameterProtocolConstants.Version;
        request.Request.Flags = DiameterMessageFlags.Request;
        request.Request.CommandCode = 999U;
        request.Request.ApplicationId = 10000U;
        return request;
    }

    private class FacadeApplication : IDiameterApplication
    {
        internal int StartCount;
        internal int DrainCount;
        internal int StopCount;

        public DiameterApplicationRegistration
            ReadDiameterApplicationRegistration()
        {
            DiameterApplicationRegistration registration =
                new DiameterApplicationRegistration();
            registration.Name = "facade-application";
            registration.ApplicationId = 10000U;
            registration.Roles = DiameterNodeRole.Server;
            DiameterCommandRegistration command =
                new DiameterCommandRegistration();
            command.CommandCode = 10000U;
            command.Name = "Facade-Command";
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
}
