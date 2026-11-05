namespace iso20022;

/// <summary>Classifies the overall compatibility impact of compared message definitions.</summary>
public enum CompatibilityKind
{
    /// <summary>The compatibility outcome is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>The compared definitions are compatible in every assessed dimension.</summary>
    Compatible = 1,
    /// <summary>The compared definitions are compatible with non-breaking cautions.</summary>
    CompatibleWithWarnings = 2,
    /// <summary>Compatibility depends on stated profile, value, or implementation conditions.</summary>
    ConditionallyCompatible = 3,
    /// <summary>At least one assessed reader, writer, schema, or semantic dimension is incompatible.</summary>
    Incompatible = 4,
    /// <summary>Migration or mapping can proceed only with loss of source information.</summary>
    DataLoss = 5
}
