using System.Net;

namespace diameter.Tests;

public class NodeLifecycleMatrixTests
{
    [Test]
    public void PartialListenerStartupFailureCleansUpAndCanRetry()
    {
        DiameterNodeConfiguration configuration =
            CreateValidConfiguration();
        configuration.Roles =
            DiameterNodeRole.Client | DiameterNodeRole.Server;
        configuration.Transport.EnableListener = true;
        MatrixTransportFactory transport =
            new MatrixTransportFactory();
        MatrixListener failedListener =
            new MatrixListener(configuration.Transport);
        transport.Listener = failedListener;
        transport.FailListener = true;
        configuration.TransportFactory = transport;
        NodeLifecycle lifecycle = new NodeLifecycle();
        lifecycle.ConfigureDiameterNode(configuration);

        NodeLifecycleResult failed = lifecycle.StartDiameterNode();
        MatrixListener retryListener =
            new MatrixListener(configuration.Transport);
        transport.Listener = retryListener;
        transport.FailListener = false;
        NodeLifecycleResult retried = lifecycle.StartDiameterNode();
        NodeLifecycleResult stopped = lifecycle.StopDiameterNode();

        Assert.Multiple(() =>
        {
            Assert.That(failed.Succeeded, Is.False);
            Assert.That(failed.Code, Is.EqualTo("test-listener-failed"));
            Assert.That(failedListener.CloseCount, Is.EqualTo(1));
            Assert.That(retried.Succeeded, Is.True);
            Assert.That(stopped.Succeeded, Is.True);
            Assert.That(retryListener.CloseCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void DrainWaitsForInflightApplicationWork()
    {
        NodeLifecycle lifecycle = CreateRunningLifecycle();
        ManualResetEventSlim release = new ManualResetEventSlim(false);
        CountdownEvent handlerEntered = new CountdownEvent(1);
        MatrixHandler handler = new MatrixHandler(
            release,
            handlerEntered);
        MatrixApplication application = new MatrixApplication(
            "graceful-matrix",
            11001U,
            2000,
            handler,
            null);
        DiameterApplicationRegistration registration =
            lifecycle.RegisterDiameterNodeApplication(application);
        ApplicationWorkQueue queue = ReadApplicationQueue(
            lifecycle,
            registration.Name);
        MatrixCompletionCollector collector =
            new MatrixCompletionCollector(1);
        queue.EnqueueApplicationWork(
            handler,
            CreateContext(registration, 1U),
            collector);
        bool entered = handlerEntered.Wait(2000);

        Task<NodeLifecycleResult> drain = Task.Run(() =>
            lifecycle.DrainDiameterNode());
        bool drainEntered = application.DrainEntered.Wait(2000);
        bool completedBeforeRelease = drain.Wait(50);
        release.Set();
        bool drainCompleted = drain.Wait(5000);
        bool workCompleted = collector.Completed.Wait(5000);

        Assert.Multiple(() =>
        {
            Assert.That(entered, Is.True);
            Assert.That(drainEntered, Is.True);
            Assert.That(completedBeforeRelease, Is.False);
            Assert.That(drainCompleted, Is.True);
            Assert.That(workCompleted, Is.True);
            Assert.That(drain.Result.Succeeded, Is.True);
            Assert.That(drain.Result.Forced, Is.False);
            Assert.That(application.StopCount, Is.EqualTo(1));
        });

        lifecycle.StopDiameterNode();
    }

    [Test]
    public void StopReportsForcedApplicationQueueShutdown()
    {
        NodeLifecycle lifecycle = CreateRunningLifecycle();
        ManualResetEventSlim release = new ManualResetEventSlim(false);
        CountdownEvent handlerEntered = new CountdownEvent(1);
        MatrixHandler handler = new MatrixHandler(
            release,
            handlerEntered);
        MatrixApplication application = new MatrixApplication(
            "forced-matrix",
            11002U,
            25,
            handler,
            null);
        DiameterApplicationRegistration registration =
            lifecycle.RegisterDiameterNodeApplication(application);
        ApplicationWorkQueue queue = ReadApplicationQueue(
            lifecycle,
            registration.Name);
        MatrixCompletionCollector collector =
            new MatrixCompletionCollector(2);
        queue.EnqueueApplicationWork(
            handler,
            CreateContext(registration, 2U),
            collector);
        bool entered = handlerEntered.Wait(2000);
        ApplicationWorkAdmission queued = queue.EnqueueApplicationWork(
            handler,
            CreateContext(registration, 3U),
            collector);

        NodeLifecycleResult stopped = lifecycle.StopDiameterNode();
        ApplicationWorkCompletion forced =
            collector.ReadCompletion(queued.WorkIdentifier);
        release.Set();
        bool workCompleted = collector.Completed.Wait(5000);

        Assert.Multiple(() =>
        {
            Assert.That(entered, Is.True);
            Assert.That(stopped.Succeeded, Is.True);
            Assert.That(stopped.Forced, Is.True);
            Assert.That(forced.HandlerFailed, Is.True);
            Assert.That(forced.Result.Code,
                Is.EqualTo("application-work-forced-stop"));
            Assert.That(application.StopCount, Is.EqualTo(1));
            Assert.That(workCompleted, Is.True);
        });
    }

    [Test]
    public void ApplicationCallbacksCanReenterNodeWithoutLifecycleLock()
    {
        NodeLifecycle lifecycle = CreateRunningLifecycle();
        MatrixHandler handler = new MatrixHandler(null, null);
        MatrixApplication application = new MatrixApplication(
            "reentrant-matrix",
            11003U,
            2000,
            handler,
            lifecycle);

        DiameterApplicationRegistration registration =
            lifecycle.RegisterDiameterNodeApplication(application);
        List<DiameterRouteSnapshot> routes = lifecycle
            .ReadDiameterNodeComposition().Router
            .ReadDiameterRouteSnapshots();
        lifecycle.StopDiameterNode();

        Assert.Multiple(() =>
        {
            Assert.That(registration.Name,
                Is.EqualTo("reentrant-matrix"));
            Assert.That(application.Reentered, Is.True);
            Assert.That(application.ObservedLifecycleLock, Is.False);
            Assert.That(routes.Count, Is.EqualTo(1));
            Assert.That(routes[0].Configuration.RouteIdentifier,
                Is.EqualTo("reentrant-route"));
        });
    }

    [Test]
    public void ConcurrentPeerAndRouteChangesRemainConsistent()
    {
        NodeLifecycle lifecycle = CreateRunningLifecycle();
        Task[] changes = new Task[16];
        int index = 0;
        while (index < 8)
        {
            int changeIndex = index;
            changes[index] = Task.Run(() =>
                lifecycle.AddDiameterNodePeer(
                    CreatePeerConfiguration(changeIndex)));
            changes[index + 8] = Task.Run(() =>
                lifecycle.AddDiameterNodeRoute(
                    CreateRouteConfiguration(changeIndex)));
            index = index + 1;
        }

        bool completed = Task.WaitAll(changes, 5000);
        List<DiameterPeerSnapshot> peers = lifecycle
            .ReadDiameterNodeComposition().PeerManager
            .ReadDiameterPeerSnapshots();
        List<DiameterRouteSnapshot> routes = lifecycle
            .ReadDiameterNodeComposition().Router
            .ReadDiameterRouteSnapshots();
        NodeLifecycleResult drained = lifecycle.DrainDiameterNode();

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True);
            Assert.That(peers.Count, Is.EqualTo(8));
            Assert.That(routes.Count, Is.EqualTo(8));
            Assert.That(drained.Succeeded, Is.True);
            Assert.Throws<ArgumentException>(() =>
                lifecycle.AddDiameterNodeRoute(
                    CreateRouteConfiguration(9)));
        });

        lifecycle.StopDiameterNode();
    }

    private static NodeLifecycle CreateRunningLifecycle()
    {
        NodeLifecycle lifecycle = new NodeLifecycle();
        lifecycle.ConfigureDiameterNode(CreateValidConfiguration());
        lifecycle.StartDiameterNode();
        return lifecycle;
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

    private static DiameterPeerConfiguration CreatePeerConfiguration(
        int index)
    {
        string host = "peer" + index + ".example";
        DiameterPeerConfiguration peer =
            new DiameterPeerConfiguration();
        peer.Identity.Value = host;
        peer.Realm.Value = "example";
        peer.Source = DiameterPeerSource.Static;
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = host;
        endpoint.Host = host;
        endpoint.Address = IPAddress.Loopback;
        endpoint.Port = 3868 + index;
        endpoint.Transport = DiameterTransportKind.Tcp;
        endpoint.Security = DiameterSecurityMode.None;
        peer.Endpoints.Add(endpoint);
        return peer;
    }

    private static DiameterRouteConfiguration CreateRouteConfiguration(
        int index)
    {
        DiameterRouteConfiguration route =
            new DiameterRouteConfiguration();
        route.RouteIdentifier = "matrix-route-" + index;
        route.DestinationRealm.Value = "realm" + index + ".example";
        route.Action = DiameterRouteAction.Local;
        route.AgentRole = DiameterAgentRole.None;
        return route;
    }

    private static DiameterRouteConfiguration CreateReentrantRoute()
    {
        DiameterRouteConfiguration route =
            new DiameterRouteConfiguration();
        route.RouteIdentifier = "reentrant-route";
        route.DestinationRealm.Value = "reentrant.example";
        route.Action = DiameterRouteAction.Local;
        route.AgentRole = DiameterAgentRole.None;
        return route;
    }

    private static ApplicationWorkQueue ReadApplicationQueue(
        NodeLifecycle lifecycle,
        string registrationName)
    {
        DiameterApplicationHost host = (DiameterApplicationHost)lifecycle
            .ReadDiameterNodeComposition().ApplicationHost;
        ApplicationWorkQueue? queue = host.Lifecycle
            .ReadApplicationWorkQueue(registrationName);
        if (queue == null)
        {
            throw new InvalidOperationException(
                "The application work queue was not created.");
        }

        return queue;
    }

    private static DiameterRequestContext CreateContext(
        DiameterApplicationRegistration registration,
        uint hopByHopIdentifier)
    {
        DiameterRequestContext context = new DiameterRequestContext();
        context.Registration = registration;
        context.Command = registration.Commands[0];
        context.Request.Version = DiameterProtocolConstants.Version;
        context.Request.Flags = DiameterMessageFlags.Request;
        context.Request.CommandCode = context.Command.CommandCode;
        context.Request.ApplicationId = registration.ApplicationId;
        context.Request.HopByHopIdentifier = hopByHopIdentifier;
        context.PeerIdentity.Value = "peer.example";
        context.RouteIdentifier = "matrix-route";
        context.ReceivedAt = DateTimeOffset.UtcNow;
        return context;
    }

    private class MatrixApplication : IDiameterApplication
    {
        private string Name;
        private uint ApplicationId;
        private int DrainTimeoutMilliseconds;
        private IDiameterRequestHandler Handler;
        private NodeLifecycle? Lifecycle;
        internal ManualResetEventSlim DrainEntered =
            new ManualResetEventSlim(false);
        internal int StopCount;
        internal bool Reentered;
        internal bool ObservedLifecycleLock;

        internal MatrixApplication(
            string name,
            uint applicationId,
            int drainTimeoutMilliseconds,
            IDiameterRequestHandler handler,
            NodeLifecycle? lifecycle)
        {
            Name = name;
            ApplicationId = applicationId;
            DrainTimeoutMilliseconds = drainTimeoutMilliseconds;
            Handler = handler;
            Lifecycle = lifecycle;
        }

        public DiameterApplicationRegistration
            ReadDiameterApplicationRegistration()
        {
            DiameterApplicationRegistration registration =
                new DiameterApplicationRegistration();
            registration.Name = Name;
            registration.ApplicationId = ApplicationId;
            registration.Roles = DiameterNodeRole.Server;
            registration.MaximumConcurrency = 1;
            registration.QueueCapacity = 2;
            registration.DrainTimeoutMilliseconds =
                DrainTimeoutMilliseconds;
            DiameterCommandRegistration command =
                new DiameterCommandRegistration();
            command.CommandCode = ApplicationId;
            command.Name = "Matrix-Command";
            command.RequireValidation = false;
            registration.Commands.Add(command);
            return registration;
        }

        public IDiameterRequestHandler?
            FindDiameterApplicationRequestHandler(uint commandCode)
        {
            return commandCode == ApplicationId ? Handler : null;
        }

        public List<IMessageInterceptor>
            ReadDiameterApplicationInterceptors()
        {
            return new List<IMessageInterceptor>();
        }

        public void StartDiameterApplication()
        {
            if (Lifecycle != null)
            {
                ObservedLifecycleLock = Monitor.IsEntered(
                    Lifecycle.LifecycleLock);
                Lifecycle.AddDiameterNodeRoute(
                    CreateReentrantRoute());
                Reentered = Lifecycle
                    .ReadDiameterNodeLifecycleState() ==
                    NodeLifecycleState.Running;
            }
        }

        public void DrainDiameterApplication()
        {
            if (Lifecycle != null)
            {
                ObservedLifecycleLock = ObservedLifecycleLock ||
                    Monitor.IsEntered(Lifecycle.LifecycleLock);
                Reentered = Reentered && Lifecycle
                    .ReadDiameterNodeLifecycleState() ==
                    NodeLifecycleState.Draining;
            }

            DrainEntered.Set();
        }

        public void StopDiameterApplication()
        {
            if (Lifecycle != null)
            {
                ObservedLifecycleLock = ObservedLifecycleLock ||
                    Monitor.IsEntered(Lifecycle.LifecycleLock);
            }

            StopCount = StopCount + 1;
        }
    }

    private class MatrixHandler : IDiameterRequestHandler
    {
        private ManualResetEventSlim? Release;
        private CountdownEvent? Entered;

        internal MatrixHandler(
            ManualResetEventSlim? release,
            CountdownEvent? entered)
        {
            Release = release;
            Entered = entered;
        }

        public DiameterApplicationResult HandleDiameterApplicationRequest(
            DiameterRequestContext context)
        {
            if (Entered != null && Entered.CurrentCount > 0)
            {
                Entered.Signal();
            }

            if (Release != null)
            {
                Release.Wait();
            }

            DiameterApplicationResult result =
                new DiameterApplicationResult();
            result.Continue = false;
            result.Drop = true;
            result.Code = "matrix-handler-complete";
            return result;
        }
    }

    private class MatrixCompletionCollector :
        IApplicationWorkCompletionHandler
    {
        internal object CompletionLock = new object();
        internal CountdownEvent Completed;
        internal List<ApplicationWorkCompletion> Completions =
            new List<ApplicationWorkCompletion>();

        internal MatrixCompletionCollector(int completionCount)
        {
            Completed = new CountdownEvent(completionCount);
        }

        public void CompleteApplicationWork(
            ApplicationWorkCompletion completion)
        {
            lock (CompletionLock)
            {
                Completions.Add(completion);
            }

            Completed.Signal();
        }

        internal ApplicationWorkCompletion ReadCompletion(
            long workIdentifier)
        {
            int attempts = 0;
            while (attempts < 200)
            {
                lock (CompletionLock)
                {
                    int index = 0;
                    while (index < Completions.Count)
                    {
                        if (Completions[index].WorkIdentifier ==
                            workIdentifier)
                        {
                            return Completions[index];
                        }

                        index = index + 1;
                    }
                }

                Thread.Sleep(5);
                attempts = attempts + 1;
            }

            throw new InvalidOperationException(
                "The application work completion was not observed.");
        }
    }

    private class MatrixTransportFactory : IDiameterTransportFactory
    {
        internal bool FailListener;
        internal MatrixListener? Listener;

        public DiameterTransportSupportResult
            ProbeDiameterTransportSupport(
                DiameterTransportConfiguration? transport,
                DiameterSecurityConfiguration? security)
        {
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
            result.Listener = Listener;
            if (Listener != null)
            {
                result.Metadata = Listener.ReadDiameterListenerMetadata();
            }

            if (FailListener)
            {
                result.Status = DiameterTransportStatus.Failed;
                result.Code = "test-listener-failed";
                result.Message = "The test listener failed during startup.";
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
            return result;
        }
    }

    private class MatrixListener : IDiameterListener
    {
        internal ManualResetEventSlim Closed =
            new ManualResetEventSlim(false);
        internal DiameterListenerMetadata Metadata =
            new DiameterListenerMetadata();
        internal int CloseCount;

        internal MatrixListener(
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
}
