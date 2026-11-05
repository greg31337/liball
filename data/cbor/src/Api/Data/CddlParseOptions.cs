namespace cbor;

/// <summary>Holds CDDL lexer and parser recovery and retention policies.</summary>
public class CddlParseOptions
{
    /// <summary>The optional logical source name used in spans and diagnostics.</summary>
    public string? SourceName;
    /// <summary>Indicates whether comments and whitespace are retained.</summary>
    public bool RetainTrivia;
    /// <summary>Indicates whether parsing continues after recoverable errors.</summary>
    public bool Recover = true;
    /// <summary>The maximum number of returned diagnostics.</summary>
    public int MaxDiagnostics = 100;
    /// <summary>The maximum nested syntax depth.</summary>
    public int MaxNestingDepth = 128;
    /// <summary>The maximum number of produced AST nodes.</summary>
    public int MaxNodes = 100000;
}
