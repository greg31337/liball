namespace cbor;

/// <summary>Identifies the severity of a CDDL diagnostic.</summary>
public enum CddlDiagnosticSeverity
{
    /// <summary>Informational context that does not reject the operation.</summary>
    Information = 0,
    /// <summary>A warning about a usable but suspicious definition.</summary>
    Warning = 1,
    /// <summary>An error that prevents complete success.</summary>
    Error = 2
}
