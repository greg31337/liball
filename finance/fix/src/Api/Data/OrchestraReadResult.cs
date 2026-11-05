using System.Collections.Generic;

namespace fix;

/// <summary>Contains the explicit outcome of an Orchestra operation.</summary>
public class OrchestraReadResult
{
    /// <summary>Whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>The resulting repository when available.</summary>
    public OrchestraRepository? Repository;
    /// <summary>The ordered diagnostics.</summary>
    public List<OrchestraDiagnostic> Diagnostics = [];
}
