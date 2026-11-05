namespace config;

/// <summary>Defines one-call loading for common configuration inputs.</summary>
public interface IConfigLoader
{
    /// <summary>Loads the conventional application source stack from the current directory.</summary>
    Config LoadDefaultApplication(string environmentName, string[] args);
    /// <summary>Loads the conventional application source stack from a selected directory.</summary>
    Config LoadDefaultApplicationFromDirectory(string directory, string environmentName, string[] args);
    /// <summary>Loads a snapshot from JSON text.</summary>
    Config LoadJson(string text);
    /// <summary>Loads a snapshot from dotenv text.</summary>
    Config LoadDotEnv(string text);
    /// <summary>Loads a snapshot from the process environment.</summary>
    Config LoadEnvironment();
    /// <summary>Loads a snapshot from command-line arguments.</summary>
    Config LoadCommandLine(string[] args);
}
