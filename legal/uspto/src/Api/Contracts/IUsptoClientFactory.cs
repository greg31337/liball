namespace uspto;

/// <summary>
/// Creates a configured suite of USPTO clients that share transport and policies.
/// </summary>
public interface IUsptoClientFactory
{
    /// <summary>Creates a client suite from caller-provided options.</summary>
    /// <param name="options">The endpoints, credentials, transport, policies, and safety limits.</param>
    /// <returns>The configured client suite.</returns>
    UsptoClients CreateUsptoClients(UsptoClientOptions options);
}
