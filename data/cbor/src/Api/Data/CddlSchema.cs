namespace cbor;

/// <summary>Holds an immutable-by-convention compiled CDDL schema model.</summary>
public class CddlSchema
{
    /// <summary>The source document used for compilation.</summary>
    public CddlDocument? Document;
    /// <summary>The merged rules in stable definition order.</summary>
    public CddlRule[] Rules = [];
    /// <summary>The selected root rule.</summary>
    public CddlRule? RootRule;
    /// <summary>The normalized validation nodes.</summary>
    public CddlNode[] NormalizedNodes = [];
    /// <summary>Source ranges corresponding to <see cref="NormalizedNodes"/>.</summary>
    public CddlSourceSpan[] SourceMap = [];
    /// <summary>The custom controls available to the compiled schema.</summary>
    public CddlControlRegistration[] ControlRegistrations = [];
    /// <summary>The feature names known to the schema.</summary>
    public string[] Features = [];
}
