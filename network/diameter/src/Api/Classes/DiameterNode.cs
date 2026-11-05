namespace diameter;

/// <summary>
/// Composes and operates a complete Diameter node through one high-level facade.
/// </summary>
public class DiameterNode : IDiameterNode
{
    internal NodeLifecycle Lifecycle = new NodeLifecycle();

    /// <summary>Initializes an unconfigured Diameter node.</summary>
    public DiameterNode()
    {
    }

    /// <summary>Validates and owns one complete node configuration.</summary>
    /// <param name="configuration">The node configuration to compose.</param>
    /// <returns>The configuration transition outcome.</returns>
    public DiameterNodeLifecycleResult ConfigureDiameterNode(
        DiameterNodeConfiguration? configuration)
    {
        return CreateDiameterNodeLifecycleResult(
            Lifecycle.ConfigureDiameterNode(configuration));
    }

    /// <summary>Starts configured transports and applications.</summary>
    /// <returns>The start transition outcome.</returns>
    public DiameterNodeLifecycleResult StartDiameterNode()
    {
        return CreateDiameterNodeLifecycleResult(
            Lifecycle.StartDiameterNode());
    }

    /// <summary>Stops accepting new work and finishes accepted work.</summary>
    /// <returns>The drain transition outcome.</returns>
    public DiameterNodeLifecycleResult DrainDiameterNode()
    {
        return CreateDiameterNodeLifecycleResult(
            Lifecycle.DrainDiameterNode());
    }

    /// <summary>Stops the node and releases owned runtime resources.</summary>
    /// <returns>The stop transition outcome.</returns>
    public DiameterNodeLifecycleResult StopDiameterNode()
    {
        return CreateDiameterNodeLifecycleResult(
            Lifecycle.StopDiameterNode());
    }

    /// <summary>Reads the current node lifecycle state.</summary>
    /// <returns>The current lifecycle state.</returns>
    public DiameterNodeState ReadDiameterNodeState()
    {
        return (DiameterNodeState)Lifecycle
            .ReadDiameterNodeLifecycleState();
    }

    /// <summary>Sends one request through the composed request pipeline.</summary>
    /// <param name="request">The outbound request and delivery policy.</param>
    /// <param name="handler">The handler for terminal and requested late outcomes.</param>
    /// <returns>The immediate send-admission outcome.</returns>
    public DiameterSendResult SendDiameterRequest(
        DiameterSendRequest? request,
        IDiameterAnswerHandler? handler)
    {
        return Lifecycle.SendDiameterNodeRequest(request, handler);
    }

    /// <summary>Adds or replaces one peer while changes are accepted.</summary>
    /// <param name="configuration">The peer configuration to own.</param>
    /// <returns>The peer snapshot after the change.</returns>
    public DiameterPeerSnapshot AddDiameterPeer(
        DiameterPeerConfiguration? configuration)
    {
        return Lifecycle.AddDiameterNodePeer(configuration);
    }

    /// <summary>Removes one disconnected peer while changes are accepted.</summary>
    /// <param name="identity">The peer identity to remove.</param>
    /// <returns><c>true</c> when the peer was removed.</returns>
    public bool RemoveDiameterPeer(DiameterIdentity? identity)
    {
        return Lifecycle.RemoveDiameterNodePeer(identity);
    }

    /// <summary>Adds or replaces one static route while changes are accepted.</summary>
    /// <param name="configuration">The route configuration to own.</param>
    /// <returns>The route snapshot after the change.</returns>
    public DiameterRouteSnapshot AddDiameterRoute(
        DiameterRouteConfiguration? configuration)
    {
        return Lifecycle.AddDiameterNodeRoute(configuration);
    }

    /// <summary>Removes one static route while changes are accepted.</summary>
    /// <param name="routeIdentifier">The stable route identifier.</param>
    /// <returns><c>true</c> when the route was removed.</returns>
    public bool RemoveDiameterRoute(string? routeIdentifier)
    {
        return Lifecycle.RemoveDiameterNodeRoute(routeIdentifier);
    }

    /// <summary>Registers one dictionary module while changes are accepted.</summary>
    /// <param name="module">The dictionary module to compose.</param>
    /// <returns>The dictionary registration outcome.</returns>
    public DiameterDictionaryRegistrationResult RegisterDiameterDictionary(
        DiameterDictionaryModule? module)
    {
        return Lifecycle.RegisterDiameterNodeDictionary(module);
    }

    /// <summary>Registers and starts one application module.</summary>
    /// <param name="application">The application module to register.</param>
    /// <returns>The owned active registration.</returns>
    public DiameterApplicationRegistration RegisterDiameterApplication(
        IDiameterApplication? application)
    {
        return Lifecycle.RegisterDiameterNodeApplication(application);
    }

