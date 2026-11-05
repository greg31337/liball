namespace config;

/// <summary>Creates configuration snapshots from common inputs in one call.</summary>
/// <remarks>Each operation uses a fresh builder with the same conservative defaults as <see cref="ConfigBuilder"/>.</remarks>
public class ConfigLoader : IConfigLoader
{
    /// <summary>Creates a stateless configuration loader.</summary>
    public ConfigLoader()
    {
    }

    /// <summary>Loads optional appsettings files and dotenv from the current directory, followed by process environment and command line.</summary>
    /// <param name="environmentName">The environment suffix used in <c>appsettings.Environment.json</c>, or an empty string to omit that file.</param>
    /// <param name="args">The command-line tokens applied last.</param>
    /// <returns>A new immutable configuration snapshot.</returns>
    public Config LoadDefaultApplication(string environmentName, string[] args)
    {
        return new ConfigBuilder().AddDefaultAppSources(environmentName, args).Build();
    }

    /// <summary>Loads optional appsettings files and dotenv from a selected directory, followed by process environment and command line.</summary>
    /// <param name="directory">The directory containing appsettings files and <c>.env</c>.</param>
    /// <param name="environmentName">The environment suffix, or an empty string to omit the environment-specific file.</param>
    /// <param name="args">The command-line tokens applied last.</param>
    /// <returns>A new immutable configuration snapshot.</returns>
    public Config LoadDefaultApplicationFromDirectory(string directory, string environmentName, string[] args)
    {
        return new ConfigBuilder().AddDefaultAppSourcesFromDirectory(directory, environmentName, args).Build();
    }

    /// <summary>Loads JSON text using the default parser and merge policies.</summary>
    /// <param name="text">The complete JSON source text.</param>
    /// <returns>A new immutable configuration snapshot.</returns>
    public Config LoadJson(string text)
    {
        return new ConfigBuilder().AddString(text, ConfigFormat.Json, "json").Build();
    }

    /// <summary>Loads dotenv text using the default parser and expansion policies.</summary>
    /// <param name="text">The complete dotenv source text.</param>
    /// <returns>A new immutable configuration snapshot.</returns>
    public Config LoadDotEnv(string text)
    {
        return new ConfigBuilder().AddString(text, ConfigFormat.DotEnv, "dotenv").Build();
    }

    /// <summary>Loads all process environment variables, mapping double underscores to path delimiters.</summary>
    /// <returns>A new immutable configuration snapshot.</returns>
    public Config LoadEnvironment()
    {
        return new ConfigBuilder().AddEnvironment().Build();
    }

    /// <summary>Loads command-line values using long, short, or slash switch forms.</summary>
    /// <param name="args">The argument tokens to parse.</param>
    /// <returns>A new immutable configuration snapshot.</returns>
    public Config LoadCommandLine(string[] args)
    {
        return new ConfigBuilder().AddCommandLine(args).Build();
    }
}
