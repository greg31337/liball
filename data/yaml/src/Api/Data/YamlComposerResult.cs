using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Contains the stream and diagnostics produced while composing parser events.
/// </summary>
public class YamlComposerResult
{
    /// <summary>
    /// Whether the operation completed without errors.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The composed YAML stream; it can contain partial data when diagnostics include errors.
    /// </summary>
    public YamlStream Stream;
    /// <summary>
    /// The diagnostics in the order they were reported.
    /// </summary>
    public List<YamlDiagnostic> Diagnostics;

    /// <summary>
    /// Initializes a new YAML composer result with empty or default state.
    /// </summary>
    public YamlComposerResult()
    {
        Success = true;
        Stream = new YamlStream();
        Diagnostics = new List<YamlDiagnostic>();
    }

    internal void AddDiagnostic(YamlDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            return;
        }

        Diagnostics.Add(diagnostic);
        Stream.AddDiagnostic(diagnostic);
        if (diagnostic.IsError())
        {
            Success = false;
        }
    }
}
