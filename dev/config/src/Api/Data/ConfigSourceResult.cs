namespace config;

/// <summary>Contains the value tree and diagnostics produced by one configuration source.</summary>
public class ConfigSourceResult
{
    /// <summary>Contains the loaded root value.</summary>
    public ConfigValue Root;
    /// <summary>Contains load and parse diagnostics for the source.</summary>
    public ConfigDiagnostics Diagnostics;
    /// <summary>Optionally groups the source in merge traces.</summary>
    public string SourceGroup;

    /// <summary>Creates a source result from a root value and diagnostics.</summary>
    /// <param name="root">The loaded root value.</param>
    /// <param name="diagnostics">The diagnostics produced while loading.</param>
    public ConfigSourceResult(ConfigValue root, ConfigDiagnostics diagnostics)
    {
        Root = root;
        Diagnostics = diagnostics;
        SourceGroup = "";
    }
}
