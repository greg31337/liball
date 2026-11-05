namespace asn1;

/// <summary>
/// Compiles X.692 ECN source into validated executable encoding plans.
/// </summary>
public interface IEcnCompiler
{
    /// <summary>
    /// Compiles one ECN source against ASN.1 modules.
    /// </summary>
    /// <param name="source">The ECN source unit.</param>
    /// <param name="modules">The governing ASN.1 modules.</param>
    /// <returns>A compilation result containing plans and diagnostics.</returns>
    public AsnCompilationResult CompileEcn(AsnSourceUnit source, System.ReadOnlySpan<AsnModule> modules);
}
