namespace asn1;

/// <summary>
/// Exposes ECN compilation through a composed compiler implementation.
/// </summary>
public class EcnCompiler : IEcnCompiler
{
    internal IEcnCompiler Implementation;

    /// <summary>
    /// Initializes an ECN compiler facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that compiles ECN source.</param>
    public EcnCompiler(IEcnCompiler implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Compiles one ECN source against ASN.1 modules.
    /// </summary>
    /// <param name="source">The ECN source unit.</param>
    /// <param name="modules">The governing ASN.1 modules.</param>
    /// <returns>The compilation result.</returns>
    public AsnCompilationResult CompileEcn(AsnSourceUnit source, System.ReadOnlySpan<AsnModule> modules)
    {
        return this.Implementation.CompileEcn(source, modules);
    }
}
