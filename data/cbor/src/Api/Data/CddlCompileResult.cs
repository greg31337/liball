namespace cbor;

/// <summary>Holds the exception-less result of compiling a CDDL schema.</summary>
public class CddlCompileResult
{
    /// <summary>Indicates whether compilation completed without errors.</summary>
    public bool IsSuccess;
    /// <summary>The compiled schema, or null when compilation failed.</summary>
    public CddlSchema? Schema;
    /// <summary>The compiler diagnostics in stable source order.</summary>
    public CddlDiagnostic[] Diagnostics = [];
    /// <summary>The options used for the operation.</summary>
    public CddlCompileOptions Options = new CddlCompileOptions();
}
