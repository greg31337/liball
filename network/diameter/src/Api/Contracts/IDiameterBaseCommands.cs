namespace diameter;

/// <summary>
/// Constructs the request and answer forms of RFC 6733 base commands.
/// </summary>
public interface IDiameterBaseCommandFactory
{
    /// <summary>Creates a Capabilities-Exchange request.</summary>
    /// <param name="options">The local capabilities and origin values.</param>
    /// <returns>The constructed request.</returns>
    DiameterMessage CreateCapabilitiesExchangeRequest(DiameterBaseCommandOptions? options);

    /// <summary>Creates a Capabilities-Exchange answer.</summary>
    /// <param name="options">The source request, result, and local capabilities.</param>
    /// <returns>The constructed answer.</returns>
    DiameterMessage CreateCapabilitiesExchangeAnswer(DiameterBaseCommandOptions? options);

    /// <summary>Creates a Device-Watchdog request.</summary>
    /// <param name="options">The local origin values.</param>
    /// <returns>The constructed request.</returns>
    DiameterMessage CreateDeviceWatchdogRequest(DiameterBaseCommandOptions? options);

    /// <summary>Creates a Device-Watchdog answer.</summary>
    /// <param name="options">The source request, result, and local origin values.</param>
    /// <returns>The constructed answer.</returns>
    DiameterMessage CreateDeviceWatchdogAnswer(DiameterBaseCommandOptions? options);

    /// <summary>Creates a Disconnect-Peer request.</summary>
    /// <param name="options">The local origin values and disconnect cause.</param>
    /// <returns>The constructed request.</returns>
    DiameterMessage CreateDisconnectPeerRequest(DiameterBaseCommandOptions? options);

    /// <summary>Creates a Disconnect-Peer answer.</summary>
    /// <param name="options">The source request, result, and local origin values.</param>
    /// <returns>The constructed answer.</returns>
    DiameterMessage CreateDisconnectPeerAnswer(DiameterBaseCommandOptions? options);

    /// <summary>Creates a Re-Auth request.</summary>
    /// <param name="options">The session, destination, application, and origin values.</param>
    /// <returns>The constructed request.</returns>
    DiameterMessage CreateReAuthRequest(DiameterBaseCommandOptions? options);

    /// <summary>Creates a Re-Auth answer.</summary>
    /// <param name="options">The source request, result, and local origin values.</param>
    /// <returns>The constructed answer.</returns>
    DiameterMessage CreateReAuthAnswer(DiameterBaseCommandOptions? options);

    /// <summary>Creates an Abort-Session request.</summary>
    /// <param name="options">The session, destination, application, and origin values.</param>
    /// <returns>The constructed request.</returns>
    DiameterMessage CreateAbortSessionRequest(DiameterBaseCommandOptions? options);

    /// <summary>Creates an Abort-Session answer.</summary>
    /// <param name="options">The source request, result, and local origin values.</param>
    /// <returns>The constructed answer.</returns>
    DiameterMessage CreateAbortSessionAnswer(DiameterBaseCommandOptions? options);

    /// <summary>Creates a Session-Termination request.</summary>
    /// <param name="options">The session, destination, application, termination, and origin values.</param>
    /// <returns>The constructed request.</returns>
    DiameterMessage CreateSessionTerminationRequest(DiameterBaseCommandOptions? options);

    /// <summary>Creates a Session-Termination answer.</summary>
    /// <param name="options">The source request, result, and local origin values.</param>
    /// <returns>The constructed answer.</returns>
    DiameterMessage CreateSessionTerminationAnswer(DiameterBaseCommandOptions? options);

    /// <summary>Creates an Accounting request.</summary>
    /// <param name="options">The session, destination, record, and origin values.</param>
    /// <returns>The constructed request.</returns>
    DiameterMessage CreateAccountingRequest(DiameterBaseCommandOptions? options);

    /// <summary>Creates an Accounting answer.</summary>
    /// <param name="options">The source request, result, and local origin values.</param>
    /// <returns>The constructed answer.</returns>
    DiameterMessage CreateAccountingAnswer(DiameterBaseCommandOptions? options);
}