    /// <summary>Removes and stops one application module.</summary>
    /// <param name="registrationName">The stable registration name.</param>
    /// <param name="drain">Allows accepted application work to drain.</param>
    /// <returns><c>true</c> when the application was removed.</returns>
    public bool UnregisterDiameterApplication(
        string? registrationName,
        bool drain)
    {
        return Lifecycle.UnregisterDiameterNodeApplication(
            registrationName,
            drain);
    }

    /// <summary>Reads a point-in-time statistics snapshot.</summary>
    /// <returns>The owned statistics snapshot.</returns>
    public DiameterNodeStatistics ReadDiameterNodeStatistics()
    {
        return Lifecycle.ReadDiameterNodeComposition()
            .Statistics.ReadDiameterNodeStatistics();
    }

    /// <summary>Reads aggregate and subsystem node health.</summary>
    /// <returns>The owned health snapshot.</returns>
    public DiameterNodeHealth ReadDiameterNodeHealth()
    {
        return Lifecycle.ReadDiameterNodeComposition()
            .Health.ReadDiameterNodeHealth();
    }

    /// <summary>Accesses the composed message codec.</summary>
    /// <returns>The message codec interface.</returns>
    public IDiameterMessageCodec ReadDiameterMessageCodec()
    {
        return Lifecycle.ReadDiameterNodeComposition().MessageCodec;
    }

    /// <summary>Accesses the composed dictionary.</summary>
    /// <returns>The dictionary interface.</returns>
    public IDiameterDictionary ReadDiameterDictionary()
    {
        return Lifecycle.ReadDiameterNodeComposition().Dictionary;
    }

    /// <summary>Accesses the composed validator.</summary>
    /// <returns>The validator interface.</returns>
    public IDiameterValidator ReadDiameterValidator()
    {
        return Lifecycle.ReadDiameterNodeComposition().Validator;
    }

    /// <summary>Accesses the composed peer manager.</summary>
    /// <returns>The peer manager interface.</returns>
    public IDiameterPeerManager ReadDiameterPeerManager()
    {
        return Lifecycle.ReadDiameterNodeComposition().PeerManager;
    }

    /// <summary>Accesses the composed router.</summary>
    /// <returns>The router interface.</returns>
    public IDiameterRouter ReadDiameterRouter()
    {
        return Lifecycle.ReadDiameterNodeComposition().Router;
    }

    /// <summary>Accesses the composed request client.</summary>
    /// <returns>The request client interface.</returns>
    public IDiameterRequestClient ReadDiameterRequestClient()
    {
        return Lifecycle.ReadDiameterNodeComposition().RequestClient;
    }

    /// <summary>Accesses the composed session manager.</summary>
    /// <returns>The session manager interface.</returns>
    public IDiameterSessionManager ReadDiameterSessionManager()
    {
        return Lifecycle.ReadDiameterNodeComposition().SessionManager;
    }

    /// <summary>Accesses the composed accounting client.</summary>
    /// <returns>The accounting client interface.</returns>
    public IDiameterAccountingClient ReadDiameterAccountingClient()
    {
        return Lifecycle.ReadDiameterNodeComposition().AccountingClient;
    }

    /// <summary>Accesses the optional composed credit-control client.</summary>
    /// <returns>The credit-control client, or <c>null</c> when disabled.</returns>
    public IDiameterCreditControlClient? ReadDiameterCreditControlClient()
    {
        return Lifecycle.ReadDiameterNodeComposition().CreditControlClient;
    }

    /// <summary>Accesses the composed application host.</summary>
    /// <returns>The application host interface.</returns>
    public IDiameterApplicationHost ReadDiameterApplicationHost()
    {
        return Lifecycle.ReadDiameterNodeComposition().ApplicationHost;
    }

    /// <summary>Accesses the optional composed overload manager.</summary>
    /// <returns>The overload manager, or <c>null</c> when disabled.</returns>
    public IDiameterOverloadManager? ReadDiameterOverloadManager()
    {
        return Lifecycle.ReadDiameterNodeComposition().OverloadManager;
    }

    internal DiameterNodeLifecycleResult CreateDiameterNodeLifecycleResult(
        NodeLifecycleResult source)
    {
        DiameterNodeLifecycleResult result =
            new DiameterNodeLifecycleResult();
        result.State = (DiameterNodeState)source.State;
        result.Succeeded = source.Succeeded;
        result.Repeated = source.Repeated;
        result.Forced = source.Forced;
        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }
}
