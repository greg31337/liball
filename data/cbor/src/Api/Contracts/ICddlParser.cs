namespace cbor;

/// <summary>Parses CDDL source text into the public source AST.</summary>
public interface ICddlParser
{
    /// <summary>Parses a complete CDDL document or throws on failure.</summary>
    /// <param name="source">The CDDL source text.</param>
    /// <param name="options">The parsing policies and limits.</param>
    /// <returns>The parsed document.</returns>
    CddlDocument ParseDocument(string source, CddlParseOptions options);

    /// <summary>Parses a CDDL document and returns recoverable diagnostics.</summary>
    /// <param name="source">The CDDL source text.</param>
    /// <param name="options">The parsing policies and limits.</param>
    /// <returns>The exception-less parse result.</returns>
    CddlParseResult TryParseDocument(string source, CddlParseOptions options);
}
