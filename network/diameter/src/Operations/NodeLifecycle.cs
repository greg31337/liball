using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

internal enum NodeLifecycleState
{
    Unconfigured = 0,
    Configuring = 1,
    Configured = 2,
    Starting = 3,
    Running = 4,
    Draining = 5,
    Drained = 6,
    Stopping = 7,
    Stopped = 8
}

internal class NodeLifecycleResult
{
    internal NodeLifecycleState State;
    internal bool Succeeded;
    internal bool Repeated;
    internal bool Forced;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class NodeTransportStartResult
{
    internal bool Succeeded;
    internal IDiameterListener? Listener;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class NodeLifecycle
{
    internal object LifecycleLock = new object();
    internal ManualResetEventSlim TransitionSignal =
        new ManualResetEventSlim(true);
    internal ManualResetEventSlim MutationSignal =
        new ManualResetEventSlim(true);
    internal NodeLifecycleState State = NodeLifecycleState.Unconfigured;
    internal NodeComposition? Composition;
    internal IDiameterListener? Listener;
    internal Task? ListenerWorker;
    internal List<IDiameterConnection> AcceptedConnections =
        new List<IDiameterConnection>();
    internal bool StopRequested;
    internal bool ApplicationsShutdown;
    internal bool ForcedShutdown;
    internal int ActiveMutations;

    internal NodeLifecycleResult ConfigureDiameterNode(
        DiameterNodeConfiguration? configuration)
    {
        bool waitForConfiguration = false;
        lock (LifecycleLock)
        {
            if (State == NodeLifecycleState.Configuring)
            {
                waitForConfiguration = true;
            }
            else if (Composition != null &&
                State != NodeLifecycleState.Stopped &&
                State != NodeLifecycleState.Unconfigured)
            {
                return CreateNodeLifecycleResult(
                    true,
                    true,
                    "node-already-configured",
                    "The Diameter node is already configured.");
            }
            else
            {
                State = NodeLifecycleState.Configuring;
                TransitionSignal.Reset();
            }
        }

        if (waitForConfiguration)
        {
            TransitionSignal.Wait();
            lock (LifecycleLock)
            {
                return CreateNodeLifecycleResult(
                    Composition != null,
                    true,
                    Composition == null ?
                        "node-configuration-failed" :
                        "node-already-configured",
                    Composition == null ?
                        "The Diameter node configuration failed." :
                        "The Diameter node is already configured.");
            }
        }

        Task<NodeComposition> work = Task.Run(() =>
            ConfigurationValidator.ComposeDiameterNode(configuration));
        WaitForDiameterTask(work);
        if (work.IsFaulted || work.IsCanceled)
        {
            ObserveDiameterTaskFailure(work);
            lock (LifecycleLock)
            {
                Composition = null;
                State = NodeLifecycleState.Unconfigured;
                TransitionSignal.Set();
                return CreateNodeLifecycleResult(
                    false,
                    false,
                    "node-configuration-failed",
                    "The Diameter node configuration is invalid.");
            }
        }

        NodeComposition composition = work.Result;
        lock (LifecycleLock)
        {
            Composition = composition;
            Listener = null;
            ListenerWorker = null;
            AcceptedConnections.Clear();
            StopRequested = false;
            ApplicationsShutdown = false;
            ForcedShutdown = false;
            ActiveMutations = 0;
            MutationSignal.Set();
            State = NodeLifecycleState.Configured;
            TransitionSignal.Set();
        }

        composition.Health.UpdateDiameterNodeActivity(
            false,
            false,
            false);
        return CreateNodeLifecycleResult(
            true,
            false,
            "node-configured",
            "The Diameter node was configured.");
    }

    internal NodeLifecycleResult StartDiameterNode()
    {
        bool waitForStart = false;
        NodeComposition? composition = null;
        lock (LifecycleLock)
        {
            if (State == NodeLifecycleState.Starting)
            {
                waitForStart = true;
            }
            else if (State == NodeLifecycleState.Running)
            {
                return CreateNodeLifecycleResult(
                    true,
                    true,
                    "node-already-running",
                    "The Diameter node is already running.");
            }
            else if (State != NodeLifecycleState.Configured ||
                Composition == null)
            {
                return CreateNodeLifecycleResult(
                    false,
                    false,
                    "node-not-configured",
                    "The Diameter node must be configured before it starts.");
            }
            else
            {
                State = NodeLifecycleState.Starting;
                TransitionSignal.Reset();
            }

            if (!waitForStart)
            {
                composition = Composition;
            }
        }

        if (waitForStart)
        {
            TransitionSignal.Wait();
            lock (LifecycleLock)
            {
                return CreateNodeLifecycleResult(
                    State == NodeLifecycleState.Running,
                    true,
                    State == NodeLifecycleState.Running ?
                        "node-already-running" :
                        "node-start-failed",
                    State == NodeLifecycleState.Running ?
                        "The Diameter node is already running." :
                        "The Diameter node did not start.");
            }
        }

        if (composition == null)
        {
            return CreateNodeLifecycleResult(
                false,
                false,
                "node-not-configured",
                "The Diameter node must be configured before it starts.");
        }

        Task<NodeTransportStartResult> transportWork = Task.Run(() =>
            StartDiameterTransport(composition));
        WaitForDiameterTask(transportWork);
        if (transportWork.IsFaulted || transportWork.IsCanceled)
        {
            ObserveDiameterTaskFailure(transportWork);
            return CompleteDiameterNodeStartFailure(
                composition,
                "node-transport-start-failed",
                "The Diameter transport failed during startup.");
        }

        NodeTransportStartResult transport = transportWork.Result;
        if (!transport.Succeeded)
        {
            if (transport.Listener != null)
            {
                CloseDiameterListener(transport.Listener);
            }

            return CompleteDiameterNodeStartFailure(
                composition,
                transport.Code,
                transport.Message);
        }

        if (composition.CreditControlModule != null)
        {
            IDiameterApplication module = composition.CreditControlModule;
            Task<DiameterApplicationRegistration> applicationWork =
                Task.Run(() => composition.ApplicationHost
                    .RegisterDiameterApplication(module));
            WaitForDiameterTask(applicationWork);
            if (applicationWork.IsFaulted || applicationWork.IsCanceled)
            {
                ObserveDiameterTaskFailure(applicationWork);
                if (transport.Listener != null)
                {
                    CloseDiameterListener(transport.Listener);
                }

                return CompleteDiameterNodeStartFailure(
                    composition,
                    "node-application-start-failed",
                    "A configured Diameter application failed during startup.");
            }
        }

        lock (LifecycleLock)
        {
            Listener = transport.Listener;
            StopRequested = false;
            State = NodeLifecycleState.Running;
            TransitionSignal.Set();
        }

        ReportDiameterListenerHealth(
            composition,
            DiameterHealthStatus.Healthy,
            transport.Listener == null ?
                "The listener is disabled." :
                "The listener is running.");
        composition.Health.UpdateDiameterNodeActivity(
            true,
            false,
            false);
        if (transport.Listener != null)
        {
            IDiameterListener workerListener = transport.Listener;
            Task worker = Task.Run(() =>
                RunDiameterListenerWorker(workerListener));
            lock (LifecycleLock)
            {
                ListenerWorker = worker;
            }

            worker.ContinueWith(completed =>
                CompleteDiameterListenerWorker(completed),
                TaskScheduler.Default);
        }

        return CreateNodeLifecycleResult(
            true,
            false,
            "node-running",
            "The Diameter node is running.");
    }

    internal NodeLifecycleResult DrainDiameterNode()
    {
        bool waitForTransition = false;
        NodeComposition? composition = null;
        IDiameterListener? listener = null;
        lock (LifecycleLock)
        {
            if (State == NodeLifecycleState.Starting ||
                State == NodeLifecycleState.Configuring)
            {
                waitForTransition = true;
            }
            else if (State == NodeLifecycleState.Draining)
            {
                waitForTransition = true;
            }
            else if (State == NodeLifecycleState.Configured ||
                State == NodeLifecycleState.Drained ||
                State == NodeLifecycleState.Stopped ||
                State == NodeLifecycleState.Unconfigured)
            {
                return CreateNodeLifecycleResult(
                    true,
                    true,
                    "node-not-running",
                    "The Diameter node has no running work to drain.");
            }
            else if (State != NodeLifecycleState.Running ||
                Composition == null)
            {
                return CreateNodeLifecycleResult(
                    false,
                    false,
                    "node-drain-unavailable",
                    "The Diameter node cannot begin draining now.");
            }
            else
            {
                State = NodeLifecycleState.Draining;
                StopRequested = true;
                TransitionSignal.Reset();
                composition = Composition;
                listener = Listener;
            }
        }

        if (waitForTransition)
        {
            TransitionSignal.Wait();
            lock (LifecycleLock)
            {
                if (State == NodeLifecycleState.Drained)
                {
                    return CreateNodeLifecycleResult(
                        true,
                        true,
                        "node-already-drained",
                        "The Diameter node is already drained.");
                }
            }

            return DrainDiameterNode();
        }

        if (composition == null)
        {
            return CreateNodeLifecycleResult(
                false,
                false,
                "node-drain-unavailable",
                "The Diameter node cannot begin draining now.");
        }

        composition.Health.UpdateDiameterNodeActivity(
            false,
            true,
            false);
        if (listener != null)
        {
            CloseDiameterListener(listener);
            lock (LifecycleLock)
            {
                if (object.ReferenceEquals(Listener, listener))
                {
                    Listener = null;
                }
            }
        }

        WaitForDiameterListenerWorker();
        MutationSignal.Wait();
        ApplicationShutdownResult shutdown =
            ShutdownDiameterApplications(composition);
        composition.Events.WaitForDiameterEvents(
            composition.Configuration.Timers.DrainTimeoutMilliseconds);
        lock (LifecycleLock)
        {
            ApplicationsShutdown = true;
            ForcedShutdown = ForcedShutdown ||
                shutdown.ForcedCount > 0;
            State = NodeLifecycleState.Drained;
            TransitionSignal.Set();
        }

        return CreateNodeLifecycleResult(
            true,
            false,
            "node-drained",
            "The Diameter node drained accepted work.");
    }

    internal NodeLifecycleResult StopDiameterNode()
    {
        bool waitForTransition = false;
        bool drainFirst = false;
        lock (LifecycleLock)
        {
            if (State == NodeLifecycleState.Stopped ||
                State == NodeLifecycleState.Unconfigured)
            {
                State = NodeLifecycleState.Stopped;
                return CreateNodeLifecycleResult(
                    true,
                    true,
                    "node-already-stopped",
                    "The Diameter node is already stopped.");
            }

            if (State == NodeLifecycleState.Configuring ||
                State == NodeLifecycleState.Starting ||
                State == NodeLifecycleState.Draining ||
                State == NodeLifecycleState.Stopping)
            {
                waitForTransition = true;
            }
            else if (State == NodeLifecycleState.Running)
            {
                drainFirst = true;
            }
        }

        if (waitForTransition)
        {
            TransitionSignal.Wait();
            return StopDiameterNode();
        }

        if (drainFirst)
        {
            DrainDiameterNode();
        }

        NodeComposition? composition;
        IDiameterListener? listener;
        lock (LifecycleLock)
        {
            if (State == NodeLifecycleState.Stopped)
            {
                return CreateNodeLifecycleResult(
                    true,
                    true,
                    "node-already-stopped",
                    "The Diameter node is already stopped.");
            }

            State = NodeLifecycleState.Stopping;
            StopRequested = true;
            TransitionSignal.Reset();
            composition = Composition;
            listener = Listener;
        }

        if (listener != null)
        {
            CloseDiameterListener(listener);
        }

        WaitForDiameterListenerWorker();
        MutationSignal.Wait();
        if (composition != null)
        {
            if (!ApplicationsShutdown)
            {
                ApplicationShutdownResult shutdown =
                    ShutdownDiameterApplications(composition);
                lock (LifecycleLock)
                {
                    ForcedShutdown = ForcedShutdown ||
                        shutdown.ForcedCount > 0;
                }
            }

            DisconnectDiameterPeers(composition);
            CloseDiameterConnections(composition);
            composition.Events.WaitForDiameterEvents(
                composition.Configuration.Timers.DrainTimeoutMilliseconds);
            StopDiameterStoreResources(composition);
            composition.Health.UpdateDiameterNodeActivity(
                false,
                false,
                true);
            ReportDiameterListenerHealth(
                composition,
                DiameterHealthStatus.Stopped,
                "The listener is stopped.");
        }

        lock (LifecycleLock)
        {
            Listener = null;
            ListenerWorker = null;
            AcceptedConnections.Clear();
            State = NodeLifecycleState.Stopped;
            TransitionSignal.Set();
        }

        return CreateNodeLifecycleResult(
            true,
            false,
            "node-stopped",
            "The Diameter node stopped and released its resources.");
    }

    internal NodeLifecycleState ReadDiameterNodeLifecycleState()
    {
        lock (LifecycleLock)
        {
            return State;
        }
    }

    internal NodeComposition ReadDiameterNodeComposition()
    {
        lock (LifecycleLock)
        {
            if (Composition == null)
            {
                throw new ArgumentException(
                    "The Diameter node is not configured.");
            }

            return Composition;
        }
    }

    internal DiameterPeerSnapshot AddDiameterNodePeer(
        DiameterPeerConfiguration? configuration)
    {
        NodeComposition composition = BeginDiameterNodeMutation();
        Task<DiameterPeerSnapshot> work = Task.Run(() =>
            composition.PeerManager.AddDiameterPeer(configuration));
        WaitForDiameterTask(work);
        CompleteDiameterNodeMutation();
        ThrowDiameterMutationFailure(
            work,
            "The Diameter peer change failed.");
        return work.Result;
    }

    internal bool RemoveDiameterNodePeer(DiameterIdentity? identity)
    {
        NodeComposition composition = BeginDiameterNodeMutation();
        Task<bool> work = Task.Run(() =>
            composition.PeerManager.RemoveDiameterPeer(identity));
        WaitForDiameterTask(work);
        CompleteDiameterNodeMutation();
        ThrowDiameterMutationFailure(
            work,
            "The Diameter peer removal failed.");
        return work.Result;
    }

    internal DiameterRouteSnapshot AddDiameterNodeRoute(
        DiameterRouteConfiguration? configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentException(
                "A Diameter route configuration is required.");
        }

        NodeComposition composition = BeginDiameterNodeMutation();
        DiameterRouter router = (DiameterRouter)composition.Router;
        Task<DiameterRouteSnapshot> work = Task.Run(() =>
            router.Table.AddOrReplaceStaticRoute(configuration));
        WaitForDiameterTask(work);
        CompleteDiameterNodeMutation();
        ThrowDiameterMutationFailure(
            work,
            "The Diameter route change failed.");
        return work.Result;
    }

    internal bool RemoveDiameterNodeRoute(string? routeIdentifier)
    {
        if (string.IsNullOrWhiteSpace(routeIdentifier))
        {
            throw new ArgumentException(
                "A route identifier is required.");
        }

        NodeComposition composition = BeginDiameterNodeMutation();
        DiameterRouter router = (DiameterRouter)composition.Router;
        string identifier = routeIdentifier;
        Task<bool> work = Task.Run(() =>
            router.Table.RemoveStaticRoute(identifier));
        WaitForDiameterTask(work);
        CompleteDiameterNodeMutation();
        ThrowDiameterMutationFailure(
            work,
            "The Diameter route removal failed.");
        return work.Result;
    }

    internal DiameterDictionaryRegistrationResult
        RegisterDiameterNodeDictionary(
            DiameterDictionaryModule? module)
    {
        NodeComposition composition = BeginDiameterNodeMutation();
        Task<DiameterDictionaryRegistrationResult> work = Task.Run(() =>
            composition.Dictionary.RegisterDictionaryModule(module));
        WaitForDiameterTask(work);
        CompleteDiameterNodeMutation();
        ThrowDiameterMutationFailure(
            work,
            "The Diameter dictionary change failed.");
        return work.Result;
    }

    internal DiameterApplicationRegistration RegisterDiameterNodeApplication(
        IDiameterApplication? application)
    {
        NodeComposition composition = BeginDiameterNodeMutation();
        Task<DiameterApplicationRegistration> work = Task.Run(() =>
            composition.ApplicationHost.RegisterDiameterApplication(
                application));
        WaitForDiameterTask(work);
        CompleteDiameterNodeMutation();
        ThrowDiameterMutationFailure(
            work,
            "The Diameter application registration failed.");
        return work.Result;
    }

    internal bool UnregisterDiameterNodeApplication(
        string? registrationName,
        bool drain)
    {
        NodeComposition composition = BeginDiameterNodeMutation();
        Task<bool> work = Task.Run(() =>
            composition.ApplicationHost.UnregisterDiameterApplication(
                registrationName,
                drain));
        WaitForDiameterTask(work);
        CompleteDiameterNodeMutation();
        ThrowDiameterMutationFailure(
            work,
            "The Diameter application removal failed.");
        return work.Result;
    }

    internal DiameterSendResult SendDiameterNodeRequest(
        DiameterSendRequest? request,
        IDiameterAnswerHandler? handler)
    {
        NodeComposition composition = BeginRunningDiameterNodeOperation();
        Task<DiameterSendResult> work = Task.Run(() =>
            composition.RequestClient.SendDiameterRequest(
                request,
                handler));
        WaitForDiameterTask(work);
        CompleteDiameterNodeMutation();
        ThrowDiameterMutationFailure(
            work,
            "The Diameter request send failed.");
        return work.Result;
    }

    internal NodeComposition BeginRunningDiameterNodeOperation()
    {
        lock (LifecycleLock)
        {
            if (State != NodeLifecycleState.Running ||
                Composition == null)
            {
                throw new ArgumentException(
                    "The Diameter node must be running before sending requests.");
            }

            ActiveMutations = ActiveMutations + 1;
            MutationSignal.Reset();
            return Composition;
        }
    }

    internal NodeComposition BeginDiameterNodeMutation()
    {
        lock (LifecycleLock)
        {
            if ((State != NodeLifecycleState.Configured &&
                State != NodeLifecycleState.Running) ||
                Composition == null)
            {
                throw new ArgumentException(
                    "Node changes are unavailable during lifecycle transitions.");
            }

            ActiveMutations = ActiveMutations + 1;
            MutationSignal.Reset();
            return Composition;
        }
    }

    internal void CompleteDiameterNodeMutation()
    {
        lock (LifecycleLock)
        {
            ActiveMutations = ActiveMutations - 1;
            if (ActiveMutations == 0)
            {
                MutationSignal.Set();
            }
        }
    }

    internal NodeTransportStartResult StartDiameterTransport(
        NodeComposition composition)
    {
        NodeTransportStartResult result =
            new NodeTransportStartResult();
        DiameterTransportSupportResult support =
            composition.TransportFactory.ProbeDiameterTransportSupport(
                composition.Configuration.Transport,
                composition.Configuration.Security);
        if (support == null ||
            support.Status != DiameterTransportStatus.Success)
        {
            result.Code = support == null ?
                "transport-probe-result-invalid" : support.Code;
            result.Message = support == null ?
                "The transport returned no support result." :
                support.Message;
            return result;
        }

        if (!composition.Configuration.Transport.EnableListener)
        {
            result.Succeeded = true;
            result.Code = "listener-disabled";
            return result;
        }

        DiameterListenerResult listener =
            composition.TransportFactory.CreateDiameterListener(
                composition.Configuration.Transport,
                composition.Configuration.Security);
        if (listener == null ||
            listener.Status != DiameterTransportStatus.Success ||
            listener.Listener == null)
        {
            result.Listener = listener == null ? null : listener.Listener;
            result.Code = listener == null ?
                "listener-result-invalid" : listener.Code;
            result.Message = listener == null ?
                "The transport returned no listener result." :
                listener.Message;
            return result;
        }

        result.Succeeded = true;
        result.Listener = listener.Listener;
        result.Code = "listener-created";
        return result;
    }

    internal void RunDiameterListenerWorker(IDiameterListener listener)
    {
        bool stopping = false;
        while (!stopping)
        {
            lock (LifecycleLock)
            {
                stopping = StopRequested;
            }

            if (!stopping)
            {
                DiameterConnectionResult accepted =
                    listener.AcceptDiameterConnection(250);
                if (accepted != null &&
                    accepted.Status == DiameterTransportStatus.Success &&
                    accepted.Connection != null)
                {
                    bool closeConnection = false;
                    lock (LifecycleLock)
                    {
                        if (StopRequested)
                        {
                            closeConnection = true;
                        }
                        else
                        {
                            AcceptedConnections.Add(accepted.Connection);
                        }
                    }

                    if (closeConnection)
                    {
                        CloseDiameterConnection(accepted.Connection);
                    }
                }
                else if (accepted != null &&
                    accepted.Status == DiameterTransportStatus.Closed)
                {
                    stopping = true;
                }
            }
        }
    }

    internal void CompleteDiameterListenerWorker(Task worker)
    {
        if (worker.IsFaulted)
        {
            ObserveDiameterTaskFailure(worker);
            NodeComposition? composition = null;
            lock (LifecycleLock)
            {
                composition = Composition;
            }

            if (composition != null)
            {
                ReportDiameterListenerHealth(
                    composition,
                    DiameterHealthStatus.Unhealthy,
                    "The listener worker failed.");
                DiameterOperationalEvent failure =
                    new DiameterOperationalEvent();
                failure.Source = "listener";
                failure.Category = "transport";
                failure.Code = "listener.worker.failed";
                failure.Description = "The listener worker failed.";
                failure.IssueSeverity = DiameterIssueSeverity.Error;
                composition.Health.RecordDiameterHealthFailure(failure);
            }
        }
    }

    internal void WaitForDiameterListenerWorker()
    {
        Task? worker;
        lock (LifecycleLock)
        {
            worker = ListenerWorker;
        }

        if (worker != null)
        {
            WaitForDiameterTask(worker);
            ObserveDiameterTaskFailure(worker);
        }
    }

    internal ApplicationShutdownResult ShutdownDiameterApplications(
        NodeComposition composition)
    {
        DiameterApplicationHost host =
            (DiameterApplicationHost)composition.ApplicationHost;
        Task<ApplicationShutdownResult> work = Task.Run(() =>
            host.Lifecycle.ShutdownApplicationModules());
        WaitForDiameterTask(work);
        if (work.IsFaulted || work.IsCanceled)
        {
            ObserveDiameterTaskFailure(work);
            ApplicationShutdownResult failure =
                new ApplicationShutdownResult();
            failure.ForcedCount = 1;
            failure.Code = "application-shutdown-failed";
            return failure;
        }

        return work.Result;
    }

    internal void DisconnectDiameterPeers(NodeComposition composition)
    {
        List<DiameterPeerSnapshot> peers =
            composition.PeerManager.ReadDiameterPeerSnapshots();
        int index = 0;
        while (index < peers.Count)
        {
            DiameterPeerSnapshot peer = peers[index];
            if (peer.State != DiameterPeerState.Closed ||
                peer.Connection != null)
            {
                DiameterDisconnectRequest request =
                    new DiameterDisconnectRequest();
                request.PeerIdentity.Value =
                    peer.Configuration.Identity.Value;
                request.Cause =
                    DiameterDisconnectCause.DoNotWantToTalkToYou;
                request.WaitForAnswer = false;
                request.SuppressReconnect = true;
                request.TimeoutMilliseconds =
                    composition.Configuration.Timers
                        .DrainTimeoutMilliseconds;
                Task<DiameterPeerSnapshot> work = Task.Run(() =>
                    composition.PeerManager.DisconnectDiameterPeer(
                        request));
                WaitForDiameterTask(work);
                ObserveDiameterTaskFailure(work);
            }

            index = index + 1;
        }
    }

    internal void CloseDiameterConnections(NodeComposition composition)
    {
        List<IDiameterConnection> connections =
            new List<IDiameterConnection>();
        lock (LifecycleLock)
        {
            int index = 0;
            while (index < AcceptedConnections.Count)
            {
                AddDiameterConnectionForShutdown(
                    connections,
                    AcceptedConnections[index]);
                index = index + 1;
            }
        }

        DiameterPeerManager manager =
            (DiameterPeerManager)composition.PeerManager;
        lock (manager.ConnectionLock)
        {
            foreach (IDiameterConnection connection in
                manager.Connections.Values)
            {
                AddDiameterConnectionForShutdown(
                    connections,
                    connection);
            }

            manager.Connections.Clear();
        }

        int connectionIndex = 0;
        while (connectionIndex < connections.Count)
        {
            CloseDiameterConnection(connections[connectionIndex]);
            connectionIndex = connectionIndex + 1;
        }
    }

    internal void AddDiameterConnectionForShutdown(
        List<IDiameterConnection> connections,
        IDiameterConnection connection)
    {
        int index = 0;
        while (index < connections.Count)
        {
            if (object.ReferenceEquals(connections[index], connection))
            {
                return;
            }

            index = index + 1;
        }

        connections.Add(connection);
    }

    internal void StopDiameterStoreResources(NodeComposition composition)
    {
        List<IDiameterNodeResource> resources =
            new List<IDiameterNodeResource>();
        AddDiameterNodeResource(
            resources,
            composition.TransportFactory as IDiameterNodeResource);
        DiameterExternalTransportFactory? external =
            composition.TransportFactory as DiameterExternalTransportFactory;
        if (external != null)
        {
            AddDiameterNodeResource(
                resources,
                external.OwnedFactory as IDiameterNodeResource);
        }
        AddDiameterNodeResource(
            resources,
            composition.SessionStore as IDiameterNodeResource);
        AddDiameterNodeResource(
            resources,
            composition.AccountingRecordStore as IDiameterNodeResource);
        AddDiameterNodeResource(
            resources,
            composition.CreditControlStore as IDiameterNodeResource);
        AddDiameterNodeResource(
            resources,
            composition.DuplicateRequestStore as IDiameterNodeResource);

        int index = 0;
        while (index < resources.Count)
        {
            IDiameterNodeResource resource = resources[index];
            Task work = Task.Run(() =>
                resource.StopDiameterNodeResource());
            WaitForDiameterTask(work);
            ObserveDiameterTaskFailure(work);
            index = index + 1;
        }
    }

    internal void AddDiameterNodeResource(
        List<IDiameterNodeResource> resources,
        IDiameterNodeResource? resource)
    {
        if (resource == null)
        {
            return;
        }

        int index = 0;
        while (index < resources.Count)
        {
            if (object.ReferenceEquals(resources[index], resource))
            {
                return;
            }

            index = index + 1;
        }

        resources.Add(resource);
    }

    internal void CloseDiameterListener(IDiameterListener listener)
    {
        Task<DiameterTransportOperationResult> work = Task.Run(() =>
            listener.CloseDiameterListener());
        WaitForDiameterTask(work);
        ObserveDiameterTaskFailure(work);
    }

    internal void CloseDiameterConnection(IDiameterConnection connection)
    {
        Task<DiameterTransportOperationResult> work = Task.Run(() =>
            connection.CloseDiameterConnection());
        WaitForDiameterTask(work);
        ObserveDiameterTaskFailure(work);
    }

    internal void WaitForDiameterTask(Task task)
    {
        if (task.IsCompleted)
        {
            return;
        }

        ManualResetEventSlim completed =
            new ManualResetEventSlim(false);
        task.ContinueWith(finished => completed.Set(),
            TaskScheduler.Default);
        completed.Wait();
    }

    internal void ObserveDiameterTaskFailure(Task task)
    {
        if (task.IsFaulted)
        {
            task.Exception?.GetBaseException();
        }
    }

    internal void ThrowDiameterMutationFailure(
        Task task,
        string message)
    {
        if (task.IsFaulted || task.IsCanceled)
        {
            ObserveDiameterTaskFailure(task);
            throw new ArgumentException(message);
        }
    }

    internal NodeLifecycleResult CompleteDiameterNodeStartFailure(
        NodeComposition composition,
        string code,
        string message)
    {
        ReportDiameterListenerHealth(
            composition,
            DiameterHealthStatus.Unhealthy,
            message);
        DiameterOperationalEvent failure =
            new DiameterOperationalEvent();
        failure.Source = "node-lifecycle";
        failure.Category = "lifecycle";
        failure.Code = code;
        failure.Description = message;
        failure.IssueSeverity = DiameterIssueSeverity.Error;
        composition.Health.RecordDiameterHealthFailure(failure);
        lock (LifecycleLock)
        {
            Listener = null;
            State = NodeLifecycleState.Configured;
            TransitionSignal.Set();
        }

        return CreateNodeLifecycleResult(
            false,
            false,
            code,
            message);
    }

    internal void ReportDiameterListenerHealth(
        NodeComposition composition,
        DiameterHealthStatus status,
        string detail)
    {
        DiameterSubsystemHealth health =
            new DiameterSubsystemHealth();
        health.Name = HealthCollector.ListenersSubsystem;
        health.Status = status;
        health.Detail = detail;
        health.Required =
            composition.Configuration.Transport.EnableListener;
        composition.Health.ReportDiameterSubsystemHealth(health);
    }

    internal NodeLifecycleResult CreateNodeLifecycleResult(
        bool succeeded,
        bool repeated,
        string code,
        string message)
    {
        NodeLifecycleResult result = new NodeLifecycleResult();
        result.State = State;
        result.Succeeded = succeeded;
        result.Repeated = repeated;
        result.Forced = ForcedShutdown;
        result.Code = code;
        result.Message = message;
        return result;
    }
}
