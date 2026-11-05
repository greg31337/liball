using System.Collections.Generic;

namespace args;

/// <summary>
/// Defines command-line completion configuration and generation operations.
/// </summary>
public interface IArgCompletionProvider
{
    /// <summary>Replaces the parser settings.</summary>
    ArgCompletionProvider UseSettings(ArgParserSettings settings);
    /// <summary>Returns the active parser settings.</summary>
    ArgParserSettings GetSettings();
    /// <summary>Generates candidates for a partial command line.</summary>
    List<ArgCompletion> Complete(ArgCommand command, string[] argv);
    /// <summary>Generates a completion script for the selected shell.</summary>
    string GenerateScript(ArgCommand command, ArgCompletionShell shell);
}
