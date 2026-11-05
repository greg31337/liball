namespace asn1;

/// <summary>
/// Configures which C# artifacts are generated.
/// </summary>
public class AsnGenerationOptions
{
    /// <summary>
    /// Selects the generation target.
    /// </summary>
    public AsnCodeGenerationTarget Target;

    /// <summary>
    /// Contains the root namespace used for generated C# declarations.
    /// </summary>
    public string NamespaceName = "";

    /// <summary>
    /// Indicates whether generated outputs include source mappings.
    /// </summary>
    public bool IncludeSourceMappings;
}
