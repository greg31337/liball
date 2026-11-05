namespace asn1;

/// <summary>
/// Compiles ASN.1 inputs into classified public artifacts.
/// </summary>
public interface IAsnCompiler
{
    /// <summary>
    /// Compiles one request.
    /// </summary>
    /// <param name="input">The compilation input.</param>
    /// <returns>The compilation result.</returns>
    public AsnCompilationResult Compile(AsnCompilationInput input);
}
