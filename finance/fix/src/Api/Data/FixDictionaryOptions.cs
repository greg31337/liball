namespace fix;

/// <summary>Controls dictionary loading, normalization, and resource limits.</summary>
public class FixDictionaryOptions
{
    /// <summary>The maximum total number of definitions accepted from one source.</summary>
    public int MaxDefinitions = FixDefaults.MaxDictionaryDefinitions;
    /// <summary>The first tag treated as user-defined by default.</summary>
    public int UserDefinedTagMinimum = 5000;
    /// <summary>Whether duplicate identifiers or names are accepted with diagnostics.</summary>
    public bool AllowDuplicateDefinitions;
    /// <summary>Whether fields may reference datatypes absent from the source.</summary>
    public bool AllowUnknownDatatypes;
    /// <summary>Whether unresolved member references make loading unsuccessful.</summary>
    public bool RequireResolvedReferences = true;
    /// <summary>Whether names are normalized for deterministic lookup.</summary>
    public bool NormalizeNames = true;
    /// <summary>Whether source order is retained alongside normalized indexes.</summary>
    public bool PreserveSourceOrder = true;
}
