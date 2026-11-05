using System.Collections.Generic;

namespace fix;

/// <summary>Contains an ordered deterministic comparison of two Orchestra repositories.</summary>
public class OrchestraDiff
{
    /// <summary>The earlier repository metadata.</summary>
    public OrchestraMetadata BeforeMetadata = new OrchestraMetadata();
    /// <summary>The later repository metadata.</summary>
    public OrchestraMetadata AfterMetadata = new OrchestraMetadata();
    /// <summary>The ordered changes.</summary>
    public List<OrchestraChange> Changes = [];
    /// <summary>The comparison diagnostics.</summary>
    public List<OrchestraDiagnostic> Diagnostics = [];
}
