namespace args;

/// <summary>
/// Defines version, help, and usage generation operations with optional ANSI styling.
/// </summary>
public interface IArgHelpBuilder
{
    /// <summary>Replaces the parser settings.</summary>
    ArgHelpBuilder UseSettings(ArgParserSettings settings);
    /// <summary>Sets the target help width.</summary>
    ArgHelpBuilder Width(int width);
    /// <summary>Returns the target help width.</summary>
    int GetHelpWidth();
    /// <summary>Returns the active parser settings.</summary>
    ArgParserSettings GetSettings();
    /// <summary>Returns application version text.</summary>
    string Version(ArgApp app);
    /// <summary>Returns command-root version text.</summary>
    string CommandVersion(ArgCommand command);
    /// <summary>Builds application help.</summary>
    string RootHelp(ArgApp app);
    /// <summary>Builds command help.</summary>
    string Help(ArgCommand command);
    /// <summary>Builds a command usage line.</summary>
    string UsageLine(ArgCommand command);
}
