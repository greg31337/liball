namespace uspto;

/// <summary>
/// Creates configured USPTO client suites whose components share transport and policies.
/// </summary>
public class UsptoClientFactory : IUsptoClientFactory
{
    /// <summary>Initializes a USPTO client-suite factory.</summary>
    public UsptoClientFactory()
    {
    }

    /// <summary>Creates a configured suite from caller-provided options.</summary>
    /// <param name="options">The endpoints, credentials, transport, policies, and limits.</param>
    /// <returns>The configured client suite and its resource lifetime.</returns>
    public UsptoClients CreateUsptoClients(UsptoClientOptions options)
    {
        return ConfiguredClientSuiteBuilder.CreateConfiguredClients(options);
    }
}
