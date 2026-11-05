namespace asn1;

/// <summary>
/// Exposes ASN.1 compilation through a composed compiler implementation.
/// </summary>
public class AsnCompiler : IAsnCompiler
{
    internal IAsnCompiler Implementation;

    /// <summary>
    /// Initializes a compiler facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that performs compilation.</param>
    public AsnCompiler(IAsnCompiler implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Compiles one request.
    /// </summary>
    /// <param name="input">The compilation input.</param>
    /// <returns>The compilation result.</returns>
    public AsnCompilationResult Compile(AsnCompilationInput input)
    {
        return this.Implementation.Compile(input);
    }
}
