namespace asn1;

/// <summary>
/// Carries the options for a compilation request.
/// </summary>
public class AsnCompilationInput
{
    /// <summary>
    /// Contains the source units in caller-defined compilation order.
    /// </summary>
    public AsnSourceUnit[] SourceUnits = [];

    /// <summary>
    /// Contains XML Schema documents to map before ASN.1 compilation.
    /// </summary>
    public AsnXsdInput[] XsdInputs = [];

    /// <summary>
    /// Contains XML Schema mapping options when XML Schema inputs are supplied.
    /// </summary>
    public AsnXsdImportOptions? XsdImportOptions;

    /// <summary>
    /// Contains compiler-wide options when supplied.
    /// </summary>
    public AsnCompilerOptions? CompilerOptions;

    /// <summary>
    /// Contains application-provided PER instruction handlers for this compilation.
    /// </summary>
    public IPerInstructionRegistry? PerInstructionRegistry;

    /// <summary>
    /// Contains code-generation options when supplied.
    /// </summary>
    public AsnGenerationOptions? GenerationOptions;
}
