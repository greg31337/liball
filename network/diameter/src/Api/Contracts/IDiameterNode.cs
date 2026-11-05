namespace diameter;

/// <summary>
/// Provides one high-level facade for Diameter node lifecycle and operations.
/// </summary>
public interface IDiameterNode
{
    /// <summary>Validates and owns one complete node configuration.</summary>
    /// <param name="configuration">The node configuration to compose.</param>
    /// <returns>The configuration transition outcome.</returns>
    DiameterNodeLifecycleResult ConfigureDiameterNode(
        DiameterNodeConfiguration? configuration);

    /// <summary>Starts configured transports and applications.</summary>
    /// <returns>The start transition outcome.</returns>
    DiameterNodeLifecycleResult StartDiameterNode();

    /// <summary>Stops accepting new work and finishes accepted work.</summary>
    /// <returns>The drain transition outcome.</returns>
    DiameterNodeLifecycleResult DrainDiameterNode();

    /// <summary>Stops the node and releases owned runtime resources.</summary>
    /// <returns>The stop transition outcome.</returns>
    DiameterNodeLifecycleResult StopDiameterNode();

    /// <summary>Reads the current node lifecycle state.</summary>
    /// <returns>The current lifecycle state.</returns>
    DiameterNodeState ReadDiameterNodeState();

    /// <summary>Sends one request through the composed request pipeline.</summary>
    /// <param name="request">The outbound request and delivery policy.</param>
    /// <param name="handler">The handler for terminal and requested late outcomes.</param>
    /// <returns>The immediate send-admission outcome.</returns>
    DiameterSendResult SendDiameterRequest(
        DiameterSendRequest? request,
        IDiameterAnswerHandler? handler);

    /// <summary>Adds or replaces one peer while changes are accepted.</summary>
    /// <param name="configuration">The peer configuration to own.</param>
    /// <returns>The peer snapshot after the change.</returns>
    DiameterPeerSnapshot AddDiameterPeer(
        DiameterPeerConfiguration? configuration);

    /// <summary>Removes one disconnected peer while changes are accepted.</summary>
    /// <param name="identity">The peer identity to remove.</param>
    /// <returns><c>true</c> when the peer was removed.</returns>
    bool RemoveDiameterPeer(DiameterIdentity? identity);

    /// <summary>Adds or replaces one static route while changes are accepted.</summary>
    /// <param name="configuration">The route configuration to own.</param>
    /// <returns>The route snapshot after the change.</returns>
    DiameterRouteSnapshot AddDiameterRoute(
        DiameterRouteConfiguration? configuration);

    /// <summary>Removes one static route while changes are accepted.</summary>
    /// <param name="routeIdentifier">The stable route identifier.</param>
    /// <returns><c>true</c> when the route was removed.</returns>
    bool RemoveDiameterRoute(string? routeIdentifier);

    /// <summary>Registers one dictionary module while changes are accepted.</summary>
    /// <param name="module">The dictionary module to compose.</param>
    /// <returns>The dictionary registration outcome.</returns>
    DiameterDictionaryRegistrationResult RegisterDiameterDictionary(
        DiameterDictionaryModule? module);

    /// <summary>Registers and starts one application module.</summary>
    /// <param name="application">The application module to register.</param>
    /// <returns>The owned active registration.</returns>
    DiameterApplicationRegistration RegisterDiameterApplication(
        IDiameterApplication? application);

    /// <summary>Removes and stops one application module.</summary>
    /// <param name="registrationName">The stable registration name.</param>
    /// <param name="drain">Allows accepted application work to drain.</param>
    /// <returns><c>true</c> when the application was removed.</returns>
    bool UnregisterDiameterApplication(
        string? registrationName,
        bool drain);

    /// <summary>Reads a point-in-time statistics snapshot.</summary>
    /// <returns>The owned statistics snapshot.</returns>
    DiameterNodeStatistics ReadDiameterNodeStatistics();

    /// <summary>Reads aggregate and subsystem node health.</summary>
    /// <returns>The owned health snapshot.</returns>
    DiameterNodeHealth ReadDiameterNodeHealth();

    /// <summary>Accesses the composed message codec.</summary>
    /// <returns>The message codec interface.</returns>
    IDiameterMessageCodec ReadDiameterMessageCodec();

    /// <summary>Accesses the composed dictionary.</summary>
    /// <returns>The dictionary interface.</returns>
    IDiameterDictionary ReadDiameterDictionary();

    /// <summary>Accesses the composed validator.</summary>
    /// <returns>The validator interface.</returns>
    IDiameterValidator ReadDiameterValidator();

    /// <summary>Accesses the composed peer manager.</summary>
    /// <returns>The peer manager interface.</returns>
    IDiameterPeerManager ReadDiameterPeerManager();

    /// <summary>Accesses the composed router.</summary>
    /// <returns>The router interface.</returns>
    IDiameterRouter ReadDiameterRouter();

    /// <summary>Accesses the composed request client.</summary>
    /// <returns>The request client interface.</returns>
    IDiameterRequestClient ReadDiameterRequestClient();

    /// <summary>Accesses the composed session manager.</summary>
    /// <returns>The session manager interface.</returns>
    IDiameterSessionManager ReadDiameterSessionManager();

    /// <summary>Accesses the composed accounting client.</summary>
    /// <returns>The accounting client interface.</returns>
    IDiameterAccountingClient ReadDiameterAccountingClient();

    /// <summary>Accesses the optional composed credit-control client.</summary>
    /// <returns>The credit-control client, or <c>null</c> when disabled.</returns>
    IDiameterCreditControlClient? ReadDiameterCreditControlClient();

    /// <summary>Accesses the composed application host.</summary>
    /// <returns>The application host interface.</returns>
    IDiameterApplicationHost ReadDiameterApplicationHost();

    /// <summary>Accesses the optional composed overload manager.</summary>
    /// <returns>The overload manager, or <c>null</c> when disabled.</returns>
    IDiameterOverloadManager? ReadDiameterOverloadManager();
}
