namespace config;

/// <summary>Defines optional labels applied to conventional configuration source groups.</summary>
public class ConfigSourceGroupLabels
{
    /// <summary>Labels in-memory default sources.</summary>
    public string Defaults;
    /// <summary>Labels file, file-search, directory, and key-per-file sources.</summary>
    public string Files;
    /// <summary>Labels process and supplied environment sources.</summary>
    public string Environment;
    /// <summary>Labels command-line sources.</summary>
    public string CommandLine;
    /// <summary>Labels programmatic override sources.</summary>
    public string Overrides;

    /// <summary>Creates a set of source-group labels.</summary>
    /// <param name="defaults">The defaults label, or an empty string to omit it.</param>
    /// <param name="files">The files label, or an empty string to omit it.</param>
    /// <param name="environment">The environment label, or an empty string to omit it.</param>
    /// <param name="commandLine">The command-line label, or an empty string to omit it.</param>
    /// <param name="overrides">The overrides label, or an empty string to omit it.</param>
    public ConfigSourceGroupLabels(string defaults, string files, string environment, string commandLine, string overrides)
    {
        Defaults = defaults;
        Files = files;
        Environment = environment;
        CommandLine = commandLine;
        Overrides = overrides;
    }

    internal ConfigSourceGroupLabels Copy()
    {
        return new ConfigSourceGroupLabels(Defaults, Files, Environment, CommandLine, Overrides);
    }
}
