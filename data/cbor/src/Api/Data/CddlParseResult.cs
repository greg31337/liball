namespace cbor;

/// <summary>Holds the exception-less result of parsing a CDDL document.</summary>
public class CddlParseResult
{
    /// <summary>Indicates whether parsing completed without errors.</summary>
    public bool IsSuccess;
    /// <summary>The parsed or partially recovered document.</summary>
    public CddlDocument? Document;
    /// <summary>The parser diagnostics in stable source order.</summary>
    public CddlDiagnostic[] Diagnostics = [];
    /// <summary>The options used for the operation.</summary>
    public CddlParseOptions Options = new CddlParseOptions();
}
