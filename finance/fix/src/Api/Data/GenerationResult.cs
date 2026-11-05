using System.Collections.Generic;

namespace fix;

/// <summary>Contains generated files, diagnostics, and a deterministic manifest.</summary>
public class GenerationResult
{
    /// <summary>Whether generation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>The generated files in deterministic path order.</summary>
    public List<GeneratedFile> Files = [];
    /// <summary>The ordered generation diagnostics.</summary>
    public List<GenerationDiagnostic> Diagnostics = [];
    /// <summary>The deterministic output manifest.</summary>
    public string Manifest = "";
}
