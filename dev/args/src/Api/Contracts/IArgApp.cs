namespace args;

/// <summary>
/// Defines command-line application configuration, parsing, validation, and invocation operations.
/// </summary>
public interface IArgApp
{
    /// <summary>Replaces the parser settings.</summary>
    ArgApp UseSettings(ArgParserSettings settings);
    /// <summary>Returns the application name.</summary>
    string GetName();
    /// <summary>Returns the application description.</summary>
    string GetDescriptionText();
    /// <summary>Returns the application version.</summary>
    string GetVersionText();
    /// <summary>Returns the dynamic version text provider.</summary>
    System.Func<string>? GetVersionProvider();
    /// <summary>Returns the root command.</summary>
    ArgCommand GetRootCommand();
    /// <summary>Returns the active parser settings.</summary>
    ArgParserSettings GetSettings();
    /// <summary>Sets the application description.</summary>
    ArgApp Description(string text);
    /// <summary>Sets the application version.</summary>
    ArgApp Version(string text);
    /// <summary>Sets the dynamic application version text provider.</summary>
    ArgApp VersionProvider(System.Func<string> provider);
    /// <summary>Adds a root child command.</summary>
    ArgCommand Command(string name);
    /// <summary>Adds a root-local option.</summary>
    ArgOption Option(string name);
    /// <summary>Adds a root positional argument.</summary>
    ArgArgument Argument(string name);
    /// <summary>Parses an argument array.</summary>
    ArgParseResult Parse(string[] argv);
    /// <summary>Parses and validates an argument array.</summary>
    ArgValidationResult Validate(string[] argv);
    /// <summary>Parses, validates, and invokes an argument array.</summary>
    ArgRunResult Run(string[] argv);
}
