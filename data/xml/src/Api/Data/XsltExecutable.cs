namespace xml;

/// <summary>
/// Carries an immutable compiled XSLT stylesheet or package handle.
/// </summary>
public class XsltExecutable
{
    internal XsltStylesheetPlan? CompiledPlan;
    internal XsltCompileOptions? CompiledOptions;

    /// <summary>
    /// The compiled package name.
    /// </summary>
    public XmlName? PackageName;
    /// <summary>
    /// The compiled package version.
    /// </summary>
    public string? PackageVersion;
    /// <summary>
    /// The internal immutable executable plan.
    /// </summary>
    public object? Plan;
    /// <summary>
    /// The compilation options snapshot.
    /// </summary>
    public XsltCompileOptions? CompileOptions;
    /// <summary>
    /// The streamability analysis report.
    /// </summary>
    public XsltStreamabilityReport? StreamabilityReport;
    /// <summary>
    /// The compilation diagnostics.
    /// </summary>
    public XmlDiagnostic[]? Diagnostics;
    /// <summary>
    /// Whether compilation completed and the handle is immutable.
    /// </summary>
    public bool IsImmutable;
    /// <summary>
    /// Whether the executable is schema-aware.
    /// </summary>
    public bool IsSchemaAware;
    /// <summary>
    /// Whether the executable was compiled for guaranteed streaming conformance.
    /// </summary>
    public bool IsStreaming;
    /// <summary>
    /// Whether the executable can be reused concurrently with isolated transformation state.
    /// </summary>
    public bool IsThreadSafe;
}
