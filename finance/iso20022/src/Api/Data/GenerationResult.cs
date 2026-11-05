using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains the exception-less generation outcome, files, manifest, diagnostics, and error detail.</summary>
public class GenerationResult
{
    /// <summary>Whether generation completed successfully.</summary>
    public bool Success;
    /// <summary>The exact plan evaluated by the generator.</summary>
    public GenerationPlan Plan = new GenerationPlan();
    /// <summary>The generated files in deterministic output-path order.</summary>
    public List<GeneratedFile> Files = new List<GeneratedFile>();
    /// <summary>The deterministic ownership, provenance, symbol, and hash manifest.</summary>
    public GenerationManifest Manifest = new GenerationManifest();
    /// <summary>Stable ordered diagnostic codes produced during planning or generation.</summary>
    public List<string> DiagnosticCodes = new List<string>();
    /// <summary>Human-readable diagnostic messages corresponding to the diagnostic codes.</summary>
    public List<string> DiagnosticMessages = new List<string>();
    /// <summary>Source locations corresponding to diagnostics when available.</summary>
    public List<SourceLocation> DiagnosticLocations = new List<SourceLocation>();
    /// <summary>Source identifiers corresponding to diagnostics when available.</summary>
    public List<string> DiagnosticSourceIdentifiers = new List<string>();
    /// <summary>A stable error code when generation did not succeed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when generation did not succeed.</summary>
    public string ErrorMessage = string.Empty;
}
