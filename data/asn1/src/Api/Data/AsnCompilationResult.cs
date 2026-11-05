namespace asn1;

/// <summary>
/// Contains the classified public artifacts produced by compilation.
/// </summary>
public class AsnCompilationResult
{
    /// <summary>
    /// Indicates whether all requested phases completed without error diagnostics.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains compilation diagnostics.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];

    /// <summary>
    /// Contains retained lexical tokens.
    /// </summary>
    public AsnToken[] Tokens = [];

    /// <summary>
    /// Contains classified assignments.
    /// </summary>
    public AsnAssignment[] Assignments = [];

    /// <summary>
    /// Contains parsed or validated modules in stable order.
    /// </summary>
    public AsnModule[] Modules = [];

    /// <summary>
    /// Contains lowered encoding plans.
    /// </summary>
    public AsnEncodingPlan[] EncodingPlans = [];

    /// <summary>
    /// Contains the compiled reflection catalog when reflection lowering ran.
    /// </summary>
    public AsnReflectionCatalog? ReflectionCatalog;

    /// <summary>
    /// Contains per-source lexer results.
    /// </summary>
    public AsnLexerResult[] LexerResults = [];

    /// <summary>
    /// Contains parser results for the compilation.
    /// </summary>
    public AsnParserResult[] ParserResults = [];

    /// <summary>
    /// Contains the semantic validation result when validation ran.
    /// </summary>
    public AsnValidationResult? ValidationResult;

    /// <summary>
    /// Contains generated outputs.
    /// </summary>
    public AsnGeneratedFile[] GeneratedFiles = [];

    /// <summary>
    /// Contains XML Schema mapping results.
    /// </summary>
    public AsnXsdImportResult[] XsdImportResults = [];
}
