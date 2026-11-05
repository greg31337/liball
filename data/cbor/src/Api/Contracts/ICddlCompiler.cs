namespace cbor;

/// <summary>Compiles parsed CDDL documents into reusable schemas.</summary>
public interface ICddlCompiler
{
    /// <summary>Compiles a parsed document or throws on failure.</summary>
    /// <param name="document">The parsed source document.</param>
    /// <param name="options">The compilation policies and limits.</param>
    /// <returns>The compiled schema.</returns>
    CddlSchema CompileSchema(CddlDocument document, CddlCompileOptions options);

    /// <summary>Compiles a parsed document and returns recoverable diagnostics.</summary>
    /// <param name="document">The parsed source document.</param>
    /// <param name="options">The compilation policies and limits.</param>
    /// <returns>The exception-less compilation result.</returns>
    CddlCompileResult TryCompileSchema(CddlDocument document, CddlCompileOptions options);
}
